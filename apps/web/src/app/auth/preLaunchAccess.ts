import { roles } from "./roles";
import { hasRole } from "./permissions";
import type { UserRole } from "./roles";

/** Default landing page for founding hosts during pre-launch. */
export const PRE_LAUNCH_HOST_HOME = "/app/listings";

/**
 * Routes founding hosts may open while `prelaunch.enabled` is on.
 * Everything else in `/app` redirects to {@link PRE_LAUNCH_HOST_HOME}.
 */
export function isPreLaunchHostPath(pathname: string): boolean {
  if (pathname === "/app/channels") return true;
  if (pathname === "/app/listings" || pathname.startsWith("/app/listings/")) {
    return true;
  }
  return false;
}

/** Operational staff keep the full product during pre-launch. */
export function isPreLaunchStaff(
  role: UserRole | string | number | null | undefined,
): boolean {
  return hasRole(role, [roles.arbitrator]);
}

/**
 * Non-staff authenticated users are limited to the host listing surface
 * while pre-launch is on (marketplace stays closed separately).
 */
export function isPreLaunchLimitedHost(
  preLaunchEnabled: boolean,
  role: UserRole | string | number | null | undefined,
): boolean {
  return preLaunchEnabled && !isPreLaunchStaff(role);
}
