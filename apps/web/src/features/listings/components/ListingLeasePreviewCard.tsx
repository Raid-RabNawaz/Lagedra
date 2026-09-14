import { Link, useLocation } from "react-router-dom";
import { FileText, ShieldCheck } from "lucide-react";
import type { ListingDetailsDto } from "@/api/types";
import { tenantStandardLeaseTitle } from "@/features/listings/lib/standardLeaseCopy";
import { ListingLeaseSpecimenPreview } from "@/features/listings/components/ListingLeaseSpecimenPreview";

type ListingLeasePreviewCardProps = {
  listing: ListingDetailsDto;
  isSignedIn: boolean;
};

/**
 * Lets a prospective tenant download the lease that would bind their booking
 * — the host's own document when they supplied one, otherwise a blank
 * specimen of the jurisdiction's standard lease.
 */
export function ListingLeasePreviewCard({
  listing,
  isSignedIn,
}: ListingLeasePreviewCardProps) {
  const location = useLocation();
  const isHostProvided = listing.leaseAgreementSource === "HostProvided";
  const standardTitle = tenantStandardLeaseTitle(listing.jurisdictionCode);

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
              : standardTitle}
          </p>
          <p className="text-sm text-muted-foreground">
            {isHostProvided
              ? "Download the host's lease before you request a booking. It will be attached to your booking exactly as shown."
              : "Download a blank copy with this listing's rent, deposit and lease terms already filled in. Names, dates and the exact address are completed when a booking is confirmed."}
          </p>
        </div>
      </div>

      {isSignedIn ? (
        <ListingLeaseSpecimenPreview
          listingId={listing.id}
          downloadLabel={isHostProvided ? "Download the host's lease" : "Download the lease"}
        />
      ) : (
        <p className="text-sm text-muted-foreground">
          <Link
            to="/login"
            state={{ from: location }}
            className="font-medium text-primary underline underline-offset-4"
          >
            Sign in
          </Link>{" "}
          to download the lease agreement.
        </p>
      )}
    </div>
  );
}
