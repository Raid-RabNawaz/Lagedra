import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { reviewsApi } from "@/features/reviews/services/reviewsApi";
import type {
  SubmitStayReviewRequest,
  SubmitPartnerServiceReviewRequest,
} from "@/api/types";

export function useDealReviews(dealId: string | undefined) {
  return useQuery({
    queryKey: ["reviews", "deal", dealId],
    queryFn: () => reviewsApi.getDealReviews(dealId!),
    enabled: Boolean(dealId),
    staleTime: 15_000,
    retry: false,
  });
}

export function useSubmitStayReview(dealId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (payload: SubmitStayReviewRequest) =>
      reviewsApi.submitStayReview(dealId, payload),
    onSuccess: (review) => {
      void qc.invalidateQueries({ queryKey: ["reviews", "deal", dealId] });
      void qc.invalidateQueries({ queryKey: ["reviews", "listing", review.listingId] });
      void qc.invalidateQueries({ queryKey: ["reviews", "reputation", review.revieweeUserId] });
      void qc.invalidateQueries({ queryKey: ["reviews", "user", review.revieweeUserId] });
    },
  });
}

export function useUserReputation(userId: string | undefined) {
  return useQuery({
    queryKey: ["reviews", "reputation", userId],
    queryFn: () => reviewsApi.getUserReputation(userId!),
    enabled: Boolean(userId),
    staleTime: 60_000,
  });
}

export function useUserReviews(userId: string | undefined) {
  return useQuery({
    queryKey: ["reviews", "user", userId],
    queryFn: () => reviewsApi.getUserReviews(userId!),
    enabled: Boolean(userId),
    staleTime: 60_000,
  });
}

export function useListingReviews(listingId: string | undefined) {
  return useQuery({
    queryKey: ["reviews", "listing", listingId],
    queryFn: () => reviewsApi.getListingReviews(listingId!),
    enabled: Boolean(listingId),
    staleTime: 60_000,
  });
}

export function usePartnerReputation(orgId: string | undefined) {
  return useQuery({
    queryKey: ["reviews", "partner-reputation", orgId],
    queryFn: () => reviewsApi.getPartnerReputation(orgId!),
    enabled: Boolean(orgId),
    staleTime: 30_000,
  });
}

export function usePartnerReviews(orgId: string | undefined) {
  return useQuery({
    queryKey: ["reviews", "partner", orgId],
    queryFn: () => reviewsApi.listPartnerReviews(orgId!),
    enabled: Boolean(orgId),
    staleTime: 60_000,
  });
}

export function useSubmitPartnerReview(orgId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (payload: SubmitPartnerServiceReviewRequest) =>
      reviewsApi.submitPartnerReview(orgId, payload),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ["reviews", "partner-reputation", orgId] });
      void qc.invalidateQueries({ queryKey: ["reviews", "partner", orgId] });
    },
  });
}
