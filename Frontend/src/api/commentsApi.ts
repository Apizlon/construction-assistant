import { projectHttp } from "./http";

export type ViewerComment = {
  id: string;
  projectId: string;
  userId: string;
  email: string;
  commentText: string;
  commentDate: string;
};

export async function getComments(projectId: string) {
  const { data } = await projectHttp.get<ViewerComment[]>(`/projects/${projectId}/comments`);
  return data;
}

export async function addComment(projectId: string, payload: { userId: string; email: string; commentText: string }) {
  const { data } = await projectHttp.post<ViewerComment>(`/projects/${projectId}/comments`, payload);
  return data;
}

