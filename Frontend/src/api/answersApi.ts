import { projectHttp } from "./http";

export type UpsertStepAnswerRequest = {
  stepCode: string;
  answerType: "Option" | "Slider" | "Number" | "Text" | "MultiSelect" | "Composite";
  selectedOptionCode?: string | null;
  valueJson?: string | null;
  source?: "User" | "Balanced" | "Recommendation";
  updatedByUserId?: string | null;
};

export type StepAnswer = {
  id: string;
  projectId: string;
  stepCode: string;
  answerType: UpsertStepAnswerRequest["answerType"];
  selectedOptionCode?: string | null;
  valueJson?: string | null;
  source?: UpsertStepAnswerRequest["source"];
  updatedByUserId?: string | null;
  updatedAt: string;
};

export async function getAnswers(projectId: string) {
  const { data } = await projectHttp.get<StepAnswer[]>(`/projects/${projectId}/answers`);
  return data;
}

export async function upsertAnswer(projectId: string, request: UpsertStepAnswerRequest) {
  const { data } = await projectHttp.put(`/projects/${projectId}/answers`, request);
  return data;
}
