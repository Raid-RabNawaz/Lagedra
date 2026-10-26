import type { ImportedPhotoCandidateDto } from "@/api/types";
import { listingApi } from "@/features/listings/services/listingApi";
import { MAX_IMPORT_PHOTOS } from "./listingImportShared";

export type PhotoImportResult = {
  uploaded: number;
  failed: number;
};

/** Soft cap kept in sync with listingImportShared / the server-side importer. */
export { MAX_IMPORT_PHOTOS };

/**
 * Imports selected photo candidates into an already-created listing.
 *
 * The server fetches each URL and stores it in the listings bucket. A browser
 * fetch cannot do this for Airbnb and most other CDNs, which omit CORS headers,
 * so those photos never reached the gallery and had nothing to preview.
 * Individual fetch failures are counted and skipped rather than aborting the
 * listing that was just created.
 */
export async function importListingPhotos(
  listingId: string,
  photos: readonly ImportedPhotoCandidateDto[],
): Promise<PhotoImportResult> {
  if (!photos || photos.length === 0) {
    return { uploaded: 0, failed: 0 };
  }

  const urls = photos
    .slice(0, MAX_IMPORT_PHOTOS)
    .map((photo) => photo.url)
    .filter((url) => url.trim().length > 0);

  if (urls.length === 0) {
    return { uploaded: 0, failed: 0 };
  }

  try {
    return await listingApi.importPhotosFromUrls(listingId, urls);
  } catch {
    return { uploaded: 0, failed: urls.length };
  }
}
