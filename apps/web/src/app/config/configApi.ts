import { endpoints } from "@/api/endpoints";
import { http } from "@/api/http";
import type { PublicConfigDto } from "@/api/types";

export const configApi = {
  async getPublicConfig(): Promise<PublicConfigDto> {
    const response = await http.get<PublicConfigDto>(endpoints.platform.publicConfig);
    return response.data;
  },
};
