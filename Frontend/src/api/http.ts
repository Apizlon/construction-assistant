import axios from "axios";

export const authHttp = axios.create({
  baseURL: import.meta.env.VITE_AUTH_URL ?? "http://localhost:5001"
});

export const userHttp = axios.create({
  baseURL: import.meta.env.VITE_USER_URL ?? "http://localhost:5002"
});

export const projectHttp = axios.create({
  baseURL: import.meta.env.VITE_PROJECT_URL ?? "http://localhost:5003"
});

