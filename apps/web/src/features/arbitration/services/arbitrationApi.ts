import { endpoints } from "@/api/endpoints";
import { http } from "@/api/http";
import type {
  CaseDto,
  DecisionDto,
  ArbitrationStatus,
  ArbitrationTier,
  ArbitrationCategory,
  ArbitrationFeeCheckoutDto,
  IssueDecisionRequest,
} from "@/api/types";

export const arbitrationApi = {
  async fileCase(
    dealId: string,
    tier: ArbitrationTier,
    category: ArbitrationCategory,
  ): Promise<CaseDto> {
    const response = await http.post<CaseDto>(endpoints.arbitration.fileCase, {
      dealId,
      tier,
      category,
    });
    return response.data;
  },

  async getCase(caseId: string): Promise<CaseDto> {
    const response = await http.get<CaseDto>(endpoints.arbitration.getCase(caseId));
    return response.data;
  },

  async createFilingFeeCheckout(
    caseId: string,
  ): Promise<ArbitrationFeeCheckoutDto> {
    const response = await http.post<ArbitrationFeeCheckoutDto>(
      endpoints.arbitration.filingFeeCheckout(caseId),
    );
    return response.data;
  },

  async listByStatus(status: ArbitrationStatus): Promise<CaseDto[]> {
    const response = await http.get<CaseDto[]>(endpoints.arbitration.list, {
      params: { status },
    });
    return response.data;
  },

  async attachEvidence(
    caseId: string,
    slotType: string,
    evidenceManifestId: string,
  ): Promise<void> {
    await http.post(endpoints.arbitration.attachEvidence(caseId), {
      slotType,
      evidenceManifestId,
    });
  },

  async markEvidenceComplete(caseId: string): Promise<void> {
    await http.post(endpoints.arbitration.markEvidenceComplete(caseId));
  },

  async beginReview(caseId: string): Promise<void> {
    await http.post(endpoints.arbitration.beginReview(caseId));
  },

  async assignArbitrator(
    caseId: string,
    arbitratorUserId: string,
    concurrentCaseCount?: number,
  ): Promise<void> {
    await http.post(endpoints.arbitration.assignArbitrator(caseId), {
      arbitratorUserId,
      ...(concurrentCaseCount !== undefined ? { concurrentCaseCount } : {}),
    });
  },

  async issueDecision(caseId: string, body: IssueDecisionRequest): Promise<DecisionDto> {
    const response = await http.post<DecisionDto>(
      endpoints.arbitration.issueDecision(caseId),
      body,
    );
    return response.data;
  },

  async closeCase(caseId: string): Promise<void> {
    await http.put(endpoints.arbitration.closeCase(caseId));
  },

  async appealCase(caseId: string, reason: string): Promise<void> {
    await http.post(endpoints.arbitration.appeal(caseId), { reason });
  },
};
