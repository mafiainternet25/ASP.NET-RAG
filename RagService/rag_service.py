import os
import sys
from contextlib import asynccontextmanager
from fastapi import FastAPI, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel
import chromadb
from sentence_transformers import SentenceTransformer
from dotenv import load_dotenv

load_dotenv()

os.environ.pop('HTTP_PROXY', None)
os.environ.pop('HTTPS_PROXY', None)
os.environ.pop('http_proxy', None)
os.environ.pop('https_proxy', None)

try:
    from groq import Groq
except Exception as e:
    print(f"Warning: Groq import issue: {e}")
    print("Trying alternate import...")
    from groq import Groq

from ingestor import ingest
from retriever import vector_search, sql_realtime_context

GROQ_API_KEY  = os.environ.get("GROQ_API_KEY", "")
GROQ_MODEL    = "llama-3.1-8b-instant"
EMBED_MODEL   = "intfloat/multilingual-e5-small"  
CHROMA_PATH   = "./chroma_db"
COLLECTION    = "cinema_docs"

embed_model: SentenceTransformer = None
collection:  chromadb.Collection = None
groq_client: Groq = None

@asynccontextmanager
async def lifespan(app: FastAPI):
    global embed_model, collection, groq_client
    print("Loading embedding model...")
    try:
        if not GROQ_API_KEY:
            print("[WARNING] GROQ_API_KEY not set. Set it with: export GROQ_API_KEY='your_key'")
            print("Chat will still work but Groq API may fail. Continuing...")
        
        embed_model  = SentenceTransformer(EMBED_MODEL)
        chroma       = chromadb.PersistentClient(path=CHROMA_PATH)
        collection   = chroma.get_or_create_collection(COLLECTION)
        
        if GROQ_API_KEY:
            groq_client  = Groq(api_key=GROQ_API_KEY)
        else:
            groq_client = None
            
        count = collection.count()
        print(f"Ready. Collection has {count} documents.")
    except Exception as e:
        print(f"Error during startup: {e}")
        print(f"Debug: GROQ_API_KEY = {'<set>' if GROQ_API_KEY else '<not set>'}")
        sys.exit(1)
    yield
    print("Shutting down...")

app = FastAPI(lifespan=lifespan)

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

class ChatMessage(BaseModel):
    role: str     
    content: str

class ChatRequest(BaseModel):
    message: str
    history: list[ChatMessage] = []



@app.post("/ingest")
def ingest_endpoint():
    """Nạp/cập nhật toàn bộ dữ liệu MySQL vào ChromaDB"""
    try:
        total = ingest(embed_model, collection)
        return {"status": "ok", "total_documents": total}
    except Exception as e:
        print(f"Ingest error: {e}")
        raise HTTPException(status_code=500, detail=str(e))


@app.post("/chat")
def chat_endpoint(req: ChatRequest):
    """Nhận câu hỏi → RAG → trả lời"""
    try:
        chunks = vector_search(req.message, embed_model, collection, top_k=4)

        realtime = sql_realtime_context(req.message)

        context = "\n".join(f"- {c}" for c in chunks) if chunks else "Không có thông tin liên quan trong cơ sở dữ liệu."
        if realtime:
            context = realtime + "\n\n" + context

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

        messages = [{"role": "system", "content": system_prompt}]
        
        if req.history:
            for h in req.history[-8:]:  
                messages.append({"role": h.role, "content": h.content})
        
        messages.append({"role": "user", "content": req.message})

        if not groq_client or not GROQ_API_KEY:
            return {"reply": "Xin lỗi, GROQ_API_KEY chưa được cấu hình. Hãy set: export GROQ_API_KEY='your_key'"}

        resp = groq_client.chat.completions.create(
            model=GROQ_MODEL,
            messages=messages,
            max_tokens=512,
            temperature=0.3
        )
        
        reply = resp.choices[0].message.content if resp.choices else "Không nhận được phản hồi."
        return {"reply": reply}

    except Exception as e:
        print(f"Chat error: {e}")
        return {"reply": f"Xin lỗi, tôi đang gặp sự cố: {str(e)}"}


@app.get("/health")
def health():
    return {
        "status": "ok",
        "documents": collection.count() if collection else 0
    }


if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8001)
