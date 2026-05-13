import React, { useEffect, useState } from "react";
import { createFeedback, getAllFeedback } from "../api/userApi";
import { useAuth } from "../state/auth";
import { Badge } from "./ui";
import { getRuErrorMessage } from "../utils/errors";

export function FeedbackPage() {
  const { accessToken, user } = useAuth();
  const isAdmin = user?.role === "Admin";
  const [message, setMessage] = useState("");
  const [sending, setSending] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [ok, setOk] = useState<string | null>(null);

  const [adminList, setAdminList] = useState<Array<{ id: string; email: string; message: string; createdAt: string }>>([]);

  async function onSend() {
    if (!accessToken) return;
    if (!message.trim()) return;
    setSending(true);
    setError(null);
    setOk(null);
    try {
      await createFeedback(accessToken, { message: message.trim() });
      setMessage("");
      setOk("Спасибо! Сообщение отправлено.");
    } catch (e: any) {
      setError(getRuErrorMessage(e, "Не удалось отправить"));
    } finally {
      setSending(false);
    }
  }

  useEffect(() => {
    if (!accessToken) return;
    if (!isAdmin) return;
    getAllFeedback(accessToken)
      .then((xs) => setAdminList(xs))
      .catch(() => setAdminList([]));
  }, [accessToken, isAdmin]);

  return (
    <div className="space-y-6">
      <div className="flex items-end justify-between gap-4">
        <div>
          <h2 className="text-xl font-semibold tracking-tight">Обратная связь</h2>
          <div className="text-sm text-slate-400">Опишите проблему, идею или пожелание — мы используем это для улучшений.</div>
        </div>
        <Badge>{user?.role === "Admin" ? "Админ" : "Пользователь"}</Badge>
      </div>

      {!isAdmin && (
        <div className="card space-y-3">
          <div className="label">Сообщение</div>
          <textarea
            className="input min-h-[120px]"
            value={message}
            onChange={(e) => setMessage(e.target.value)}
            placeholder="Например: хочу возможность экспортировать диаграмму Ганта в PDF…"
          />
          <div className="flex items-center gap-3">
            <button className="btn-primary" onClick={onSend} disabled={sending}>
              {sending ? "Отправляем..." : "Отправить"}
            </button>
            {ok && <div className="text-sm text-emerald-300">{ok}</div>}
            {error && <div className="text-sm text-rose-300">{error}</div>}
          </div>
        </div>
      )}

      {isAdmin && (
        <div className="card">
          <div className="flex items-center justify-between mb-3">
            <div className="font-medium">Все сообщения</div>
            <div className="text-xs text-slate-400">{adminList.length}</div>
          </div>
          {adminList.length === 0 ? (
            <div className="text-sm text-slate-400">Пока нет сообщений.</div>
          ) : (
            <div className="space-y-3">
              {adminList.map((x) => (
                <div key={x.id} className="rounded-xl border border-slate-800 bg-slate-950/50 px-4 py-3">
                  <div className="flex items-center justify-between gap-3">
                    <div className="text-sm text-slate-200">{x.email}</div>
                    <div className="text-xs text-slate-500">{new Date(x.createdAt).toLocaleString()}</div>
                  </div>
                  <div className="mt-2 text-sm text-slate-300 whitespace-pre-wrap">{x.message}</div>
                </div>
              ))}
            </div>
          )}
        </div>
      )}
    </div>
  );
}
