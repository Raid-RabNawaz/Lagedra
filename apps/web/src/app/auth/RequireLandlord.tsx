import { Navigate, Outlet } from "react-router-dom";
import { roles } from "./roles";
import { hasRole } from "./permissions";
import { useAuthStore } from "./authStore";

const allowed = [roles.landlord, roles.platformAdmin] as const;

export const RequireLandlord = () => {
  const user = useAuthStore((state) => state.user);

  if (!user) {
    return <Navigate to="/auth/login" replace />;
  }

  if (!hasRole(user.role, [...allowed])) {
    return <Navigate to="/app" replace />;
  }

  return <Outlet />;
};
