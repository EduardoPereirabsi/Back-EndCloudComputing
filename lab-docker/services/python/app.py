# services/python/app.py
from fastapi import FastAPI
from pydantic import BaseModel
from sqlalchemy import create_engine, text
import socket, os

app = FastAPI()
SERVICE_NAME = "ms-python"
DB_URL = os.getenv("DB_URL", "postgresql+psycopg2://postgres:postgres@postgres:5432/messages")
engine = create_engine(DB_URL, pool_pre_ping=True)

class MsgIn(BaseModel):
    text: str

@app.get("/health")
def health():
    return {"status": "ok", "service": SERVICE_NAME, "hostname": socket.gethostname()}

@app.get("/messages")
def list_messages():
    with engine.connect() as conn:
        rows = conn.execute(text("SELECT id, text FROM messages ORDER BY id")).mappings().all()
        return [dict(r) for r in rows]

@app.post("/messages", status_code=201)
def create_message(msg: MsgIn):
    with engine.begin() as conn:
        row = conn.execute(text("INSERT INTO messages(text) VALUES (:t) RETURNING id"), {"t": msg.text}).fetchone()
        return {"id": row.id, "text": msg.text}
