import React, { useEffect, useMemo, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { getAnswers } from "../api/answersApi";
import { addComment, getComments, ViewerComment } from "../api/commentsApi";
import { createShareCode, getProject, updateProject } from "../api/projectApi";
import { useAuth } from "../state/auth";
import { getRuErrorMessage } from "../utils/errors";
import { getPassportFromAnswers, isPassportComplete } from "../utils/passport";
import { builderMultiStepCodes, builderStepCodes, estimateProject, parseMultiValues } from "../utils/builder";

export function ProjectPage() {
  const { projectId } = useParams();
  const { user } = useAuth();

  const [shareCode, setShareCode] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [passportComplete, setPassportComplete] = useState<boolean | null>(null);
  const [projectName, setProjectName] = useState<string>(projectId ?? "");
  const [renaming, setRenaming] = useState(false);
  const [nameDraft, setNameDraft] = useState("");
  const [savingName, setSavingName] = useState(false);
  const [comments, setComments] = useState<ViewerComment[]>([]);
  const [loadingComments, setLoadingComments] = useState(false);
  const [commentDraft, setCommentDraft] = useState("");
  const [estimate, setEstimate] = useState<{ totalRub: number; months: number } | null>(null);

  useEffect(() => {
    setShareCode(null);
    setError(null);
    setRenaming(false);
    setNameDraft("");
  }, [projectId]);

  useEffect(() => {
    if (!projectId) return;
    let cancelled = false;

    (async () => {
      try {
        const [p, answers] = await Promise.all([getProject(projectId), getAnswers(projectId)]);
        if (cancelled) return;
        setProjectName(p.name);
        const passport = getPassportFromAnswers(answers);
        const complete = isPassportComplete(passport);
        setPassportComplete(complete);

        const selectedSingle: Record<string, string | null> = {};
        for (const code of Object.values(builderStepCodes)) {
          const a = answers.find((x) => x.stepCode === code);
          selectedSingle[code] = a?.selectedOptionCode ?? null;
        }
        const selectedMulti: Record<string, string[]> = {};
        for (const code of Object.values(builderMultiStepCodes)) {
          const a = answers.find((x) => x.stepCode === code);
          selectedMulti[code] = parseMultiValues(a?.valueJson);
        }
        const est = estimateProject(passport.buildingType, passport.areaM2, selectedSingle, selectedMulti);
        setEstimate(est.totalRub > 0 ? est : null);
      } catch {
        if (!cancelled) setPassportComplete(null);
      }
    })();

    return () => {
      cancelled = true;
    };
  }, [projectId]);

  useEffect(() => {
    if (!projectId) return;
    let cancelled = false;

    (async () => {
      setLoadingComments(true);
      try {
        const data = await getComments(projectId);
        if (!cancelled) setComments(data.slice().sort((a, b) => new Date(a.commentDate).getTime() - new Date(b.commentDate).getTime()));
      } catch {
        // ignore
      } finally {
        if (!cancelled) setLoadingComments(false);
      }
    })();

    return () => {
      cancelled = true;
    };
  }, [projectId]);

  const passportHint = useMemo(() => {
    if (passportComplete === true) return "Паспорт заполнен";
    if (passportComplete === false) return "Заполните паспорт, чтобы начать конструктор";
    return null;
  }, [passportComplete]);

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

  async function onSaveName() {
    if (!projectId) return;
    const next = nameDraft.trim();
    if (!next) {
      setError("Введите название проекта");
      return;
    }
    setError(null);
    setSavingName(true);
    try {
      const updated = await updateProject(projectId, { name: next });
      setProjectName(updated.name);
      setRenaming(false);
      setNameDraft("");
    } catch (e: any) {
      setError(getRuErrorMessage(e, "Не удалось переименовать проект"));
    } finally {
      setSavingName(false);
    }
  }

  async function onSendComment() {
    if (!projectId) return;
    if (!user) return;
    const text = commentDraft.trim();
    if (!text) return;
    setError(null);
    try {
      await addComment(projectId, { userId: user.id, email: user.email, commentText: text });
      setCommentDraft("");
      const data = await getComments(projectId);
      setComments(data.slice().sort((a, b) => new Date(a.commentDate).getTime() - new Date(b.commentDate).getTime()));
    } catch (e: any) {
      setError(getRuErrorMessage(e, "Не удалось отправить сообщение"));
    }
  }

  return (
    <div className="space-y-5">
      <div className="card">
        <div className="flex items-center justify-between gap-3">
          <div>
            <div className="text-xs text-slate-400">Проект</div>
            <div className="text-lg font-semibold">{projectName || projectId}</div>
            {passportHint && <div className="mt-1 text-xs text-slate-500">{passportHint}</div>}
          </div>
          <div className="flex items-center gap-2">
            <Link className="btn-ghost" to={`/app/projects/${projectId}/passport`}>
              Паспорт
            </Link>
            <Link className="btn-primary" to={`/app/projects/${projectId}/builder`}>
              Конструктор
            </Link>
            {(user?.role === "Creator" || user?.role === "Admin") && (
              <button
                className="btn-ghost"
                onClick={() => {
                  setError(null);
                  setRenaming((v) => {
                    const next = !v;
                    if (next) setNameDraft(projectName);
                    return next;
                  });
                }}
                disabled={savingName}
              >
                Переименовать
              </button>
            )}
            {user?.role === "Creator" && (
              <button className="btn-ghost" onClick={onShare}>
                Поделиться
              </button>
            )}
          </div>
        </div>
        {error && <div className="mt-3 text-sm text-rose-300">{error}</div>}
        {renaming && (
          <div className="mt-4 rounded-xl border border-slate-800 bg-slate-950/60 px-4 py-3">
            <div className="text-xs text-slate-400">Название проекта</div>
            <div className="mt-2 flex flex-col sm:flex-row gap-3">
              <input
                className="input"
                value={nameDraft}
                onChange={(e) => setNameDraft(e.target.value)}
                placeholder="Например: Дом 180 м², МО"
                onKeyDown={(e) => {
                  if (e.key === "Enter") onSaveName();
                }}
                disabled={savingName}
              />
              <div className="flex gap-2 sm:w-56">
                <button className="btn-primary flex-1" onClick={onSaveName} disabled={savingName}>
                  Сохранить
                </button>
                <button
                  className="btn-ghost flex-1"
                  onClick={() => {
                    setRenaming(false);
                    setNameDraft("");
                    setError(null);
                  }}
                  disabled={savingName}
                >
                  Отмена
                </button>
              </div>
            </div>
          </div>
        )}
        {shareCode && (
          <div className="mt-4 rounded-xl border border-slate-800 bg-slate-950/60 px-4 py-3">
            <div className="text-xs text-slate-400">Код доступа</div>
            <div className="mt-1 font-mono text-lg tracking-widest">{shareCode}</div>
            <div className="mt-2 text-xs text-slate-500">Передайте этот код наблюдателю, чтобы он добавил проект в свой кабинет.</div>
          </div>
        )}
      </div>

      <div className="card">
        <div className="text-sm text-slate-300">Здесь будет развиваться конфигуратор проекта: паспорт → конструктор → расчёты → отчёты/экспорт.</div>
      </div>

      {estimate && (
        <div className="card">
          <div className="flex items-center justify-between">
            <div className="font-medium">Краткий итог</div>
            <div className="text-xs text-slate-500">оценка</div>
          </div>
          <div className="mt-3 grid grid-cols-1 md:grid-cols-2 gap-3">
            <div className="rounded-xl border border-slate-800 bg-slate-950/50 px-4 py-3">
              <div className="text-xs text-slate-500">Сумма</div>
              <div className="mt-1 text-xl font-semibold">{estimate.totalRub.toLocaleString("ru-RU")} ₽</div>
              <div className="mt-1 text-xs text-slate-500">Площадь × базовая ставка × решения</div>
            </div>
            <div className="rounded-xl border border-slate-800 bg-slate-950/50 px-4 py-3">
              <div className="text-xs text-slate-500">Срок</div>
              <div className="mt-1 text-xl font-semibold">≈ {estimate.months} мес.</div>
              <div className="mt-1 text-xs text-slate-500">Без учёта согласований и поставок</div>
            </div>
          </div>
        </div>
      )}

      <div className="card">
        <div className="flex items-center justify-between">
          <div className="font-medium">Чат по проекту</div>
          <div className="text-xs text-slate-500">{loadingComments ? "Загрузка…" : `${comments.length}`}</div>
        </div>
        <div className="mt-3 space-y-2 max-h-72 overflow-auto pr-1">
          {comments.length === 0 ? (
            <div className="text-sm text-slate-400">Пока нет сообщений.</div>
          ) : (
            comments.map((c) => {
              const mine = c.userId === user?.id;
              return (
                <div key={c.id} className={mine ? "flex justify-end" : "flex justify-start"}>
                  <div className="max-w-[80%] rounded-xl border border-slate-800 bg-slate-950/50 px-4 py-3">
                    <div className="text-xs text-slate-500 flex items-center justify-between gap-3">
                      <span>{mine ? "Вы" : c.email}</span>
                      <span>{new Date(c.commentDate).toLocaleString()}</span>
                    </div>
                    <div className="mt-1 text-sm text-slate-200 whitespace-pre-wrap">{c.commentText}</div>
                  </div>
                </div>
              );
            })
          )}
        </div>
        <div className="mt-3 flex flex-col sm:flex-row gap-3">
          <input
            className="input"
            value={commentDraft}
            onChange={(e) => setCommentDraft(e.target.value)}
            placeholder="Напишите сообщение…"
            onKeyDown={(e) => {
              if (e.key === "Enter") onSendComment();
            }}
          />
          <button className="btn-primary sm:w-40" onClick={onSendComment} disabled={!commentDraft.trim()}>
            Отправить
          </button>
        </div>
      </div>
    </div>
  );
}
