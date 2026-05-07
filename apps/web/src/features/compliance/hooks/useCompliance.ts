import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { complianceApi } from "@/features/compliance/services/complianceApi";
import type { MonitoredViolationCategory } from "@/api/types";

export function useComplianceStatus(dealId: string | undefined) {
  return useQuery({
    queryKey: ["compliance", "status", dealId],
    queryFn: () => complianceApi.getDealComplianceStatus(dealId!),
    enabled: Boolean(dealId),
    staleTime: 30_000,
  });
}

export function useDealViolations(dealId: string | undefined) {
  return useQuery({
    queryKey: ["compliance", "violations", dealId],
    queryFn: () => complianceApi.getDealViolations(dealId!),
    enabled: Boolean(dealId),
    staleTime: 30_000,
  });
}

export function useCoreViolations(dealId: string | undefined) {
  return useQuery({
    queryKey: ["compliance", "core-violations", dealId],
    queryFn: () => complianceApi.getCoreViolations(dealId!),
    enabled: Boolean(dealId),
    staleTime: 30_000,
  });
}

export function useDetectViolation() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      dealId,
      category,
      cureDeadline,
    }: {
      dealId: string;
      category: MonitoredViolationCategory;
      cureDeadline?: string | null;
    }) => complianceApi.detectViolation(dealId, category, cureDeadline),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ["compliance"] });
    },
  });
}

export function useCureViolation() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      dealId,
      violationId,
    }: {
      dealId: string;
      violationId: string;
    }) => complianceApi.cureViolation(dealId, violationId),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ["compliance"] });
    },
  });
}

export function useResolveViolation() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => complianceApi.resolveViolation(id),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ["compliance"] });
    },
  });
}

export function useDismissViolation() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => complianceApi.dismissViolation(id),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ["compliance"] });
    },
  });
}

export function useEscalateViolation() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => complianceApi.escalateViolation(id),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ["compliance"] });
    },
  });
}

export function useUserLedger(userId: string | undefined) {
  return useQuery({
    queryKey: ["trust-ledger", "user", userId],
    queryFn: () => complianceApi.getUserLedger(userId!),
    enabled: Boolean(userId),
    staleTime: 30_000,
  });
}

export function useDealLedger(dealId: string | undefined) {
  return useQuery({
    queryKey: ["trust-ledger", "deal", dealId],
    queryFn: () => complianceApi.getDealLedger(dealId!),
    enabled: Boolean(dealId),
    staleTime: 30_000,
  });
}
