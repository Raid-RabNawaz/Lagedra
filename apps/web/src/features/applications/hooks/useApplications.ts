import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { applicationApi } from "@/features/applications/services/applicationApi";
import type { ApproveApplicationRequest, SubmitApplicationRequest } from "@/api/types";

export function useMyApplications() {
  return useQuery({
    queryKey: ["applications", "mine"],
    queryFn: () => applicationApi.listMine(),
    staleTime: 30_000,
  });
}

export function useApplicationsForListing(listingId: string | undefined) {
  return useQuery({
    queryKey: ["applications", "listing", listingId],
    queryFn: () => applicationApi.listForListing(listingId!),
    enabled: Boolean(listingId),
    staleTime: 30_000,
  });
}

export function useApplicationDetail(id: string | undefined) {
  return useQuery({
    queryKey: ["application", id],
    queryFn: () => applicationApi.getById(id!),
    enabled: Boolean(id),
    staleTime: 30_000,
  });
}

/**
 * Reservation price breakdown shown in the apply dialog before the tenant
 * submits: predetermined deposit for their verification tier (+ reason),
 * rent, fees and the total they'll be charged on host approval. Only runs
 * once a valid date range is selected.
 */
export function useReservationPreview(
  listingId: string | undefined,
  checkIn: string,
  checkOut: string,
  enabled: boolean,
) {
  return useQuery({
    queryKey: ["reservation-preview", listingId, checkIn, checkOut],
    queryFn: () => applicationApi.preview(listingId!, checkIn, checkOut),
    enabled: Boolean(listingId) && enabled && Boolean(checkIn) && Boolean(checkOut),
    staleTime: 60_000,
  });
}

export function useSubmitApplication() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (payload: SubmitApplicationRequest) =>
      applicationApi.submit(payload),
    onSuccess: (_data, variables) => {
      void queryClient.invalidateQueries({
        queryKey: ["applications", "listing", variables.listingId],
      });
    },
  });
}

/**
 * Phase 16.9 — kicks off the booking SetupIntent so the apply dialog
 * can mount Stripe Elements in card-on-file mode. The mutation is
 * idempotent server-side (keyed on tenant + listing), so it's safe to
 * fire when the dialog opens or when the user clicks the "save card"
 * step.
 */
export function useCreateBookingSetupIntent() {
  return useMutation({
    mutationFn: (listingId: string) => applicationApi.createSetupIntent(listingId),
  });
}

export function useApproveApplication() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: ApproveApplicationRequest }) =>
      applicationApi.approve(id, payload),
    onSuccess: (data) => {
      queryClient.setQueryData(["application", data.applicationId], data);
      void queryClient.invalidateQueries({
        queryKey: ["applications", "listing", data.listingId],
      });
      // Phase 16.11: the host inbox query (`applications/mine`) and
      // the deals list both surface this application — refresh them
      // so the inline approve action reflects immediately.
      void queryClient.invalidateQueries({
        queryKey: ["applications", "mine"],
      });
      void queryClient.invalidateQueries({ queryKey: ["deals"] });
    },
  });
}

export function useRejectApplication() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => applicationApi.reject(id),
    onSuccess: (data) => {
      queryClient.setQueryData(["application", data.applicationId], data);
      void queryClient.invalidateQueries({
        queryKey: ["applications", "listing", data.listingId],
      });
      void queryClient.invalidateQueries({
        queryKey: ["applications", "mine"],
      });
      void queryClient.invalidateQueries({ queryKey: ["deals"] });
    },
  });
}

export function useAttachApplicationPayment() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      id,
      payload,
    }: {
      id: string;
      payload: import("@/api/types").AttachApplicationPaymentRequest;
    }) => applicationApi.attachPayment(id, payload),
    onSuccess: (data) => {
      queryClient.setQueryData(["application", data.applicationId], data);
      void queryClient.invalidateQueries({
        queryKey: ["applications", "mine"],
      });
    },
  });
}
