import { Navigate, Outlet } from "react-router-dom";
import { useAuthStore } from "./authStore";
import type { UserRole } from "./roles";
import { hasRole } from "./permissions";

type RequireRoleProps = {
  allowed: readonly (UserRole | string)[];
  redirectTo?: string;
};

export const RequireRole = ({
  allowed,
  redirectTo = "/app",
}: RequireRoleProps) => {
  const user = useAuthStore((state) => state.user);

  if (!user) {
    return <Navigate to="/auth/login" replace />;
  }

  if (!hasRole(user.role, allowed)) {
    return <Navigate to={redirectTo} replace />;
  }

  return <Outlet />;
};
