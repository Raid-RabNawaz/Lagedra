import { Navigate, Outlet, useLocation } from "react-router-dom";
import { useAuthStore } from "./authStore";
import { roles } from "./roles";
import { hasRole } from "./permissions";
import { usePublicConfigStore } from "@/app/config/publicConfigStore";
import { Loader } from "@/components/shared/Loader";
import { PRE_LAUNCH_HOST_HOME } from "./preLaunchAccess";

// Roles that keep full product access while the platform is in pre-launch mode
// (operational staff who need to run the platform and flip the flag off).
// PlatformAdmin is always allowed by hasRole, so we only list the arbitrator.
const preLaunchExemptRoles = [roles.arbitrator] as const;

type RequireLaunchAccessProps = {
  /**
   * `marketplace` — closed to everyone except staff during pre-launch.
   * `app` — staff and signed-in founding hosts may enter (hosts are further
   * limited by RequirePreLaunchHostSurface).
   */
  surface?: "marketplace" | "app";
};

/**
 * When pre-launch mode is on, gates marketplace vs authenticated product
 * access. When pre-launch is off this is a no-op passthrough.
 */
export const RequireLaunchAccess = ({
  surface = "marketplace",
}: RequireLaunchAccessProps) => {
  const preLaunchEnabled = usePublicConfigStore((s) => s.preLaunchEnabled);
  const isConfigLoaded = usePublicConfigStore((s) => s.isLoaded);
  const isAuthInitialized = useAuthStore((s) => s.isInitialized);
  const user = useAuthStore((s) => s.user);
  const location = useLocation();

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

  // Only Members (founding hosts) get the limited app surface; other roles
  // would ping-pong between RequireMember's `/app` bounce and our listings
  // redirect, so they go to the join page like everyone else.
  const isMember = Boolean(user && hasRole(user.role, [roles.member]));

  if (surface === "app" && isMember) {
    // Send bare `/app` (dashboard) straight to listings so hosts land usefully.
    if (location.pathname === "/app" || location.pathname === "/app/") {
      return <Navigate to={PRE_LAUNCH_HOST_HOME} replace />;
    }
    return <Outlet />;
  }

  // Marketplace stays closed; signed-in hosts go to their listing home.
  if (isMember) {
    return <Navigate to={PRE_LAUNCH_HOST_HOME} replace />;
  }

  return <Navigate to="/join" replace />;
};
