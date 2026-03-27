import { useQuery } from "@tanstack/react-query";
import { listingApi } from "@/features/listings/services/listingApi";

export function useListingDefinitions() {
  return useQuery({
    queryKey: ["listing-definitions"],
    queryFn: async () => {
      const [amenities, safetyDevices, considerations] = await Promise.all([
        listingApi.getAmenityDefinitions(),
        listingApi.getSafetyDeviceDefinitions(),
        listingApi.getConsiderationDefinitions(),
      ]);
      return { amenities, safetyDevices, considerations };
    },
    staleTime: 10 * 60_000,
  });
}
