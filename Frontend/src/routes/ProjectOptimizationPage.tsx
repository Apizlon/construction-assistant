import React, { useEffect, useMemo, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import {
  createOptimization,
  deleteOptimization,
  getOptimization,
  getOptimizations,
  getOptimizationTemplates,
  OptimizationDetails,
  OptimizationListItem,
  OptimizationPreview,
  OptimizationTemplate,
  previewOptimization
} from "../api/optimizationApi";
import { getProject } from "../api/projectApi";
import { useAuth } from "../state/auth";
import { getRuErrorMessage } from "../utils/errors";

function inRect(p: { x: number; y: number }, r: { x: number; y: number; w: number; h: number }) {
  return p.x >= r.x && p.y >= r.y && p.x < r.x + r.w && p.y < r.y + r.h;
}

function isBlocked(t: OptimizationTemplate, p: { x: number; y: number }) {
  return t.walls.some((r) => inRect(p, r)) || t.forbiddenZones.some((r) => inRect(p, r));
}

function formatCategory(cat: string) {
  if (cat === "Apartment") return "Квартира";
  if (cat === "House") return "Дом";
  if (cat === "Office") return "Офис";
  if (cat === "Warehouse") return "Склад";
  return cat;
}

function PlanCanvas({
  template,
  start,
  end,
  setStart,
  setEnd,
  preview
}: {
  template: OptimizationTemplate;
  start: { x: number; y: number } | null;
  end: { x: number; y: number } | null;
  setStart: (p: { x: number; y: number } | null) => void;
  setEnd: (p: { x: number; y: number } | null) => void;
  preview: OptimizationPreview | null;
}) {
  const [pickMode, setPickMode] = useState<"start" | "end">("start");

  const cell = 16;
  const w = template.width * cell;
  const h = template.height * cell;

  const variantPaths = useMemo(() => {
    const wall = preview?.variants.find((v) => v.variantType === "WallFriendly");
    const direct = preview?.variants.find((v) => v.variantType === "Direct");
    return {
      wall: wall?.isFound ? wall.path : [],
      direct: direct?.isFound ? direct.path : []
    };
  }, [preview]);

  const toPolyline = (pts: { x: number; y: number }[]) => pts.map((p) => `${p.x * cell + cell / 2},${p.y * cell + cell / 2}`).join(" ");

  return (
    <div className="rounded-xl border border-slate-800 bg-slate-950/50 p-3">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="text-sm text-slate-300">
          Выбор точек:{" "}
          <span className="text-slate-100 font-medium">{pickMode === "start" ? "Старт" : "Финиш"}</span> • клик по свободной клетке
        </div>
        <div className="flex items-center gap-2">
          <button className={pickMode === "start" ? "btn-primary" : "btn-ghost"} onClick={() => setPickMode("start")}>
            Старт
          </button>
          <button className={pickMode === "end" ? "btn-primary" : "btn-ghost"} onClick={() => setPickMode("end")}>
            Финиш
          </button>
          <button className="btn-ghost" onClick={() => (setStart(null), setEnd(null))}>
            Сброс
          </button>
        </div>
      </div>

      <div className="mt-3 overflow-auto">
        <svg
          width={w}
          height={h}
          viewBox={`0 0 ${w} ${h}`}
          className="rounded-lg border border-slate-800 bg-slate-950"
          onClick={(e) => {
            const target = e.currentTarget.getBoundingClientRect();
            const x = Math.floor((e.clientX - target.left) / cell);
            const y = Math.floor((e.clientY - target.top) / cell);
            const p = { x, y };
            if (x < 0 || y < 0 || x >= template.width || y >= template.height) return;
            if (isBlocked(template, p)) return;
            if (pickMode === "start") setStart(p);
            else setEnd(p);
          }}
        >
          <defs>
            <pattern id="grid" width={cell} height={cell} patternUnits="userSpaceOnUse">
              <path d={`M ${cell} 0 L 0 0 0 ${cell}`} fill="none" stroke="rgba(148,163,184,0.10)" strokeWidth="1" />
            </pattern>
          </defs>
          <rect width="100%" height="100%" fill="url(#grid)" />

          {template.walls.map((r, i) => (
            <rect key={`w${i}`} x={r.x * cell} y={r.y * cell} width={r.w * cell} height={r.h * cell} fill="rgba(148,163,184,0.35)" />
          ))}
          {template.forbiddenZones.map((r, i) => (
            <rect
              key={`z${i}`}
              x={r.x * cell}
              y={r.y * cell}
              width={r.w * cell}
              height={r.h * cell}
              fill="rgba(239,68,68,0.40)"
              stroke="rgba(239,68,68,0.65)"
            />
          ))}

          {variantPaths.wall.length > 1 && (
            <polyline points={toPolyline(variantPaths.wall)} fill="none" stroke="rgba(99,102,241,0.95)" strokeWidth="3" strokeLinejoin="round" />
          )}
          {variantPaths.direct.length > 1 && (
            <polyline points={toPolyline(variantPaths.direct)} fill="none" stroke="rgba(34,197,94,0.90)" strokeWidth="3" strokeLinejoin="round" />
          )}

          {start && <circle cx={start.x * cell + cell / 2} cy={start.y * cell + cell / 2} r="6" fill="rgba(59,130,246,0.95)" />}
          {end && <circle cx={end.x * cell + cell / 2} cy={end.y * cell + cell / 2} r="6" fill="rgba(251,191,36,0.95)" />}
        </svg>
      </div>

      <div className="mt-2 text-xs text-slate-500">
        Стены/перегородки: серым • Запрещённые зоны: красным • Вариант 1 (вдоль стен): индиго • Вариант 2 (прямой): зелёным
      </div>
    </div>
  );
}

function PlanStatic({ template, start, end, preview }: { template: OptimizationTemplate; start: { x: number; y: number }; end: { x: number; y: number }; preview: OptimizationPreview }) {
  const cell = 16;
  const w = template.width * cell;
  const h = template.height * cell;

  const wall = preview.variants.find((v) => v.variantType === "WallFriendly");
  const direct = preview.variants.find((v) => v.variantType === "Direct");
  const toPolyline = (pts: { x: number; y: number }[]) => pts.map((p) => `${p.x * cell + cell / 2},${p.y * cell + cell / 2}`).join(" ");

  return (
    <div className="rounded-xl border border-slate-800 bg-slate-950/50 p-3">
      <div className="text-sm text-slate-300">План и сохранённые маршруты</div>
      <div className="mt-3 overflow-auto">
        <svg width={w} height={h} viewBox={`0 0 ${w} ${h}`} className="rounded-lg border border-slate-800 bg-slate-950">
          <defs>
            <pattern id="grid_static" width={cell} height={cell} patternUnits="userSpaceOnUse">
              <path d={`M ${cell} 0 L 0 0 0 ${cell}`} fill="none" stroke="rgba(148,163,184,0.10)" strokeWidth="1" />
            </pattern>
          </defs>
          <rect width="100%" height="100%" fill="url(#grid_static)" />

          {template.walls.map((r, i) => (
            <rect key={`w${i}`} x={r.x * cell} y={r.y * cell} width={r.w * cell} height={r.h * cell} fill="rgba(148,163,184,0.35)" />
          ))}
          {template.forbiddenZones.map((r, i) => (
            <rect
              key={`z${i}`}
              x={r.x * cell}
              y={r.y * cell}
              width={r.w * cell}
              height={r.h * cell}
              fill="rgba(239,68,68,0.40)"
              stroke="rgba(239,68,68,0.65)"
            />
          ))}

          {wall?.isFound && wall.path.length > 1 && (
            <polyline points={toPolyline(wall.path)} fill="none" stroke="rgba(99,102,241,0.95)" strokeWidth="3" strokeLinejoin="round" />
          )}
          {direct?.isFound && direct.path.length > 1 && (
            <polyline points={toPolyline(direct.path)} fill="none" stroke="rgba(34,197,94,0.90)" strokeWidth="3" strokeLinejoin="round" />
          )}

          <circle cx={start.x * cell + cell / 2} cy={start.y * cell + cell / 2} r="6" fill="rgba(59,130,246,0.95)" />
          <circle cx={end.x * cell + cell / 2} cy={end.y * cell + cell / 2} r="6" fill="rgba(251,191,36,0.95)" />
        </svg>
      </div>
      <div className="mt-2 text-xs text-slate-500">Индиго: «вдоль стен» • Зелёный: «прямой»</div>
    </div>
  );
}

export function ProjectOptimizationPage() {
  const { projectId, optimizationId } = useParams();
  const { user } = useAuth();
  const nav = useNavigate();

  const actorUserId = user?.id ?? "";
  const [ownerUserId, setOwnerUserId] = useState<string | null>(null);
  const canEdit = (user?.role === "Creator" || user?.role === "Admin") && !!ownerUserId && ownerUserId === actorUserId;

  const [templates, setTemplates] = useState<OptimizationTemplate[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [items, setItems] = useState<OptimizationListItem[]>([]);
  const [selected, setSelected] = useState<OptimizationDetails | null>(null);
  const [loadingSelected, setLoadingSelected] = useState(false);

  const [creating, setCreating] = useState(false);
  const [templateId, setTemplateId] = useState<string>("");
  const [communicationType, setCommunicationType] = useState<"Water" | "Electricity">("Water");
  const [start, setStart] = useState<{ x: number; y: number } | null>(null);
  const [end, setEnd] = useState<{ x: number; y: number } | null>(null);
  const [preview, setPreview] = useState<OptimizationPreview | null>(null);
  const [previewLoading, setPreviewLoading] = useState(false);
  const [selectedVariant, setSelectedVariant] = useState<string>("WallFriendly");
  const [saving, setSaving] = useState(false);

  const activeTemplate = useMemo(() => templates.find((t) => t.id === templateId) ?? null, [templates, templateId]);
  const templateTitle = useMemo(() => {
    const map = new Map(templates.map((t) => [t.id, t.title]));
    return (id: string) => map.get(id) ?? id;
  }, [templates]);

  const selectedTemplate = useMemo(() => {
    if (!selected) return null;
    return templates.find((t) => t.id === selected.templateId) ?? null;
  }, [selected, templates]);

  useEffect(() => {
    if (!projectId || !actorUserId) return;
    let cancelled = false;
    setLoading(true);
    setError(null);
    (async () => {
      try {
        const [project, tpls, list] = await Promise.all([
          getProject(projectId).catch(() => null),
          getOptimizationTemplates(projectId, actorUserId),
          getOptimizations(projectId, actorUserId)
        ]);
        if (cancelled) return;
        setOwnerUserId(project?.ownerUserId ?? null);
        setTemplates(tpls);
        setItems(list);
        if (!templateId && tpls.length > 0) setTemplateId(tpls[0].id);
      } catch (e: any) {
        if (!cancelled) setError(getRuErrorMessage(e, "Не удалось загрузить оптимизации"));
      } finally {
        if (!cancelled) setLoading(false);
      }
    })();
    return () => {
      cancelled = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [projectId, actorUserId]);

  useEffect(() => {
    if (!projectId || !optimizationId || !actorUserId) {
      setSelected(null);
      return;
    }
    let cancelled = false;
    setLoadingSelected(true);
    (async () => {
      try {
        const data = await getOptimization(projectId, optimizationId, actorUserId);
        if (!cancelled) setSelected(data);
      } catch {
        if (!cancelled) setSelected(null);
      } finally {
        if (!cancelled) setLoadingSelected(false);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [projectId, optimizationId, actorUserId]);

  useEffect(() => {
    if (!projectId || !actorUserId || !creating) return;
    if (!templateId || !start || !end) {
      setPreview(null);
      return;
    }
    let cancelled = false;
    setPreviewLoading(true);
    (async () => {
      try {
        const data = await previewOptimization(projectId, { actorUserId, templateId, communicationType, start, end });
        if (!cancelled) setPreview(data);
      } catch (e: any) {
        if (!cancelled) setPreview(null);
      } finally {
        if (!cancelled) setPreviewLoading(false);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [creating, projectId, actorUserId, templateId, communicationType, start, end]);

  async function refreshList() {
    if (!projectId || !actorUserId) return;
    const list = await getOptimizations(projectId, actorUserId).catch(() => []);
    setItems(list);
  }

  async function onCreate() {
    if (!projectId || !actorUserId || !templateId || !start || !end) return;
    setSaving(true);
    setError(null);
    try {
      await createOptimization(projectId, { actorUserId, templateId, communicationType, start, end, selectedVariant });
      setCreating(false);
      setPreview(null);
      setStart(null);
      setEnd(null);
      await refreshList();
    } catch (e: any) {
      setError(getRuErrorMessage(e, "Не удалось сохранить оптимизацию"));
    } finally {
      setSaving(false);
    }
  }

  async function onDelete(id: string) {
    if (!projectId || !actorUserId) return;
    if (!confirm("Удалить оптимизацию?")) return;
    setError(null);
    try {
      await deleteOptimization(projectId, id, actorUserId);
      if (optimizationId === id) nav(`/app/projects/${projectId}/optimization`);
      await refreshList();
    } catch (e: any) {
      setError(getRuErrorMessage(e, "Не удалось удалить оптимизацию"));
    }
  }

  const variants = preview?.variants ?? [];
  const selectedVariantObj = variants.find((v) => v.variantType === selectedVariant) ?? null;
  const canSave = !!selectedVariantObj?.isFound && !saving;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between gap-3">
        <div>
          <div className="text-xs text-slate-500">
            <Link to={`/app/projects/${projectId}`} className="hover:text-slate-200">
              ← Проект
            </Link>
          </div>
          <div className="text-xl font-semibold">Оптимизация коммуникаций</div>
          <div className="mt-1 text-sm text-slate-400">
            Алгоритм строит 2 оптимальных маршрута в заданной модели: «вдоль стен» и «минимальная длина».
          </div>
        </div>
        {canEdit && (
          <button className="btn-primary" onClick={() => (setCreating(true), nav(`/app/projects/${projectId}/optimization`))}>
            Новая оптимизация
          </button>
        )}
      </div>

      {error && <div className="rounded-xl border border-rose-900 bg-rose-950/30 px-4 py-3 text-sm text-rose-200">{error}</div>}

      {loading ? (
        <div className="text-sm text-slate-400">Загрузка…</div>
      ) : (
        <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">
          <div className="lg:col-span-5 space-y-3">
            <div className="rounded-xl border border-slate-800 bg-slate-950/50 p-4">
              <div className="flex items-center justify-between">
                <div className="font-medium">Ваши решения</div>
                <div className="text-xs text-slate-500">{items.length}</div>
              </div>
              {items.length === 0 ? (
                <div className="mt-3 text-sm text-slate-400">Пока нет сохранённых оптимизаций.</div>
              ) : (
                <div className="mt-3 space-y-2">
                  {items.map((it) => (
                    <div key={it.id} className="rounded-lg border border-slate-800 bg-slate-950/40 p-3">
                      <div className="flex items-start justify-between gap-3">
                        <div>
                          <div className="text-sm text-slate-100 font-medium">
                            <Link to={`/app/projects/${projectId}/optimization/${it.id}`} className="hover:underline">
                              {templateTitle(it.templateId)}
                            </Link>
                          </div>
                          <div className="mt-1 text-xs text-slate-400">
                            {it.communicationType} • {it.selectedVariant ?? "—"} • длина:{" "}
                            {typeof it.selectedLengthUnits === "number" ? it.selectedLengthUnits.toFixed(1) : "—"} • {new Date(it.createdAt).toLocaleString()}
                          </div>
                          <div className="mt-1 text-xs text-slate-500">
                            ({it.start.x},{it.start.y}) → ({it.end.x},{it.end.y})
                          </div>
                        </div>
                        {canEdit && (
                          <button className="btn-ghost" onClick={() => onDelete(it.id)}>
                            Удалить
                          </button>
                        )}
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>

            {optimizationId && (
              <div className="rounded-xl border border-slate-800 bg-slate-950/50 p-4">
                <div className="font-medium">Детали</div>
                {loadingSelected ? (
                  <div className="mt-3 text-sm text-slate-400">Загрузка…</div>
                ) : !selected ? (
                  <div className="mt-3 text-sm text-slate-400">Не удалось загрузить.</div>
                ) : (
                  <div className="mt-3 space-y-3">
                    <div className="text-sm text-slate-200">{templateTitle(selected.templateId)}</div>
                    <div className="text-xs text-slate-500">
                      {selected.communicationType} • выбран: {selected.selectedVariant ?? "—"} • {new Date(selected.createdAt).toLocaleString()}
                    </div>
                    {selectedTemplate && (
                      <PlanStatic template={selectedTemplate} start={selected.start} end={selected.end} preview={selected.result} />
                    )}
                    <div className="rounded-lg border border-slate-800 bg-slate-950/40 p-3">
                      <div className="text-xs text-slate-400 mb-2">Варианты</div>
                      <div className="space-y-2">
                        {selected.result.variants.map((v) => (
                          <div key={v.variantType} className="text-sm">
                            <div className="flex items-center justify-between">
                              <div className="text-slate-200">
                                {v.variantType} {v.isFound ? "" : "— нет пути"}
                              </div>
                              {v.isFound && (
                                <div className="text-xs text-slate-400">
                                  длина {v.lengthUnits.toFixed(1)} • повороты {v.turns} • cost {v.weightedCost.toFixed(2)}
                                </div>
                              )}
                            </div>
                            {!v.isFound && v.message && <div className="text-xs text-rose-200 mt-1">{v.message}</div>}
                          </div>
                        ))}
                      </div>
                    </div>
                  </div>
                )}
              </div>
            )}
          </div>

          <div className="lg:col-span-7 space-y-3">
            {!creating ? (
              <div className="rounded-xl border border-slate-800 bg-slate-950/50 p-6 text-sm text-slate-400">
                {canEdit ? "Нажмите «Новая оптимизация», чтобы построить маршрут." : "Наблюдатель может только просматривать сохранённые решения."}
              </div>
            ) : (
              <div className="space-y-3">
                <div className="rounded-xl border border-slate-800 bg-slate-950/50 p-4">
                  <div className="font-medium">Параметры</div>
                  <div className="mt-3 grid grid-cols-1 md:grid-cols-2 gap-3">
                    <label className="text-sm">
                      <div className="text-xs text-slate-500 mb-1">Шаблон</div>
                      <select className="input" value={templateId} onChange={(e) => setTemplateId(e.target.value)}>
                        {templates.map((t) => (
                          <option key={t.id} value={t.id}>
                            {formatCategory(t.category)} • {t.title}
                          </option>
                        ))}
                      </select>
                    </label>
                    <label className="text-sm">
                      <div className="text-xs text-slate-500 mb-1">Коммуникация</div>
                      <select className="input" value={communicationType} onChange={(e) => setCommunicationType(e.target.value as any)}>
                        <option value="Water">Вода</option>
                        <option value="Electricity">Электричество</option>
                      </select>
                    </label>
                  </div>
                </div>

                {activeTemplate && <PlanCanvas template={activeTemplate} start={start} end={end} setStart={setStart} setEnd={setEnd} preview={preview} />}

                <div className="rounded-xl border border-slate-800 bg-slate-950/50 p-4">
                  <div className="flex items-center justify-between">
                    <div className="font-medium">2 оптимальных варианта</div>
                    <div className="text-xs text-slate-500">{previewLoading ? "Расчёт…" : preview ? "" : "Выберите 2 точки"}</div>
                  </div>

                  {!preview ? (
                    <div className="mt-3 text-sm text-slate-400">Укажите старт и финиш на плане.</div>
                  ) : (
                    <div className="mt-3 grid grid-cols-1 md:grid-cols-2 gap-3">
                      {variants.map((v) => (
                        <div key={v.variantType} className="rounded-lg border border-slate-800 bg-slate-950/40 p-4">
                          <div className="flex items-start justify-between gap-3">
                          <div>
                              <div className="font-medium text-slate-100">{v.variantType === "WallFriendly" ? "Вдоль стен" : "Прямой (минимальная длина)"}</div>
                              <div className="mt-1 text-xs text-slate-400">{v.variantType}</div>
                            </div>
                            <input
                              type="radio"
                              name="variant"
                              checked={selectedVariant === v.variantType}
                              onChange={() => setSelectedVariant(v.variantType)}
                              disabled={!v.isFound}
                            />
                          </div>

                          {!v.isFound ? (
                            <div className="mt-3 text-sm text-rose-200">{v.message ?? "Путь не найден."}</div>
                          ) : (
                            <>
                              <div className="mt-3 text-sm text-slate-300">
                                Длина: <span className="text-slate-100">{v.lengthUnits.toFixed(1)}</span> • Повороты:{" "}
                                <span className="text-slate-100">{v.turns}</span>
                              </div>
                              <div className="mt-3">
                                <div className="text-xs text-slate-400">Плюсы</div>
                                <ul className="mt-1 list-disc pl-5 text-xs text-slate-300 space-y-1">
                                  {v.pros.map((p, i) => (
                                    <li key={i}>{p}</li>
                                  ))}
                                </ul>
                              </div>
                              <div className="mt-3">
                                <div className="text-xs text-slate-400">Минусы</div>
                                <ul className="mt-1 list-disc pl-5 text-xs text-slate-300 space-y-1">
                                  {v.cons.map((c, i) => (
                                    <li key={i}>{c}</li>
                                  ))}
                                </ul>
                              </div>
                            </>
                          )}
                        </div>
                      ))}
                    </div>
                  )}

                  <div className="mt-4 flex flex-col sm:flex-row gap-3 sm:items-center sm:justify-between">
                    <div className="text-xs text-slate-500">
                      {start && end ? (
                        <>
                          Точки: ({start.x},{start.y}) → ({end.x},{end.y})
                        </>
                      ) : (
                        "Точки не выбраны."
                      )}
                    </div>
                    <div className="flex gap-2">
                      <button className="btn-ghost" onClick={() => setCreating(false)} disabled={saving}>
                        Отмена
                      </button>
                      <button className="btn-primary" onClick={onCreate} disabled={!canSave}>
                        {saving ? "Сохранение…" : "Сохранить выбранный"}
                      </button>
                    </div>
                  </div>
                  {preview && selectedVariantObj && !selectedVariantObj.isFound && <div className="mt-2 text-xs text-rose-200">Этот вариант сохранить нельзя.</div>}
                </div>
              </div>
            )}
          </div>
        </div>
      )}
    </div>
  );
}
