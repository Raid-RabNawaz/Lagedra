import { useEffect, useState, useMemo } from "react";
import { Link, useNavigate } from "react-router-dom";
import { AlertCircle, ShieldAlert, Loader2, MessageSquare, Send } from "lucide-react";
import { Alert, AlertDescription } from "@/components/ui/alert";
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
  // Controls whether the date field shows its inline error state. Flipped
  // on by clicking "Request to book" with no / invalid dates; cleared the
  // moment the user changes either date so they get instant feedback that
  // their fix has been registered.
  const [showDateError, setShowDateError] = useState(false);
  // Drives the ApplyDialog in controlled mode so we can decide whether to
  // open it based on local validity *before* the user gets in front of
  // the dialog. (The default trigger inside ApplyDialog opens
  // unconditionally and bypasses this check.)
  const [applyOpen, setApplyOpen] = useState(false);

  const rawDayDelta = useMemo(() => {
    if (!checkIn || !checkOut) return 0;
    return Math.round(
      (new Date(checkOut).getTime() - new Date(checkIn).getTime()) / 86_400_000,
    );
  }, [checkIn, checkOut]);

  const stayDays = Math.max(0, rawDayDelta);

  const minStay = listing.minStayDays ?? 30;
  const maxStay = listing.maxStayDays ?? 365;
  const stayInRange = stayDays >= minStay && stayDays <= maxStay;
  const datesValid = Boolean(checkIn && checkOut && stayDays > 0 && stayInRange);

  const dateErrorMessage = !checkIn && !checkOut
    ? "Select check-in and check-out dates."
    : !checkIn
      ? "Select a check-in date."
      : !checkOut
        ? "Select a check-out date."
        : rawDayDelta <= 0
          ? "Check-out must be after check-in."
          : !stayInRange
            ? `Stay must be between ${minStay} and ${maxStay} days.`
            : "";

  // Whenever the user touches the date field we drop the error styling.
  // It comes back the next time they click the (still-invalid) CTA.
  useEffect(() => {
    if (showDateError && datesValid) setShowDateError(false);
  }, [showDateError, datesValid]);

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

  // Phase 17 — pre-booking inquiry. Listing CTA always says "Ask the host
  // a question": open listing-scoped threads reopen; deal-linked history
  // lives on the booking and is never continued from the listing page.
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

  // Gate the CTA on everything the booking flow needs in one place:
  //   * dates entered AND in-range (datesValid)
  //   * availability check has returned without error
  //   * the listing is actually available for the picked window
  // Anything missing → button is soft-disabled and the matching Alert
  // above the button already explains the reason; the tooltip below
  // mirrors that text for users hovering the dimmed button.
  const checkingAvailability = datesValid && availability.isLoading;
  const availabilityFailed = datesValid && availability.isError;
  const datesUnavailable =
    datesValid && !availability.isLoading && !availability.isError && !isAvailable;
  const canSubmit =
    datesValid && !availability.isLoading && !availability.isError && isAvailable;

  const submitDisabledReason = !datesValid
    ? dateErrorMessage || "Pick valid check-in and check-out dates first."
    : checkingAvailability
      ? "Checking availability…"
      : availabilityFailed
        ? "Couldn't check availability — try again in a moment."
        : datesUnavailable
          ? "Those dates are unavailable. Try another window."
          : undefined;

  const handleRequestToBook = () => {
    if (!datesValid) {
      // Highlight the date field instead of silently swallowing the
      // click — this is the entire point of the inline-validation
      // requirement. The button stays clickable (aria-disabled) so the
      // error path actually runs.
      setShowDateError(true);
      return;
    }
    if (!canSubmit) {
      // Dates are syntactically valid but the listing isn't bookable
      // for that window (still loading, errored, or already taken).
      // The surrounding Alert is already on screen explaining the
      // reason, so we just swallow the click.
      return;
    }
    setApplyOpen(true);
  };

  return (
    <div className="space-y-4">
      <DateRangeField
        value={{ checkIn, checkOut }}
        onChange={(next) => {
          setCheckIn(next.checkIn);
          setCheckOut(next.checkOut);
          // Picking *anything* clears the error so the user sees they're
          // making progress without having to retry the CTA first.
          if (showDateError) setShowDateError(false);
        }}
        minStayDays={minStay}
        maxStayDays={maxStay}
        error={showDateError}
        errorMessage={showDateError ? dateErrorMessage : undefined}
        id="booking-panel-dates"
      />

      {/* The inline `errorMessage` on DateRangeField already covers the
          "stay too long / too short" copy when the user attempts to
          submit. The separate Alert is kept only for the case where the
          user typed valid dates that just happen to fall outside the
          listing's stay range — visible *before* they click anything,
          which the inline error (gated on submit-attempt) won't catch. */}
      {checkIn && checkOut && stayDays > 0 && !stayInRange && !showDateError && (
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
          <AlertDescription>
            Those dates are unavailable. Try another window.
          </AlertDescription>
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
            <span>Due at booking</span>
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
       *   2. Otherwise → soft-disabled "Request to book" button. Click
       *      with valid dates opens the ApplyDialog (controlled mode);
       *      click with missing/invalid dates flips the date field into
       *      its inline error state so the user knows *why* the form
       *      isn't progressing instead of pressing a dead button.
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
        <>
          <Button
            type="button"
            variant="accent"
            size="lg"
            className={cn("w-full gap-2", !canSubmit && "opacity-60")}
            aria-disabled={!canSubmit}
            onClick={handleRequestToBook}
            title={submitDisabledReason}
          >
            <Send className="h-4 w-4" />
            {listing.instantBookingEnabled ? "Book instantly" : "Request to book"}
          </Button>
          <ApplyDialog
            listing={listing}
            initialCheckIn={datesValid && isAvailable ? checkIn : undefined}
            initialCheckOut={datesValid && isAvailable ? checkOut : undefined}
            controlledOpen={applyOpen}
            onOpenChange={setApplyOpen}
          />
        </>
      )}

      {/*
       * Pre-booking "Ask the host a question" CTA. Always the same label:
       * once a prior thread is linked to a booking it is no longer returned
       * by /inquiry/mine, so this starts a new listing-scoped thread.
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
          {startInquiry.isPending ? "Starting…" : "Ask the host a question"}
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
