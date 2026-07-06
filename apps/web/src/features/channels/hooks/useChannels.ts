import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { channelsApi } from "@/features/channels/services/channelsApi";
import type { ConnectChannelRequest } from "@/api/types";

const channelKeys = {
  providers: ["channels", "providers"] as const,
  connections: ["channels", "connections"] as const,
  listings: (id: string) => ["channels", "listings", id] as const,
};

export function useChannelProviders() {
  return useQuery({
    queryKey: channelKeys.providers,
    queryFn: () => channelsApi.listProviders(),
    staleTime: 5 * 60_000,
  });
}

export function useChannelConnections() {
  return useQuery({
    queryKey: channelKeys.connections,
    queryFn: () => channelsApi.listConnections(),
    staleTime: 30_000,
  });
}

export function useChannelListings(connectionId: string | null) {
  return useQuery({
    queryKey: channelKeys.listings(connectionId ?? ""),
    queryFn: () => channelsApi.listListings(connectionId as string),
    enabled: Boolean(connectionId),
    staleTime: 15_000,
  });
}

export function useConnectChannel() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: ConnectChannelRequest) => channelsApi.connect(payload),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: channelKeys.connections });
    },
  });
}

export function useSyncChannel() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (connectionId: string) => channelsApi.sync(connectionId),
    onSuccess: (_data, connectionId) => {
      void queryClient.invalidateQueries({ queryKey: channelKeys.connections });
      void queryClient.invalidateQueries({
        queryKey: channelKeys.listings(connectionId),
      });
    },
  });
}

export function useSetChannelEnabled() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, enabled }: { id: string; enabled: boolean }) =>
      enabled ? channelsApi.enable(id) : channelsApi.disable(id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: channelKeys.connections });
    },
  });
}
