import React from "react";
import { Link } from "react-router-dom";

export function PageShell({ title, children, right }: { title: string; children: React.ReactNode; right?: React.ReactNode }) {
  return (
    <div className="min-h-screen bg-gradient-to-b from-slate-950 via-slate-950 to-slate-900">
      <div className="mx-auto max-w-5xl px-6 py-10">
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
  );
}

export function AuthShell({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div className="min-h-screen bg-slate-950">
      <div className="min-h-screen grid grid-cols-1 lg:grid-cols-12">
        <div className="lg:col-span-4 flex items-center justify-center px-6 py-12">
          <div className="w-full max-w-md">
            <div className="mb-6">
              <div className="text-sm text-slate-400">
                <Link to="/" className="hover:text-slate-200">
                  Строительный помощник
                </Link>
              </div>
              <h1 className="mt-2 text-2xl font-semibold tracking-tight text-slate-100">{title}</h1>
              <div className="mt-2 text-sm text-slate-400">Войдите, чтобы управлять проектами и расчетами.</div>
            </div>
            {children}
          </div>
        </div>

        <div className="hidden lg:block lg:col-span-8 relative overflow-hidden">
          <div className="absolute inset-0 bg-gradient-to-br from-indigo-600/20 via-slate-950 to-slate-950" />
          <div className="absolute -top-24 -right-24 h-96 w-96 rounded-full bg-indigo-500/20 blur-3xl" />
          <div className="absolute -bottom-24 -left-24 h-96 w-96 rounded-full bg-cyan-500/10 blur-3xl" />
          <div className="absolute inset-0 [mask-image:radial-gradient(60%_60%_at_50%_50%,black,transparent)]">
            <div className="h-full w-full bg-[linear-gradient(to_right,rgba(148,163,184,0.08)_1px,transparent_1px),linear-gradient(to_bottom,rgba(148,163,184,0.08)_1px,transparent_1px)] bg-[size:40px_40px]" />
          </div>
          <div className="relative h-full p-12 flex items-end">
            <div className="max-w-lg">
              <div className="text-sm text-slate-300/80">Помощник для строительных проектов</div>
              <div className="mt-3 text-3xl font-semibold tracking-tight text-slate-100">
                Сроки, бюджет и решения — в одном месте
              </div>
              <div className="mt-4 text-sm text-slate-300/80">
                Собирайте паспорт объекта, выбирайте варианты по карточкам и получайте сбалансированный вариант на основе весов параметров.
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

export function Badge({ children }: { children: React.ReactNode }) {
  return <span className="rounded-full border border-slate-700 bg-slate-900/60 px-3 py-1 text-xs text-slate-200">{children}</span>;
}
