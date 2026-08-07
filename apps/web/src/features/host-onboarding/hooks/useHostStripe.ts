import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { hostStripeApi } from "@/features/host-onboarding/services/hostStripeApi";
import type { SavePaymentDetailsRequest } from "@/api/types";

export function useHostStripeStatus() {
  return useQuery({
    queryKey: ["hostStripe", "status"],
    queryFn: () => hostStripeApi.getStatus(),
    // Always re-sync from Stripe when landing back from Account Links.
    staleTime: 0,
    refetchOnMount: "always",
    refetchOnWindowFocus: true,
    retry: false,
  });
}

export function useHostStripeOnboard() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: () => hostStripeApi.onboard(),
    onSuccess: (data) => {
      queryClient.setQueryData(["hostStripe", "status"], data);
    },
  });
}

export function useRefreshOnboardingLink() {
  return useMutation({
    mutationFn: () => hostStripeApi.refreshLink(),
  });
}

export function useHostExpressLogin() {
  return useMutation({
    mutationFn: () => hostStripeApi.openExpressDashboard(),
  });
}

export function useHostAccountUpdateLink() {
  return useMutation({
    mutationFn: () => hostStripeApi.updateAccountLink(),
  });
}

export function useHostPaymentDetails() {
  return useQuery({
    queryKey: ["hostPayment", "details"],
    queryFn: () => hostStripeApi.getPaymentDetails(),
    staleTime: 60_000,
    retry: false,
  });
}

export function useSaveHostPaymentDetails() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: SavePaymentDetailsRequest) =>
      hostStripeApi.savePaymentDetails(payload),
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: ["hostPayment", "details"],
      });
    },
  });
}

/**
 * Whether the current host can actually receive funds. Non-custodial (Option A):
 * the booking charge is a Stripe destination charge settled on the host's
 * connected account, so readiness requires a Connect account with charges and
 * payouts enabled. Free-text payout notes are only the months-2+ rent channel
 * and no longer make a host payout-ready. Mirrors the backend precondition that
 * gates host approval / off-session charges, so the UI can warn before the host
 * accepts a request they can't be paid for. `settled` is false until the status
 * lookup resolves, so callers can avoid flashing a false "not ready" warning
 * during load.
 */
export function useHostPayoutReadiness() {
  const status = useHostStripeStatus();

  const settled = !status.isLoading;
  const ready =
    status.data?.chargesEnabled === true &&
    status.data?.payoutsEnabled === true;

  return { ready, settled };
}
