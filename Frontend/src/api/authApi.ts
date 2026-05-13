import { authHttp } from "./http";

export type TokenRequest = { email: string; password: string };
export type TokenResponse = {
  accessToken: string;
  tokenType: string;
  refreshToken: string;
  user: { id: string; email: string; role: "Creator" | "Viewer" | "Admin" };
};

export async function login(request: TokenRequest) {
  const { data } = await authHttp.post<TokenResponse>("/auth/token", request);
  return data;
}

