import { useState } from "react";
import { Download, Loader2 } from "lucide-react";
import { listingApi } from "@/features/listings/services/listingApi";
import { getApiErrorMessage } from "@/api/errors";
import { Button } from "@/components/ui/button";
import { Alert, AlertDescription } from "@/components/ui/alert";

type ListingLeaseSpecimenPreviewProps = {
  listingId: string;
  /**
   * Always fetch Lagedra's blank template, even if the listing currently
   * stores a host-uploaded document.
   */
  specimenOnly?: boolean;
  downloadLabel: string;
};

/** Downloads the listing lease PDF. Nothing is rendered inline. */
export function ListingLeaseSpecimenPreview({
  listingId,
  specimenOnly = false,
  downloadLabel,
}: ListingLeaseSpecimenPreviewProps) {
  const [isDownloading, setIsDownloading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleDownload = async () => {
    setIsDownloading(true);
    setError(null);
    try {
      const { blob, fileName } = await listingApi.downloadLeasePreview(listingId, {
        specimen: specimenOnly,
      });
      const url = URL.createObjectURL(blob);
      const anchor = document.createElement("a");
      anchor.href = url;
      anchor.download = fileName;
      anchor.click();
      URL.revokeObjectURL(url);
    } catch (err) {
      setError(getApiErrorMessage(err, "The lease agreement could not be opened."));
    } finally {
      setIsDownloading(false);
    }
  };

  return (
    <div className="space-y-3">
      <Button
        type="button"
        variant="outline"
        size="sm"
        disabled={isDownloading}
        onClick={() => void handleDownload()}
      >
        {isDownloading ? (
          <Loader2 className="mr-2 h-4 w-4 animate-spin" />
        ) : (
          <Download className="mr-2 h-4 w-4" />
        )}
        {downloadLabel}
      </Button>

      {error && (
        <Alert variant="destructive">
          <AlertDescription>{error}</AlertDescription>
        </Alert>
      )}
    </div>
  );
}
