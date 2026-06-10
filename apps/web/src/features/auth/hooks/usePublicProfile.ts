import { useQuery } from "@tanstack/react-query";
import { authApi } from "@/features/auth/services/authApi";

export const PUBLIC_PROFILE_KEY = "public-profile";

export function usePublicProfile(userId: string | undefined | null) {
  return useQuery({
    queryKey: [PUBLIC_PROFILE_KEY, userId],
    queryFn: () => authApi.getPublicProfile(userId!),
    enabled: Boolean(userId),
    staleTime: 60_000,
  });
}
