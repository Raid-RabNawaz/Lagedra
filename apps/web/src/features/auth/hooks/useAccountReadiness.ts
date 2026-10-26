import { useAuthStore } from "@/app/auth/authStore";
import { buildAccountReadiness, type ReadinessAudience } from "@/features/auth/lib/accountReadiness";
import { useHostStripeStatus } from "@/features/host-onboarding/hooks/useHostStripe";
import { useVerificationStatus } from "@/features/verification/hooks/useVerification";

export function useAccountReadiness(audience: ReadinessAudience) {
  const user = useAuthStore((s) => s.user);
  const verification = useVerificationStatus(user?.userId, { pollWhilePending: false });
  const stripe = useHostStripeStatus({ enabled: audience === "host" && Boolean(user?.userId) });

  const readiness = user
    ? buildAccountReadiness({
        audience,
        user,
        kycStatus: verification.data?.status,
        hostKind: verification.data?.hostKind ?? user.hostKind,
        stripe: stripe.data,
      })
    : null;

  const isLoading =
    Boolean(user)
    && (verification.isLoading || (audience === "host" && stripe.isLoading));

  return { readiness, isLoading };
}
