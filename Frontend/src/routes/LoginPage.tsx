import React, { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { login } from "../api/authApi";
import { useAuth } from "../state/auth";
import { AuthShell } from "./ui";
import { getRuErrorMessage } from "../utils/errors";
import { isValidEmail, isValidPassword } from "../utils/validation";

export function LoginPage() {
  const navigate = useNavigate();
  const { setAuth } = useAuth();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const isAdminLogin = email.trim().toLowerCase() === "admin";

  const validationError =
    !email.trim()
      ? "Введите email"
      : !isAdminLogin && !isValidEmail(email)
        ? "Введите корректный email"
        : !password
          ? "Введите пароль"
          : !isAdminLogin && !isValidPassword(password)
            ? "Пароль должен быть минимум 6 символов"
            : null;

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    if (validationError) {
      setError(validationError);
      return;
    }
    setLoading(true);
    try {
      const token = await login({ email, password });
      setAuth(token.accessToken, token.user);
      navigate("/app/projects", { replace: true });
    } catch (err: any) {
      setError(getRuErrorMessage(err, "Не удалось войти"));
    } finally {
      setLoading(false);
    }
  }

  return (
    <AuthShell title="Вход">
      <div className="card">
        <form onSubmit={onSubmit} className="space-y-4">
          <div>
            <div className="label mb-1">Email</div>
            <input className="input" value={email} onChange={(e) => setEmail(e.target.value)} placeholder="you@company.com" />
          </div>
          <div>
            <div className="label mb-1">Пароль</div>
            <input className="input" value={password} onChange={(e) => setPassword(e.target.value)} type="password" placeholder="••••••••" />
          </div>
          {error && <div className="text-sm text-rose-300">{error}</div>}
          <button className="btn-primary w-full" disabled={loading}>
            {loading ? "Входим..." : "Войти"}
          </button>
          <div className="text-sm text-slate-400">
            Нет аккаунта?{" "}
            <Link to="/register" className="text-indigo-300 hover:text-indigo-200">
              Регистрация
            </Link>
          </div>
        </form>
      </div>
    </AuthShell>
  );
}
