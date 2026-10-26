import { useState } from "react";
import { ImageOff } from "lucide-react";
import { cn } from "@/lib/utils";

type PhotoThumbBoxProps = {
  src?: string | null;
  alt?: string;
  className?: string;
};

/** Ask Airbnb's CDN for a small render. The original URL is still what gets imported. */
function displaySrc(src: string): string {
  try {
    const parsed = new URL(src);
    if (parsed.hostname.endsWith("muscache.com")) {
      parsed.searchParams.set("im_w", "480");
      return parsed.toString();
    }
  } catch {
    return src;
  }
  return src;
}

/**
 * Renders an already-uploaded (or remote) photo inside a small square, matching
 * the listing details gallery. This does not generate a separate thumbnail file.
 */
export function PhotoThumbBox({ src, alt = "", className }: PhotoThumbBoxProps) {
  const [failed, setFailed] = useState(false);
  const showImage = Boolean(src) && !failed;

  return (
    <div className={cn("aspect-square overflow-hidden rounded-md bg-muted", className)}>
      {showImage ? (
        <img
          src={src ? displaySrc(src) : undefined}
          alt={alt}
          className="h-full w-full object-cover"
          loading="lazy"
          decoding="async"
          referrerPolicy="no-referrer"
          onError={() => setFailed(true)}
        />
      ) : (
        <div className="flex h-full w-full items-center justify-center">
          <ImageOff className="h-5 w-5 text-muted-foreground/40" />
        </div>
      )}
    </div>
  );
}
