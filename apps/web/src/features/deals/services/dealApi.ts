import { endpoints } from "@/api/endpoints";
import { http } from "@/api/http";
import type { DealStayAccessDto, DealSummaryDto, DealPhaseFilter } from "@/api/types";

export const dealApi = {
  async getMyDeals(phase?: DealPhaseFilter): Promise<DealSummaryDto[]> {
    const response = await http.get<DealSummaryDto[]>(endpoints.deals.mine, {
      params: phase ? { phase } : undefined,
    });
    return response.data;
  },

  async getStayAccess(dealId: string): Promise<DealStayAccessDto> {
    const response = await http.get<DealStayAccessDto>(endpoints.deals.stayAccess(dealId));
    return response.data;
  },
};
