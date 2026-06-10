import { useState, useMemo } from "react";
import { Link, useNavigate } from "react-router-dom";
import { AlertCircle, ShieldAlert, Loader2, MessageSquare } from "lucide-react";
import { Alert } from "@/components/ui/alert";
import { Separator } from "@/components/ui/separator";
import { Button } from "@/components/ui/button";
import { buttonVariants } from "@/components/ui/button-variants";
import { cn } from "@/lib/utils";
import { ApplyDialog } from "@/features/applications/components/ApplyDialog";
import { DateRangeField } from "@/features/listings/components/DateRangeField";
import {
  useListingAvailabilityRange,
  useListingQuote,
  useMyConsentStatus,
} from "@/features/listings/hooks/useListings";
import {
  useMyListingInquiry,
  useStartListingInquiry,
} from "@/features/inquiry/hooks/useInquiry";
import { formatMoney } from "@/utils/format";
import { getApiErrorMessage } from "@/api/errors";
import type { ListingDetailsDto } from "@/api/types";

type Props = {
  listing: ListingDetailsDto;
  /** True only when the viewer is signed in and is not the listing host. */
  isProspectiveGuest: boolean;
};

/**
 * Phase 16 booking widget rendered inside the Listing Detail price card.
 * Wires the inline date picker to:
 *   1. `GET /v1/listings/{id}/availability?from=&to=` (gates the CTA)
 *   2. `POST /v1/listings/{id}/quote` (itemised total)
 *   3. `GET /v1/privacy/consents/me/status` (KYC banner replaces CTA)
 *
 * The Apply CTA stays disabled until both availability and consent pass.
 */
export const BookingPanel = ({ listing, isProspectiveGuest }: Props) => {
  const [checkIn, setCheckIn] = useState("");
  const [checkOut, setCheckOut] = useState("");

  const stayDays = useMemo(() => {
    if (!checkIn || !checkOut) return 0;
    return Math.max(
      0,
      Math.round(
        (new Date(checkOut).getTime() - new Date(checkIn).getTime()) /
          86_400_000,
      ),
    );
  }, [checkIn, checkOut]);

  const minStay = listing.minStayDays ?? 30;
  const maxStay = listing.maxStayDays ?? 365;
  const stayInRange = stayDays >= minStay && stayDays <= maxStay;
  const datesValid = Boolean(checkIn && checkOut && stayDays > 0 && stayInRange);

  const availability = useListingAvailabilityRange(
    listing.id,
    datesValid ? checkIn : undefined,
    datesValid ? checkOut : undefined,
  );

  const isAvailable = availability.data?.available ?? false;

  const quote = useListingQuote(
    listing.id,
    checkIn,
    checkOut,
    datesValid && isAvailable,
  );

  // Only fetch consent state for signed-in prospects — anonymous viewers
  // see a "sign in to book" CTA elsewhere and the host doesn't need this.
  const consents = useMyConsentStatus(isProspectiveGuest);
  const needsConsents =
    consents.data !== undefined && consents.data.hasRequired === false;

  // Phase 17 — pre-booking inquiry hook. We only fetch for prospective
  // guests (signed-in non-hosts); the host won't see an "ask a question"
  // CTA on their own listing.
  const navigate = useNavigate();
  const existingInquiry = useMyListingInquiry(
    isProspectiveGuest ? listing.id : undefined,
  );
  const startInquiry = useStartListingInquiry();

  const handleAskQuestion = () => {
    if (existingInquiry.data) {
      navigate(`/app/inquiry/${existingInquiry.data.sessionId}`);
      return;
    }
    startInquiry.mutate(listing.id, {
      onSuccess: (session) => {
        navigate(`/app/inquiry/${session.sessionId}`);
      },
    });
  };

  return (
    <div className="space-y-4">
      <DateRangeField
        value={{ checkIn, checkOut }}
        onChange={(next) => {
          setCheckIn(next.checkIn);
          setCheckOut(next.checkOut);
        }}
        minStayDays={minStay}
        maxStayDays={maxStay}
      />

      {checkIn && checkOut && stayDays > 0 && !stayInRange && (
        <Alert variant="destructive" className="text-xs">
          Stay must be {minStay}–{maxStay} days. You picked {stayDays}.
        </Alert>
      )}

      {datesValid && availability.isLoading && (
        <p className="flex items-center gap-1 text-xs text-muted-foreground">
          <Loader2 className="h-3 w-3 animate-spin" />
          Checking availability…
        </p>
      )}

      {datesValid && availability.isError && (
        <Alert variant="destructive" className="text-xs">
          {getApiErrorMessage(availability.error, "Failed to check availability.")}
        </Alert>
      )}

      {datesValid && !availability.isLoading && !isAvailable && (
        <Alert variant="destructive" className="text-xs">
          <AlertCircle className="h-4 w-4" />
          Those dates are unavailable. Try another window.
        </Alert>
      )}

      {datesValid && isAvailable && quote.data && (
        <div className="rounded-lg border bg-muted/40 p-3 text-sm">
          <div className="flex items-center justify-between">
            <span className="text-muted-foreground">First-month rent</span>
            <span>{formatMoney(quote.data.rentCents)}</span>
          </div>
          <div className="flex items-center justify-between">
            <span className="text-muted-foreground">Security deposit</span>
            <span>{formatMoney(quote.data.depositCents)}</span>
          </div>
          {quote.data.insuranceFeeCents > 0 && (
            <div className="flex items-center justify-between">
              <span className="text-muted-foreground">Insurance</span>
              <span>{formatMoney(quote.data.insuranceFeeCents)}</span>
            </div>
          )}
          {quote.data.serviceFeeCents > 0 && (
            <div className="flex items-center justify-between">
              <span className="text-muted-foreground">Service fee</span>
              <span>{formatMoney(quote.data.serviceFeeCents)}</span>
            </div>
          )}
          <Separator className="my-2" />
          <div className="flex items-center justify-between font-semibold">
            <span>Due at checkout</span>
            <span>{formatMoney(quote.data.totalCents)}</span>
          </div>
          {quote.data.protocolFeeCents > 0 && (
            <p className="mt-1 text-[11px] text-muted-foreground">
              Hosts pay a separate {formatMoney(quote.data.protocolFeeCents)}/mo
              protocol fee — not added to your total.
            </p>
          )}
        </div>
      )}

      {datesValid && isAvailable && quote.isError && (
        <Alert variant="destructive" className="text-xs">
          {getApiErrorMessage(quote.error, "Failed to compute price quote.")}
        </Alert>
      )}

      {/*
       * Three CTA states, in priority order:
       *   1. KYC required → deep link to verification
       *   2. Dates valid + available → ApplyDialog with prefilled dates
       *   3. No / invalid dates → disabled CTA via ApplyDialog (no dates)
       */}
      {isProspectiveGuest && needsConsents ? (
        <Link
          to={`/app/verification?return=/listings/${listing.id}`}
          className={cn(
            buttonVariants({ variant: "accent", size: "lg" }),
            "w-full",
          )}
        >
          <ShieldAlert className="h-4 w-4" />
          Verify your identity to book
        </Link>
      ) : (
        <ApplyDialog
          listing={listing}
          initialCheckIn={datesValid && isAvailable ? checkIn : undefined}
          initialCheckOut={datesValid && isAvailable ? checkOut : undefined}
        />
      )}

      {/*
       * Phase 17 — secondary "Ask the host a question" CTA. Lets the
       * tenant start a pre-booking conversation without committing to
       * an application. If they already have an open thread for this
       * listing the same button reads "Continue conversation" and
       * routes them back to the existing session.
       */}
      {isProspectiveGuest && (
        <Button
          type="button"
          variant="outline"
          className="w-full gap-2"
          onClick={handleAskQuestion}
          disabled={startInquiry.isPending || existingInquiry.isLoading}
        >
          <MessageSquare className="h-4 w-4" />
          {existingInquiry.data
            ? "Continue conversation"
            : startInquiry.isPending
              ? "Starting…"
              : "Ask the host a question"}
        </Button>
      )}
      {startInquiry.isError && (
        <Alert variant="destructive" className="text-xs">
          {getApiErrorMessage(startInquiry.error, "Failed to start inquiry.")}
        </Alert>
      )}
    </div>
  );
};
