import { Navigate, Outlet, useLocation } from "react-router-dom";
import { useAuthStore } from "./authStore";
import { Loader } from "@/components/shared/Loader";

export const RequireAuth = () => {
  const accessToken = useAuthStore((state) => state.accessToken);
  const isInitialized = useAuthStore((state) => state.isInitialized);
  const location = useLocation();

  if (!isInitialized) {
    return <Loader fullPage label="Loading session..." />;
  }

  if (!accessToken) {
    return <Navigate to="/auth/login" replace state={{ from: location.pathname }} />;
  }

  return <Outlet />;
};
