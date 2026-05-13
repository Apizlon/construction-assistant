import { StepAnswer } from "../api/answersApi";

export type BuildingType = "house" | "apartment" | "office" | "warehouse";

export type PassportData = {
  buildingType: BuildingType | null;
  areaM2: number | null;
  floors: number | null;
  region: string | null;
};

function tryParseValueJson<T>(valueJson: string | null | undefined): T | null {
  if (!valueJson) return null;
  try {
    return JSON.parse(valueJson) as T;
  } catch {
    return null;
  }
}

export function getPassportFromAnswers(answers: StepAnswer[]): PassportData {
  const byCode = new Map(answers.map((a) => [a.stepCode, a]));

  const buildingTypeRaw = byCode.get("passport.building_type")?.selectedOptionCode ?? null;
  const buildingType: BuildingType | null =
    buildingTypeRaw === "house" || buildingTypeRaw === "office" || buildingTypeRaw === "warehouse" || buildingTypeRaw === "apartment" ? buildingTypeRaw : null;

  const area = tryParseValueJson<{ value?: number }>(byCode.get("passport.area_m2")?.valueJson)?.value;
  const floors = tryParseValueJson<{ value?: number }>(byCode.get("passport.floors")?.valueJson)?.value;
  const region = tryParseValueJson<{ value?: string }>(byCode.get("passport.region")?.valueJson)?.value;

  return {
    buildingType,
    areaM2: typeof area === "number" && Number.isFinite(area) ? area : null,
    floors: typeof floors === "number" && Number.isFinite(floors) ? floors : null,
    region: typeof region === "string" && region.trim() ? region.trim() : null
  };
}

export function isPassportComplete(passport: PassportData) {
  return Boolean(passport.buildingType && passport.areaM2 && passport.floors && passport.region);
}
