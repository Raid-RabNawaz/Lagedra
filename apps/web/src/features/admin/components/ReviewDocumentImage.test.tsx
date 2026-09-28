/**
 * @vitest-environment jsdom
 */
import { act, type ReactNode } from "react";
import { createRoot, type Root } from "react-dom/client";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { ManualVerificationDocumentDto } from "@/api/types";
import { ReviewDocumentImage } from "./ReviewDocumentImage";

const getManualVerificationDocument = vi.fn();

vi.mock("@/features/admin/services/adminApi", () => ({
  adminApi: {
    getManualVerificationDocument: (...args: unknown[]) => getManualVerificationDocument(...args),
  },
}));

const pngBytes = Uint8Array.from(
  atob(
    "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==",
  ),
  (char) => char.charCodeAt(0),
);

const doc: ManualVerificationDocumentDto = {
  documentType: "IdFront",
  fileName: "id-front.png",
  mimeType: "image/png",
  uploadedAt: "2026-09-23T00:05:09Z",
  downloadUrl: "https://example.invalid/should-not-be-used",
};

describe("ReviewDocumentImage", () => {
  let root: Root | null = null;
  let container: HTMLDivElement | null = null;

  beforeEach(() => {
    getManualVerificationDocument.mockReset();
    URL.createObjectURL = vi.fn(() => "blob:kyc-preview");
    URL.revokeObjectURL = vi.fn();
    container = document.createElement("div");
    document.body.appendChild(container);
    root = createRoot(container);
  });

  afterEach(() => {
    act(() => {
      root?.unmount();
    });
    container?.remove();
    root = null;
    container = null;
  });

  it("renders the fetched photo instead of the storage link", async () => {
    getManualVerificationDocument.mockResolvedValue(new Blob([pngBytes], { type: "image/png" }));

    await act(async () => {
      root?.render((<ReviewDocumentImage profileId="profile-1" doc={doc} />) as ReactNode);
    });

    for (let attempt = 0; attempt < 20; attempt += 1) {
      if (container?.querySelector("img")) break;
      await act(async () => {
        await new Promise((resolve) => setTimeout(resolve, 10));
      });
    }

    const image = container?.querySelector("img");
    expect(image).not.toBeNull();
    expect(image?.getAttribute("src")).toBe("blob:kyc-preview");
    expect(image?.getAttribute("alt")).toBe("ID — front");
    expect(getManualVerificationDocument).toHaveBeenCalledWith(
      "profile-1",
      "IdFront",
      expect.any(AbortSignal),
    );
    expect(container?.innerHTML).not.toContain("example.invalid");
  });
});
