import React, { useEffect, useMemo, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { createProject, joinByCode } from "../api/projectApi";
import { useAuth } from "../state/auth";
import { useProjects } from "../state/projects";
import { Badge } from "./ui";
import { getRuErrorMessage } from "../utils/errors";

export function ProjectsPage() {
  const { user } = useAuth();
  const navigate = useNavigate();
  const { owned, viewer, loading, error, refresh } = useProjects();

  const [localError, setLocalError] = useState<string | null>(null);
  const [newProjectName, setNewProjectName] = useState("");
  const [joinCode, setJoinCode] = useState("");

  const role = user?.role ?? "Viewer";

  useEffect(() => {
    refresh();
  }, [user?.id, refresh]);

  const allCount = useMemo(() => owned.length + viewer.length, [owned.length, viewer.length]);
  const zeroStateText = useMemo(() => {
    if (role === "Creator") return "У вас пока 0 проектов — попробуйте создать новый.";
    if (role === "Viewer") return "У вас пока 0 проектов — попробуйте добавить проект по коду.";
    if (role === "Admin") return "Проекты для админа недоступны.";
    return "У вас пока 0 проектов.";
  }, [role]);

  async function onCreateProject() {
    if (!user) return;
    if (!newProjectName.trim()) return;
    setLocalError(null);
    try {
      const created = await createProject(user.id, newProjectName.trim());
      setNewProjectName("");
      await refresh();
      navigate(`/app/projects/${created.id}/passport`);
    } catch (e: any) {
      setLocalError(getRuErrorMessage(e, "Не удалось создать проект"));
    }
  }

  async function onJoinByCode() {
    if (!user) return;
    if (!joinCode.trim()) return;
    setLocalError(null);
    try {
      await joinByCode(user.id, joinCode.trim());
      setJoinCode("");
      await refresh();
    } catch (e: any) {
      setLocalError(getRuErrorMessage(e, "Не удалось добавить проект по коду"));
    }
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h2 className="text-xl font-semibold tracking-tight">Проекты</h2>
          <div className="text-sm text-slate-400">
            {allCount === 0 ? zeroStateText : `Всего: ${allCount}`}
          </div>
        </div>
        <Badge>{role === "Creator" ? "Создатель" : role === "Admin" ? "Админ" : "Наблюдатель"}</Badge>
      </div>

      {(localError || error) && <div className="text-sm text-rose-300">{localError ?? error}</div>}

      {role === "Creator" && (
        <div className="card">
          <div className="text-sm text-slate-300 mb-3">Создать новый проект</div>
          <div className="flex flex-col sm:flex-row gap-3">
            <input className="input" value={newProjectName} onChange={(e) => setNewProjectName(e.target.value)} placeholder="Например: Дом 180 м², МО" />
            <button className="btn-primary sm:w-40" onClick={onCreateProject}>
              Создать
            </button>
          </div>
        </div>
      )}

      {role !== "Creator" && (
        <div className="card">
          <div className="text-sm text-slate-300 mb-3">Добавить проект по коду</div>
          <div className="flex flex-col sm:flex-row gap-3">
            <input className="input" value={joinCode} onChange={(e) => setJoinCode(e.target.value)} placeholder="XXXX-XXXX" />
            <button className="btn-primary sm:w-40" onClick={onJoinByCode}>
              Добавить
            </button>
          </div>
        </div>
      )}

      {role === "Creator" && (
        <div className="card">
          <div className="flex items-center justify-between mb-3">
            <div className="font-medium">Мои проекты</div>
            <div className="text-xs text-slate-400">{owned.length}</div>
          </div>
          {loading ? (
            <div className="text-sm text-slate-400">Загрузка...</div>
          ) : owned.length === 0 ? (
            <div className="text-sm text-slate-400">Пока пусто.</div>
          ) : (
            <div className="space-y-2">
              {owned.map((p) => (
                <Link key={p.id} to={`/app/projects/${p.id}`} className="block rounded-xl border border-slate-800 bg-slate-950/50 px-4 py-3 hover:border-slate-700">
                  <div className="flex items-center justify-between">
                    <div className="font-medium">{p.name}</div>
                    <span className="text-xs text-slate-400">{p.status}</span>
                  </div>
                  <div className="text-xs text-slate-500 mt-1">Обновлено: {new Date(p.updatedAt).toLocaleString()}</div>
                </Link>
              ))}
            </div>
          )}
        </div>
      )}

      {role !== "Creator" && (
        <div className="card">
          <div className="flex items-center justify-between mb-3">
            <div className="font-medium">Добавленные проекты</div>
            <div className="text-xs text-slate-400">{viewer.length}</div>
          </div>
          {loading ? (
            <div className="text-sm text-slate-400">Загрузка...</div>
          ) : viewer.length === 0 ? (
            <div className="text-sm text-slate-400">Пока пусто.</div>
          ) : (
            <div className="space-y-2">
              {viewer.map((p) => (
                <Link key={p.id} to={`/app/projects/${p.id}`} className="block rounded-xl border border-slate-800 bg-slate-950/50 px-4 py-3 hover:border-slate-700">
                  <div className="flex items-center justify-between">
                    <div className="font-medium">{p.name}</div>
                    <span className="text-xs text-slate-400">{p.status}</span>
                  </div>
                  <div className="text-xs text-slate-500 mt-1">Обновлено: {new Date(p.updatedAt).toLocaleString()}</div>
                </Link>
              ))}
            </div>
          )}
        </div>
      )}
    </div>
  );
}
