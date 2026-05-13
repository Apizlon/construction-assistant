import React, { useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { upsertAnswer } from "../api/answersApi";
import { useAuth } from "../state/auth";
import { getRuErrorMessage } from "../utils/errors";

type BuildingType = "house" | "office" | "warehouse";

const buildingTypeCards: Array<{ code: BuildingType; title: string; subtitle: string }> = [
  { code: "house", title: "Жилой дом", subtitle: "Для постоянного проживания" },
  { code: "office", title: "Офис", subtitle: "Коммерческая эксплуатация" },
  { code: "warehouse", title: "Склад", subtitle: "Простота и скорость" }
];

export function PassportPage() {
  const { projectId } = useParams();
  const { user } = useAuth();
  const navigate = useNavigate();

  const [step, setStep] = useState(1);
  const [buildingType, setBuildingType] = useState<BuildingType | null>(null);
  const [areaM2, setAreaM2] = useState<string>("");
  const [floors, setFloors] = useState<string>("");
  const [region, setRegion] = useState<string>("");
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  const title = useMemo(() => {
    if (step === 1) return "Паспорт объекта: тип здания";
    if (step === 2) return "Паспорт объекта: площадь";
    if (step === 3) return "Паспорт объекта: этажность";
    return "Паспорт объекта: регион";
  }, [step]);

  async function saveCurrentStep() {
    if (!projectId) return;
    setError(null);
    setSaving(true);
    try {
      if (step === 1) {
        if (!buildingType) throw new Error("Выберите тип здания");
        await upsertAnswer(projectId, {
          stepCode: "passport.building_type",
          answerType: "Option",
          selectedOptionCode: buildingType,
          source: "User",
          updatedByUserId: user?.id ?? null
        });
      } else if (step === 2) {
        const n = Number(areaM2);
        if (!Number.isFinite(n) || n <= 0) throw new Error("Введите корректную площадь");
        await upsertAnswer(projectId, {
          stepCode: "passport.area_m2",
          answerType: "Number",
          valueJson: JSON.stringify({ value: n }),
          source: "User",
          updatedByUserId: user?.id ?? null
        });
      } else if (step === 3) {
        const n = Number(floors);
        if (!Number.isFinite(n) || n <= 0) throw new Error("Введите корректную этажность");
        await upsertAnswer(projectId, {
          stepCode: "passport.floors",
          answerType: "Number",
          valueJson: JSON.stringify({ value: n }),
          source: "User",
          updatedByUserId: user?.id ?? null
        });
      } else if (step === 4) {
        if (!region.trim()) throw new Error("Введите регион");
        await upsertAnswer(projectId, {
          stepCode: "passport.region",
          answerType: "Text",
          valueJson: JSON.stringify({ value: region.trim() }),
          source: "User",
          updatedByUserId: user?.id ?? null
        });
      }
    } catch (e: any) {
      setError(getRuErrorMessage(e, e?.message ?? "Не удалось сохранить"));
      throw e;
    } finally {
      setSaving(false);
    }
  }

  async function onNext() {
    try {
      await saveCurrentStep();
      if (step < 4) setStep(step + 1);
      else navigate(`/app/projects/${projectId}`);
    } catch {
      // keep on step
    }
  }

  function onBack() {
    if (step === 1) {
      navigate("/app/projects");
      return;
    }
    setStep(step - 1);
  }

  return (
    <div className="space-y-5">
      <div className="card">
        <div className="flex items-center justify-between gap-3">
          <div>
            <div className="text-xs text-slate-400">Шаг {step} из 4</div>
            <div className="text-lg font-semibold">{title}</div>
          </div>
          <div className="flex gap-2">
            <button className="btn-ghost" onClick={onBack} disabled={saving}>
              Назад
            </button>
            <button className="btn-primary" onClick={onNext} disabled={saving}>
              {step < 4 ? "Далее" : "Перейти к конструктору"}
            </button>
          </div>
        </div>
        {error && <div className="mt-3 text-sm text-rose-300">{error}</div>}
      </div>

      {step === 1 && (
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          {buildingTypeCards.map((c) => (
            <button
              key={c.code}
              className={buildingType === c.code ? "card border-indigo-500/60" : "card hover:border-slate-700"}
              onClick={() => setBuildingType(c.code)}
            >
              <div className="text-base font-semibold">{c.title}</div>
              <div className="mt-1 text-sm text-slate-400">{c.subtitle}</div>
            </button>
          ))}
        </div>
      )}

      {step === 2 && (
        <div className="card max-w-xl">
          <div className="label mb-2">Площадь, м²</div>
          <input className="input" value={areaM2} onChange={(e) => setAreaM2(e.target.value)} placeholder="Например: 180" />
          <div className="mt-2 text-xs text-slate-500">Можно ввести приблизительно — потом уточним.</div>
        </div>
      )}

      {step === 3 && (
        <div className="card max-w-xl">
          <div className="label mb-2">Этажность</div>
          <input className="input" value={floors} onChange={(e) => setFloors(e.target.value)} placeholder="Например: 2" />
        </div>
      )}

      {step === 4 && (
        <div className="card max-w-xl">
          <div className="label mb-2">Регион</div>
          <input className="input" value={region} onChange={(e) => setRegion(e.target.value)} placeholder="Например: Московская область" />
        </div>
      )}
    </div>
  );
}

