import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { inquiryApi } from "@/features/inquiry/services/inquiryApi";
import type {
  InquiryCategory,
  SubmitInquiryQuestionRequest,
  SubmitLandlordResponseRequest,
} from "@/api/types";

export function useInquiryThread(dealId: string | undefined) {
  return useQuery({
    queryKey: ["inquiry", dealId],
    queryFn: () => inquiryApi.getThread(dealId!),
    enabled: Boolean(dealId),
    staleTime: 15_000,
    retry: (failureCount, error) => {
      if ((error as { response?: { status?: number } })?.response?.status === 404) {
        return false;
      }
      return failureCount < 3;
    },
  });
}

export function usePredefinedQuestions(category?: InquiryCategory) {
  return useQuery({
    queryKey: ["inquiry", "predefined-questions", category ?? "all"],
    queryFn: () => inquiryApi.listPredefinedQuestions(category),
    staleTime: 5 * 60_000,
  });
}

export function useRequestUnlock() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (dealId: string) => inquiryApi.requestUnlock(dealId),
    onSuccess: (data) => {
      queryClient.setQueryData(["inquiry", data.dealId], data);
    },
  });
}

export function useApproveUnlock() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (dealId: string) => inquiryApi.approveUnlock(dealId),
    onSuccess: (data) => {
      queryClient.setQueryData(["inquiry", data.dealId], data);
    },
  });
}

export function useSubmitQuestion() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      dealId,
      payload,
    }: {
      dealId: string;
      payload: SubmitInquiryQuestionRequest;
    }) => inquiryApi.submitQuestion(dealId, payload),
    onSuccess: (_data, variables) => {
      void queryClient.invalidateQueries({
        queryKey: ["inquiry", variables.dealId],
      });
    },
  });
}

export function useSubmitAnswer() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      dealId,
      payload,
    }: {
      dealId: string;
      payload: SubmitLandlordResponseRequest;
    }) => inquiryApi.submitAnswer(dealId, payload),
    onSuccess: (_data, variables) => {
      void queryClient.invalidateQueries({
        queryKey: ["inquiry", variables.dealId],
      });
    },
  });
}
