import { beforeEach, describe, expect, it, vi } from "vitest";
import { listingApi } from "@/features/listings/services/listingApi";
import {
  imageFilesFromList,
  uploadListingPhotoFiles,
} from "./uploadListingPhotoFiles";

vi.mock("@/features/listings/services/listingApi", () => ({
  listingApi: {
    uploadMedia: vi.fn(),
  },
}));

function photo(name: string): File {
  return new File(["x"], name, { type: "image/jpeg" });
}

describe("imageFilesFromList", () => {
  it("keeps images and drops non-image files", () => {
    const files = [
      photo("a.jpg"),
      new File(["x"], "notes.txt", { type: "text/plain" }),
      new File(["x"], "tour.heic", { type: "" }),
    ];
    const list = {
      length: files.length,
      item: (i: number) => files[i] ?? null,
      [Symbol.iterator]: () => files[Symbol.iterator](),
    } as unknown as FileList;

    expect(imageFilesFromList(list).map((f) => f.name)).toEqual([
      "a.jpg",
      "tour.heic",
    ]);
  });

  it("returns an empty list when nothing was selected", () => {
    expect(imageFilesFromList(null)).toEqual([]);
  });
});

describe("uploadListingPhotoFiles", () => {
  beforeEach(() => {
    vi.mocked(listingApi.uploadMedia).mockReset();
  });

  it("is a no-op when no files are given", async () => {
    await expect(uploadListingPhotoFiles("listing-1", [])).resolves.toEqual({
      uploaded: 0,
      failed: 0,
    });
    expect(listingApi.uploadMedia).not.toHaveBeenCalled();
  });

  it("uploads every file and reports progress", async () => {
    vi.mocked(listingApi.uploadMedia).mockResolvedValue({
      kind: "Photo",
      url: "https://cdn.example.com/a.jpg",
      storageKey: "k",
      photo: null,
    });
    const progress: Array<[number, number]> = [];

    await expect(
      uploadListingPhotoFiles("listing-1", [photo("a.jpg"), photo("b.jpg")], {
        caption: "Living room",
        onProgress: (completed, total) => progress.push([completed, total]),
      }),
    ).resolves.toEqual({ uploaded: 2, failed: 0 });

    expect(listingApi.uploadMedia).toHaveBeenCalledTimes(2);
    expect(listingApi.uploadMedia).toHaveBeenCalledWith(
      "listing-1",
      expect.objectContaining({ name: "a.jpg" }),
      "Living room",
    );
    expect(progress).toEqual([
      [1, 2],
      [2, 2],
    ]);
  });

  it("continues after an individual failure", async () => {
    vi.mocked(listingApi.uploadMedia)
      .mockRejectedValueOnce(new Error("too large"))
      .mockResolvedValueOnce({
        kind: "Photo",
        url: "https://cdn.example.com/b.jpg",
        storageKey: "k",
        photo: null,
      });

    await expect(
      uploadListingPhotoFiles("listing-1", [photo("a.jpg"), photo("b.jpg")]),
    ).resolves.toEqual({ uploaded: 1, failed: 1 });
  });
});
