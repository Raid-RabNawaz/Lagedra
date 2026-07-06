import { endpoints } from "@/api/endpoints";
import { http } from "@/api/http";
import type {
  ChannelConnectionDto,
  ChannelListingMapDto,
  ChannelProviderDto,
  ChannelSyncResultDto,
  ConnectChannelRequest,
} from "@/api/types";

export const channelsApi = {
  async listProviders(): Promise<ChannelProviderDto[]> {
    const response = await http.get<ChannelProviderDto[]>(
      endpoints.channels.providers,
    );
    return response.data;
  },

  async listConnections(): Promise<ChannelConnectionDto[]> {
    const response = await http.get<ChannelConnectionDto[]>(
      endpoints.channels.list,
    );
    return response.data;
  },

  async connect(payload: ConnectChannelRequest): Promise<ChannelConnectionDto> {
    const response = await http.post<ChannelConnectionDto>(
      endpoints.channels.connect,
      payload,
    );
    return response.data;
  },

  async enable(id: string): Promise<void> {
    await http.post(endpoints.channels.enable(id));
  },

  async disable(id: string): Promise<void> {
    await http.post(endpoints.channels.disable(id));
  },

  async sync(id: string): Promise<ChannelSyncResultDto> {
    const response = await http.post<ChannelSyncResultDto>(
      endpoints.channels.sync(id),
    );
    return response.data;
  },

  async listListings(id: string): Promise<ChannelListingMapDto[]> {
    const response = await http.get<ChannelListingMapDto[]>(
      endpoints.channels.listings(id),
    );
    return response.data;
  },
};
