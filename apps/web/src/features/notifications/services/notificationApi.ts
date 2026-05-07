import { endpoints } from "@/api/endpoints";
import { http } from "@/api/http";
import type {
  InAppNotificationDto,
  NotificationPreferencesDto,
  UpdatePreferencesRequest,
} from "@/api/types";

export const notificationApi = {
  async getAll(limit = 100): Promise<InAppNotificationDto[]> {
    const response = await http.get<InAppNotificationDto[]>(
      endpoints.notifications.all,
      { params: { limit } },
    );
    return response.data;
  },

  async getUnread(limit = 50): Promise<InAppNotificationDto[]> {
    const response = await http.get<InAppNotificationDto[]>(
      endpoints.notifications.unread,
      { params: { limit } },
    );
    return response.data;
  },

  async getUnreadCount(): Promise<number> {
    const response = await http.get<{ count: number }>(
      endpoints.notifications.unreadCount,
    );
    return response.data.count;
  },

  async markRead(notificationId: string): Promise<void> {
    await http.post(endpoints.notifications.markRead(notificationId));
  },

  async markAllRead(): Promise<void> {
    await http.post(endpoints.notifications.markAllRead);
  },

  async getPreferences(userId: string): Promise<NotificationPreferencesDto> {
    const response = await http.get<NotificationPreferencesDto>(
      endpoints.notifications.preferences(userId),
    );
    return response.data;
  },

  async updatePreferences(
    userId: string,
    payload: UpdatePreferencesRequest,
  ): Promise<void> {
    await http.put(endpoints.notifications.preferences(userId), payload);
  },
};
