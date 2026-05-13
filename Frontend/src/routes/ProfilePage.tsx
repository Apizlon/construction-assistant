import React from "react";
import { useAuth } from "../state/auth";
import { Badge } from "./ui";

export function ProfilePage() {
  const { user } = useAuth();
  return (
    <div className="space-y-5">
      <div className="flex items-end justify-between gap-4">
        <div>
          <h2 className="text-xl font-semibold tracking-tight">Профиль</h2>
          <div className="text-sm text-slate-400">Информация об аккаунте.</div>
        </div>
        <Badge>{user?.role ?? "—"}</Badge>
      </div>

      <div className="card">
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <div>
            <div className="label mb-1">User ID</div>
            <div className="font-mono text-sm text-slate-200">{user?.id}</div>
          </div>
          <div>
            <div className="label mb-1">Email</div>
            <div className="text-sm text-slate-200">{user?.email}</div>
          </div>
          <div>
            <div className="label mb-1">Роль</div>
            <div className="text-sm text-slate-200">{user?.role}</div>
          </div>
        </div>
      </div>
    </div>
  );
}

