import React, { createContext, useContext, useMemo, useState } from "react";

export type UserRole = "Creator" | "Viewer" | "Admin";

export type AuthUser = {
  id: string;
  email: string;
  role: UserRole;
};

type AuthState = {
  accessToken: string | null;
  user: AuthUser | null;
  isAuthenticated: boolean;
  setAuth: (token: string, user: AuthUser) => void;
  logout: () => void;
};

const AuthContext = createContext<AuthState | null>(null);

const STORAGE_KEY = "ca_auth_v1";

type Stored = { accessToken: string; user: AuthUser };

function loadStored(): Stored | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return null;
    return JSON.parse(raw) as Stored;
  } catch {
    return null;
  }
}

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const stored = loadStored();
  const [accessToken, setAccessToken] = useState<string | null>(stored?.accessToken ?? null);
  const [user, setUser] = useState<AuthUser | null>(stored?.user ?? null);

  const value = useMemo<AuthState>(
    () => ({
      accessToken,
      user,
      isAuthenticated: Boolean(accessToken && user),
      setAuth: (token, u) => {
        setAccessToken(token);
        setUser(u);
        localStorage.setItem(STORAGE_KEY, JSON.stringify({ accessToken: token, user: u } satisfies Stored));
      },
      logout: () => {
        setAccessToken(null);
        setUser(null);
        localStorage.removeItem(STORAGE_KEY);
      }
    }),
    [accessToken, user]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("AuthProvider missing");
  return ctx;
}

