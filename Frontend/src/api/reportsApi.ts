import { projectHttp } from "./http";

export type EstimateBreakdownItem = {
  code: string;
  title: string;
  percentDelta: number;
  deltaRub: number;
};

export type ProjectEstimateBreakdown = {
  projectId: string;
  generatedAt: string;
  buildingType: string | null;
  areaM2: number | null;
  basePerM2Rub: number;
  baseCostRub: number;
  factor: number;
  items: EstimateBreakdownItem[];
  totalRub: number;
  months: number;
};

export type GanttTask = {
  id: string;
  title: string;
  startDay: number;
  endDay: number;
  dependsOnTaskIds: string[];
};

export type ProjectGantt = {
  projectId: string;
  generatedAt: string;
  totalMonths: number;
  totalDays: number;
  tasks: GanttTask[];
};

export async function getEstimateReport(projectId: string) {
  const { data } = await projectHttp.get<ProjectEstimateBreakdown>(`/projects/${projectId}/report/estimate`);
  return data;
}

export async function getGanttReport(projectId: string) {
  const { data } = await projectHttp.get<ProjectGantt>(`/projects/${projectId}/report/gantt`);
  return data;
}

export async function downloadEstimateReportXlsx(projectId: string) {
  const { data, headers } = await projectHttp.get<ArrayBuffer>(`/projects/${projectId}/report/estimate/download?format=xlsx`, { responseType: "arraybuffer" });
  return { bytes: data, contentType: headers["content-type"] as string | undefined };
}

export async function downloadGanttReportHtml(projectId: string) {
  const { data, headers } = await projectHttp.get<ArrayBuffer>(`/projects/${projectId}/report/gantt/download?format=html`, { responseType: "arraybuffer" });
  return { bytes: data, contentType: headers["content-type"] as string | undefined };
}

