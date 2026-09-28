import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { truthSurfaceApi } from "@/features/truth-surface/services/truthSurfaceApi";
import type { SignDealLeaseRequest } from "@/api/types";

export const DEAL_LEASE_SIGNING_KEY = "deal-lease-signing";

export function useDealLeaseSigning(dealId: string | undefined) {
  return useQuery({
    queryKey: [DEAL_LEASE_SIGNING_KEY, dealId],
    queryFn: () => truthSurfaceApi.getDealLeaseSigning(dealId!),
    enabled: Boolean(dealId),
    staleTime: 15_000,
  });
}

export function useSignDealLease(dealId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (payload: SignDealLeaseRequest) =>
      truthSurfaceApi.signDealLease(dealId, payload),
    onSuccess: (data) => {
      queryClient.setQueryData([DEAL_LEASE_SIGNING_KEY, dealId], data);
    },
  });
}
