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
};
