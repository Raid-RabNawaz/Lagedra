import type { ImportedPhotoCandidateDto } from "@/api/types";
import { listingApi } from "@/features/listings/services/listingApi";
import { MAX_IMPORT_PHOTOS } from "./listingImportShared";

export type PhotoImportResult = {
  uploaded: number;
  failed: number;
};

/** Soft cap kept in sync with listingImportShared / the server-side importer. */
export { MAX_IMPORT_PHOTOS };

/** How many CDN fetch + uploadMedia pairs to run at once. */
const IMPORT_CONCURRENCY = 3;

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

async function uploadOne(
  listingId: string,
  photo: ImportedPhotoCandidateDto,
  index: number,
): Promise<boolean> {
  try {
    const response = await fetch(photo.url, { mode: "cors" });
    if (!response.ok) return false;

    const blob = await response.blob();
    if (!blob.type.startsWith("image/")) return false;

    const file = new File([blob], fileNameFromUrl(photo.url, index), { type: blob.type });
    await listingApi.uploadMedia(listingId, file, photo.altText ?? null);
    return true;
  } catch {
    return false;
  }
}

/**
 * Imports selected photo candidates into an already-created listing. Each photo
 * is downloaded and then re-uploaded through the existing media-upload endpoint
 * so it flows through the standard antivirus + EXIF-strip pipeline — raw
 * third-party URLs are never persisted. This is a no-op when nothing is
 * selected. Individual failures (for example a CDN that blocks cross-origin
 * reads) are counted and skipped rather than aborting the whole batch.
 *
 * Runs a small concurrency pool so Airbnb galleries (dozens of images) finish
 * much faster than a strict sequential loop without flooding the API.
 */
export async function importListingPhotos(
  listingId: string,
  photos: readonly ImportedPhotoCandidateDto[],
): Promise<PhotoImportResult> {
  if (!photos || photos.length === 0) {
    return { uploaded: 0, failed: 0 };
  }

  const capped = photos.slice(0, MAX_IMPORT_PHOTOS);
  let uploaded = 0;
  let failed = 0;
  let nextIndex = 0;

  async function worker() {
    while (nextIndex < capped.length) {
      const index = nextIndex;
      nextIndex += 1;
      const ok = await uploadOne(listingId, capped[index], index);
      if (ok) uploaded += 1;
      else failed += 1;
    }
  }

  const workers = Array.from(
    { length: Math.min(IMPORT_CONCURRENCY, capped.length) },
    () => worker(),
  );
  await Promise.all(workers);

  return { uploaded, failed };
}
