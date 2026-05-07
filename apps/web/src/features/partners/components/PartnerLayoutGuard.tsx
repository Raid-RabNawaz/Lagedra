import { Navigate, Outlet, useLocation } from "react-router-dom";
import { Loader } from "@/components/shared/Loader";
import { ErrorState } from "@/components/shared/ErrorState";
import { usePartnerMembership } from "@/features/partners/hooks/usePartnerMembership";

/**
 * Wraps every /app/partner/* page (except onboarding) and ensures the caller
 * is a member of a partner organization. If they aren't, sends them to onboarding.
 * Pass `requireMembership={false}` for the onboarding page itself.
 */
export function PartnerLayoutGuard({ requireMembership = true }: { requireMembership?: boolean }) {
  const { isLoading, membership, error, refresh } = usePartnerMembership();
  const location = useLocation();

  if (isLoading) return <Loader label="Checking organization..." />;

  if (error) {
    return <ErrorState error={error} onRetry={() => void refresh()} />;
  }

  if (requireMembership && !membership) {
    return <Navigate to="/app/partner/onboarding" replace state={{ from: location }} />;
  }

  if (!requireMembership && membership) {
    return <Navigate to="/app/partner" replace />;
  }

  return <Outlet />;
}
