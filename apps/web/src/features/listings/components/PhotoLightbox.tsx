import { useCallback, useEffect, useState } from "react";
import { createPortal } from "react-dom";
import { ChevronLeft, ChevronRight, X } from "lucide-react";
import { cn } from "@/lib/utils";

type PhotoLightboxProps = {
  open: boolean;
  photos: { id: string; url?: string | null; caption?: string | null }[];
  initialIndex?: number;
  onClose: () => void;
};

export function PhotoLightbox({ open, photos, initialIndex = 0, onClose }: PhotoLightboxProps) {
  const [index, setIndex] = useState(initialIndex);
  // Track the "session" the lightbox is in so we re-seed `index` when it
  // re-opens or the requested initialIndex changes — without an effect.
  const [seed, setSeed] = useState({ open, initialIndex });
  if (open && (open !== seed.open || initialIndex !== seed.initialIndex)) {
    setSeed({ open, initialIndex });
    setIndex(initialIndex);
  } else if (!open && seed.open) {
    setSeed({ open, initialIndex });
  }

  const next = useCallback(() => {
    setIndex((i) => (i === photos.length - 1 ? 0 : i + 1));
  }, [photos.length]);

  const prev = useCallback(() => {
    setIndex((i) => (i === 0 ? photos.length - 1 : i - 1));
  }, [photos.length]);

  useEffect(() => {
    if (!open) return;
    const onKey = (e: KeyboardEvent) => {
      if (e.key === "Escape") onClose();
      else if (e.key === "ArrowRight") next();
      else if (e.key === "ArrowLeft") prev();
    };
    document.addEventListener("keydown", onKey);
    document.body.style.overflow = "hidden";
    return () => {
      document.removeEventListener("keydown", onKey);
      document.body.style.overflow = "";
    };
  }, [open, onClose, next, prev]);

  if (!open || photos.length === 0) return null;

  const photo = photos[index];

  return createPortal(
    <div className="fixed inset-0 z-[1000] flex flex-col bg-black/95">
      <div className="flex items-center justify-between px-4 py-3 text-white">
        <p className="text-sm tabular-nums">
          {index + 1} <span className="text-white/50">/ {photos.length}</span>
        </p>
        <button
          type="button"
          onClick={onClose}
          aria-label="Close lightbox"
          className="rounded-full p-2 hover:bg-white/10 cursor-pointer"
        >
          <X className="h-5 w-5" />
        </button>
      </div>

      <div className="relative flex flex-1 items-center justify-center px-4">
        {photo?.url && (
          <img
            src={photo.url}
            alt={photo.caption ?? `Photo ${index + 1}`}
            className="max-h-full max-w-full object-contain"
          />
        )}

        {photos.length > 1 && (
          <>
            <button
              type="button"
              onClick={prev}
              aria-label="Previous photo"
              className="absolute left-4 top-1/2 -translate-y-1/2 flex h-12 w-12 items-center justify-center rounded-full bg-white/10 text-white hover:bg-white/20 cursor-pointer"
            >
              <ChevronLeft className="h-6 w-6" />
            </button>
            <button
              type="button"
              onClick={next}
              aria-label="Next photo"
              className="absolute right-4 top-1/2 -translate-y-1/2 flex h-12 w-12 items-center justify-center rounded-full bg-white/10 text-white hover:bg-white/20 cursor-pointer"
            >
              <ChevronRight className="h-6 w-6" />
            </button>
          </>
        )}
      </div>

      {photo?.caption && (
        <div className="px-4 pb-2 text-center text-sm text-white/80">{photo.caption}</div>
      )}

      {photos.length > 1 && (
        <div className="overflow-x-auto p-3">
          <div className="mx-auto flex gap-2 w-fit">
            {photos.map((p, i) => (
              <button
                key={p.id}
                type="button"
                onClick={() => setIndex(i)}
                className={cn(
                  "h-14 w-20 shrink-0 overflow-hidden rounded-md border-2 transition-opacity cursor-pointer",
                  i === index ? "border-white" : "border-transparent opacity-60 hover:opacity-100",
                )}
              >
                {p.url && (
                  <img
                    src={p.url}
                    alt={p.caption ?? ""}
                    className="h-full w-full object-cover"
                    loading="lazy"
                  />
                )}
              </button>
            ))}
          </div>
        </div>
      )}
    </div>,
    document.body,
  );
}
