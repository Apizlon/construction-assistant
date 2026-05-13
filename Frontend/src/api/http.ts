import axios from "axios";

function normalizeBaseUrl(raw: string | undefined, fallback: string) {
  const value = (raw ?? "").trim();
  if (!value) return fallback;
  if (/^https?:\/\//i.test(value)) return value;
  return `http://${value}`;
}

export const authHttp = axios.create({
  baseURL: normalizeBaseUrl(import.meta.env.VITE_AUTH_URL, "http://localhost:5001")
});

export const userHttp = axios.create({
  baseURL: normalizeBaseUrl(import.meta.env.VITE_USER_URL, "http://localhost:5002")
});

export const projectHttp = axios.create({
  baseURL: normalizeBaseUrl(import.meta.env.VITE_PROJECT_URL, "http://localhost:5003")
});
