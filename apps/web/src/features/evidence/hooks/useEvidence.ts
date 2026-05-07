import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { evidenceApi } from "@/features/evidence/services/evidenceApi";
import type { ManifestType } from "@/api/types";

export function useManifest(manifestId: string | undefined) {
  return useQuery({
    queryKey: ["evidence", "manifest", manifestId],
    queryFn: () => evidenceApi.getManifest(manifestId!),
    enabled: Boolean(manifestId),
    staleTime: 15_000,
  });
}

export function useCreateManifest() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ dealId, manifestType }: { dealId: string; manifestType: ManifestType }) =>
      evidenceApi.createManifest(dealId, manifestType),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ["evidence"] });
    },
  });
}

export function useSealManifest() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (manifestId: string) => evidenceApi.sealManifest(manifestId),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ["evidence"] });
    },
  });
}

export function useRequestUploadUrl() {
  return useMutation({
    mutationFn: ({
      manifestId,
      fileName,
      mimeType,
    }: {
      manifestId: string;
      fileName: string;
      mimeType: string;
    }) => evidenceApi.requestUploadUrl(manifestId, fileName, mimeType),
  });
}

export function useCompleteUpload() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (params: {
      uploadId: string;
      manifestId: string;
      originalFileName: string;
      storageKey: string;
      mimeType: string;
      fileHashHex: string;
    }) =>
      evidenceApi.completeUpload(
        params.uploadId,
        params.manifestId,
        params.originalFileName,
        params.storageKey,
        params.mimeType,
        params.fileHashHex,
      ),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ["evidence"] });
    },
  });
}

export function useDirectUpload() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ manifestId, file }: { manifestId: string; file: File }) =>
      evidenceApi.directUpload(manifestId, file),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ["evidence"] });
    },
  });
}

export function useScanStatus(uploadId: string | undefined) {
  return useQuery({
    queryKey: ["evidence", "scan", uploadId],
    queryFn: () => evidenceApi.getScanStatus(uploadId!),
    enabled: Boolean(uploadId),
    refetchInterval: (query) => {
      const status = query.state.data?.status;
      return status === "Pending" ? 5_000 : false;
    },
  });
}

export function useDownloadUrl() {
  return useMutation({
    mutationFn: (uploadId: string) => evidenceApi.getDownloadUrl(uploadId),
  });
}
