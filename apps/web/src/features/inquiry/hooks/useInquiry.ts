import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { inquiryApi } from "@/features/inquiry/services/inquiryApi";
import type {
  InquiryCategory,
  InquiryDto,
  SubmitInquiryQuestionRequest,
  SubmitLandlordResponseRequest,
  ProposeInquiryOfferRequest,
  CounterInquiryOfferRequest,
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
    onSuccess: (data, dealId) => {
      queryClient.setQueryData(["inquiry", data.dealId ?? dealId], data);
    },
  });
}

export function useApproveUnlock() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (dealId: string) => inquiryApi.approveUnlock(dealId),
    onSuccess: (data, dealId) => {
      queryClient.setQueryData(["inquiry", data.dealId ?? dealId], data);
    },
  });
}

export function useLockInquiry() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (dealId: string) => inquiryApi.lock(dealId),
    onSuccess: (data, dealId) => {
      queryClient.setQueryData(["inquiry", data.dealId ?? dealId], data);
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

// ── Phase 17 — pre-booking + session-id-based hooks ──────────────────────

/**
 * Phase 17 — fetch the calling tenant's existing pre-booking inquiry
 * thread for a listing, if any. 404 maps to `null` (no thread yet).
 */
export function useMyListingInquiry(listingId: string | undefined) {
  return useQuery({
    queryKey: ["inquiry", "by-listing", listingId],
    queryFn: () => inquiryApi.getMyListingInquiry(listingId!),
    enabled: Boolean(listingId),
    staleTime: 30_000,
  });
}

/**
 * Phase 17 — start (or return the existing open) pre-booking inquiry
 * thread for a listing. Idempotent on the server.
 */
export function useStartListingInquiry() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (listingId: string) => inquiryApi.startListingInquiry(listingId),
    onSuccess: (data) => {
      queryClient.setQueryData(["inquiry", "by-listing", data.listingId], data);
      queryClient.setQueryData(["inquiry", "by-session", data.sessionId], data);
      void queryClient.invalidateQueries({
        queryKey: ["inquiry", "tenant-inbox"],
      });
      void queryClient.invalidateQueries({
        queryKey: ["inquiry", "host-inbox"],
      });
    },
  });
}

/**
 * Phase 17 — fetch a thread by session id (works for both pre-booking
 * and deal-linked sessions).
 */
export function useInquirySession(sessionId: string | undefined) {
  return useQuery({
    queryKey: ["inquiry", "by-session", sessionId],
    queryFn: () => inquiryApi.getSessionThread(sessionId!),
    enabled: Boolean(sessionId),
    staleTime: 15_000,
  });
}

export function useSubmitSessionQuestion() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      sessionId,
      payload,
    }: {
      sessionId: string;
      payload: SubmitInquiryQuestionRequest;
    }) => inquiryApi.submitSessionQuestion(sessionId, payload),
    onSuccess: (_data, variables) => {
      void queryClient.invalidateQueries({
        queryKey: ["inquiry", "by-session", variables.sessionId],
      });
      void queryClient.invalidateQueries({
        queryKey: ["inquiry", "tenant-inbox"],
      });
      void queryClient.invalidateQueries({
        queryKey: ["inquiry", "host-inbox"],
      });
    },
  });
}

export function useSubmitSessionAnswer() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      sessionId,
      payload,
    }: {
      sessionId: string;
      payload: SubmitLandlordResponseRequest;
    }) => inquiryApi.submitSessionAnswer(sessionId, payload),
    onSuccess: (answer, variables) => {
      queryClient.setQueryData<InquiryDto>(
        ["inquiry", "by-session", variables.sessionId],
        (current) => {
          if (!current) return current;
          return {
            ...current,
            questions: current.questions.map((q) =>
              q.questionId === variables.payload.questionId
                ? {
                    ...q,
                    answer: {
                      answerId: answer.answerId,
                      responseType: answer.responseType,
                      answerValue: answer.answerValue,
                      answeredAt: answer.answeredAt,
                    },
                  }
                : q,
            ),
          };
        },
      );
      void queryClient.invalidateQueries({
        queryKey: ["inquiry", "by-session", variables.sessionId],
      });
      void queryClient.invalidateQueries({
        queryKey: ["inquiry", "host-inbox"],
      });
    },
  });
}

function invalidateOfferQueries(
  queryClient: ReturnType<typeof useQueryClient>,
  sessionId: string,
) {
  void queryClient.invalidateQueries({
    queryKey: ["inquiry", "by-session", sessionId],
  });
  void queryClient.invalidateQueries({ queryKey: ["inquiry", "host-inbox"] });
  void queryClient.invalidateQueries({ queryKey: ["inquiry", "tenant-inbox"] });
  void queryClient.invalidateQueries({ queryKey: ["reservation-preview"] });
}

export function useProposeInquiryOffer() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      sessionId,
      payload,
    }: {
      sessionId: string;
      payload: ProposeInquiryOfferRequest;
    }) => inquiryApi.proposeOffer(sessionId, payload),
    onSuccess: (_data, variables) => {
      invalidateOfferQueries(queryClient, variables.sessionId);
    },
  });
}

export function useAcceptInquiryOffer() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      sessionId,
      offerId,
    }: {
      sessionId: string;
      offerId: string;
    }) => inquiryApi.acceptOffer(sessionId, offerId),
    onSuccess: (_data, variables) => {
      invalidateOfferQueries(queryClient, variables.sessionId);
    },
  });
}

export function useCounterInquiryOffer() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      sessionId,
      offerId,
      payload,
    }: {
      sessionId: string;
      offerId: string;
      payload: CounterInquiryOfferRequest;
    }) => inquiryApi.counterOffer(sessionId, offerId, payload),
    onSuccess: (_data, variables) => {
      invalidateOfferQueries(queryClient, variables.sessionId);
    },
  });
}

export function useWithdrawAcceptedInquiryOffer() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (sessionId: string) =>
      inquiryApi.withdrawAcceptedOffer(sessionId),
    onSuccess: (_data, sessionId) => {
      invalidateOfferQueries(queryClient, sessionId);
    },
  });
}

function invalidatePartnerQueries(
  queryClient: ReturnType<typeof useQueryClient>,
  sessionId: string,
) {
  void queryClient.invalidateQueries({
    queryKey: ["inquiry", "by-session", sessionId],
  });
  void queryClient.invalidateQueries({ queryKey: ["inquiry", "host-inbox"] });
  void queryClient.invalidateQueries({ queryKey: ["inquiry", "tenant-inbox"] });
  void queryClient.invalidateQueries({ queryKey: ["inquiry", "partner-inbox"] });
}

export function useAddInquiryPartner() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      sessionId,
      organizationId,
    }: {
      sessionId: string;
      organizationId: string;
    }) => inquiryApi.addPartner(sessionId, organizationId),
    onSuccess: (data) => {
      queryClient.setQueryData(["inquiry", "by-session", data.sessionId], data);
      invalidatePartnerQueries(queryClient, data.sessionId);
    },
  });
}

export function useRemoveInquiryPartner() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (sessionId: string) => inquiryApi.removePartner(sessionId),
    onSuccess: (data) => {
      queryClient.setQueryData(["inquiry", "by-session", data.sessionId], data);
      invalidatePartnerQueries(queryClient, data.sessionId);
    },
  });
}

export function useStartPartnerListingInquiry() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      listingId,
      tenantUserId,
    }: {
      listingId: string;
      tenantUserId: string;
    }) => inquiryApi.startPartnerListingInquiry(listingId, tenantUserId),
    onSuccess: (data) => {
      queryClient.setQueryData(["inquiry", "by-session", data.sessionId], data);
      invalidatePartnerQueries(queryClient, data.sessionId);
    },
  });
}

export function usePartnerInquiries() {
  return useQuery({
    queryKey: ["inquiry", "partner-inbox"],
    queryFn: () => inquiryApi.listPartnerInquiries(),
    staleTime: 30_000,
    refetchInterval: 60_000,
  });
}

/**
 * Phase 17 — host inbox: every inquiry thread targeting one of the
 * calling user's listings. Polls on a short interval so new questions
 * surface without a manual refresh while the page is open.
 */
export function useHostInquiries() {
  return useQuery({
    queryKey: ["inquiry", "host-inbox"],
    queryFn: () => inquiryApi.listHostInquiries(),
    staleTime: 30_000,
    refetchInterval: 60_000,
  });
}

/**
 * Phase 17 — tenant inbox: every inquiry thread the calling user has
 * started. Mirrors {@link useHostInquiries} for the sent side.
 */
export function useMyInquiries() {
  return useQuery({
    queryKey: ["inquiry", "tenant-inbox"],
    queryFn: () => inquiryApi.listMyInquiries(),
    staleTime: 30_000,
    refetchInterval: 60_000,
  });
}
