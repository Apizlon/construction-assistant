import React, { useEffect, useMemo, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { getAnswers, upsertAnswer, StepAnswer } from "../api/answersApi";
import { getProject } from "../api/projectApi";
import { useAuth } from "../state/auth";
import { getRuErrorMessage } from "../utils/errors";
import { BuildingType, getPassportFromAnswers, isPassportComplete } from "../utils/passport";
import { estimateProject, getAnswer, OptionMeta, parseMultiValues } from "../utils/builder";

type BuilderStep =
  | "state_now"
  | "goal"
  | "finish"
  | "foundation"
  | "floor_base"
  | "floor_covering_main"
  | "tiles_areas"
  | "walls"
  | "roof"
  | "windows"
  | "heating"
  | "electricity"
  | "furniture"
  | "summary";

type StepKind = "option" | "multi" | "info";

type StepConfig = {
  step: BuilderStep;
  title: string;
  subtitle?: string;
  kind: StepKind;
  stepCode?: string; // for option/multi/slider
  min?: number; // slider
  max?: number; // slider
  marks?: Array<{ value: number; label: string }>; // slider
  options?: OptionMeta[]; // option/multi
};

const stepCodes = {
  state_now: "builder.state_now",
  goal: "builder.goal",
  finish: "builder.finish",
  foundation: "builder.foundation",
  floor_base: "builder.floor_base",
  floor_covering_main: "builder.floor_covering_main",
  tiles_areas: "builder.tiles_areas",
  walls: "builder.walls",
  roof: "builder.roof",
  windows: "builder.windows",
  heating: "builder.heating",
  electricity: "builder.electricity",
  furniture: "builder.furniture"
} as const;

function optionCardClass(selected: boolean) {
  return selected ? "card border-indigo-500/60" : "card hover:border-slate-700";
}

function formatRub(n: number) {
  return n.toLocaleString("ru-RU");
}

function sizeLabel(score0to10: number) {
  if (score0to10 <= 3) return "малый";
  if (score0to10 <= 6) return "средний";
  return "большой";
}

export function ProjectBuilderPage() {
  const { projectId } = useParams();
  const { user } = useAuth();
  const navigate = useNavigate();
  const canEdit = user?.role === "Creator" || user?.role === "Admin";

  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [projectName, setProjectName] = useState<string>(projectId ?? "");
  const [answers, setAnswers] = useState<StepAnswer[]>([]);
  const [step, setStep] = useState<BuilderStep>("state_now");

  const passport = useMemo(() => getPassportFromAnswers(answers), [answers]);
  const passportComplete = useMemo(() => isPassportComplete(passport), [passport]);

  useEffect(() => {
    if (!projectId) return;
    let cancelled = false;

    (async () => {
      setLoading(true);
      setError(null);
      try {
        const [p, a] = await Promise.all([getProject(projectId), getAnswers(projectId)]);
        if (cancelled) return;
        setProjectName(p.name);
        setAnswers(a);
      } catch (e: any) {
        if (!cancelled) setError(getRuErrorMessage(e, "Не удалось загрузить проект"));
      } finally {
        if (!cancelled) setLoading(false);
      }
    })();

    return () => {
      cancelled = true;
    };
  }, [projectId]);

  const steps = useMemo<StepConfig[]>(() => {
    const t = passport.buildingType;

    const apartmentSteps: StepConfig[] = [
      {
        step: "state_now",
        title: "Состояние квартиры сейчас",
        subtitle: "Чтобы не считать лишние демонтажи/работы",
        kind: "option",
        stepCode: stepCodes.state_now,
        options: [
          { code: "bare", title: "Без отделки", subtitle: "Бетон/минимум от застройщика", cost: 7, time: 7 },
          { code: "rough", title: "Черновая", subtitle: "Стены/пол под чистовую", cost: 6, time: 6 },
          { code: "whitebox", title: "White box", subtitle: "Почти готово, нужна чистовая", cost: 4, time: 4 },
          { code: "finished", title: "Чистовая", subtitle: "Нужен косметический/частичный ремонт", cost: 2, time: 3 }
        ]
      },
      {
        step: "goal",
        title: "Цель ремонта",
        subtitle: "Что хотим получить в итоге",
        kind: "option",
        stepCode: stepCodes.goal,
        options: [
          { code: "cosmetic", title: "Косметический", subtitle: "Обновить без глобальной переделки", cost: 3, time: 3 },
          { code: "major", title: "Капитальный", subtitle: "Инженерия + выравнивание + отделка", cost: 6, time: 7 },
          { code: "turnkey", title: "Под ключ", subtitle: "Включая мебель/оснащение", cost: 8, time: 8 }
        ]
      },
      {
        step: "floor_base",
        title: "Пол: основание",
        subtitle: "Как выравниваем пол",
        kind: "option",
        stepCode: stepCodes.floor_base,
        options: [
          { code: "dry_screed", title: "Сухая стяжка", subtitle: "Быстро и чисто", cost: 6, time: 4 },
          { code: "wet_screed", title: "Мокрая стяжка", subtitle: "Нужно время на высыхание", cost: 5, time: 7 },
          { code: "self_leveling", title: "Наливной пол", subtitle: "Для точного выравнивания", cost: 7, time: 5 }
        ]
      },
      {
        step: "floor_covering_main",
        title: "Пол: покрытие (комнаты)",
        subtitle: "Кухня/санузел вынесем отдельно плиткой",
        kind: "option",
        stepCode: stepCodes.floor_covering_main,
        options: [
          { code: "linoleum", title: "Линолеум", subtitle: "Практично и недорого", cost: 3, time: 3 },
          { code: "laminate", title: "Ламинат", subtitle: "Баланс цены и вида", cost: 5, time: 4 },
          { code: "lvt", title: "Кварцвинил", subtitle: "Износостойко и влагостойко", cost: 6, time: 4 },
          { code: "parquet", title: "Паркет", subtitle: "Натурально, дороже", cost: 8, time: 6 }
        ]
      },
      {
        step: "tiles_areas",
        title: "Плитка: где делаем",
        subtitle: "Можно выбрать несколько зон",
        kind: "multi",
        stepCode: stepCodes.tiles_areas,
        options: [
          { code: "kitchen", title: "Кухня", subtitle: "Фартук/пол", cost: 4, time: 4 },
          { code: "hallway", title: "Прихожая", subtitle: "Износостойко", cost: 3, time: 3 },
          { code: "bathroom", title: "Санузел", subtitle: "Влага/стены/пол", cost: 6, time: 6 }
        ]
      },
      {
        step: "walls",
        title: "Стены/перегородки",
        subtitle: "Планировка и перегородки",
        kind: "option",
        stepCode: stepCodes.walls,
        options: [
          { code: "keep", title: "Без изменений", subtitle: "Не трогаем несущие", cost: 2, time: 2 },
          { code: "partitions", title: "Перегородки", subtitle: "ГКЛ/газобетон", cost: 5, time: 5 }
        ]
      },
      {
        step: "windows",
        title: "Окна",
        subtitle: "Оставляем или меняем",
        kind: "option",
        stepCode: stepCodes.windows,
        options: [
          { code: "keep", title: "Оставить как есть", subtitle: "Если окна в порядке", cost: 1, time: 1 },
          { code: "replace", title: "Заменить", subtitle: "Шумо/теплоизоляция", cost: 5, time: 4 }
        ]
      },
      {
        step: "heating",
        title: "Отопление",
        subtitle: "Можно выбрать несколько",
        kind: "multi",
        stepCode: stepCodes.heating,
        options: [
          { code: "radiators", title: "Радиаторы", subtitle: "Сохранить/заменить", cost: 4, time: 3 },
          { code: "floor", title: "Тёплый пол", subtitle: "Комфорт (дороже)", cost: 6, time: 5 }
        ]
      },
      {
        step: "electricity",
        title: "Электрика",
        subtitle: "Удобство управления светом и сценарии",
        kind: "option",
        stepCode: stepCodes.electricity,
        options: [
          { code: "basic", title: "Базовая", subtitle: "Минимум точек и линий", cost: 3, time: 3 },
          { code: "standard", title: "Стандарт", subtitle: "Удобнее розетки/линии", cost: 5, time: 4 },
          { code: "pass_through_switches", title: "Проходные выключатели", subtitle: "Переключение света из нескольких мест", cost: 6, time: 5 },
          { code: "smart", title: "Умный дом", subtitle: "Сценарии/датчики", cost: 8, time: 6 }
        ]
      },
      {
        step: "furniture",
        title: "Мебель",
        subtitle: "Нужно ли комплектовать",
        kind: "option",
        stepCode: stepCodes.furniture,
        options: [
          { code: "none", title: "Без мебели", subtitle: "Только отделка и инженерия", cost: 1, time: 1 },
          { code: "partial", title: "Частично", subtitle: "Кухня/санузел/основное", cost: 5, time: 4 },
          { code: "turnkey", title: "Под ключ", subtitle: "Полный комплект", cost: 8, time: 6 }
        ]
      },
      { step: "summary", title: "Итог", kind: "info" }
    ];

    const houseSteps: StepConfig[] = [
      {
        step: "finish",
        title: "Отделка (уровень)",
        subtitle: "Чтобы оценить бюджет/сроки",
        kind: "option",
        stepCode: stepCodes.finish,
        options: [
          { code: "rough", title: "Черновая", subtitle: "Под чистовую", cost: 4, time: 4 },
          { code: "standard", title: "Чистовая", subtitle: "Можно жить/работать", cost: 6, time: 6 },
          { code: "premium", title: "Премиум", subtitle: "Дороже и дольше", cost: 9, time: 8 }
        ]
      },
      {
        step: "foundation",
        title: "Фундамент",
        subtitle: "Основание дома",
        kind: "option",
        stepCode: stepCodes.foundation,
        options: [
          { code: "strip", title: "Лента", subtitle: "Классика", cost: 5, time: 6, isBalancedCandidate: true },
          { code: "slab", title: "Плита", subtitle: "Надёжно, дороже", cost: 7, time: 6 },
          { code: "piles", title: "Сваи", subtitle: "Быстрее, зависит от грунта", cost: 6, time: 4 }
        ]
      },
      {
        step: "walls",
        title: "Стены",
        subtitle: "Материал стен",
        kind: "option",
        stepCode: stepCodes.walls,
        options: [
          { code: "brick", icon: "🧱", title: "Кирпич", subtitle: "Дорого, долговечно", cost: 9, time: 8 },
          { code: "aerated_concrete", icon: "🧊", title: "Газобетон", subtitle: "Дешевле, быстрее", cost: 6, time: 5 },
          { code: "wood", icon: "🌲", title: "Дерево", subtitle: "Быстро строится", cost: 7, time: 4 }
        ]
      },
      {
        step: "roof",
        title: "Крыша",
        subtitle: "Тип кровли",
        kind: "option",
        stepCode: stepCodes.roof,
        options: [
          { code: "gable", title: "Двускатная", subtitle: "Универсально", cost: 5, time: 5 },
          { code: "hip", title: "Вальмовая", subtitle: "Сложнее, устойчивее", cost: 7, time: 6 },
          { code: "flat", title: "Плоская", subtitle: "Современно, нюансы", cost: 6, time: 6 }
        ]
      },
      {
        step: "windows",
        title: "Окна",
        subtitle: "Остекление",
        kind: "option",
        stepCode: stepCodes.windows,
        options: [
          { code: "standard", title: "Стандартные", subtitle: "Оптимальный баланс", cost: 4, time: 4, isBalancedCandidate: true },
          { code: "panoramic", title: "Панорамные", subtitle: "Дороже, больше света", cost: 7, time: 6 },
          { code: "warm", title: "Тёплые", subtitle: "Энергоэффективность", cost: 6, time: 5 }
        ]
      },
      {
        step: "heating",
        title: "Отопление",
        subtitle: "Можно выбрать несколько",
        kind: "multi",
        stepCode: stepCodes.heating,
        options: [
          { code: "radiators", title: "Радиаторы", subtitle: "Классика", cost: 4, time: 4 },
          { code: "floor", title: "Тёплый пол", subtitle: "Комфорт", cost: 6, time: 5 },
          { code: "gas", title: "Газовый котёл", subtitle: "Экономично", cost: 6, time: 5 },
          { code: "electric", title: "Электрокотёл", subtitle: "Проще", cost: 5, time: 4 },
          { code: "heat_pump", title: "Тепловой насос", subtitle: "Дороже на старте", cost: 9, time: 6 }
        ]
      },
      {
        step: "electricity",
        title: "Электрика",
        subtitle: "Управление светом и сценарии",
        kind: "option",
        stepCode: stepCodes.electricity,
        options: [
          { code: "basic", title: "Базовая", subtitle: "Минимум линий и точек", cost: 3, time: 3 },
          { code: "standard", title: "Стандарт", subtitle: "Удобнее розетки/линии", cost: 5, time: 4 },
          { code: "pass_through_switches", title: "Проходные выключатели", subtitle: "Переключение света из нескольких мест", cost: 6, time: 5 },
          { code: "smart", title: "Умный дом", subtitle: "Сценарии/датчики", cost: 8, time: 6 }
        ]
      },
      {
        step: "furniture",
        title: "Мебель",
        subtitle: "Нужно ли комплектовать",
        kind: "option",
        stepCode: stepCodes.furniture,
        options: [
          { code: "none", title: "Без мебели", subtitle: "Только дом", cost: 1, time: 1 },
          { code: "partial", title: "Частично", subtitle: "Базовый комплект", cost: 5, time: 4 },
          { code: "turnkey", title: "Под ключ", subtitle: "Полный комплект", cost: 8, time: 6 }
        ]
      },
      { step: "summary", title: "Итог", kind: "info" }
    ];

    if (t === "apartment") return apartmentSteps;
    return houseSteps;
  }, [passport.buildingType]);

  const stepIndex = Math.max(0, steps.findIndex((s) => s.step === step));
  const current = steps[stepIndex] ?? steps[0];

  const selectedSingle = useMemo(() => {
    const map: Record<string, string | null> = {};
    for (const s of steps) {
      if (s.kind !== "option") continue;
      if (!s.stepCode) continue;
      map[s.stepCode] = getAnswer(answers, s.stepCode)?.selectedOptionCode ?? null;
    }
    return map;
  }, [answers, steps]);

  const selectedMulti = useMemo(() => {
    const map: Record<string, string[]> = {};
    for (const s of steps) {
      if (s.kind !== "multi") continue;
      if (!s.stepCode) continue;
      map[s.stepCode] = parseMultiValues(getAnswer(answers, s.stepCode)?.valueJson);
    }
    return map;
  }, [answers, steps]);

  const estimate = useMemo(() => estimateProject(passport.buildingType, passport.areaM2, selectedSingle, selectedMulti), [passport.buildingType, passport.areaM2, selectedSingle, selectedMulti]);

  const isComplete = useMemo(() => {
    if (!passportComplete) return false;
    for (const s of steps) {
      if (s.step === "summary") continue;
      if (s.kind === "info") continue;
      if (!s.stepCode) continue;
      if (s.kind === "multi") {
        if ((selectedMulti[s.stepCode] ?? []).length === 0) return false;
      } else if (s.kind === "option") {
        if (!selectedSingle[s.stepCode]) return false;
      }
    }
    return true;
  }, [passportComplete, steps, selectedMulti, selectedSingle]);

  useEffect(() => {
    if (!loading && isComplete) setStep("summary");
  }, [loading, isComplete]);

  async function refreshAnswers() {
    if (!projectId) return;
    const updated = await getAnswers(projectId);
    setAnswers(updated);
  }

  async function saveOption(stepCode: string, code: string, source: "User" | "Balanced" | "Recommendation" = "User") {
    if (!projectId) return;
    if (!canEdit) return;
    await upsertAnswer(projectId, {
      stepCode,
      answerType: "Option",
      selectedOptionCode: code,
      source,
      updatedByUserId: user?.id ?? null
    });
  }

  async function toggleMulti(stepCode: string, code: string, currentValues: string[]) {
    if (!projectId) return;
    if (!canEdit) return;
    const next = currentValues.includes(code) ? currentValues.filter((x) => x !== code) : [...currentValues, code];
    await upsertAnswer(projectId, {
      stepCode,
      answerType: "MultiSelect",
      valueJson: JSON.stringify({ values: next }),
      source: "User",
      updatedByUserId: user?.id ?? null
    });
  }

  function goPrev() {
    if (stepIndex <= 0) {
      navigate(`/app/projects/${projectId}`);
      return;
    }
    setStep(steps[stepIndex - 1].step);
  }

  function goNext() {
    if (stepIndex >= steps.length - 1) {
      navigate(`/app/projects/${projectId}`);
      return;
    }
    setStep(steps[stepIndex + 1].step);
  }

  if (!projectId) return null;

  return (
    <div className="space-y-5">
      <div className="card">
        <div className="flex items-center justify-between gap-3">
          <div>
            <div className="text-xs text-slate-400">Проект</div>
            <div className="text-lg font-semibold">{projectName}</div>
            <div className="mt-1 text-xs text-slate-500">
              Оценка сейчас: {formatRub(estimate.totalRub)} ₽ · ≈ {estimate.months} мес.
            </div>
          </div>
          <div className="flex items-center gap-2">
            <Link className="btn-ghost" to={`/app/projects/${projectId}/passport`}>
              Паспорт
            </Link>
            <Link className="btn-ghost" to={`/app/projects/${projectId}`}>
              Обзор
            </Link>
          </div>
        </div>
        {error && <div className="mt-3 text-sm text-rose-300">{error}</div>}
        {!canEdit && <div className="mt-3 text-xs text-amber-200/90">Режим просмотра: менять конструктор может только создатель.</div>}
        {!passportComplete && (
          <div className="mt-3 rounded-xl border border-amber-500/20 bg-amber-500/5 px-4 py-3 text-sm text-amber-200/90">
            Паспорт заполнен не полностью. Перейдите в «Паспорт», чтобы указать тип здания, площадь, этажность и регион.
          </div>
        )}
      </div>

      <div className="card">
        <div className="flex items-center justify-between gap-3">
          <div>
            <div className="text-xs text-slate-400">
              Шаг {stepIndex + 1} из {steps.length}
            </div>
            <div className="text-lg font-semibold">{current.title}</div>
            {current.subtitle && <div className="mt-1 text-sm text-slate-400">{current.subtitle}</div>}
          </div>
          <div className="flex gap-2">
            <button className="btn-ghost" onClick={goPrev} disabled={loading || saving}>
              Назад
            </button>
            <button className="btn-primary" onClick={goNext} disabled={loading || saving}>
              {step === "summary" ? "Готово" : "Далее"}
            </button>
          </div>
        </div>
      </div>

      {current.kind === "option" && current.stepCode && current.options && (
        <div className="space-y-4">
          {canEdit && !selectedSingle[current.stepCode] && current.options.some((o) => o.isBalancedCandidate) && (
            <div className="card border-emerald-500/20 bg-emerald-500/5">
              <div className="flex items-center justify-between gap-3">
                <div className="text-sm text-emerald-200/90">Можно выбрать сбалансированный вариант (средний по цене/срокам).</div>
                <button
                  className="btn-primary"
                  onClick={async () => {
                    const pick = current.options!.find((o) => o.isBalancedCandidate);
                    if (!pick) return;
                    setSaving(true);
                    setError(null);
                    try {
                      await saveOption(current.stepCode!, pick.code, "Balanced");
                      await refreshAnswers();
                    } catch (e: any) {
                      setError(getRuErrorMessage(e, "Не удалось применить сбалансированный вариант"));
                    } finally {
                      setSaving(false);
                    }
                  }}
                  disabled={loading || saving}
                >
                  Сбалансированный
                </button>
              </div>
            </div>
          )}

          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            {current.options.map((o) => {
              const selected = selectedSingle[current.stepCode!] === o.code;
              return (
                <button
                  key={o.code}
                  className={optionCardClass(selected)}
                  onClick={async () => {
                    if (!canEdit) return;
                    setSaving(true);
                    setError(null);
                    try {
                      await saveOption(current.stepCode!, o.code, "User");
                      await refreshAnswers();
                    } catch (e: any) {
                      setError(getRuErrorMessage(e, "Не удалось сохранить выбор"));
                    } finally {
                      setSaving(false);
                    }
                  }}
                  disabled={!canEdit || loading || saving}
                >
                  <div className="text-base font-semibold flex items-center gap-2">
                    {o.icon && <span className="text-xl">{o.icon}</span>}
                    <span>{o.title}</span>
                  </div>
                {o.subtitle && <div className="mt-1 text-sm text-slate-400">{o.subtitle}</div>}
                <div className="mt-3 text-xs text-slate-500 flex items-center justify-between">
                  <span>Бюджет: {sizeLabel(o.cost)}</span>
                  <span>Срок: {sizeLabel(o.time)}</span>
                </div>
                {getAnswer(answers, current.stepCode!)?.source === "Balanced" && selected && (
                  <div className="mt-2 text-xs text-emerald-300/80">Сбалансированный выбор</div>
                )}
              </button>
              );
            })}
          </div>
        </div>
      )}

      {current.kind === "multi" && current.stepCode && current.options && (
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          {current.options.map((o) => {
            const values = selectedMulti[current.stepCode!] ?? [];
            const selected = values.includes(o.code);
            return (
              <button
                key={o.code}
                className={optionCardClass(selected)}
                onClick={async () => {
                  if (!canEdit) return;
                  setSaving(true);
                  setError(null);
                  try {
                    await toggleMulti(current.stepCode!, o.code, values);
                    await refreshAnswers();
                  } catch (e: any) {
                    setError(getRuErrorMessage(e, "Не удалось сохранить"));
                  } finally {
                    setSaving(false);
                  }
                }}
                disabled={!canEdit || loading || saving}
              >
                <div className="text-base font-semibold flex items-center gap-2">
                  {o.icon && <span className="text-xl">{o.icon}</span>}
                  <span>{o.title}</span>
                </div>
                {o.subtitle && <div className="mt-1 text-sm text-slate-400">{o.subtitle}</div>}
                <div className="mt-3 text-xs text-slate-500 flex items-center justify-between">
                  <span>Бюджет: {sizeLabel(o.cost)}</span>
                  <span>Срок: {sizeLabel(o.time)}</span>
                </div>
                <div className="mt-2 text-xs text-slate-500">{selected ? "Выбрано" : "Нажмите, чтобы выбрать"}</div>
              </button>
            );
          })}
        </div>
      )}

      {step === "summary" && (
        <div className="card">
          <div className="font-medium">Итог (оценка)</div>
          <div className="mt-2 text-sm text-slate-300">Это приблизительно: точность зависит от региона, состояния объекта, материалов и инженерии.</div>
          <div className="mt-4 grid grid-cols-1 md:grid-cols-2 gap-3">
            <div className="rounded-xl border border-slate-800 bg-slate-950/50 px-4 py-3">
              <div className="text-xs text-slate-500">Сумма</div>
              <div className="mt-1 text-xl font-semibold">{formatRub(estimate.totalRub)} ₽</div>
              <div className="mt-1 text-xs text-slate-500">Площадь × базовая ставка × коэффициенты решений</div>
            </div>
            <div className="rounded-xl border border-slate-800 bg-slate-950/50 px-4 py-3">
              <div className="text-xs text-slate-500">Срок</div>
              <div className="mt-1 text-xl font-semibold">≈ {estimate.months} мес.</div>
              <div className="mt-1 text-xs text-slate-500">Без учёта согласований, сезонности и поставок</div>
            </div>
          </div>
          <div className="mt-4 rounded-xl border border-slate-800 bg-slate-950/50 px-4 py-3">
            <div className="text-xs text-slate-500">Последовательность работ (очень кратко)</div>
            <div className="mt-2 text-sm text-slate-300">
              Проектирование/подготовка → демонтаж (если нужен) → инженерия (электрика/отопление) → черновые работы → чистовая отделка → мебель/оснащение.
            </div>
          </div>
        </div>
      )}

      <div className="card">
        <div className="font-medium mb-2">Принятые решения</div>
        <div className="grid grid-cols-1 md:grid-cols-3 gap-3 text-sm">
          {steps
            .filter((s) => s.step !== "summary" && s.kind !== "info")
            .map((s) => {
              const title =
                s.kind === "multi" && s.stepCode
                  ? (selectedMulti[s.stepCode] ?? [])
                      .map((c) => s.options?.find((o) => o.code === c)?.title ?? c)
                      .join(", ") || null
                  : s.stepCode
                    ? (s.options?.find((o) => o.code === selectedSingle[s.stepCode])?.title ?? null)
                    : null;

              return (
                <div key={s.stepCode ?? s.title} className="rounded-xl border border-slate-800 bg-slate-950/50 px-4 py-3">
                  <div className="text-xs text-slate-500">{s.title}</div>
                  <div className="mt-1 text-slate-200">{title ?? "—"}</div>
                </div>
              );
            })}
        </div>
      </div>
    </div>
  );
}
