import { useEffect } from "react";
import {
  HubConnectionBuilder,
  LogLevel,
  HubConnectionState,
  type HubConnection,
} from "@microsoft/signalr";
import { useQueryClient, type QueryClient } from "@tanstack/react-query";
import { appConfig } from "@/app/config";
import { authStore } from "@/app/auth/authStore";
import { NOTIFICATIONS_KEY, UNREAD_COUNT_KEY } from "./useNotifications";
import type { InAppNotificationDto } from "@/api/types";

let singletonConnection: HubConnection | null = null;
let activeSubscribers = 0;

function getOrCreateConnection(): HubConnection {
  if (!singletonConnection) {
    singletonConnection = new HubConnectionBuilder()
      .withUrl(`${appConfig.apiBaseUrl}/hubs/notifications`, {
        accessTokenFactory: () =>
          authStore.getState().accessToken ?? "",
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(LogLevel.Warning)
      .build();
  }
  return singletonConnection;
}

function bindQueryClient(connection: HubConnection, qc: QueryClient) {
  connection.off("ReceiveNotification");
  connection.on("ReceiveNotification", (notification: InAppNotificationDto) => {
    qc.setQueryData<InAppNotificationDto[]>(
      [...NOTIFICATIONS_KEY],
      (old) => (old ? [notification, ...old] : [notification]),
    );
    qc.setQueryData<number>(
      [...UNREAD_COUNT_KEY],
      (old) => (old ?? 0) + 1,
    );
  });
}

export function useNotificationHub() {
  const queryClient = useQueryClient();

  useEffect(() => {
    const token = authStore.getState().accessToken;
    if (!token) return;

    const connection = getOrCreateConnection();
    bindQueryClient(connection, queryClient);
    activeSubscribers++;

    if (connection.state === HubConnectionState.Disconnected) {
      connection.start().catch(() => {});
    }

    return () => {
      activeSubscribers--;
      if (activeSubscribers <= 0) {
        activeSubscribers = 0;
        const conn = singletonConnection;
        singletonConnection = null;
        if (conn && conn.state !== HubConnectionState.Disconnected) {
          conn.stop().catch(() => {});
        }
      }
    };
  }, [queryClient]);
}
