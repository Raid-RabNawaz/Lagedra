import { Navigate, Outlet, useLocation } from "react-router-dom";
import { useAuthStore } from "./authStore";
import { usePublicConfigStore } from "@/app/config/publicConfigStore";
import { Loader } from "@/components/shared/Loader";
import {
  isPreLaunchHostPath,
  isPreLaunchLimitedHost,
  PRE_LAUNCH_HOST_HOME,
} from "./preLaunchAccess";

/**
 * While pre-launch is on, founding hosts may only use My Listings / create /
 * edit / Hostaway import. Staff are unrestricted. When the flag is off this
 * is a passthrough.
 */
export const RequirePreLaunchHostSurface = () => {
  const preLaunchEnabled = usePublicConfigStore((s) => s.preLaunchEnabled);
  const isConfigLoaded = usePublicConfigStore((s) => s.isLoaded);
  const isAuthInitialized = useAuthStore((s) => s.isInitialized);
  const user = useAuthStore((s) => s.user);
  const location = useLocation();

  if (!isConfigLoaded || !isAuthInitialized) {
    return <Loader fullPage label="Loading..." />;
  }

  if (!isPreLaunchLimitedHost(preLaunchEnabled, user?.role)) {
    return <Outlet />;
  }

  if (isPreLaunchHostPath(location.pathname)) {
    return <Outlet />;
  }

  return <Navigate to={PRE_LAUNCH_HOST_HOME} replace />;
};
