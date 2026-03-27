import { endpoints } from "@/api/endpoints";
import { http } from "@/api/http";
import type {
  ApproveApplicationRequest,
  DealApplicationDto,
  SubmitApplicationRequest,
} from "@/api/types";

export const applicationApi = {
  async listMine(): Promise<DealApplicationDto[]> {
    const response = await http.get<DealApplicationDto[]>(endpoints.applications.mine);
    return response.data;
  },

  async submit(payload: SubmitApplicationRequest): Promise<DealApplicationDto> {
    const response = await http.post<DealApplicationDto>(
      endpoints.applications.submit,
      payload,
    );
    return response.data;
  },

  async getById(id: string): Promise<DealApplicationDto> {
    const response = await http.get<DealApplicationDto>(
      endpoints.applications.detail(id),
    );
    return response.data;
  },

  async listForListing(listingId: string): Promise<DealApplicationDto[]> {
    const response = await http.get<DealApplicationDto[]>(
      endpoints.applications.forListing(listingId),
    );
    return response.data;
  },

  async approve(id: string, payload: ApproveApplicationRequest): Promise<DealApplicationDto> {
    const response = await http.post<DealApplicationDto>(
      endpoints.applications.approve(id),
      payload,
    );
    return response.data;
  },

  async reject(id: string): Promise<DealApplicationDto> {
    const response = await http.post<DealApplicationDto>(
      endpoints.applications.reject(id),
    );
    return response.data;
  },
};
