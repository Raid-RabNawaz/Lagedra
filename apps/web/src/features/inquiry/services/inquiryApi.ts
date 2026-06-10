import { endpoints } from "@/api/endpoints";
import { http } from "@/api/http";
import type {
  InquiryDto,
  InquiryQuestionDto,
  InquiryAnswerDto,
  PredefinedQuestionDto,
  InquiryCategory,
  SubmitInquiryQuestionRequest,
  SubmitLandlordResponseRequest,
  HostInquirySummaryDto,
  TenantInquirySummaryDto,
} from "@/api/types";

export const inquiryApi = {
  async getThread(dealId: string): Promise<InquiryDto> {
    const response = await http.get<InquiryDto>(endpoints.inquiry.thread(dealId));
    return response.data;
  },

  async requestUnlock(dealId: string): Promise<InquiryDto> {
    const response = await http.post<InquiryDto>(
      endpoints.inquiry.requestUnlock(dealId),
    );
    return response.data;
  },

  async approveUnlock(dealId: string): Promise<InquiryDto> {
    const response = await http.post<InquiryDto>(
      endpoints.inquiry.approveUnlock(dealId),
    );
    return response.data;
  },

  async lock(dealId: string): Promise<InquiryDto> {
    const response = await http.post<InquiryDto>(endpoints.inquiry.lock(dealId));
    return response.data;
  },

  async submitQuestion(
    dealId: string,
    payload: SubmitInquiryQuestionRequest,
  ): Promise<InquiryQuestionDto> {
    const response = await http.post<InquiryQuestionDto>(
      endpoints.inquiry.submitQuestion(dealId),
      payload,
    );
    return response.data;
  },

  async submitAnswer(
    dealId: string,
    payload: SubmitLandlordResponseRequest,
  ): Promise<InquiryAnswerDto> {
    const response = await http.post<InquiryAnswerDto>(
      endpoints.inquiry.submitAnswer(dealId),
      payload,
    );
    return response.data;
  },

  async close(dealId: string): Promise<void> {
    await http.post(endpoints.inquiry.close(dealId));
  },

  async listPredefinedQuestions(
    category?: InquiryCategory,
  ): Promise<PredefinedQuestionDto[]> {
    const response = await http.get<PredefinedQuestionDto[]>(
      endpoints.inquiry.predefinedQuestions,
      { params: category ? { category } : undefined },
    );
    return response.data;
  },

  // Phase 17 — pre-booking inquiry endpoints (listing-scoped).

  async startListingInquiry(listingId: string): Promise<InquiryDto> {
    const response = await http.post<InquiryDto>(
      endpoints.inquiry.startListingInquiry(listingId),
    );
    return response.data;
  },

  async getMyListingInquiry(listingId: string): Promise<InquiryDto | null> {
    try {
      const response = await http.get<InquiryDto>(
        endpoints.inquiry.myListingInquiry(listingId),
      );
      return response.data;
    } catch (err: unknown) {
      const status = (err as { response?: { status?: number } })?.response?.status;
      if (status === 404) {
        return null;
      }
      throw err;
    }
  },

  // Phase 17 — session-id-based routes; work for both pre-booking
  // (listing-scoped) and deal-linked threads.

  async getSessionThread(sessionId: string): Promise<InquiryDto> {
    const response = await http.get<InquiryDto>(
      endpoints.inquiry.sessionThread(sessionId),
    );
    return response.data;
  },

  async submitSessionQuestion(
    sessionId: string,
    payload: SubmitInquiryQuestionRequest,
  ): Promise<InquiryQuestionDto> {
    const response = await http.post<InquiryQuestionDto>(
      endpoints.inquiry.sessionQuestions(sessionId),
      payload,
    );
    return response.data;
  },

  async submitSessionAnswer(
    sessionId: string,
    payload: SubmitLandlordResponseRequest,
  ): Promise<InquiryAnswerDto> {
    const response = await http.post<InquiryAnswerDto>(
      endpoints.inquiry.sessionAnswers(sessionId),
      payload,
    );
    return response.data;
  },

  // Phase 17 — host inbox: every inquiry thread targeting one of the
  // calling user's listings, ordered by most recent activity.
  async listHostInquiries(): Promise<HostInquirySummaryDto[]> {
    const response = await http.get<HostInquirySummaryDto[]>(
      endpoints.inquiry.hostInbox,
    );
    return response.data;
  },

  // Phase 17 — tenant inbox: every inquiry thread the calling user has
  // started, across every listing. Mirrors `listHostInquiries` for the
  // sent side of the conversation.
  async listMyInquiries(): Promise<TenantInquirySummaryDto[]> {
    const response = await http.get<TenantInquirySummaryDto[]>(
      endpoints.inquiry.myInbox,
    );
    return response.data;
  },
};
