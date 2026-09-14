const RELOAD_KEY = "lagedra:stale-chunk-reload";
const RELOAD_COOLDOWN_MS = 30_000;

/**
 * Vite hashed chunks 404 after a deploy when CloudFront still has an old
 * index.html, or when a sync drops a file and the SPA fallback serves HTML
 * as JavaScript. Chrome surfaces that as this TypeError.
 */
export function isStaleChunkError(error: unknown): boolean {
  const message =
    error instanceof Error
      ? error.message
      : typeof error === "string"
        ? error
        : "";
  return /Failed to fetch dynamically imported module|error loading dynamically imported module|Importing a module script failed|Loading chunk [\w-]+ failed|Cannot read properties of undefined \(reading ['"]\w+Page['"]\)/i.test(
    message,
  );
}

/** Reloads once per cooldown so a persistently missing file cannot loop. */
export function reloadOnceOnStaleChunk(error: unknown): boolean {
  if (typeof window === "undefined" || !isStaleChunkError(error)) {
    return false;
  }

  try {
    const last = Number(sessionStorage.getItem(RELOAD_KEY) ?? "0");
    if (last && Date.now() - last < RELOAD_COOLDOWN_MS) {
      return false;
    }
    sessionStorage.setItem(RELOAD_KEY, String(Date.now()));
  } catch {
    // Private mode can block sessionStorage; still try a single reload.
  }

  window.location.reload();
  return true;
}

/** Vite fires this when a preload / lazy import fails after a new deploy. */
export function installStaleChunkReload(): void {
  window.addEventListener("vite:preloadError", (event) => {
    event.preventDefault();
    const payload =
      "payload" in event ? (event as Event & { payload?: unknown }).payload : undefined;
    reloadOnceOnStaleChunk(
      payload ?? new TypeError("Failed to fetch dynamically imported module"),
    );
  });
}
