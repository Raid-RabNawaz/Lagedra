/**
 * @vitest-environment jsdom
 */
import { act, type ReactNode } from "react";
import { createRoot, type Root } from "react-dom/client";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { afterEach, describe, expect, it } from "vitest";
import type { ListingDetailsDto } from "@/api/types";
import { ListingPhotosEditor } from "./ListingPhotosEditor";
import { PhotoThumbBox } from "./PhotoThumbBox";

function mount(node: ReactNode) {
  const container = document.createElement("div");
  document.body.appendChild(container);
  const root = createRoot(container);
  act(() => {
    root.render(node);
  });
  return { container, root };
}

describe("PhotoThumbBox", () => {
  let root: Root | null = null;
  let container: HTMLDivElement | null = null;

  afterEach(() => {
    act(() => {
      root?.unmount();
    });
    container?.remove();
    root = null;
    container = null;
  });

  it("renders the uploaded image in a square tile", () => {
    const mounted = mount(
      <PhotoThumbBox src="https://cdn.example.com/kitchen.jpg" alt="Kitchen" />,
    );
    root = mounted.root;
    container = mounted.container;

    const img = container.querySelector("img");
    expect(img?.getAttribute("src")).toBe("https://cdn.example.com/kitchen.jpg");
    expect(img?.getAttribute("alt")).toBe("Kitchen");
    expect(img?.getAttribute("referrerpolicy")).toBe("no-referrer");
    expect(container.querySelector("div")?.className).toContain("aspect-square");
  });

  it("asks Airbnb for a small render instead of the full photo", () => {
    const mounted = mount(
      <PhotoThumbBox
        src="https://a0.muscache.com/im/pictures/Hosting-1/original/room.jpeg?im_w=1200"
        alt="Room"
      />,
    );
    root = mounted.root;
    container = mounted.container;

    expect(container.querySelector("img")?.getAttribute("src")).toBe(
      "https://a0.muscache.com/im/pictures/Hosting-1/original/room.jpeg?im_w=480",
    );
  });
});

describe("ListingPhotosEditor thumbnails", () => {
  let root: Root | null = null;
  let container: HTMLDivElement | null = null;

  afterEach(() => {
    act(() => {
      root?.unmount();
    });
    container?.remove();
    root = null;
    container = null;
  });

  it("shows each saved photo in a small box", () => {
    const listing = {
      id: "listing-1",
      photos: [
        {
          id: "photo-1",
          url: "https://cdn.example.com/cover.jpg",
          caption: "Cover",
          isCover: true,
          sortOrder: 0,
        },
      ],
    } as ListingDetailsDto;

    const client = new QueryClient();
    const mounted = mount(
      <QueryClientProvider client={client}>
        <ListingPhotosEditor listing={listing} />
      </QueryClientProvider>,
    );
    root = mounted.root;
    container = mounted.container;

    const img = container.querySelector("img");
    expect(img?.getAttribute("src")).toBe("https://cdn.example.com/cover.jpg");
    expect(img?.getAttribute("alt")).toBe("Cover");
  });
});
