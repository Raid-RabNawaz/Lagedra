import { endpoints } from "@/api/endpoints";
import { http } from "@/api/http";
import type {
  VerificationStatusDto,
  StartKycRequest,
  CompleteKycRequest,
  BackgroundCheckConsentRequest,
  RiskViewDto,
  MyVerificationTierDto,
  KycDocumentDto,
  KycDocumentType,
  SubmitManualKycRequest,
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

  async getMyVerificationTier(): Promise<MyVerificationTierDto> {
    const response = await http.get<MyVerificationTierDto>(
      endpoints.me.verificationTier,
    );
    return response.data;
  },

  async uploadKycDocument(
    documentType: KycDocumentType,
    file: File | Blob,
    fileName?: string,
  ): Promise<KycDocumentDto> {
    const form = new FormData();
    form.append("documentType", documentType);
    form.append("file", file, fileName ?? (file instanceof File ? file.name : "capture.jpg"));
    const response = await http.post<KycDocumentDto>(
      endpoints.identity.manualKycDocuments,
      form,
      { headers: { "Content-Type": "multipart/form-data" } },
    );
    return response.data;
  },

  async getMyKycDocuments(): Promise<KycDocumentDto[]> {
    const response = await http.get<KycDocumentDto[]>(
      endpoints.identity.manualKycDocuments,
    );
    return response.data;
  },

  async submitManualKyc(
    payload: SubmitManualKycRequest,
  ): Promise<VerificationStatusDto> {
    const response = await http.post<VerificationStatusDto>(
      endpoints.identity.manualKycSubmit,
      payload,
    );
    return response.data;
  },
};
