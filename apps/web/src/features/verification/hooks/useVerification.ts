import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { verificationApi } from "@/features/verification/services/verificationApi";
import type {
  StartKycRequest,
  CompleteKycRequest,
  BackgroundCheckConsentRequest,
} from "@/api/types";

type VerificationStatusOptions = {
  /** Poll interval in ms while the status is `Pending`. Set to `false` to disable. */
  pollWhilePending?: number | false;
};

export function useVerificationStatus(
  userId: string | undefined,
  options: VerificationStatusOptions = {},
) {
  const { pollWhilePending = 3_000 } = options;
  return useQuery({
    queryKey: ["verification", userId],
    queryFn: () => verificationApi.getStatus(userId!),
    enabled: Boolean(userId),
    staleTime: 30_000,
    retry: false,
    refetchInterval: (query) => {
      if (pollWhilePending === false) return false;
      const status = query.state.data?.status;
      return status === "Pending" ? pollWhilePending : false;
    },
  });
}

export function useStartKyc() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: StartKycRequest) =>
      verificationApi.startKyc(payload),
    onSuccess: (data) => {
      queryClient.setQueryData(["verification", data.userId], data);
    },
  });
}

export function useCompleteKyc() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: CompleteKycRequest) =>
      verificationApi.completeKyc(payload),
    onSuccess: (data) => {
      queryClient.setQueryData(["verification", data.userId], data);
    },
  });
}

export function useSubmitBackgroundCheckConsent() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: BackgroundCheckConsentRequest) =>
      verificationApi.submitBackgroundCheckConsent(payload),
    onSuccess: (_data, variables) => {
      void queryClient.invalidateQueries({
        queryKey: ["verification", variables.userId],
      });
    },
  });
}

export function useRiskView(userId: string | undefined) {
  return useQuery({
    queryKey: ["risk", userId],
    queryFn: () => verificationApi.getRiskView(userId!),
    enabled: Boolean(userId),
    staleTime: 60_000,
    retry: false,
  });
}
