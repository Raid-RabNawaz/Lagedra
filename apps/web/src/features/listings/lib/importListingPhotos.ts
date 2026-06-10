import type { ImportedPhotoCandidateDto } from "@/api/types";
import { listingApi } from "@/features/listings/services/listingApi";

export type PhotoImportResult = {
  uploaded: number;
  failed: number;
};

function fileNameFromUrl(url: string, index: number): string {
  try {
    const { pathname } = new URL(url);
    const last = pathname.split("/").filter(Boolean).pop();
    if (last && /\.(jpe?g|png|gif|webp|heic|heif)$/i.test(last)) {
      return last;
    }
  } catch {
    // Fall through to a generated name.
  }
  return `imported-photo-${index + 1}.jpg`;
}

/**
 * Imports selected photo candidates into an already-created listing. Each photo
 * is downloaded and then re-uploaded through the existing media-upload endpoint
 * so it flows through the standard antivirus + EXIF-strip pipeline — raw
 * third-party URLs are never persisted. This is a no-op when nothing is
 * selected. Individual failures (for example a CDN that blocks cross-origin
 * reads) are counted and skipped rather than aborting the whole batch.
 */
export async function importListingPhotos(
  listingId: string,
  photos: readonly ImportedPhotoCandidateDto[],
): Promise<PhotoImportResult> {
  if (!photos || photos.length === 0) {
    return { uploaded: 0, failed: 0 };
  }

  let uploaded = 0;
  let failed = 0;

  for (let index = 0; index < photos.length; index += 1) {
    const photo = photos[index];
    try {
      const response = await fetch(photo.url, { mode: "cors" });
      if (!response.ok) {
        failed += 1;
        continue;
      }

      const blob = await response.blob();
      if (!blob.type.startsWith("image/")) {
        failed += 1;
        continue;
      }

      const file = new File([blob], fileNameFromUrl(photo.url, index), { type: blob.type });
      await listingApi.uploadMedia(listingId, file, photo.altText ?? null);
      uploaded += 1;
    } catch {
      failed += 1;
    }
  }

  return { uploaded, failed };
}
