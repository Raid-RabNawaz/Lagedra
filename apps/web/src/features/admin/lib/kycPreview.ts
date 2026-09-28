import type { KycDocumentType } from "@/api/types";

const heicMimeTypes = new Set([
  "image/heic",
  "image/heif",
  "image/heic-sequence",
  "image/heif-sequence",
]);

const heicBrands = new Set(["heic", "heix", "hevc", "hevx", "heim", "heis", "mif1", "msf1"]);

const previewOrder: KycDocumentType[] = ["IdFront", "Selfie", "IdBack"];

export function orderReviewDocuments<T extends { documentType: KycDocumentType }>(documents: T[]): T[] {
  return [...documents].sort((a, b) => rank(a.documentType) - rank(b.documentType));
}

function rank(documentType: KycDocumentType): number {
  const index = previewOrder.indexOf(documentType);
  return index === -1 ? previewOrder.length : index;
}

export function isHeicMime(mimeType: string, fileName: string): boolean {
  if (heicMimeTypes.has(mimeType.trim().toLowerCase())) return true;
  const name = fileName.trim().toLowerCase();
  return name.endsWith(".heic") || name.endsWith(".heif");
}

export async function blobLooksLikeHeic(blob: Blob): Promise<boolean> {
  if (blob.size < 12) return false;
  const header = new Uint8Array(await blob.slice(0, 16).arrayBuffer());
  const box = ascii(header, 4, 4);
  if (box !== "ftyp") return false;
  return heicBrands.has(ascii(header, 8, 4));
}

export function sniffImageType(bytes: Uint8Array): string | null {
  if (bytes.length >= 3 && bytes[0] === 0xff && bytes[1] === 0xd8 && bytes[2] === 0xff) {
    return "image/jpeg";
  }
  if (
    bytes.length >= 8 &&
    bytes[0] === 0x89 &&
    bytes[1] === 0x50 &&
    bytes[2] === 0x4e &&
    bytes[3] === 0x47
  ) {
    return "image/png";
  }
  if (
    bytes.length >= 12 &&
    bytes[0] === 0x52 &&
    bytes[1] === 0x49 &&
    bytes[2] === 0x46 &&
    bytes[3] === 0x46 &&
    bytes[8] === 0x57 &&
    bytes[9] === 0x45 &&
    bytes[10] === 0x42 &&
    bytes[11] === 0x50
  ) {
    return "image/webp";
  }
  return null;
}

/**
 * Turns a stored KYC file into a blob the browser can paint. Phone uploads
 * are often HEIC, which Chrome and Firefox will not decode in an img tag.
 */
export async function prepareKycPreviewBlob(
  blob: Blob,
  mimeType: string,
  fileName: string,
): Promise<Blob> {
  if (isHeicMime(mimeType, fileName) || (await blobLooksLikeHeic(blob))) {
    return convertHeicToJpeg(blob);
  }

  const header = new Uint8Array(await blob.slice(0, 16).arrayBuffer());
  const sniffed = sniffImageType(header);
  const declared = mimeType.trim().toLowerCase();
  const type = sniffed ?? (declared.startsWith("image/") ? declared : blob.type);
  if (!type || type === blob.type) return blob;
  return new Blob([blob], { type });
}

async function convertHeicToJpeg(blob: Blob): Promise<Blob> {
  const { default: heic2any } = await import("heic2any");
  const converted = await heic2any({ blob, toType: "image/jpeg", quality: 0.92 });
  const jpeg = Array.isArray(converted) ? converted[0] : converted;
  if (!(jpeg instanceof Blob)) {
    throw new Error("Could not convert HEIC photo.");
  }
  return jpeg.type ? jpeg : new Blob([jpeg], { type: "image/jpeg" });
}

function ascii(bytes: Uint8Array, offset: number, length: number): string {
  let value = "";
  for (let i = 0; i < length; i += 1) {
    value += String.fromCharCode(bytes[offset + i] ?? 0);
  }
  return value;
}
