import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { arbitrationApi } from "@/features/arbitration/services/arbitrationApi";
import type { ArbitrationStatus, ArbitrationTier, ArbitrationCategory } from "@/api/types";

export function useCases(status: ArbitrationStatus) {
  return useQuery({
    queryKey: ["arbitration", "cases", status],
    queryFn: () => arbitrationApi.listByStatus(status),
    staleTime: 30_000,
  });
}

export function useCase(caseId: string | undefined) {
  return useQuery({
    queryKey: ["arbitration", "case", caseId],
    queryFn: () => arbitrationApi.getCase(caseId!),
    enabled: Boolean(caseId),
    staleTime: 15_000,
  });
}

export function useFileCase() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      dealId,
      tier,
      category,
    }: {
      dealId: string;
      tier: ArbitrationTier;
      category: ArbitrationCategory;
    }) => arbitrationApi.fileCase(dealId, tier, category),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ["arbitration"] });
    },
  });
}

export function useAttachEvidence() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      caseId,
      slotType,
      submittedBy,
      evidenceManifestId,
    }: {
      caseId: string;
      slotType: string;
      submittedBy: string;
      evidenceManifestId: string;
    }) => arbitrationApi.attachEvidence(caseId, slotType, submittedBy, evidenceManifestId),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ["arbitration"] });
    },
  });
}

export function useMarkEvidenceComplete() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (caseId: string) => arbitrationApi.markEvidenceComplete(caseId),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ["arbitration"] });
    },
  });
}

export function useIssueDecision() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      caseId,
      decisionSummary,
      awardAmount,
    }: {
      caseId: string;
      decisionSummary: string;
      awardAmount?: number | null;
    }) => arbitrationApi.issueDecision(caseId, decisionSummary, awardAmount),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ["arbitration"] });
    },
  });
}

export function useAssignArbitrator() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      caseId,
      arbitratorUserId,
      concurrentCaseCount,
    }: {
      caseId: string;
      arbitratorUserId: string;
      concurrentCaseCount: number;
    }) => arbitrationApi.assignArbitrator(caseId, arbitratorUserId, concurrentCaseCount),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ["arbitration"] });
    },
  });
}

export function useCloseCase() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (caseId: string) => arbitrationApi.closeCase(caseId),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ["arbitration"] });
    },
  });
}

export function useAppealCase() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ caseId, reason }: { caseId: string; reason: string }) =>
      arbitrationApi.appealCase(caseId, reason),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ["arbitration"] });
    },
  });
}
