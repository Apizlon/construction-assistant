import { Navigate, Route, Routes } from "react-router-dom";
import { LoginPage } from "./LoginPage";
import { RegisterPage } from "./RegisterPage";
import { DashboardLayout } from "./DashboardLayout";
import { ProjectsPage } from "./ProjectsPage";
import { FeedbackPage } from "./FeedbackPage";
import { ProfilePage } from "./ProfilePage";
import { ProjectPage } from "./ProjectPage";
import { PassportPage } from "./PassportPage";
import { ProjectBuilderPage } from "./ProjectBuilderPage";
import { useAuth } from "../state/auth";

function NonAdminOnly({ children }: { children: React.ReactNode }) {
  const { user } = useAuth();
  if (user?.role === "Admin") return <Navigate to="/app/feedback" replace />;
  return <>{children}</>;
}

export function App() {
  const { isAuthenticated, user } = useAuth();

  return (
    <Routes>
      <Route
        path="/"
        element={<Navigate to={isAuthenticated ? (user?.role === "Admin" ? "/app/feedback" : "/app/projects") : "/login"} replace />}
      />
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />

      <Route path="/app" element={isAuthenticated ? <DashboardLayout /> : <Navigate to="/login" replace />}>
        <Route
          path="projects"
          element={
            <NonAdminOnly>
              <ProjectsPage />
            </NonAdminOnly>
          }
        />
        <Route
          path="projects/:projectId"
          element={
            <NonAdminOnly>
              <ProjectPage />
            </NonAdminOnly>
          }
        />
        <Route
          path="projects/:projectId/passport"
          element={
            <NonAdminOnly>
              <PassportPage />
            </NonAdminOnly>
          }
        />
        <Route
          path="projects/:projectId/builder"
          element={
            <NonAdminOnly>
              <ProjectBuilderPage />
            </NonAdminOnly>
          }
        />
        <Route path="feedback" element={<FeedbackPage />} />
        <Route path="profile" element={<ProfilePage />} />
      </Route>

      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}
