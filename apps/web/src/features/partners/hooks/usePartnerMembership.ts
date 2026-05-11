import { useQuery } from "@tanstack/react-query";
import { partnerApi } from "@/features/partners/services/partnerApi";

export function usePartnerMembership() {
  const query = useQuery({
    queryKey: ["partner", "my-membership"],
    queryFn: () => partnerApi.getMyMembership(),
    staleTime: 60_000,
  });

  return {
    isLoading: query.isLoading,
    membership: query.data ?? null,
    error: query.error,
    refresh: query.refetch,
  };
}
