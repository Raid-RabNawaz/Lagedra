import { useEffect } from "react";
import { createPortal } from "react-dom";
import { X } from "lucide-react";
import { lockBodyScroll } from "@/lib/bodyScrollLock";
import { cn } from "@/lib/utils";

type GalleryPhoto = {
  id: string;
  url?: string | null;
  caption?: string | null;
};

type PhotoGalleryModalProps = {
  open: boolean;
  photos: GalleryPhoto[];
  onClose: () => void;
  /** Opens the immersive lightbox at the chosen photo. */
  onSelectPhoto: (index: number) => void;
};

/**
 * Airbnb-style "All photos" view: a scrollable full-screen grid. Clicking a
 * photo hands off to the immersive lightbox; closing the lightbox returns
 * here so the guest can keep browsing.
 */
export function PhotoGalleryModal({
  open,
  photos,
  onClose,
  onSelectPhoto,
}: PhotoGalleryModalProps) {
  useEffect(() => {
    if (!open) return;
    const onKey = (e: KeyboardEvent) => {
      if (e.key === "Escape") onClose();
    };
    document.addEventListener("keydown", onKey);
    const unlock = lockBodyScroll();
    return () => {
      document.removeEventListener("keydown", onKey);
      unlock();
    };
  }, [open, onClose]);

  if (!open || photos.length === 0) return null;

  return createPortal(
    <div className="fixed inset-0 z-[1000] flex flex-col bg-background">
      <header className="sticky top-0 z-10 flex items-center justify-between border-b bg-background/95 px-4 py-3 backdrop-blur sm:px-6">
        <button
          type="button"
          onClick={onClose}
          aria-label="Close photo gallery"
          className="rounded-full p-2 hover:bg-muted cursor-pointer"
        >
          <X className="h-5 w-5" />
        </button>
        <p className="text-sm font-semibold tabular-nums">
          {photos.length} photo{photos.length === 1 ? "" : "s"}
        </p>
        {/* Spacer so the count stays visually centered. */}
        <div className="w-9" aria-hidden />
      </header>

      <div className="flex-1 overflow-y-auto">
        <div className="mx-auto grid max-w-5xl grid-cols-1 gap-3 p-4 sm:grid-cols-2 sm:gap-4 sm:p-6 lg:p-8">
          {photos.map((photo, index) => (
            <button
              key={photo.id}
              type="button"
              onClick={() => onSelectPhoto(index)}
              aria-label={photo.caption?.trim() || `Photo ${index + 1}`}
              className={cn(
                "group relative overflow-hidden rounded-xl bg-muted cursor-zoom-in focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring",
                // Lead with a wide hero tile, then a steady 2-column grid.
                index === 0 ? "sm:col-span-2 aspect-[16/9]" : "aspect-[4/3]",
              )}
            >
              {photo.url ? (
                <img
                  src={photo.url}
                  alt={photo.caption ?? `Photo ${index + 1}`}
                  className="h-full w-full object-cover transition-transform duration-300 group-hover:scale-[1.02]"
                  loading={index < 4 ? "eager" : "lazy"}
                />
              ) : null}
              {photo.caption?.trim() && (
                <span className="absolute inset-x-0 bottom-0 bg-gradient-to-t from-black/55 to-transparent px-3 pb-2 pt-8 text-left text-xs text-white opacity-0 transition-opacity group-hover:opacity-100">
                  {photo.caption}
                </span>
              )}
            </button>
          ))}
        </div>
      </div>
    </div>,
    document.body,
  );
}
