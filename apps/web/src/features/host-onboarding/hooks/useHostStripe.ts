import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { hostStripeApi } from "@/features/host-onboarding/services/hostStripeApi";
import type { SavePaymentDetailsRequest } from "@/api/types";

export function useHostStripeStatus() {
  return useQuery({
    queryKey: ["hostStripe", "status"],
    queryFn: () => hostStripeApi.getStatus(),
    staleTime: 30_000,
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
