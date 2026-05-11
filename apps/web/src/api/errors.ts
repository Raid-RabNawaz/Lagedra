import { isAxiosError, type AxiosError } from "axios";
import type { ErrorResponse } from "@/api/types";

const DEFAULT_MESSAGE = "Something went wrong. Please try again.";

const FORBIDDEN_FALLBACK_BY_PREFIX: Record<string, string> = {
  "Application.": "You do not have access to this application.",
  "Checkout.": "You are not authorized to use this checkout.",
  "Cancel.": "You are not authorized to cancel this booking.",
  "DamageClaim.": "You are not authorized to file a damage claim for this deal.",
  "PaymentConfirmation.":
    "You do not have access to this deal's payment information.",
  "PaymentDetails.":
    "You do not have access to the host's payment details for this deal.",
  "BillingAccount.": "You do not have access to this deal's billing.",
  "Proration.": "You do not have access to this deal's proration quote.",
  "Inquiry.": "You do not have access to this deal's inquiry thread.",
};

const NOT_FOUND_FALLBACK_BY_PREFIX: Record<string, string> = {
  "Application.": "Application not found.",
  "Checkout.": "Checkout details not found for this deal.",
  "PaymentConfirmation.": "Payment information not found for this deal.",
  "BillingAccount.": "Billing account not found for this deal.",
  "Inquiry.": "No inquiry session found for this deal.",
};

const findFallback = (
  fallbacks: Record<string, string>,
  code: string,
): string | undefined => {
  for (const [prefix, message] of Object.entries(fallbacks)) {
    if (code.startsWith(prefix)) {
      return message;
    }
  }
  return undefined;
};

const isAxiosErrorResponse = (
  error: unknown,
): error is AxiosError<ErrorResponse> => isAxiosError<ErrorResponse>(error);

/**
 * Extract a user-facing message from an unknown error.
 *
 * Prefers, in order:
 * 1. The server's `detail`/`message` field.
 * 2. A friendly fallback inferred from the server's `error` code (so 403/404
 *    responses still show a meaningful message even when the backend only
 *    returns a code).
 * 3. The Axios error message.
 * 4. A standard `Error` message.
 * 5. A generic default.
 */
export const getApiErrorMessage = (
  error: unknown,
  fallback: string = DEFAULT_MESSAGE,
): string => {
  if (isAxiosErrorResponse(error)) {
    const data = error.response?.data;
    const status = error.response?.status;

    if (data?.detail) return data.detail;
    if (data?.message) return data.message;

    if (data?.error) {
      if (status === 403) {
        const friendly = findFallback(FORBIDDEN_FALLBACK_BY_PREFIX, data.error);
        if (friendly) return friendly;
        return "You do not have permission to perform this action.";
      }

      if (status === 404) {
        const friendly = findFallback(NOT_FOUND_FALLBACK_BY_PREFIX, data.error);
        if (friendly) return friendly;
      }

      return data.error;
    }

    if (status === 401) {
      return "Your session has expired. Please sign in again.";
    }
    if (status === 403) {
      return "You do not have permission to perform this action.";
    }
    if (status === 404) {
      return "The requested resource could not be found.";
    }

    return error.message || fallback;
  }

  if (error instanceof Error && error.message) {
    return error.message;
  }

  return fallback;
};

/**
 * Returns the HTTP status code for an axios error, or undefined if it cannot
 * be determined (e.g. network errors or non-axios exceptions).
 */
export const getApiErrorStatus = (error: unknown): number | undefined => {
  if (isAxiosErrorResponse(error)) {
    return error.response?.status;
  }
  return undefined;
};

export const isForbiddenError = (error: unknown): boolean =>
  getApiErrorStatus(error) === 403;

export const isNotFoundError = (error: unknown): boolean =>
  getApiErrorStatus(error) === 404;
