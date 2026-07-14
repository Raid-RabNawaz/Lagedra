import { Navigate, Outlet } from "react-router-dom";
import { useAuthStore } from "./authStore";
import { roles } from "./roles";
import { hasRole } from "./permissions";
import { usePublicConfigStore } from "@/app/config/publicConfigStore";
import { Loader } from "@/components/shared/Loader";

// Roles that keep full product access while the platform is in pre-launch mode
// (operational staff who need to run the platform and flip the flag off).
// PlatformAdmin is always allowed by hasRole, so we only list the arbitrator.
const preLaunchExemptRoles = [roles.arbitrator] as const;

/**
 * When pre-launch mode is on, the dashboard/product is hidden from everyone
 * except operational staff — non-staff sessions are bounced to the founding
 * partner join flow. When pre-launch is off this is a no-op passthrough.
 */
export const RequireLaunchAccess = () => {
  const preLaunchEnabled = usePublicConfigStore((s) => s.preLaunchEnabled);
  const isConfigLoaded = usePublicConfigStore((s) => s.isLoaded);
  const isAuthInitialized = useAuthStore((s) => s.isInitialized);
  const user = useAuthStore((s) => s.user);

  // Decide only once the flag + session are known. This blocks the very first
  // paint on a cold load (direct URL / hard refresh) so a blocked page never
  // flashes; returning visitors have a cached flag and skip the loader.
  if (!isConfigLoaded || !isAuthInitialized) {
    return <Loader fullPage label="Loading..." />;
  }

  if (!preLaunchEnabled) {
    return <Outlet />;
  }

  if (user && hasRole(user.role, preLaunchExemptRoles)) {
    return <Outlet />;
  }

  return <Navigate to="/join" replace />;
};
