import { useQuery } from "@tanstack/react-query";
import { adminApi } from "@/features/admin/services/adminApi";

export function useProtocolFeeReconciliation(enabled = true) {
  return useQuery({
    queryKey: ["admin", "protocol-fee-reconciliation"],
    queryFn: () => adminApi.getProtocolFeeReconciliation(),
    enabled,
    staleTime: 60_000,
  });
}
