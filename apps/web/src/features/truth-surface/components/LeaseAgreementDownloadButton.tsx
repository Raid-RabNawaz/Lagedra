import { useState } from "react";
import { FileDown, Loader2 } from "lucide-react";
import { truthSurfaceApi } from "@/features/truth-surface/services/truthSurfaceApi";
import { extractErrorMessage } from "@/lib/errors";
import { Button } from "@/components/ui/button";

/**
 * Downloads the filled lease agreement PDF attached to a confirmed deal.
 * Generation runs after Truth Surface seal (async) and again on demand if
 * the PDF is not stored yet.
 */
export function LeaseAgreementDownloadButton({
  dealId,
  variant = "outline",
  size = "lg",
}: {
  dealId: string;
  variant?: "default" | "outline" | "ghost";
  size?: "default" | "sm" | "lg";
}) {
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState<string | null>(null);

  const handleDownload = async () => {
    setBusy(true);
    setMessage(null);
    try {
      const result = await truthSurfaceApi.downloadLeasePdf(dealId);
      if (!result) {
        setMessage(
          "The lease PDF is still being prepared. Try again in a moment.",
        );
        return;
      }
      const url = URL.createObjectURL(result.blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = result.filename;
      a.click();
      URL.revokeObjectURL(url);
    } catch (err) {
      const raw = extractErrorMessage(err);
      setMessage(
        /property address|fullAddress|precise address/i.test(raw)
          ? "This listing has no locked property address, so the lease PDF can't be generated. Open the listing, lock the full street address, then try again."
          : raw,
      );
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="flex flex-col items-center gap-1">
      <Button
        type="button"
        variant={variant}
        size={size}
        className="gap-2"
        onClick={() => void handleDownload()}
        disabled={busy}
      >
        {busy ? (
          <Loader2 className="h-4 w-4 animate-spin" />
        ) : (
          <FileDown className="h-4 w-4" />
        )}
        Download lease (PDF)
      </Button>
      {message && (
        <p className="max-w-sm text-center text-xs text-destructive">{message}</p>
      )}
    </div>
  );
}
