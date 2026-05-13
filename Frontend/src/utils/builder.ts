import { StepAnswer } from "../api/answersApi";
import { BuildingType } from "./passport";

export type SliderValue = number;

export type BuilderEstimate = {
  totalRub: number;
  months: number;
};

export type Priorities = {
  budget: SliderValue; // 0..100 (higher = more budget-sensitive)
  time: SliderValue; // 0..100 (higher = more time-sensitive)
};

export const builderStepCodes = {
  finish: "builder.finish",
  floor_base: "builder.floor_base",
  floor_covering_main: "builder.floor_covering_main",
  windows: "builder.windows",
  electricity: "builder.electricity",
  furniture: "builder.furniture"
} as const;

export const builderMultiStepCodes = {
  heating: "builder.heating",
  tiles_areas: "builder.tiles_areas"
} as const;

export function getAnswer(answers: StepAnswer[], stepCode: string) {
  return answers.find((a) => a.stepCode === stepCode) ?? null;
}

export function parseNumberValueJson(valueJson: string | null | undefined): number | null {
  if (!valueJson) return null;
  try {
    const parsed = JSON.parse(valueJson) as { value?: unknown };
    return typeof parsed.value === "number" && Number.isFinite(parsed.value) ? parsed.value : null;
  } catch {
    return null;
  }
}

export function parseMultiValues(valueJson: string | null | undefined): string[] {
  if (!valueJson) return [];
  try {
    const parsed = JSON.parse(valueJson) as { values?: unknown };
    if (!Array.isArray(parsed.values)) return [];
    return parsed.values.filter((v): v is string => typeof v === "string");
  } catch {
    return [];
  }
}

export function getPriorities(answers: StepAnswer[]): Priorities {
  const budget = 50;
  const time = 50;
  return {
    budget: clamp0to100(budget),
    time: clamp0to100(time)
  };
}

export function clamp0to100(n: number) {
  return Math.max(0, Math.min(100, Math.round(n)));
}

export type OptionMeta = {
  code: string;
  icon?: string;
  title: string;
  subtitle?: string;
  cost: number; // 0..10 (higher=more expensive)
  time: number; // 0..10 (higher=longer)
  isBalancedCandidate?: boolean;
};

export function pickBalancedOption(options: OptionMeta[], priorities: Priorities): OptionMeta | null {
  const candidates = options.filter((o) => o.isBalancedCandidate);
  if (candidates.length > 0) return candidates[0];
  if (options.length === 0) return null;
  const wBudget = priorities.budget / 100;
  const wTime = priorities.time / 100;

  let best = options[0];
  let bestScore = score(best);
  for (const o of options.slice(1)) {
    const s = score(o);
    if (s < bestScore) {
      best = o;
      bestScore = s;
    }
  }
  return best;

  function score(o: OptionMeta) {
    return wBudget * o.cost + wTime * o.time;
  }
}

export function estimateProject(buildingType: BuildingType | null, areaM2: number | null, selected: Record<string, string | null>, selectedMulti: Record<string, string[]>): BuilderEstimate {
  const area = areaM2 && areaM2 > 0 ? areaM2 : 0;

  const basePerM2 = buildingType === "apartment" ? 45000 : buildingType === "warehouse" ? 35000 : buildingType === "office" ? 55000 : 65000;

  let factor = 1;
  if (selected["builder.windows"] === "panoramic") factor += 0.08;
  if (selected["builder.windows"] === "warm") factor += 0.05;
  if (selected["builder.electricity"] === "smart") factor += 0.1;
  if (selected["builder.electricity"] === "pass_through_switches") factor += 0.04;
  if (selected["builder.finish"] === "premium") factor += 0.2;
  if (selected["builder.finish"] === "rough") factor -= 0.08;
  if (selected["builder.furniture"] === "partial") factor += 0.1;
  if (selected["builder.furniture"] === "turnkey") factor += 0.25;
  if (buildingType === "apartment" && selected["builder.floor_covering_main"] === "parquet") factor += 0.08;
  if (buildingType === "apartment" && selected["builder.floor_covering_main"] === "lvt") factor += 0.06;
  if (buildingType === "apartment" && selected["builder.floor_base"] === "wet_screed") factor += 0.03;

  const heatingValues = selectedMulti["builder.heating"] ?? [];
  if (heatingValues.includes("floor")) factor += 0.08;
  if (heatingValues.includes("heat_pump")) factor += 0.12;
  if (heatingValues.includes("gas")) factor += 0.04;
  if (heatingValues.includes("electric")) factor += 0.03;

  const tilesAreas = selectedMulti["builder.tiles_areas"] ?? [];
  if (buildingType === "apartment") factor += Math.min(0.08, tilesAreas.length * 0.03);

  const total = Math.round(area * basePerM2 * factor);

  const baseMonths = Math.max(2, Math.round(area / 35));
  let months = baseMonths;
  if (selected["builder.finish"] === "premium") months += 2;
  if (selected["builder.electricity"] === "smart") months += 1;
  if (selected["builder.furniture"] === "turnkey") months += 1;
  if (heatingValues.includes("floor")) months += 1;
  if (buildingType === "warehouse") months = Math.max(1, Math.round(area / 200) + 1);
  if (buildingType === "apartment") months = Math.max(2, Math.round(area / 30) + 2);

  return { totalRub: total, months };
}
