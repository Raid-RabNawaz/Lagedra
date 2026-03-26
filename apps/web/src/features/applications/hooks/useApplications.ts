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
    },
  });
}
