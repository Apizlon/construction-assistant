import React, { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { register } from "../api/userApi";
import { login } from "../api/authApi";
import { PageShell } from "./ui";
import { useAuth } from "../state/auth";
import { getRuErrorMessage } from "../utils/errors";
import { isValidEmail, isValidPassword } from "../utils/validation";

export function RegisterPage() {
  const navigate = useNavigate();
  const { setAuth } = useAuth();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [role, setRole] = useState<"Creator" | "Viewer">("Creator");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const validationError =
    !email.trim()
      ? "Введите email"
      : !isValidEmail(email)
        ? "Введите корректный email"
        : !password
          ? "Введите пароль"
          : !isValidPassword(password)
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
      await register({ email, password, role });
      const token = await login({ email, password });
      setAuth(token.accessToken, token.user);
      navigate("/app/projects", { replace: true });
    } catch (err: any) {
      setError(getRuErrorMessage(err, "Не удалось зарегистрироваться"));
    } finally {
      setLoading(false);
    }
  }

  return (
    <PageShell title="Регистрация аккаунта">
      <div className="card max-w-md">
        <form onSubmit={onSubmit} className="space-y-4">
          <div>
            <div className="label mb-1">Email</div>
            <input className="input" value={email} onChange={(e) => setEmail(e.target.value)} placeholder="you@company.com" />
          </div>
          <div>
            <div className="label mb-1">Пароль</div>
            <input className="input" value={password} onChange={(e) => setPassword(e.target.value)} type="password" placeholder="••••••••" />
          </div>
          <div>
            <div className="label mb-2">Роль</div>
            <div className="grid grid-cols-2 gap-3">
              <button
                type="button"
                className={role === "Creator" ? "btn-primary" : "btn-ghost"}
                onClick={() => setRole("Creator")}
              >
                Создатель
              </button>
              <button
                type="button"
                className={role === "Viewer" ? "btn-primary" : "btn-ghost"}
                onClick={() => setRole("Viewer")}
              >
                Наблюдатель
              </button>
            </div>
            <div className="mt-2 text-xs text-slate-400">
              Наблюдатель не может создавать проекты — только добавлять по коду и оставлять комментарии.
            </div>
          </div>
          {error && <div className="text-sm text-rose-300">{error}</div>}
          <button className="btn-primary w-full" disabled={loading}>
            {loading ? "Создаем..." : "Создать аккаунт"}
          </button>
          <div className="text-sm text-slate-400">
            Уже есть аккаунт?{" "}
            <Link to="/login" className="text-indigo-300 hover:text-indigo-200">
              Войти
            </Link>
          </div>
        </form>
      </div>
    </PageShell>
  );
}
