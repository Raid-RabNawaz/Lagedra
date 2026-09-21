import { listingApi } from "@/features/listings/services/listingApi";

export type PhotoFileUploadResult = {
  uploaded: number;
  failed: number;
};

/** How many device-file uploads to run at once. */
const UPLOAD_CONCURRENCY = 3;

/**
 * Uploads host-selected image files through the existing media-upload
 * endpoint so each file still goes through antivirus + EXIF-strip. Failures
 * are counted and skipped rather than aborting the rest of the batch.
 */
export async function uploadListingPhotoFiles(
  listingId: string,
  files: readonly File[],
  options?: {
    caption?: string | null;
    onProgress?: (completed: number, total: number) => void;
  },
): Promise<PhotoFileUploadResult> {
  if (files.length === 0) {
    return { uploaded: 0, failed: 0 };
  }

  const caption = options?.caption ?? null;
  let uploaded = 0;
  let failed = 0;
  let nextIndex = 0;
  let completed = 0;
  const total = files.length;

  async function worker() {
    while (nextIndex < files.length) {
      const index = nextIndex;
      nextIndex += 1;
      try {
        await listingApi.uploadMedia(listingId, files[index], caption);
        uploaded += 1;
      } catch {
        failed += 1;
      }
      completed += 1;
      options?.onProgress?.(completed, total);
    }
  }

  await Promise.all(
    Array.from(
      { length: Math.min(UPLOAD_CONCURRENCY, files.length) },
      () => worker(),
    ),
  );

  return { uploaded, failed };
}

export function imageFilesFromList(list: FileList | null): File[] {
  if (!list || list.length === 0) return [];
  const files: File[] = [];
  for (let i = 0; i < list.length; i += 1) {
    const file = list.item(i);
    if (
      file &&
      (file.type.startsWith("image/") ||
        /\.(jpe?g|png|gif|webp|heic|heif)$/i.test(file.name))
    ) {
      files.push(file);
    }
  }
  return files;
}
