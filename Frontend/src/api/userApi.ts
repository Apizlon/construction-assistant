import { userHttp } from "./http";

export type RegisterRequest = { email: string; password: string; role: "Creator" | "Viewer" };
export type UserResponse = { id: string; email: string; role: "Creator" | "Viewer" | "Admin" };

export async function register(request: RegisterRequest) {
  const { data } = await userHttp.post<UserResponse>("/user/register", request);
  return data;
}

export type CreateFeedbackRequest = { message: string };
export type FeedbackResponse = { id: string; userId: string; email: string; message: string; createdAt: string };

export async function createFeedback(accessToken: string, request: CreateFeedbackRequest) {
  const { data } = await userHttp.post<FeedbackResponse>("/feedback", request, {
    headers: { Authorization: `Bearer ${accessToken}` }
  });
  return data;
}

export async function getAllFeedback(accessToken: string) {
  const { data } = await userHttp.get<FeedbackResponse[]>("/feedback", {
    headers: { Authorization: `Bearer ${accessToken}` }
  });
  return data;
}

