import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { truthSurfaceApi } from "@/features/truth-surface/services/truthSurfaceApi";
import type { ConfirmingParty } from "@/api/types";

export function useSnapshot(snapshotId: string | undefined) {
  return useQuery({
    queryKey: ["truth-surface", snapshotId],
    queryFn: () => truthSurfaceApi.getSnapshot(snapshotId!),
    enabled: Boolean(snapshotId),
    staleTime: 15_000,
  });
}

/**
 * `data` resolves to the active Truth Surface for the deal, or `null` if
 * none has been created yet (404 is treated as a normal pre-confirmation
 * state, not an error). Use `data` truthiness to gate "review" vs
 * "create" affordances.
 */
export function useSnapshotByDealId(dealId: string | undefined) {
  return useQuery({
    queryKey: ["truth-surface", "by-deal", dealId],
    queryFn: () => truthSurfaceApi.getSnapshotByDealId(dealId!),
    enabled: Boolean(dealId),
    staleTime: 15_000,
  });
}

export function useSnapshotProof(snapshotId: string | undefined, enabled = false) {
  return useQuery({
    queryKey: ["truth-surface", snapshotId, "proof"],
    queryFn: () => truthSurfaceApi.verify(snapshotId!),
    enabled: Boolean(snapshotId) && enabled,
    staleTime: 60_000,
  });
}

export function useCreateFromDeal() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (dealId: string) => truthSurfaceApi.createFromDeal(dealId),
    onSuccess: (data) => {
      queryClient.setQueryData(["truth-surface", data.snapshotId], data);
      queryClient.setQueryData(["truth-surface", "by-deal", data.dealId], data);
      queryClient.invalidateQueries({ queryKey: ["deals"] });
    },
  });
}

export function useConfirmSnapshot() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      snapshotId,
      party,
    }: {
      snapshotId: string;
      party: ConfirmingParty;
    }) => truthSurfaceApi.confirm(snapshotId, { party }),
    onSuccess: (data) => {
      queryClient.setQueryData(["truth-surface", data.snapshotId], data);
      queryClient.setQueryData(["truth-surface", "by-deal", data.dealId], data);
      // Phase 16.5: tenant confirmation seals the TS, which fires the
      // domain event that creates the DealPaymentConfirmation row.
      // Refresh the checkout query so the inline checkout page can
      // transition straight from the confirmation panel to the
      // payment element without a manual reload.
      queryClient.invalidateQueries({ queryKey: ["checkout", data.dealId] });
      queryClient.invalidateQueries({ queryKey: ["deals"] });
    },
  });
}

export function useDownloadSnapshotReceipt() {
  return useMutation({
    mutationFn: async (snapshotId: string) => {
      const { blob, filename } = await truthSurfaceApi.downloadReceipt(snapshotId);
      const url = URL.createObjectURL(blob);
      try {
        const link = document.createElement("a");
        link.href = url;
        link.download = filename;
        document.body.appendChild(link);
        link.click();
        link.remove();
      } finally {
        URL.revokeObjectURL(url);
      }
      return { filename };
    },
  });
}

export function useReconfirmSnapshot() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      snapshotId,
      newJurisdictionPackVersion,
      updatedCanonicalContent,
      reason,
    }: {
      snapshotId: string;
      newJurisdictionPackVersion: string;
      updatedCanonicalContent: string;
      reason: string;
    }) =>
      truthSurfaceApi.reconfirm(snapshotId, {
        newJurisdictionPackVersion,
        updatedCanonicalContent,
        reason,
      }),
    onSuccess: (data) => {
      queryClient.setQueryData(["truth-surface", data.snapshotId], data);
    },
  });
}
