import React, { createContext, useContext, useMemo, useRef, useState } from "react";
import { getOwnedProjects, getViewerProjects, ProjectListItem } from "../api/projectApi";
import { AuthUser } from "./auth";

type ProjectsState = {
  owned: ProjectListItem[];
  viewer: ProjectListItem[];
  loading: boolean;
  error: string | null;
  refresh: () => Promise<void>;
};

const ProjectsContext = createContext<ProjectsState | null>(null);

export function ProjectsProvider({ user, children }: { user: AuthUser | null; children: React.ReactNode }) {
  const [owned, setOwned] = useState<ProjectListItem[]>([]);
  const [viewer, setViewer] = useState<ProjectListItem[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const inFlight = useRef<Promise<void> | null>(null);

  const refresh = useMemo(() => {
    return async () => {
      if (!user) return;
      if (inFlight.current) return inFlight.current;

      setLoading(true);
      setError(null);

      const p = (async () => {
        try {
          if (user.role === "Creator") {
            const o = await getOwnedProjects(user.id);
            setOwned(o);
            setViewer([]);
            return;
          }

          if (user.role === "Viewer") {
            const v = await getViewerProjects(user.id);
            setOwned([]);
            setViewer(v);
            return;
          }

          // Admin: can see both sets (useful for testing)
          const [o, v] = await Promise.all([getOwnedProjects(user.id).catch(() => []), getViewerProjects(user.id).catch(() => [])]);
          setOwned(o);
          setViewer(v);
        } catch (e: any) {
          setError(e?.response?.data?.message ?? "Не удалось загрузить проекты");
        } finally {
          setLoading(false);
          inFlight.current = null;
        }
      })();

      inFlight.current = p;
      return p;
    };
  }, [user]);

  const value = useMemo(
    () => ({
      owned,
      viewer,
      loading,
      error,
      refresh
    }),
    [owned, viewer, loading, error, refresh]
  );

  return <ProjectsContext.Provider value={value}>{children}</ProjectsContext.Provider>;
}

export function useProjects() {
  const ctx = useContext(ProjectsContext);
  if (!ctx) throw new Error("ProjectsProvider missing");
  return ctx;
}

