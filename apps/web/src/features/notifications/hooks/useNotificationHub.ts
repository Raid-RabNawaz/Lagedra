import { useEffect } from "react";
import {
  HubConnectionBuilder,
  LogLevel,
  HubConnectionState,
  type HubConnection,
  type IRetryPolicy,
  type RetryContext,
} from "@microsoft/signalr";
import { useQueryClient, type QueryClient } from "@tanstack/react-query";
import { appConfig } from "@/app/config";
import { authStore } from "@/app/auth/authStore";
import {
  ALL_NOTIFICATIONS_KEY,
  NOTIFICATIONS_KEY,
  UNREAD_COUNT_KEY,
} from "./useNotifications";
import type { InAppNotificationDto } from "@/api/types";

let singletonConnection: HubConnection | null = null;
let activeSubscribers = 0;
let starting = false;
let startRetryTimer: ReturnType<typeof setTimeout> | null = null;
let recoveryListenersBound = false;

/**
 * Reconnects indefinitely with a capped backoff. The default fixed-array policy
 * stops retrying after a handful of attempts, so a long network outage (e.g. the
 * device sleeping, which surfaces as ERR_NETWORK_IO_SUSPENDED) would leave the
 * hub permanently disconnected. Returning a number forever keeps it trying.
 */
const indefiniteRetryPolicy: IRetryPolicy = {
  nextRetryDelayInMilliseconds(ctx: RetryContext): number {
    switch (ctx.previousRetryCount) {
      case 0:
        return 0;
      case 1:
        return 2000;
      case 2:
        return 5000;
      case 3:
        return 10000;
      default:
        return 30000;
    }
  },
};

/**
 * Starts the connection if it is fully disconnected and still needed. SignalR's
 * automatic reconnect only covers drops after a successful connection; an
 * initial start() failure (or a closed connection) needs an explicit restart,
 * which this provides. Safe to call repeatedly.
 */
function ensureStarted(): void {
  const conn = singletonConnection;
  if (!conn || activeSubscribers <= 0) return;
  if (!authStore.getState().accessToken) return;
  if (conn.state !== HubConnectionState.Disconnected) return;
  if (starting) return;

  starting = true;
  conn
    .start()
    .catch(() => scheduleStart(5000))
    .finally(() => {
      starting = false;
    });
}

function scheduleStart(delayMs: number): void {
  if (startRetryTimer) return;
  startRetryTimer = setTimeout(() => {
    startRetryTimer = null;
    ensureStarted();
  }, delayMs);
}

function bindRecoveryListeners(): void {
  if (recoveryListenersBound || typeof window === "undefined") return;
  recoveryListenersBound = true;

  // When the network returns or the tab becomes visible again (the common
  // wake-from-sleep case), retry immediately instead of waiting for a timer.
  window.addEventListener("online", ensureStarted);
  document.addEventListener("visibilitychange", () => {
    if (document.visibilityState === "visible") ensureStarted();
  });
}

function getOrCreateConnection(): HubConnection {
  if (!singletonConnection) {
    const connection = new HubConnectionBuilder()
      .withUrl(`${appConfig.apiBaseUrl}/hubs/notifications`, {
        accessTokenFactory: () =>
          authStore.getState().accessToken ?? "",
      })
      .withAutomaticReconnect(indefiniteRetryPolicy)
      .configureLogging(LogLevel.Warning)
      .build();

    // If automatic reconnect ever exhausts/closes, retry from scratch so a long
    // outage does not permanently kill live notifications.
    connection.onclose(() => scheduleStart(2000));

    singletonConnection = connection;
    bindRecoveryListeners();
  }
  return singletonConnection;
}

function bindQueryClient(connection: HubConnection, qc: QueryClient) {
  connection.off("ReceiveNotification");
  connection.on("ReceiveNotification", (notification: InAppNotificationDto) => {
    // The push DTO omits isRead (a pushed notification is unread by
    // definition); normalize so list consumers can rely on the field.
    const unread: InAppNotificationDto = { ...notification, isRead: notification.isRead ?? false };

    qc.setQueryData<InAppNotificationDto[]>(
      [...NOTIFICATIONS_KEY],
      (old) => (old ? [unread, ...old] : [unread]),
    );
    qc.setQueryData<InAppNotificationDto[]>(
      [...ALL_NOTIFICATIONS_KEY],
      (old) => (old ? [unread, ...old] : [unread]),
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

    ensureStarted();

    return () => {
      activeSubscribers--;
      if (activeSubscribers <= 0) {
        activeSubscribers = 0;
        if (startRetryTimer) {
          clearTimeout(startRetryTimer);
          startRetryTimer = null;
        }
        const conn = singletonConnection;
        singletonConnection = null;
        if (conn && conn.state !== HubConnectionState.Disconnected) {
          conn.stop().catch(() => {});
        }
      }
    };
  }, [queryClient]);
}
