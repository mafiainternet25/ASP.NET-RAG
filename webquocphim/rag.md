# 🤖 Hướng dẫn RAG Chatbot - Cinema Booking (ASP.NET MVC + Python RAG Service)

> Nâng cấp ChatbotService từ SQL keyword matching sang RAG thật sự.
> Kiến trúc: ASP.NET MVC gọi HTTP sang Python FastAPI (ChromaDB + SentenceTransformer + Groq).

---

## 🏗️ Kiến trúc tổng quan

```
ASP.NET MVC (port 5001)
    └── ChatbotService.cs
            │  HTTP POST /chat
            ▼
Python RAG Service (port 8001)
    ├── /ingest  — nạp dữ liệu MySQL vào ChromaDB (1 lần)
    └── /chat    — nhận câu hỏi → vector search → Groq → trả lời
            │
            ├── ChromaDB (vector store, lưu local ./chroma_db/)
            ├── SentenceTransformer (embedding model, chạy local)
            ├── MySQL (SQL fallback cho dữ liệu realtime)
            └── Groq API (LLM sinh câu trả lời)
```

---

## 📦 Cài đặt môi trường Python

```bash
# Tạo virtual environment
python -m venv venv
source venv/bin/activate        # Linux/Mac
venv\Scripts\activate           # Windows

# Cài thư viện
pip install fastapi uvicorn chromadb sentence-transformers groq mysql-connector-python
```

---

## 📁 Cấu trúc file cần tạo

```
CinemaBooking/
├── RagService/                    # Thư mục Python riêng
│   ├── rag_service.py             # FastAPI app chính
│   ├── ingestor.py                # Logic nạp dữ liệu vào ChromaDB
│   ├── retriever.py               # Logic tìm kiếm vector
│   ├── requirements.txt           # Danh sách thư viện
│   └── chroma_db/                 # ChromaDB lưu vector (tự tạo)
└── Services/
    └── ChatbotService.cs          # Cập nhật gọi sang Python
```

---

## 🐍 File Python

### `RagService/requirements.txt`

```
fastapi==0.111.0
uvicorn==0.30.0
chromadb==0.5.0
sentence-transformers==3.0.0
groq==0.9.0
mysql-connector-python==8.4.0
```

---

### `RagService/ingestor.py` — Nạp dữ liệu vào ChromaDB

```python
import mysql.connector
import chromadb
from sentence_transformers import SentenceTransformer

DB_CONFIG = {
    "host": "localhost",
    "database": "cinemadbaspnet",
    "user": "root",
    "password": "yourpassword"
}

def get_connection():
    return mysql.connector.connect(**DB_CONFIG)

def build_documents(cursor) -> tuple[list[str], list[str]]:
    """Query MySQL và build text documents để embedding"""
    documents = []
    ids = []

    # ── Phim ──────────────────────────────────────────────
    cursor.execute("""
        SELECT id, title, genre, duration_min,
               description, rating, status
        FROM movies
    """)
    for m in cursor.fetchall():
        status_text = "đang chiếu" if m["status"] == "NOW_SHOWING" else "sắp chiếu"
        text = (
            f"Phim: {m['title']}. "
            f"Thể loại: {m['genre'] or 'chưa xác định'}. "
            f"Thời lượng: {m['duration_min']} phút. "
            f"Mô tả: {m['description'] or ''}. "
            f"Đánh giá: {m['rating']}/10. "
            f"Trạng thái: {status_text}."
        )
        documents.append(text)
        ids.append(f"movie_{m['id']}")

    # ── Suất chiếu ────────────────────────────────────────
    cursor.execute("""
        SELECT s.id, m.title movie_title,
               ci.name cinema_name, r.name room_name,
               r.room_type, s.start_time, s.price
        FROM showtimes s
        JOIN movies m  ON s.movie_id  = m.id
        JOIN rooms r   ON s.room_id   = r.id
        JOIN cinemas ci ON r.cinema_id = ci.id
        WHERE s.start_time >= NOW()
        ORDER BY s.start_time
        LIMIT 50
    """)
    for s in cursor.fetchall():
        text = (
            f"Suất chiếu phim {s['movie_title']} "
            f"tại rạp {s['cinema_name']}, "
            f"phòng {s['room_name']} ({s['room_type']}), "
            f"lúc {s['start_time'].strftime('%H:%M ngày %d/%m/%Y')}. "
            f"Giá vé: {int(s['price']):,} VND."
        )
        documents.append(text)
        ids.append(f"showtime_{s['id']}")

    # ── Snacks ────────────────────────────────────────────
    cursor.execute("""
        SELECT id, name, description, price, category
        FROM snacks
        WHERE is_available = 1
    """)
    for sn in cursor.fetchall():
        cat_map = {"FOOD": "đồ ăn", "DRINK": "đồ uống", "COMBO": "combo"}
        text = (
            f"Snack: {sn['name']}. "
            f"Loại: {cat_map.get(sn['category'], sn['category'])}. "
            f"Mô tả: {sn['description'] or ''}. "
            f"Giá: {int(sn['price']):,} VND."
        )
        documents.append(text)
        ids.append(f"snack_{sn['id']}")

    # ── Rạp chiếu ─────────────────────────────────────────
    cursor.execute("SELECT id, name, address, city FROM cinemas")
    for c in cursor.fetchall():
        text = (
            f"Rạp chiếu phim: {c['name']}. "
            f"Địa chỉ: {c['address']}, {c['city']}."
        )
        documents.append(text)
        ids.append(f"cinema_{c['id']}")

    # ── Khuyến mãi ────────────────────────────────────────
    cursor.execute("""
        SELECT id, code, description, discount_percent, valid_to
        FROM promotions
        WHERE valid_from <= NOW() AND valid_to >= NOW()
          AND used_count < max_uses
    """)
    for p in cursor.fetchall():
        text = (
            f"Khuyến mãi: mã {p['code']}. "
            f"Mô tả: {p['description']}. "
            f"Giảm {p['discount_percent']}%. "
            f"Hạn sử dụng: {p['valid_to'].strftime('%d/%m/%Y')}."
        )
        documents.append(text)
        ids.append(f"promo_{p['id']}")

    return documents, ids


def ingest(embed_model: SentenceTransformer,
           collection: chromadb.Collection) -> int:
    """Xóa collection cũ, nạp lại toàn bộ từ MySQL"""
    conn = get_connection()
    cursor = conn.cursor(dictionary=True)

    documents, ids = build_documents(cursor)

    cursor.close()
    conn.close()

    # Xóa dữ liệu cũ
    existing = collection.get()["ids"]
    if existing:
        collection.delete(ids=existing)

    # Tạo embedding — prefix "passage:" theo chuẩn E5 model
    embeddings = embed_model.encode(
        [f"passage: {d}" for d in documents],
        batch_size=32,
        show_progress_bar=True
    ).tolist()

    collection.add(
        documents=documents,
        embeddings=embeddings,
        ids=ids
    )

    return len(documents)
```

---

### `RagService/retriever.py` — Tìm kiếm vector + SQL fallback

```python
import mysql.connector
from sentence_transformers import SentenceTransformer
import chromadb

DB_CONFIG = {
    "host": "localhost",
    "database": "cinemadbaspnet",
    "user": "root",
    "password": "yourpassword"
}

def vector_search(query: str,
                  embed_model: SentenceTransformer,
                  collection: chromadb.Collection,
                  top_k: int = 4) -> list[str]:
    """Tìm top_k chunk gần nhất với câu hỏi"""
    query_vec = embed_model.encode(f"query: {query}").tolist()
    results = collection.query(
        query_embeddings=[query_vec],
        n_results=top_k
    )
    return results["documents"][0]  # list[str]


def sql_realtime_context(message: str) -> str:
    """SQL fallback cho dữ liệu thay đổi liên tục (lịch chiếu hôm nay, tồn kho)"""
    msg = message.lower()
    ctx_lines = []

    conn = mysql.connector.connect(**DB_CONFIG)
    cursor = conn.cursor(dictionary=True)

    # Lịch chiếu hôm nay
    if any(k in msg for k in ["hôm nay", "hom nay", "lịch", "lich",
                               "giờ", "gio", "suất", "suat"]):
        cursor.execute("""
            SELECT m.title, s.start_time, r.name room,
                   ci.name cinema, s.price
            FROM showtimes s
            JOIN movies m  ON s.movie_id  = m.id
            JOIN rooms r   ON s.room_id   = r.id
            JOIN cinemas ci ON r.cinema_id = ci.id
            WHERE DATE(s.start_time) = CURDATE()
            ORDER BY s.start_time
        """)
        rows = cursor.fetchall()
        if rows:
            ctx_lines.append("=== LỊCH CHIẾU HÔM NAY ===")
            for r in rows:
                ctx_lines.append(
                    f"{r['title']} | {r['start_time'].strftime('%H:%M')} "
                    f"| {r['cinema']} - {r['room']} | {int(r['price']):,} VND"
                )

    # Ghế còn trống theo suất chiếu
    if any(k in msg for k in ["ghế", "ghe", "còn chỗ", "con cho", "chỗ ngồi"]):
        cursor.execute("""
            SELECT s.id, m.title, r.name room,
                   r.total_seats,
                   (r.total_seats - COUNT(bs.id)) available
            FROM showtimes s
            JOIN movies m ON s.movie_id = m.id
            JOIN rooms r ON s.room_id = r.id
            LEFT JOIN bookings b ON b.showtime_id = s.id
                AND b.status != 'CANCELLED'
            LEFT JOIN booking_seats bs ON bs.booking_id = b.id
            WHERE s.start_time >= NOW()
            GROUP BY s.id, m.title, r.name, r.total_seats
            HAVING available > 0
            ORDER BY s.start_time
            LIMIT 5
        """)
        rows = cursor.fetchall()
        if rows:
            ctx_lines.append("=== GHẾ CÒN TRỐNG ===")
            for r in rows:
                ctx_lines.append(
                    f"{r['title']} - {r['room']}: "
                    f"còn {r['available']}/{r['total_seats']} ghế"
                )

    cursor.close()
    conn.close()
    return "\n".join(ctx_lines)
```

---

### `RagService/rag_service.py` — FastAPI app chính

```python
import os
from contextlib import asynccontextmanager
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
import chromadb
from sentence_transformers import SentenceTransformer
from groq import Groq

from ingestor import ingest
from retriever import vector_search, sql_realtime_context

# ── Config ───────────────────────────────────────────────
GROQ_API_KEY  = os.environ.get("GROQ_API_KEY", "")
GROQ_MODEL    = "llama-3.1-8b-instant"
EMBED_MODEL   = "intfloat/multilingual-e5-small"  # nhỏ, hỗ trợ tiếng Việt
CHROMA_PATH   = "./chroma_db"
COLLECTION    = "cinema_docs"

# ── Khởi tạo khi startup ─────────────────────────────────
embed_model: SentenceTransformer = None
collection:  chromadb.Collection = None
groq_client: Groq = None

@asynccontextmanager
async def lifespan(app: FastAPI):
    global embed_model, collection, groq_client
    print("Loading embedding model...")
    embed_model  = SentenceTransformer(EMBED_MODEL)
    chroma       = chromadb.PersistentClient(path=CHROMA_PATH)
    collection   = chroma.get_or_create_collection(COLLECTION)
    groq_client  = Groq(api_key=GROQ_API_KEY)
    print(f"Ready. Collection has {collection.count()} documents.")
    yield

app = FastAPI(lifespan=lifespan)


# ── Models ───────────────────────────────────────────────
class ChatMessage(BaseModel):
    role: str     # "user" | "assistant"
    content: str

class ChatRequest(BaseModel):
    message: str
    history: list[ChatMessage] = []


# ── Endpoints ────────────────────────────────────────────

@app.post("/ingest")
def ingest_endpoint():
    """Nạp/cập nhật toàn bộ dữ liệu MySQL vào ChromaDB"""
    try:
        total = ingest(embed_model, collection)
        return {"status": "ok", "total_documents": total}
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))


@app.post("/chat")
def chat_endpoint(req: ChatRequest):
    """Nhận câu hỏi → RAG → trả lời"""
    # Bước 1: Vector search
    chunks = vector_search(req.message, embed_model, collection, top_k=4)

    # Bước 2: SQL realtime fallback
    realtime = sql_realtime_context(req.message)

    # Bước 3: Build context
    context = "\n".join(f"- {c}" for c in chunks)
    if realtime:
        context = realtime + "\n\n" + context

    # Bước 4: System prompt
    system_prompt = f"""Bạn là trợ lý rạp phim CineStar.

Quy tắc:
1) Chỉ dùng thông tin trong DỮLIỆU dưới đây để trả lời.
2) Nếu không có thông tin liên quan: nói "Chưa có thông tin này trong hệ thống".
3) Trả lời tiếng Việt, ngắn gọn 2-5 dòng.
4) Kết thúc bằng 1 câu gợi ý hành động tiếp theo.
5) Cấm bịa giá vé, lịch chiếu, tên phim nếu không có trong dữ liệu.
6) Câu hỏi ngoài domain rạp phim: từ chối lịch sự.

DỮLIỆU:
{context}"""

    # Bước 5: Build messages (system + history + user)
    messages = [{"role": "system", "content": system_prompt}]
    for h in req.history[-8:]:  # giữ 8 lượt gần nhất
        messages.append({"role": h.role, "content": h.content})
    messages.append({"role": "user", "content": req.message})

    # Bước 6: Gọi Groq
    try:
        resp = groq_client.chat.completions.create(
            model=GROQ_MODEL,
            messages=messages,
            max_tokens=512,
            temperature=0.3
        )
        return {"reply": resp.choices[0].message.content}
    except Exception as e:
        return {"reply": "Xin lỗi, tôi đang gặp sự cố. Vui lòng thử lại."}


@app.get("/health")
def health():
    return {
        "status": "ok",
        "documents": collection.count() if collection else 0
    }
```

---

## 🔧 Cập nhật `ChatbotService.cs`

Thay toàn bộ logic cũ bằng HTTP call sang Python service:

```csharp
public class ChatbotService : IChatbotService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ChatbotService> _logger;
    private const string RAG_URL = "http://localhost:8001";

    public ChatbotService(IHttpClientFactory httpClientFactory,
                          ILogger<ChatbotService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<string> ChatAsync(ChatRequest? request,
                                         CancellationToken ct = default)
    {
        try
        {
            var body = JsonSerializer.Serialize(new
            {
                message = request?.Message ?? "",
                history = request?.History ?? new List<ChatHistoryItem>()
            });

            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(15);

            using var req = new HttpRequestMessage(
                HttpMethod.Post, $"{RAG_URL}/chat")
            {
                Content = new StringContent(
                    body, Encoding.UTF8, "application/json")
            };

            using var resp = await client.SendAsync(req, ct);
            var json = await resp.Content.ReadAsStringAsync(ct);

            using var doc = JsonDocument.Parse(json);
            return doc.RootElement
                       .GetProperty("reply")
                       .GetString()
                   ?? "Xin lỗi, không nhận được phản hồi.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RAG service error");
            return "Xin lỗi, tôi đang gặp sự cố. Vui lòng thử lại sau.";
        }
    }

    /// <summary>Gọi sau khi Admin thêm/sửa/xóa phim, snack, suất chiếu</summary>
    public async Task TriggerReIngestAsync()
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            await client.PostAsync($"{RAG_URL}/ingest",
                new StringContent(""));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Re-ingest failed (non-critical)");
        }
    }
}
```

---

## 🔄 Thêm Re-ingest vào Admin Controllers

Mỗi khi Admin thêm/sửa/xóa phim, snack, suất chiếu thì cần cập nhật ChromaDB:

```csharp
// AdminMovieController.cs — thêm vào cuối Create/Edit/Delete
[HttpPost]
public async Task<IActionResult> Create(MovieViewModel vm)
{
    // ... logic tạo phim ...
    await _db.SaveChangesAsync();

    // Cập nhật RAG index
    await _chatbotService.TriggerReIngestAsync();

    return RedirectToAction(nameof(Index));
}
```

```csharp
// Tương tự cho AdminSnackController và AdminShowtimeController
await _chatbotService.TriggerReIngestAsync();
```

---

## 🚀 Chạy toàn bộ hệ thống

```bash
# Terminal 1 — Python RAG Service
cd CinemaBooking/RagService
source venv/bin/activate
GROQ_API_KEY=gsk_xxxxxxxxxxxx uvicorn rag_service:app --port 8001 --reload

# Terminal 2 — Nạp dữ liệu vào ChromaDB (chạy 1 lần đầu)
curl -X POST http://localhost:8001/ingest
# Kết quả: {"status":"ok","total_documents":42}

# Kiểm tra health
curl http://localhost:8001/health
# Kết quả: {"status":"ok","documents":42}

# Terminal 3 — ASP.NET MVC
cd CinemaBooking
dotnet run
```

---

## 🧪 Test RAG chatbot bằng curl

```bash
# Test hỏi phim
curl -X POST http://localhost:8001/chat \
  -H "Content-Type: application/json" \
  -d '{"message": "phim khoa học viễn tưởng nào đang chiếu?", "history": []}'

# Test hỏi lịch chiếu
curl -X POST http://localhost:8001/chat \
  -H "Content-Type: application/json" \
  -d '{"message": "hôm nay có suất chiếu mấy giờ?", "history": []}'

# Test hỏi snack
curl -X POST http://localhost:8001/chat \
  -H "Content-Type: application/json" \
  -d '{"message": "rạp có bán đồ uống gì không?", "history": []}'

# Test hỏi đồng nghĩa (RAG hiểu, keyword cũ không hiểu)
curl -X POST http://localhost:8001/chat \
  -H "Content-Type: application/json" \
  -d '{"message": "film hay nhất hiện tại là gì?", "history": []}'

# Test hội thoại nhiều lượt
curl -X POST http://localhost:8001/chat \
  -H "Content-Type: application/json" \
  -d '{
    "message": "giá vé bao nhiêu?",
    "history": [
      {"role": "user", "content": "Interstellar chiếu mấy giờ?"},
      {"role": "assistant", "content": "Interstellar chiếu lúc 12:00 và 18:00 hôm nay..."}
    ]
  }'

# Test ngoài domain
curl -X POST http://localhost:8001/chat \
  -H "Content-Type: application/json" \
  -d '{"message": "thủ đô nước Pháp là gì?", "history": []}'
```

---

## 🐛 Lỗi thường gặp & cách fix

| Lỗi | Nguyên nhân | Fix |
|-----|-------------|-----|
| `Connection refused localhost:8001` | Python service chưa chạy | `uvicorn rag_service:app --port 8001` |
| `{"detail":"...mysql..."}` khi `/ingest` | Sai DB_CONFIG | Kiểm tra host/user/password trong `ingestor.py` |
| `total_documents: 0` | DB trống hoặc lỗi query | Chạy `SELECT COUNT(*) FROM movies` kiểm tra |
| Model download chậm | Lần đầu tải SentenceTransformer | Chờ ~200MB download, lần sau chạy nhanh |
| `GROQ_API_KEY not set` | Thiếu env var | `export GROQ_API_KEY=gsk_xxx` trước khi chạy |
| RAG trả lời sai | Chunk không liên quan | Tăng `top_k=6`, kiểm tra text trong `ingestor.py` |
| ASP.NET timeout | Python xử lý chậm | Tăng `client.Timeout = TimeSpan.FromSeconds(30)` |

---

## ⚡ Tối ưu sau khi chạy ổn định

```python
# 1. Tự động re-ingest mỗi ngày lúc 6:00 sáng (cập nhật lịch chiếu mới)
# Thêm vào rag_service.py:
from apscheduler.schedulers.background import BackgroundScheduler

scheduler = BackgroundScheduler()
scheduler.add_job(lambda: ingest(embed_model, collection),
                  "cron", hour=6, minute=0)
scheduler.start()

# 2. Cache embedding của query phổ biến
# Nếu user hỏi "hôm nay chiếu gì" nhiều lần → cache kết quả 5 phút

# 3. Dùng model tốt hơn nếu cần độ chính xác cao
EMBED_MODEL = "intfloat/multilingual-e5-base"  # lớn hơn, chính xác hơn
```

---

## 🗂️ Thứ tự triển khai

```
Bước 1 → Tạo thư mục RagService/, tạo requirements.txt
Bước 2 → pip install -r requirements.txt
Bước 3 → Tạo ingestor.py — điền đúng DB_CONFIG
Bước 4 → Tạo retriever.py
Bước 5 → Tạo rag_service.py
Bước 6 → Chạy: uvicorn rag_service:app --port 8001
Bước 7 → POST /ingest để nạp dữ liệu
Bước 8 → GET /health kiểm tra số documents
Bước 9 → Test bằng curl (xem phần Test bên trên)
Bước 10 → Cập nhật ChatbotService.cs gọi sang port 8001
Bước 11 → Thêm TriggerReIngestAsync() vào Admin controllers
Bước 12 → Test toàn bộ từ widget chatbot trên giao diện
```

---

> 💬 **Tip:** Lần đầu chạy Python service sẽ tải embedding model ~200MB về máy. Từ lần 2 trở đi load từ cache, khởi động chỉ mất ~5 giây. ChromaDB lưu vector trong thư mục `./chroma_db/` — không cần xóa khi restart, chỉ cần gọi `/ingest` khi dữ liệu thay đổi.
