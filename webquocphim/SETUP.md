# Python RAG Service Setup

## Yêu cầu

- Python 3.10+
- pip
- MySQL server (cinemadbaspnet database)
- Groq API key

## Cài đặt

### 1. Tạo và kích hoạt virtual environment

```bash
cd /home/quoctruong/Documents/laptrinhweb/web/RagService
python3 -m venv venv
source venv/bin/activate  # Linux/Mac
# hoặc: venv\Scripts\activate  # Windows
```

### 2. Cài đặt dependencies

```bash
pip install -r requirements.txt
```

(Lần đầu tiên sẽ tải models từ Hugging Face: `intfloat/multilingual-e5-small` ~220MB)

### 3. Cấu hình MySQL credentials

Chỉnh sửa `ingestor.py` và `retriever.py`:

```python
DB_CONFIG = {
    "host": "localhost",
    "database": "cinemadbaspnet",
    "user": "root",
    "password": "YOUR_PASSWORD_HERE"  # ← Sửa password của bạn
}
```

### 4. Cấu hình Groq API key

```bash
export GROQ_API_KEY="your_groq_api_key_here"
# hoặc copy khóa từ appsettings.json của ASP.NET project
```

## Chạy service

### Terminal 1: Python RAG Service (port 8001)

```bash
cd /home/quoctruong/Documents/laptrinhweb/web/RagService
source venv/bin/activate
python rag_service.py
```

Output mong muốn:

```
Loading embedding model...
Ready. Collection has 0 documents.
INFO:     Uvicorn running on http://0.0.0.0:8001
```

### Terminal 2: Nạp dữ liệu từ MySQL vào ChromaDB

Khi service đã chạy, gọi endpoint `/ingest`:

```bash
curl -X POST http://localhost:8001/ingest
# Response: {"status":"ok","total_documents":N}
```

### Terminal 3: ASP.NET web server (port 5001)

```bash
cd /home/quoctruong/Documents/laptrinhweb/web
dotnet run
```

## Test chatbot

### Qua cURL

```bash
# Test chat với câu hỏi về phim
curl -X POST http://localhost:8001/chat \
  -H "Content-Type: application/json" \
  -d '{
    "message": "Có phim gì hay không?",
    "history": []
  }'

# Response:
# {"reply":"Có những bộ phim đang chiếu tại rạp..."}
```

### Qua web UI

- Mở trình duyệt: `http://localhost:5001`
- Vào trang chat
- Nhập tin nhắn (ASP.NET sẽ gọi Python service port 8001)

## Troubleshooting

### 1. "Connection refused port 8001"

→ Kiểm tra Python service có đang chạy không?

```bash
curl http://localhost:8001/health
```

### 2. "MySQL connection error"

→ Chỉnh sửa `DB_CONFIG` trong `ingestor.py` + `retriever.py`
→ Kiểm tra MySQL server đang chạy: `mysql -u root -p cinemadbaspnet`

### 3. "GROQ_API_KEY not found"

→ Đặt environment variable:

```bash
export GROQ_API_KEY="gsk_..."
echo $GROQ_API_KEY  # kiểm tra
```

### 4. "Model download stuck"

→ Lần đầu SentenceTransformer tải ~220MB từ Hugging Face (internet chậm?\)
→ Chỉ cần 1 lần, sau đó cache tại `~/.cache/huggingface/`

### 5. "ChromaDB/chroma_db không tìm thấy"

→ Tạo thư mục nếu cần:

```bash
mkdir -p /home/quoctruong/Documents/laptrinhweb/web/RagService/chroma_db
```

## Kiến trúc

```
ASP.NET (port 5001)
    ↓ HTTP POST /chat
Python FastAPI (port 8001)
    ├─ Vector Search (ChromaDB)
    ├─ SQL Realtime (MySQL)
    └─ Groq LLM API
```

### Luồng xử lý:

1. **User → ASP.NET ChatbotController**: gửi tin nhắn
2. **ASP.NET ChatbotService**: HTTP POST → http://localhost:8001/chat
3. **Python rag_service.py**:
   - `vector_search()`: tìm top-4 documents gần nhất
   - `sql_realtime_context()`: lịch chiếu hôm nay, ghế trống
   - Build system prompt + context
   - Gọi Groq `chat.completions.create()`
4. **Groq API**: sinh câu trả lời
5. **Response**: trả về JSON `{"reply": "..."}`
6. **ASP.NET**: hiển thị trên UI

## Dependencies

- **fastapi** - Web framework
- **uvicorn** - ASGI server
- **chromadb** - Vector database
- **sentence-transformers** - Embedding model (multilingual-e5-small)
- **groq** - Groq API client
- **mysql-connector-python** - MySQL driver
- **python-multipart** - Form data parsing

## Endpoints

### POST /chat

Request:

```json
{
  "message": "Hôm nay có phim gì?",
  "history": [
    { "role": "user", "content": "..." },
    { "role": "assistant", "content": "..." }
  ]
}
```

Response:

```json
{
  "reply": "Hôm nay có những bộ phim..."
}
```

### POST /ingest

Nạp lại dữ liệu từ MySQL → ChromaDB

Response:

```json
{
  "status": "ok",
  "total_documents": 150
}
```

### GET /health

Kiểm tra service status

Response:

```json
{
  "status": "ok",
  "documents": 150
}
```

## Performance

- **Vector search**: ~50-100ms (4 chunks from ChromaDB)
- **SQL query**: ~30-50ms (realtime context)
- **Groq API call**: ~500-1000ms (LLM inference)
- **Total latency**: ~600-1100ms per message

## Notes

- Embedding model là multilingual hỗ trợ Tiếng Việt
- ChromaDB lưu local tại `./chroma_db/` (persistent)
- Mỗi lần start service, models tải vào memory (1-2 giây)
- Max history = 8 messages (để context ngắn cho Groq)
