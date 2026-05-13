import React from "react";
import { Link } from "react-router-dom";

export function PageShell({ title, children, right }: { title: string; children: React.ReactNode; right?: React.ReactNode }) {
  return (
    <div className="min-h-screen bg-gradient-to-b from-slate-950 via-slate-950 to-slate-900">
      <div className="mx-auto max-w-5xl px-6 py-10 min-h-screen flex items-center justify-center">
        <div className="w-full">
        <div className="mb-6 flex items-center justify-between gap-4">
          <div>
            <div className="text-sm text-slate-400">
              <Link to="/" className="hover:text-slate-200">
                Строительный помощник
              </Link>
            </div>
            <h1 className="text-2xl font-semibold tracking-tight">{title}</h1>
          </div>
          {right}
        </div>
        {children}
        </div>
      </div>
    </div>
  );
}

export function Badge({ children }: { children: React.ReactNode }) {
  return <span className="rounded-full border border-slate-700 bg-slate-900/60 px-3 py-1 text-xs text-slate-200">{children}</span>;
}
