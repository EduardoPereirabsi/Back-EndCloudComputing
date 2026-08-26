// services/typescript/src/index.ts
import express from "express";
import os from "os";
import mongoose from "mongoose";
import { requireAuth } from "./auth.js";

const app = express();
app.use(express.json());
const PORT = process.env.PORT || 8080;
const SERVICE_NAME = process.env.SERVICE_NAME || "ms-typescript";
const DB_URL = process.env.DB_URL || "mongodb://mongo:27017/messages";

const MessageSchema = new mongoose.Schema({ text: { type: String, required: true } }, { versionKey: false });
const Message = mongoose.model("Message", MessageSchema);

app.get("/health", (_req, res) => {
  res.json({ status: "ok", service: SERVICE_NAME, hostname: os.hostname() });
});

app.get("/messages", async (_req, res) => {
  const list = await Message.find().lean();
  res.json(list);
});

app.post("/messages", requireAuth, async (req, res) => {
  const created = await Message.create({ text: req.body?.text ?? "" });
  res.status(201).json(created);
});

mongoose.connect(DB_URL).then(() => {
  app.listen(Number(PORT), "0.0.0.0", () => {
    console.log(`${SERVICE_NAME} up on ${PORT} (mongo)`);
  });
});
