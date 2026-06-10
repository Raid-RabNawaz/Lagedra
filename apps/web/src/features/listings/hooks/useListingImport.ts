import { useMutation } from "@tanstack/react-query";
import { isAxiosError } from "axios";
import { listingApi } from "@/features/listings/services/listingApi";
import type { ImportedListingDraftDto } from "@/api/types";

type ImportInput = {
  url: string;
  hostAttestation: boolean;
};

function toFriendlyMessage(error: unknown): string {
  if (isAxiosError(error)) {
    const status = error.response?.status;
    if (status === 429) {
      return "You have reached the import limit (5 per hour, 30 per day). Please try again later.";
    }
    const detail = (error.response?.data as { detail?: string } | undefined)?.detail;
    if (detail) return detail;
    if (status === 400) {
      return "We could not import from that URL. Check the link and the confirmation box, then try again.";
    }
  }
  return "Something went wrong while importing. You can enter the listing details manually.";
}

/**
 * Wraps the import-from-url endpoint. Failures are surfaced as friendly
 * messages; the create wizard remains fully usable regardless of the outcome.
 */
export function useListingImport() {
  return useMutation<ImportedListingDraftDto, Error, ImportInput>({
    mutationFn: async ({ url, hostAttestation }) => {
      try {
        return await listingApi.importFromUrl(url, hostAttestation);
      } catch (error) {
        throw new Error(toFriendlyMessage(error));
      }
    },
  });
}
