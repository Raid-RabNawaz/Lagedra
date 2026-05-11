import { isAxiosError } from "axios";

export type FriendlyError = {
  title: string;
  message: string;
  status?: number;
  /** Stable, machine-readable code from the API, when present. */
  code?: string;
};

const STATUS_TITLES: Record<number, string> = {
  400: "Invalid request",
  401: "You're signed out",
  403: "You don't have access",
  404: "Not found",
  409: "Conflict",
  422: "Couldn't process this",
  429: "Too many requests",
  451: "Consent required",
  500: "Something went wrong",
  502: "Service unavailable",
  503: "Service unavailable",
  504: "Service unavailable",
};

const DEFAULT_MESSAGE_BY_STATUS: Record<number, string> = {
  400: "The request was rejected. Please double-check the information and try again.",
  401: "Please sign in again to continue.",
  403: "You don't have permission to view this content.",
  404: "We couldn't find what you were looking for.",
  409: "This change conflicts with the current state. Please refresh and try again.",
  422: "Some of the information provided isn't valid.",
  429: "You've made too many requests. Please wait a moment and try again.",
  500: "An unexpected error occurred on our end. Please try again in a moment.",
  502: "We're having trouble reaching one of our services. Please try again shortly.",
  503: "We're having trouble reaching one of our services. Please try again shortly.",
  504: "The request took too long to complete. Please try again shortly.",
};

const NETWORK_ERROR: FriendlyError = {
  title: "Can't reach the server",
  message:
    "Check your internet connection and try again. If the problem persists, our service may be temporarily unavailable.",
};

const UNKNOWN_ERROR: FriendlyError = {
  title: "Something went wrong",
  message: "An unexpected error occurred. Please try again.",
};

/**
 * Converts any error (axios, native Error, unknown) into a user-friendly
 * { title, message, status?, code? } shape. Never throws.
 */
export function toFriendlyError(error: unknown): FriendlyError {
  if (!error) return UNKNOWN_ERROR;

  if (isAxiosError(error)) {
    if (!error.response) {
      return NETWORK_ERROR;
    }

    const status = error.response.status;
    const data = error.response.data as
      | { title?: string; detail?: string; message?: string; error?: string; errors?: unknown }
      | string
      | undefined;

    let serverMessage: string | undefined;
    let code: string | undefined;

    if (typeof data === "string" && data.trim().length > 0) {
      serverMessage = data;
    } else if (data && typeof data === "object") {
      serverMessage =
        data.detail ||
        data.message ||
        data.title ||
        (typeof data.error === "string" ? data.error : undefined);
      if (typeof (data as { code?: string }).code === "string") {
        code = (data as { code?: string }).code;
      }
    }

    return {
      title: STATUS_TITLES[status] ?? "Something went wrong",
      message:
        serverMessage ??
        DEFAULT_MESSAGE_BY_STATUS[status] ??
        `The server responded with status ${status}.`,
      status,
      code,
    };
  }

  if (error instanceof Error) {
    return {
      title: UNKNOWN_ERROR.title,
      message: error.message || UNKNOWN_ERROR.message,
    };
  }

  if (typeof error === "string" && error.trim().length > 0) {
    return { title: UNKNOWN_ERROR.title, message: error };
  }

  return UNKNOWN_ERROR;
}

export function extractErrorMessage(error: unknown): string {
  return toFriendlyError(error).message;
}
