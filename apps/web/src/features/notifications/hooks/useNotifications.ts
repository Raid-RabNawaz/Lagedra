import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { notificationApi } from "@/features/notifications/services/notificationApi";
import type { UpdatePreferencesRequest } from "@/api/types";

export const ALL_NOTIFICATIONS_KEY = ["notifications", "all"] as const;
export const NOTIFICATIONS_KEY = ["notifications", "unread"] as const;
export const UNREAD_COUNT_KEY = ["notifications", "unreadCount"] as const;

export function useAllNotifications() {
  return useQuery({
    queryKey: [...ALL_NOTIFICATIONS_KEY],
    queryFn: () => notificationApi.getAll(),
    staleTime: 30_000,
  });
}

export function useUnreadNotifications() {
  return useQuery({
    queryKey: [...NOTIFICATIONS_KEY],
    queryFn: () => notificationApi.getUnread(),
    staleTime: 30_000,
    refetchInterval: 60_000,
  });
}

export function useUnreadCount() {
  return useQuery({
    queryKey: [...UNREAD_COUNT_KEY],
    queryFn: () => notificationApi.getUnreadCount(),
    staleTime: 15_000,
    refetchInterval: 30_000,
  });
}

export function useMarkRead() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (notificationId: string) =>
      notificationApi.markRead(notificationId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: [...ALL_NOTIFICATIONS_KEY] });
      void queryClient.invalidateQueries({ queryKey: [...NOTIFICATIONS_KEY] });
      void queryClient.invalidateQueries({ queryKey: [...UNREAD_COUNT_KEY] });
    },
  });
}

export function useMarkAllRead() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => notificationApi.markAllRead(),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: [...ALL_NOTIFICATIONS_KEY] });
      void queryClient.invalidateQueries({ queryKey: [...NOTIFICATIONS_KEY] });
      void queryClient.invalidateQueries({ queryKey: [...UNREAD_COUNT_KEY] });
    },
  });
}

export function useNotificationPreferences(userId: string | undefined) {
  return useQuery({
    queryKey: ["notifications", "preferences", userId],
    queryFn: () => notificationApi.getPreferences(userId!),
    enabled: Boolean(userId),
    staleTime: 60_000,
  });
}

export function useUpdateNotificationPreferences() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      userId,
      payload,
    }: {
      userId: string;
      payload: UpdatePreferencesRequest;
    }) => notificationApi.updatePreferences(userId, payload),
    onSuccess: (_data, variables) => {
      void queryClient.invalidateQueries({
        queryKey: ["notifications", "preferences", variables.userId],
      });
    },
  });
}
