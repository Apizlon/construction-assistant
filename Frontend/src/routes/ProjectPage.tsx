import React, { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { createShareCode } from "../api/projectApi";
import { useAuth } from "../state/auth";
import { getRuErrorMessage } from "../utils/errors";

export function ProjectPage() {
  const { projectId } = useParams();
  const { user } = useAuth();
  const [shareCode, setShareCode] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    setShareCode(null);
    setError(null);
  }, [projectId]);

  async function onShare() {
    if (!projectId) return;
    setError(null);
    try {
      const data = await createShareCode(projectId);
      setShareCode(data.code);
    } catch (e: any) {
      setError(getRuErrorMessage(e, "Не удалось сгенерировать код"));
    }
  }

  return (
    <div className="space-y-5">
      <div className="card">
        <div className="flex items-center justify-between gap-3">
          <div>
            <div className="text-xs text-slate-400">Проект</div>
            <div className="text-lg font-semibold">{projectId}</div>
          </div>
          <div className="flex items-center gap-2">
            <Link className="btn-ghost" to={`/app/projects/${projectId}/passport`}>
              Паспорт объекта
            </Link>
            {user?.role === "Creator" && (
              <button className="btn-primary" onClick={onShare}>
                Поделиться
              </button>
            )}
          </div>
        </div>
        {error && <div className="mt-3 text-sm text-rose-300">{error}</div>}
        {shareCode && (
          <div className="mt-4 rounded-xl border border-slate-800 bg-slate-950/60 px-4 py-3">
            <div className="text-xs text-slate-400">Код доступа</div>
            <div className="mt-1 font-mono text-lg tracking-widest">{shareCode}</div>
            <div className="mt-2 text-xs text-slate-500">Передайте этот код наблюдателю, чтобы он добавил проект в свой кабинет.</div>
          </div>
        )}
      </div>

      <div className="card">
        <div className="text-sm text-slate-300">Дальше здесь будет конфигуратор проекта и окно принятых решений.</div>
      </div>
    </div>
  );
}

