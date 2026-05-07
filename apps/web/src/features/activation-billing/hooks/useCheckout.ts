import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { checkoutApi } from "@/features/activation-billing/services/checkoutApi";

export function useCheckoutStatus(dealId: string | undefined) {
  return useQuery({
    queryKey: ["checkout", dealId],
    queryFn: () => checkoutApi.getCheckoutStatus(dealId!),
    enabled: Boolean(dealId),
    staleTime: 15_000,
  });
}

export function useCreateCheckout() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (dealId: string) => checkoutApi.createCheckout(dealId),
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: ["checkout"],
      });
    },
  });
}

export function useConfirmCheckout() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (dealId: string) => checkoutApi.confirmCheckout(dealId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["checkout"] });
      void queryClient.invalidateQueries({ queryKey: ["deals"] });
    },
  });
}
