import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { billingApi } from "@/features/activation-billing/services/billingApi";
import type {
  DisputePaymentRequest,
  CancelBookingRequest,
  FileDamageClaimRequest,
} from "@/api/types";

export function useBillingStatus(dealId: string | undefined) {
  return useQuery({
    queryKey: ["billing", dealId],
    queryFn: () => billingApi.getBillingStatus(dealId!),
    enabled: Boolean(dealId),
    staleTime: 30_000,
  });
}

export function useProrationQuote(
  dealId: string | undefined,
  startDate: string | undefined,
  endDate: string | undefined,
) {
  return useQuery({
    queryKey: ["billing", dealId, "proration", startDate, endDate],
    queryFn: () => billingApi.getProrationQuote(dealId!, startDate!, endDate!),
    enabled: Boolean(dealId && startDate && endDate),
    staleTime: 60_000,
  });
}

export function usePaymentStatus(dealId: string | undefined) {
  return useQuery({
    queryKey: ["payment", dealId],
    queryFn: () => billingApi.getPaymentStatus(dealId!),
    enabled: Boolean(dealId),
    staleTime: 15_000,
  });
}

export function usePaymentDetails(dealId: string | undefined) {
  return useQuery({
    queryKey: ["payment", dealId, "details"],
    queryFn: () => billingApi.getPaymentDetails(dealId!),
    enabled: Boolean(dealId),
    staleTime: 60_000,
  });
}

export function useActivateDeal() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (dealId: string) => billingApi.activateDeal(dealId),
    onSuccess: (data) => {
      queryClient.setQueryData(["billing", data.dealId], data);
    },
  });
}

export function useStopBilling() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (dealId: string) => billingApi.stopBilling(dealId),
    onSuccess: (data) => {
      queryClient.setQueryData(["billing", data.dealId], data);
    },
  });
}

export function useConfirmPayment() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (dealId: string) => billingApi.confirmPayment(dealId),
    onSuccess: (data) => {
      queryClient.setQueryData(["payment", data.dealId], data);
    },
  });
}

export function useConfirmPlatformPayment() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (dealId: string) => billingApi.confirmPlatformPayment(dealId),
    onSuccess: (data) => {
      queryClient.setQueryData(["payment", data.dealId], data);
    },
  });
}

export function useDisputePayment() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      dealId,
      payload,
    }: {
      dealId: string;
      payload: DisputePaymentRequest;
    }) => billingApi.disputePayment(dealId, payload),
    onSuccess: (data) => {
      queryClient.setQueryData(["payment", data.dealId], data);
    },
  });
}

export function useCancelBooking() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      dealId,
      payload,
    }: {
      dealId: string;
      payload: CancelBookingRequest;
    }) => billingApi.cancelBooking(dealId, payload),
    onSuccess: (_data, variables) => {
      void queryClient.invalidateQueries({
        queryKey: ["billing", variables.dealId],
      });
      void queryClient.invalidateQueries({
        queryKey: ["payment", variables.dealId],
      });
    },
  });
}

export function useFileDamageClaim() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      dealId,
      payload,
    }: {
      dealId: string;
      payload: FileDamageClaimRequest;
    }) => billingApi.fileDamageClaim(dealId, payload),
    onSuccess: (_data, variables) => {
      void queryClient.invalidateQueries({
        queryKey: ["payment", variables.dealId],
      });
    },
  });
}
