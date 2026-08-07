import { useCallback, useEffect, useRef, useState } from "react";
import { createPortal } from "react-dom";
import { ChevronLeft, ChevronRight, X } from "lucide-react";
import { lockBodyScroll } from "@/lib/bodyScrollLock";
import { cn } from "@/lib/utils";

type PhotoLightboxProps = {
  open: boolean;
  photos: { id: string; url?: string | null; caption?: string | null }[];
  initialIndex?: number;
  onClose: () => void;
};

export function PhotoLightbox({ open, photos, initialIndex = 0, onClose }: PhotoLightboxProps) {
  const [index, setIndex] = useState(initialIndex);
  const stageRef = useRef<HTMLDivElement>(null);
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
    const unlock = lockBodyScroll();
    return () => {
      document.removeEventListener("keydown", onKey);
      unlock();
    };
  }, [open, onClose, next, prev]);

  // Start each photo at the top-left so tall/wide images are browsable from
  // the beginning instead of mid-crop.
  useEffect(() => {
    const stage = stageRef.current;
    if (!stage) return;
    stage.scrollTop = 0;
    stage.scrollLeft = 0;
  }, [index, open]);

  if (!open || photos.length === 0) return null;

  const photo = photos[index];

  return createPortal(
    <div className="fixed inset-0 z-[1001] flex flex-col bg-black">
      <div className="flex shrink-0 items-center justify-between px-4 py-3 text-white">
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

      {/*
        Show the full image at its natural size. If it's taller or wider than
        the stage, the guest can scroll — nothing is cropped. Tall photos
        start at the top so the whole image is reachable by scrolling down.
      */}
      <div className="relative min-h-0 flex-1">
        <div
          ref={stageRef}
          className="h-full w-full overflow-auto overscroll-contain"
        >
          <div className="flex w-max min-h-full min-w-full items-start justify-center p-4">
            {photo?.url && (
              <img
                key={photo.id}
                src={photo.url}
                alt={photo.caption ?? `Photo ${index + 1}`}
                className="block h-auto w-auto max-w-none select-none"
                draggable={false}
              />
            )}
          </div>
        </div>

        {photos.length > 1 && (
          <>
            <button
              type="button"
              onClick={prev}
              aria-label="Previous photo"
              className="absolute left-4 top-1/2 z-10 -translate-y-1/2 flex h-12 w-12 items-center justify-center rounded-full bg-black/50 text-white hover:bg-black/70 cursor-pointer"
            >
              <ChevronLeft className="h-6 w-6" />
            </button>
            <button
              type="button"
              onClick={next}
              aria-label="Next photo"
              className="absolute right-4 top-1/2 z-10 -translate-y-1/2 flex h-12 w-12 items-center justify-center rounded-full bg-black/50 text-white hover:bg-black/70 cursor-pointer"
            >
              <ChevronRight className="h-6 w-6" />
            </button>
          </>
        )}
      </div>

      {photo?.caption && (
        <div className="shrink-0 px-4 pb-2 text-center text-sm text-white/80">{photo.caption}</div>
      )}

      {photos.length > 1 && (
        <div className="shrink-0 overflow-x-auto p-3">
          <div className="mx-auto flex w-fit gap-2">
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
