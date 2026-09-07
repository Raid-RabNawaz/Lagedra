import { useState } from "react";
import { Link, useLocation } from "react-router-dom";
import { Download, FileText, Loader2, ShieldCheck } from "lucide-react";
import type { ListingDetailsDto } from "@/api/types";
import { listingApi } from "@/features/listings/services/listingApi";
import { getApiErrorMessage } from "@/api/errors";
import { Button } from "@/components/ui/button";
import { Alert, AlertDescription } from "@/components/ui/alert";

type ListingLeasePreviewCardProps = {
  listing: ListingDetailsDto;
  isSignedIn: boolean;
};

/**
 * Lets a prospective tenant read the lease that would bind their booking
 * before they request one — the host's own document when they supplied one,
 * otherwise a blank specimen of Lagedra's lease for this jurisdiction.
 */
export function ListingLeasePreviewCard({
  listing,
  isSignedIn,
}: ListingLeasePreviewCardProps) {
  const location = useLocation();
  const [isDownloading, setIsDownloading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const isHostProvided = listing.leaseAgreementSource === "HostProvided";

  const handleDownload = async () => {
    setIsDownloading(true);
    setError(null);
    try {
      const { blob, fileName } = await listingApi.downloadLeasePreview(listing.id);
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
    <div className="rounded-xl border p-4 space-y-3">
      <div className="flex items-start gap-3">
        {isHostProvided ? (
          <FileText className="mt-0.5 h-5 w-5 shrink-0 text-muted-foreground" />
        ) : (
          <ShieldCheck className="mt-0.5 h-5 w-5 shrink-0 text-muted-foreground" />
        )}
        <div className="space-y-1">
          <p className="text-sm font-medium">
            {isHostProvided
              ? "This host uses their own lease agreement"
              : "Lagedra standard lease agreement"}
          </p>
          <p className="text-sm text-muted-foreground">
            {isHostProvided
              ? "Read the host's lease before you request a booking. It will be attached to your booking exactly as shown."
              : "Read a blank copy with this listing's rent, deposit and lease terms already filled in. Names, dates and the exact address are completed when a booking is confirmed."}
          </p>
        </div>
      </div>

      {isSignedIn ? (
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
          {isHostProvided ? "Read the host's lease" : "Read the lease"}
        </Button>
      ) : (
        <p className="text-sm text-muted-foreground">
          <Link
            to="/login"
            state={{ from: location }}
            className="font-medium text-primary underline underline-offset-4"
          >
            Sign in
          </Link>{" "}
          to read the lease agreement.
        </p>
      )}

      {error && (
        <Alert variant="destructive">
          <AlertDescription>{error}</AlertDescription>
        </Alert>
      )}
    </div>
  );
}
