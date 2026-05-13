import { projectHttp } from "./http";

export type UpsertStepAnswerRequest = {
  stepCode: string;
  answerType: "Option" | "Slider" | "Number" | "Text" | "MultiSelect" | "Composite";
  selectedOptionCode?: string | null;
  valueJson?: string | null;
  source?: "User" | "Balanced" | "Recommendation";
  updatedByUserId?: string | null;
};

export async function upsertAnswer(projectId: string, request: UpsertStepAnswerRequest) {
  const { data } = await projectHttp.put(`/projects/${projectId}/answers`, request);
  return data;
}

