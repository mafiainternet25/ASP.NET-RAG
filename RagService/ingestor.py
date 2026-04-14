import mysql.connector
import chromadb
from sentence_transformers import SentenceTransformer

DB_CONFIG = {
    "host": "localhost",
    "database": "cinemadbaspnet",
    "user": "root",
    "password": ""
}

def get_connection():
    return mysql.connector.connect(**DB_CONFIG)

def build_documents(cursor) -> tuple[list[str], list[str]]:
    """Query MySQL và build text documents để embedding"""
    documents = []
    ids = []

    try:
        cursor.execute("""
            SELECT id, title, genre, duration_min,
                   description, rating, status
            FROM movies
        """)
        for m in cursor.fetchall():
            status_text = "đang chiếu" if m[6] == "NOW_SHOWING" else "sắp chiếu"
            text = (
                f"Phim: {m[1]}. "
                f"Thể loại: {m[2] or 'chưa xác định'}. "
                f"Thời lượng: {m[3] or 0} phút. "
                f"Mô tả: {m[4] or ''}. "
                f"Đánh giá: {m[5] or 0}/10. "
                f"Trạng thái: {status_text}."
            )
            documents.append(text)
            ids.append(f"movie_{m[0]}")
    except Exception as e:
        print(f"Error fetching movies: {e}")

    try:
        cursor.execute("""
            SELECT s.id, m.title, ci.name, r.name,
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
            start_time_str = s[5].strftime('%H:%M ngày %d/%m/%Y') if hasattr(s[5], 'strftime') else str(s[5])
            text = (
                f"Suất chiếu phim {s[1]} "
                f"tại rạp {s[2]}, "
                f"phòng {s[3]} ({s[4]}), "
                f"lúc {start_time_str}. "
                f"Giá vé: {int(s[6]):,} VND."
            )
            documents.append(text)
            ids.append(f"showtime_{s[0]}")
    except Exception as e:
        print(f"Error fetching showtimes: {e}")

    try:
        cursor.execute("""
            SELECT id, name, description, price, category
            FROM snacks
            WHERE is_available = 1
        """)
        for sn in cursor.fetchall():
            cat_map = {"FOOD": "đồ ăn", "DRINK": "đồ uống", "COMBO": "combo"}
            text = (
                f"Snack: {sn[1]}. "
                f"Loại: {cat_map.get(sn[4], sn[4])}. "
                f"Mô tả: {sn[2] or ''}. "
                f"Giá: {int(sn[3]):,} VND."
            )
            documents.append(text)
            ids.append(f"snack_{sn[0]}")
    except Exception as e:
        print(f"Error fetching snacks: {e}")

    try:
        cursor.execute("SELECT id, name, address, city FROM cinemas")
        for c in cursor.fetchall():
            text = (
                f"Rạp chiếu phim: {c[1]}. "
                f"Địa chỉ: {c[2] or 'chưa cập nhật'}, {c[3] or 'chưa cập nhật'}."
            )
            documents.append(text)
            ids.append(f"cinema_{c[0]}")
    except Exception as e:
        print(f"Error fetching cinemas: {e}")

    try:
        cursor.execute("""
            SELECT id, code, description, discount_percent, valid_to
            FROM promotions
            WHERE valid_from <= NOW() AND valid_to >= NOW()
              AND used_count < max_uses
        """)
        for p in cursor.fetchall():
            valid_to_str = p[4].strftime('%d/%m/%Y') if hasattr(p[4], 'strftime') else str(p[4])
            text = (
                f"Khuyến mãi: mã {p[1]}. "
                f"Mô tả: {p[2] or ''}. "
                f"Giảm {p[3]}%. "
                f"Hạn sử dụng: {valid_to_str}."
            )
            documents.append(text)
            ids.append(f"promo_{p[0]}")
    except Exception as e:
        print(f"Error fetching promotions: {e}")

    return documents, ids


def ingest(embed_model: SentenceTransformer,
           collection: chromadb.Collection) -> int:
    """Xóa collection cũ, nạp lại toàn bộ từ MySQL"""
    try:
        conn = get_connection()
        cursor = conn.cursor()

        documents, ids = build_documents(cursor)

        cursor.close()
        conn.close()

        if len(documents) == 0:
            print("No documents to ingest")
            return 0

        try:
            existing = collection.get()
            if existing and existing.get("ids"):
                collection.delete(ids=existing["ids"])
        except Exception as e:
            print(f"Error deleting old documents: {e}")

        print(f"Encoding {len(documents)} documents...")
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

        print(f"Ingested {len(documents)} documents")
        return len(documents)

    except Exception as e:
        print(f"Error during ingest: {e}")
        raise
