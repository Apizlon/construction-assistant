import { projectHttp } from "./http";

export type ProjectListItem = {
  id: string;
  ownerUserId: string;
  name: string;
  status: string;
  createdAt: string;
  updatedAt: string;
};

export async function getOwnedProjects(ownerUserId: string) {
  const { data } = await projectHttp.get<ProjectListItem[]>(`/projects/owned/${ownerUserId}`);
  return data;
}

export async function getViewerProjects(userId: string) {
  const { data } = await projectHttp.get<ProjectListItem[]>(`/projects/viewer/${userId}`);
  return data;
}

export async function createProject(ownerUserId: string, name: string) {
  const { data } = await projectHttp.post<{ id: string; ownerUserId: string; name: string; status: string; createdAt: string; updatedAt: string }>(
    "/projects",
    { ownerUserId, name }
  );
  return data;
}

export async function createShareCode(projectId: string) {
  const { data } = await projectHttp.post<{ shareId: string; projectId: string; code: string; isActive: boolean; sharedAt: string }>(
    `/projects/${projectId}/share-code`
  );
  return data;
}

export async function joinByCode(userId: string, code: string) {
  const { data } = await projectHttp.post<{ shareId: string; projectId: string; viewerId: string; isActive: boolean }>(
    "/projects/join-by-code",
    { userId, code }
  );
  return data;
}

