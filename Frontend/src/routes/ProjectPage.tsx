import React, { useEffect, useMemo, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { getAnswers } from "../api/answersApi";
import { addComment, getComments, ViewerComment } from "../api/commentsApi";
import { createShareCode, getProject, updateProject } from "../api/projectApi";
import {
  downloadEstimateReportXlsx,
  downloadGanttReportHtml,
  getEstimateReport,
  getGanttReport,
  ProjectEstimateBreakdown,
  ProjectGantt
} from "../api/reportsApi";
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

  const [reportsLoading, setReportsLoading] = useState(false);
  const [reportsError, setReportsError] = useState<string | null>(null);
  const [estimateReport, setEstimateReport] = useState<ProjectEstimateBreakdown | null>(null);
  const [ganttReport, setGanttReport] = useState<ProjectGantt | null>(null);
  const [downloading, setDownloading] = useState<"estimate" | "ganttHtml" | null>(null);

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

        setReportsError(null);
        setReportsLoading(true);
        try {
          const [estimateData, ganttData] = await Promise.all([getEstimateReport(projectId), getGanttReport(projectId)]);
          if (!cancelled) {
            setEstimateReport(estimateData);
            setGanttReport(ganttData);
          }
        } catch (e: any) {
          if (!cancelled) {
            setEstimateReport(null);
            setGanttReport(null);
            setReportsError(getRuErrorMessage(e, "Не удалось загрузить отчёты"));
          }
        } finally {
          if (!cancelled) setReportsLoading(false);
        }
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

  function toRuTitle(title: string) {
    if (title.trim().toLowerCase() === "lvt") return "LVT (кварцвинил)";
    return title;
  }

  async function saveBlob(bytes: ArrayBuffer, fileName: string, contentType?: string) {
    const blob = new Blob([bytes], {
      type: contentType || "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
    });
    const url = URL.createObjectURL(blob);
    try {
      const a = document.createElement("a");
      a.href = url;
      a.download = fileName;
      document.body.appendChild(a);
      a.click();
      a.remove();
    } finally {
      URL.revokeObjectURL(url);
    }
  }

  function openBlobInNewTab(bytes: ArrayBuffer, contentType: string | undefined) {
    const blob = new Blob([bytes], { type: contentType || "text/html; charset=utf-8" });
    const url = URL.createObjectURL(blob);
    const win = window.open(url, "_blank", "noopener,noreferrer");
    // On some browsers window.open can be blocked; keep url so user still downloaded the file.
    if (!win) {
      setTimeout(() => URL.revokeObjectURL(url), 60_000);
      return;
    }
    setTimeout(() => URL.revokeObjectURL(url), 60_000);
  }

  async function onDownloadEstimate() {
    if (!projectId) return;
    setReportsError(null);
    setDownloading("estimate");
    try {
      const { bytes, contentType } = await downloadEstimateReportXlsx(projectId);
      await saveBlob(bytes, `Смета_проект_${projectId}.xlsx`, contentType);
    } catch (e: any) {
      setReportsError(getRuErrorMessage(e, "Не удалось скачать смету"));
    } finally {
      setDownloading(null);
    }
  }

  async function onDownloadGanttHtml() {
    if (!projectId) return;
    setReportsError(null);
    setDownloading("ganttHtml");
    try {
      const { bytes, contentType } = await downloadGanttReportHtml(projectId);
      // 1) Скачивание файла
      await saveBlob(bytes, `ДиаграммаГанта_проект_${projectId}.html`, contentType);
      // 2) Быстрый просмотр в новой вкладке
      openBlobInNewTab(bytes, contentType);
    } catch (e: any) {
      setReportsError(getRuErrorMessage(e, "Не удалось скачать диаграмму Ганта (HTML)"));
    } finally {
      setDownloading(null);
    }
  }

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
            <Link className="btn-ghost" to={`/app/projects/${projectId}/optimization`}>
              Оптимизация
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
        <div className="flex items-center justify-between gap-3">
          <div>
            <div className="font-medium">Отчётность</div>
            <div className="text-xs text-slate-500 mt-1">Смета и диаграмма Ганта (оценка)</div>
          </div>
          <div className="flex items-center gap-2">
            <button className="btn-primary" onClick={onDownloadEstimate} disabled={!estimateReport || downloading !== null}>
              {downloading === "estimate" ? "Скачивание…" : "Смета (Excel)"}
            </button>
            <button className="btn-primary" onClick={onDownloadGanttHtml} disabled={!ganttReport || downloading !== null}>
              {downloading === "ganttHtml" ? "Скачивание…" : "Гант (HTML)"}
            </button>
          </div>
        </div>

        {reportsLoading && <div className="mt-3 text-sm text-slate-400">Загрузка отчётов…</div>}
        {reportsError && <div className="mt-3 text-sm text-rose-300">{reportsError}</div>}

        {estimateReport && (
          <div className="mt-4 rounded-xl border border-slate-800 bg-slate-950/50 p-4">
            <div className="flex items-center justify-between">
              <div className="font-medium">Смета (из чего формируется цена)</div>
              <div className="text-xs text-slate-500">{new Date(estimateReport.generatedAt).toLocaleString()}</div>
            </div>
            <div className="mt-3 grid grid-cols-1 md:grid-cols-4 gap-3 text-sm">
              <div className="rounded-xl border border-slate-800 bg-slate-950/60 px-4 py-3">
                <div className="text-xs text-slate-500">База за м²</div>
                <div className="mt-1 font-semibold">{estimateReport.basePerM2Rub.toLocaleString("ru-RU")} ₽</div>
              </div>
              <div className="rounded-xl border border-slate-800 bg-slate-950/60 px-4 py-3">
                <div className="text-xs text-slate-500">Площадь</div>
                <div className="mt-1 font-semibold">{estimateReport.areaM2 ? `${estimateReport.areaM2} м²` : "—"}</div>
              </div>
              <div className="rounded-xl border border-slate-800 bg-slate-950/60 px-4 py-3">
                <div className="text-xs text-slate-500">Коэффициент</div>
                <div className="mt-1 font-semibold">× {estimateReport.factor.toFixed(2)}</div>
              </div>
              <div className="rounded-xl border border-slate-800 bg-slate-950/60 px-4 py-3">
                <div className="text-xs text-slate-500">Итого</div>
                <div className="mt-1 font-semibold">{estimateReport.totalRub.toLocaleString("ru-RU")} ₽</div>
              </div>
            </div>

            <div className="mt-4 overflow-auto">
              <table className="min-w-[720px] w-full text-sm">
                <thead className="text-xs text-slate-500">
                  <tr className="border-b border-slate-800">
                    <th className="py-2 pr-3 text-left font-medium">Пункт</th>
                    <th className="py-2 pr-3 text-right font-medium">Δ, %</th>
                    <th className="py-2 pr-0 text-right font-medium">Δ, ₽</th>
                  </tr>
                </thead>
                <tbody>
                  {estimateReport.items.length === 0 ? (
                    <tr className="border-b border-slate-900">
                      <td className="py-3 text-slate-400" colSpan={3}>
                        Нет влияющих факторов (используется базовая ставка).
                      </td>
                    </tr>
                  ) : (
                    estimateReport.items.map((it) => (
                      <tr key={it.code} className="border-b border-slate-900">
                        <td className="py-3 pr-3 text-slate-200">{toRuTitle(it.title)}</td>
                        <td className="py-3 pr-3 text-right text-slate-300">{(it.percentDelta * 100).toFixed(0)}%</td>
                        <td className="py-3 pr-0 text-right text-slate-200">{it.deltaRub.toLocaleString("ru-RU")}</td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>

            <div className="mt-3 text-xs text-slate-500">
              База: {estimateReport.baseCostRub.toLocaleString("ru-RU")} ₽ • Срок: ≈ {estimateReport.months} мес.
            </div>
          </div>
        )}

        {ganttReport && (
          <div className="mt-4 rounded-xl border border-slate-800 bg-slate-950/50 p-4">
            <div className="flex items-center justify-between">
              <div className="font-medium">Диаграмма Ганта (последовательность работ)</div>
              <div className="text-xs text-slate-500">
                {ganttReport.totalMonths} мес. • {ganttReport.totalDays} дней
              </div>
            </div>
            <div className="mt-3 space-y-2">
              {ganttReport.tasks.map((t) => {
                const total = ganttReport.totalDays || 1;
                const left = Math.max(0, Math.min(100, (t.startDay / total) * 100));
                const width = Math.max(1, Math.min(100 - left, ((t.endDay - t.startDay) / total) * 100));
                return (
                  <div key={t.id} className="grid grid-cols-12 gap-3 items-center">
                    <div className="col-span-12 md:col-span-4 text-sm text-slate-200">{t.title}</div>
                    <div className="col-span-12 md:col-span-8">
                      <div className="h-8 rounded-lg border border-slate-800 bg-slate-950/60 relative overflow-hidden">
                        <div className="absolute inset-y-0 bg-indigo-600/50" style={{ left: `${left}%`, width: `${width}%` }} />
                        <div className="absolute inset-0 flex items-center justify-between px-3 text-xs text-slate-300">
                          <span>день {t.startDay}</span>
                          <span>день {t.endDay}</span>
                        </div>
                      </div>
                    </div>
                  </div>
                );
              })}
            </div>
          </div>
        )}
      </div>

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
