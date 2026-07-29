import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { verificationApi } from "@/features/verification/services/verificationApi";
import type {
  StartKycRequest,
  CompleteKycRequest,
  BackgroundCheckConsentRequest,
  KycDocumentType,
  SubmitManualKycRequest,
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

export function useMyKycDocuments(enabled = true) {
  return useQuery({
    queryKey: ["kyc-documents", "me"],
    queryFn: () => verificationApi.getMyKycDocuments(),
    enabled,
    staleTime: 10_000,
    retry: false,
  });
}

export function useUploadKycDocument() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      documentType,
      file,
      fileName,
    }: {
      documentType: KycDocumentType;
      file: File | Blob;
      fileName?: string;
    }) => verificationApi.uploadKycDocument(documentType, file, fileName),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["kyc-documents", "me"] });
    },
  });
}

export function useSubmitManualKyc() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: SubmitManualKycRequest) =>
      verificationApi.submitManualKyc(payload),
    onSuccess: (data) => {
      queryClient.setQueryData(["verification", data.userId], data);
      void queryClient.invalidateQueries({ queryKey: ["kyc-documents", "me"] });
    },
  });
}

/** Current user's resolved verification tier ("trust level"). */
export function useMyVerificationTier(enabled = true) {
  return useQuery({
    queryKey: ["verification-tier", "me"],
    queryFn: () => verificationApi.getMyVerificationTier(),
    enabled,
    staleTime: 60_000,
    retry: false,
  });
}
