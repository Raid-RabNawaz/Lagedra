import { describe, expect, it } from "vitest";
import {
  blobLooksLikeHeic,
  isHeicMime,
  orderReviewDocuments,
  sniffImageType,
} from "./kycPreview";

describe("orderReviewDocuments", () => {
  it("puts the ID front beside the selfie, then the ID back", () => {
    const ordered = orderReviewDocuments([
      { documentType: "IdBack" as const },
      { documentType: "Selfie" as const },
      { documentType: "IdFront" as const },
    ]);

    expect(ordered.map((doc) => doc.documentType)).toEqual(["IdFront", "Selfie", "IdBack"]);
  });
});

describe("isHeicMime", () => {
  it("recognizes HEIC mime types and file names", () => {
    expect(isHeicMime("image/heic", "id.jpg")).toBe(true);
    expect(isHeicMime("application/octet-stream", "license.HEIC")).toBe(true);
    expect(isHeicMime("image/jpeg", "selfie.jpg")).toBe(false);
  });
});

describe("blobLooksLikeHeic", () => {
  it("detects an HEIC ftyp brand", async () => {
    const bytes = new Uint8Array(16);
    bytes.set([0, 0, 0, 0, 0x66, 0x74, 0x79, 0x70, 0x68, 0x65, 0x69, 0x63], 0);
    await expect(blobLooksLikeHeic(new Blob([bytes]))).resolves.toBe(true);
  });

  it("ignores JPEG bytes", async () => {
    const bytes = new Uint8Array([0xff, 0xd8, 0xff, 0xe0, 0, 0, 0, 0]);
    await expect(blobLooksLikeHeic(new Blob([bytes]))).resolves.toBe(false);
    expect(sniffImageType(bytes)).toBe("image/jpeg");
  });
});
