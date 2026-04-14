import mysql.connector
from sentence_transformers import SentenceTransformer
import chromadb
from datetime import datetime

DB_CONFIG = {
    "host": "localhost",
    "database": "cinemadbaspnet",
    "user": "root",
    "password": ""
}

def vector_search(query: str,
                  embed_model: SentenceTransformer,
                  collection: chromadb.Collection,
                  top_k: int = 4) -> list[str]:
    """Tìm top_k chunk gần nhất với câu hỏi"""
    try:
        query_vec = embed_model.encode(f"query: {query}").tolist()
        results = collection.query(
            query_embeddings=[query_vec],
            n_results=top_k
        )
        return results["documents"][0] if results["documents"] else []
    except Exception as e:
        print(f"Error in vector_search: {e}")
        return []


def sql_realtime_context(message: str) -> str:
    """SQL fallback cho dữ liệu thay đổi liên tục (lịch chiếu hôm nay, tồn kho)"""
    msg = message.lower()
    ctx_lines = []

    try:
        conn = mysql.connector.connect(**DB_CONFIG)
        cursor = conn.cursor()

        # Lịch chiếu hôm nay
        if any(k in msg for k in ["hôm nay", "hom nay", "hôm nay", "lich", "gio", "suat", "chieu"]):
            cursor.execute("""
                SELECT m.title, s.start_time, r.name room,
                       ci.name cinema, s.price
                FROM showtimes s
                JOIN movies m  ON s.movie_id  = m.id
                JOIN rooms r   ON s.room_id   = r.id
                JOIN cinemas ci ON r.cinema_id = ci.id
                WHERE DATE(s.start_time) = CURDATE()
                ORDER BY s.start_time
                LIMIT 10
            """)
            rows = cursor.fetchall()
            if rows:
                ctx_lines.append("=== LỊCH CHIẾU HÔM NAY ===")
                for r in rows:
                    start_time_str = r[1].strftime('%H:%M') if hasattr(r[1], 'strftime') else str(r[1])
                    ctx_lines.append(
                        f"{r[0]} | {start_time_str} | {r[3]} - {r[2]} | {int(r[4]):,} VND"
                    )

        # Ghế còn trống theo suất chiếu
        if any(k in msg for k in ["ghế", "ghe", "cho ngoi", "cho trong", "trong"]):
            cursor.execute("""
                SELECT s.id, m.title, r.name room, r.total_seats,
                       COALESCE(COUNT(bs.id), 0) booked
                FROM showtimes s
                JOIN movies m ON s.movie_id = m.id
                JOIN rooms r ON s.room_id = r.id
                LEFT JOIN bookings b ON b.showtime_id = s.id 
                    AND b.status NOT IN ('CANCELLED', 'cancelled')
                LEFT JOIN booking_seats bs ON bs.booking_id = b.id
                WHERE s.start_time >= NOW()
                GROUP BY s.id, m.title, r.name, r.total_seats
                HAVING (r.total_seats - COALESCE(COUNT(bs.id), 0)) > 0
                ORDER BY s.start_time
                LIMIT 5
            """)
            rows = cursor.fetchall()
            if rows:
                ctx_lines.append("=== GHẾ CÒN TRỐNG ===")
                for r in rows:
                    available = r[3] - r[4]
                    ctx_lines.append(
                        f"{r[1]} - {r[2]}: còn {available}/{r[3]} ghế"
                    )

        cursor.close()
        conn.close()

    except Exception as e:
        print(f"Error in sql_realtime_context: {e}")

    return "\n".join(ctx_lines)
