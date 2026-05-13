import React from "react";
import { Link, NavLink, Outlet, useNavigate } from "react-router-dom";
import { useAuth } from "../state/auth";
import { ProjectsProvider } from "../state/projects";

function classNames(...xs: Array<string | false | null | undefined>) {
  return xs.filter(Boolean).join(" ");
}

export function DashboardLayout() {
  const { user, logout } = useAuth();
  return (
    <ProjectsProvider user={user}>
      <DashboardLayoutInner logout={logout} />
    </ProjectsProvider>
  );
}

function DashboardLayoutInner({ logout }: { logout: () => void }) {
  const { user } = useAuth();
  const navigate = useNavigate();
  const isAdmin = user?.role === "Admin";

  return (
    <div className="min-h-screen bg-gradient-to-b from-slate-950 via-slate-950 to-slate-900">
      <div className="border-b border-slate-800 bg-slate-950/70 backdrop-blur">
        <div className="mx-auto max-w-6xl px-6 py-4 flex flex-wrap items-center justify-between gap-3">
          <div className="flex items-center gap-3">
            <Link to="/app/projects" className="font-semibold tracking-tight">
              Строительный помощник
            </Link>
            <span className="text-xs text-slate-400">Кабинет</span>
          </div>

          <div className="flex items-center gap-2">
            <div className="hidden md:block text-xs text-slate-400">{user?.email}</div>
            <button
              className="btn-ghost"
              onClick={() => {
                logout();
                navigate("/login", { replace: true });
              }}
            >
              Выйти
            </button>
          </div>
        </div>
        <div className="mx-auto max-w-6xl px-6 pb-3 flex items-center gap-2 text-sm">
          {!isAdmin && (
            <NavLink
              to="/app/projects"
              className={({ isActive }) => classNames("px-3 py-1.5 rounded-xl", isActive ? "bg-slate-800 text-white" : "text-slate-300 hover:text-white")}
            >
              Проекты
            </NavLink>
          )}
          <NavLink
            to="/app/feedback"
            className={({ isActive }) => classNames("px-3 py-1.5 rounded-xl", isActive ? "bg-slate-800 text-white" : "text-slate-300 hover:text-white")}
          >
            Обратная связь
          </NavLink>
          <NavLink
            to="/app/profile"
            className={({ isActive }) => classNames("px-3 py-1.5 rounded-xl", isActive ? "bg-slate-800 text-white" : "text-slate-300 hover:text-white")}
          >
            Профиль
          </NavLink>
        </div>
      </div>

      <div className="mx-auto max-w-6xl px-6 py-8">
        <Outlet />
      </div>
    </div>
  );
}

