import { endpoints } from "@/api/endpoints";
import { http } from "@/api/http";
import type {
  ComplianceStatusDto,
  MonitoredViolationDto,
  MonitoredViolationCategory,
  ViolationDto,
  TrustLedgerEntryDto,
} from "@/api/types";

export const complianceApi = {
  async getDealComplianceStatus(dealId: string): Promise<ComplianceStatusDto> {
    const response = await http.get<ComplianceStatusDto>(
      endpoints.complianceMonitoring.status(dealId),
    );
    return response.data;
  },

  async getDealViolations(dealId: string): Promise<MonitoredViolationDto[]> {
    const response = await http.get<MonitoredViolationDto[]>(
      endpoints.complianceMonitoring.violations(dealId),
    );
    return response.data;
  },

  async detectViolation(
    dealId: string,
    category: MonitoredViolationCategory,
    cureDeadline?: string | null,
  ): Promise<MonitoredViolationDto> {
    const response = await http.post<MonitoredViolationDto>(
      endpoints.complianceMonitoring.detectViolation(dealId),
      { category, cureDeadline },
    );
    return response.data;
  },

  async cureViolation(
    dealId: string,
    violationId: string,
  ): Promise<MonitoredViolationDto> {
    const response = await http.put<MonitoredViolationDto>(
      endpoints.complianceMonitoring.cureViolation(dealId, violationId),
    );
    return response.data;
  },

  async getCoreViolations(dealId: string): Promise<ViolationDto[]> {
    const response = await http.get<ViolationDto[]>(
      endpoints.compliance.violations,
      { params: { dealId } },
    );
    return response.data;
  },

  async resolveViolation(id: string): Promise<void> {
    await http.put(endpoints.compliance.resolveViolation(id));
  },

  async dismissViolation(id: string): Promise<void> {
    await http.put(endpoints.compliance.dismissViolation(id));
  },

  async escalateViolation(id: string): Promise<void> {
    await http.put(endpoints.compliance.escalateViolation(id));
  },

  async getUserLedger(userId: string): Promise<TrustLedgerEntryDto[]> {
    const response = await http.get<TrustLedgerEntryDto[]>(
      endpoints.compliance.userLedger(userId),
    );
    return response.data;
  },

  async getDealLedger(dealId: string): Promise<TrustLedgerEntryDto[]> {
    const response = await http.get<{
      dealId: string;
      violations: unknown[];
      ledgerEntries: TrustLedgerEntryDto[];
    }>(endpoints.compliance.dealLedger(dealId));
    return response.data.ledgerEntries ?? [];
  },
};
