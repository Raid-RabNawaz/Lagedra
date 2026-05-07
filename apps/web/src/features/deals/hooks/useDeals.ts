import { useQuery } from "@tanstack/react-query";
import { dealApi } from "@/features/deals/services/dealApi";
import type { DealPhaseFilter } from "@/api/types";

export const MY_DEALS_KEY = "my-deals";

export function useMyDeals(phase: DealPhaseFilter = "all") {
  return useQuery({
    queryKey: [MY_DEALS_KEY, phase],
    queryFn: () => dealApi.getMyDeals(phase),
    staleTime: 30_000,
  });
}
