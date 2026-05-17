import { projectHttp } from "./http";

export type GridPoint = { x: number; y: number };
export type Rect = { x: number; y: number; w: number; h: number };

export type OptimizationTemplate = {
  id: string;
  title: string;
  category: "Apartment" | "House" | "Office" | "Warehouse" | string;
  width: number;
  height: number;
  walls: Rect[];
  forbiddenZones: Rect[];
};

export type OptimizationVariant = {
  variantType: "WallFriendly" | "Direct" | string;
  isFound: boolean;
  message?: string | null;
  lengthUnits: number;
  turns: number;
  weightedCost: number;
  path: GridPoint[];
  pros: string[];
  cons: string[];
};

export type OptimizationPreview = {
  projectId: string;
  templateId: string;
  communicationType: string;
  start: GridPoint;
  end: GridPoint;
  variants: OptimizationVariant[];
};

export type OptimizationListItem = {
  id: string;
  projectId: string;
  createdByUserId: string;
  createdAt: string;
  templateId: string;
  communicationType: string;
  start: GridPoint;
  end: GridPoint;
  selectedVariant?: string | null;
  selectedLengthUnits?: number | null;
};

export type OptimizationDetails = OptimizationListItem & { result: OptimizationPreview };

export async function getOptimizationTemplates(projectId: string, actorUserId: string) {
  const { data } = await projectHttp.get<OptimizationTemplate[]>(`/projects/${projectId}/optimizations/templates`, { params: { actorUserId } });
  return data;
}

export async function getOptimizations(projectId: string, actorUserId: string) {
  const { data } = await projectHttp.get<OptimizationListItem[]>(`/projects/${projectId}/optimizations`, { params: { actorUserId } });
  return data;
}

export async function getOptimization(projectId: string, optimizationId: string, actorUserId: string) {
  const { data } = await projectHttp.get<OptimizationDetails>(`/projects/${projectId}/optimizations/${optimizationId}`, { params: { actorUserId } });
  return data;
}

export async function previewOptimization(projectId: string, payload: { actorUserId: string; templateId: string; communicationType: string; start: GridPoint; end: GridPoint }) {
  const { data } = await projectHttp.post<OptimizationPreview>(`/projects/${projectId}/optimizations/preview`, payload);
  return data;
}

export async function createOptimization(
  projectId: string,
  payload: { actorUserId: string; templateId: string; communicationType: string; start: GridPoint; end: GridPoint; selectedVariant: string }
) {
  const { data } = await projectHttp.post<OptimizationDetails>(`/projects/${projectId}/optimizations`, payload);
  return data;
}

export async function deleteOptimization(projectId: string, optimizationId: string, actorUserId: string) {
  await projectHttp.delete(`/projects/${projectId}/optimizations/${optimizationId}`, { params: { actorUserId } });
}

