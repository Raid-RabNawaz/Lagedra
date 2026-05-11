import { endpoints } from "@/api/endpoints";
import { http } from "@/api/http";
import type {
  VerificationStatusDto,
  StartKycRequest,
  CompleteKycRequest,
  BackgroundCheckConsentRequest,
  RiskViewDto,
} from "@/api/types";

export const verificationApi = {
  async getStatus(userId: string): Promise<VerificationStatusDto> {
    const response = await http.get<VerificationStatusDto>(
      endpoints.identity.status(userId),
    );
    return response.data;
  },

  async startKyc(payload: StartKycRequest): Promise<VerificationStatusDto> {
    const response = await http.post<VerificationStatusDto>(
      endpoints.identity.startKyc,
      payload,
    );
    return response.data;
  },

  async completeKyc(
    payload: CompleteKycRequest,
  ): Promise<VerificationStatusDto> {
    const response = await http.post<VerificationStatusDto>(
      endpoints.identity.completeKyc,
      payload,
    );
    return response.data;
  },

  async submitBackgroundCheckConsent(
    payload: BackgroundCheckConsentRequest,
  ): Promise<void> {
    await http.post(endpoints.verification.backgroundCheckConsent, payload);
  },

  async getRiskView(userId: string): Promise<RiskViewDto> {
    const response = await http.get<RiskViewDto>(endpoints.risk.view(userId));
    return response.data;
  },
};
