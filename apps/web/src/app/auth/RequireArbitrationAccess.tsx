import { Navigate, Outlet } from "react-router-dom";
import { useAuthStore } from "./authStore";
import { canAccessArbitration } from "./permissions";

export const RequireArbitrationAccess = () => {
  const user = useAuthStore((state) => state.user);

  if (!user) {
    return <Navigate to="/auth/login" replace />;
  }

  if (!canAccessArbitration(user.role)) {
    return <Navigate to="/app" replace />;
  }

  return <Outlet />;
};
