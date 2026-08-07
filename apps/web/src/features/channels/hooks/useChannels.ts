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
    // Refetched even on failure: a rejected sync records the reason on the
    // connection, and that is worth showing on the card.
    onSettled: (_data, _error, connectionId) => {
      void queryClient.invalidateQueries({ queryKey: channelKeys.connections });
      void queryClient.invalidateQueries({
        queryKey: channelKeys.listings(connectionId),
      });
    },
  });
}

export function useDisconnectChannel() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (connectionId: string) => channelsApi.disconnect(connectionId),
    onSuccess: (_data, connectionId) => {
      void queryClient.invalidateQueries({ queryKey: channelKeys.connections });
      queryClient.removeQueries({ queryKey: channelKeys.listings(connectionId) });
    },
  });
}

/**
 * Asks the API where to send the host to authorize Lagedra in OwnerRez. Nothing
 * is created until OwnerRez calls back, so there is no cache to invalidate here.
 */
export function useStartOwnerRezOAuth() {
  return useMutation({
    mutationFn: () => channelsApi.startOwnerRezOAuth(),
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
