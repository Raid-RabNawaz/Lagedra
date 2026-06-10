import { useEffect, useMemo, useState } from "react";
import { isAxiosError } from "axios";
import { useNavigate } from "react-router-dom";
import { Minus, Plus, Send, ShieldCheck, Users } from "lucide-react";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Alert } from "@/components/ui/alert";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { Loader } from "@/components/shared/Loader";
import { useAuthStore } from "@/app/auth/authStore";
import { useSubmitApplication } from "@/features/applications/hooks/useApplications";
import { DateRangeField } from "@/features/listings/components/DateRangeField";
import { privacyApi } from "@/features/privacy/services/privacyApi";
import { formatMoney } from "@/utils/format";
import { cn } from "@/lib/utils";
import type { ListingDetailsDto } from "@/api/types";
import { getApiErrorMessage } from "@/api/errors";

/** Server-side hard cap on the cover note. Mirrors `DealApplication.MessageMaxLength`. */
const MESSAGE_MAX_LENGTH = 1000;

type Props = {
  listing: ListingDetailsDto;
  /**
   * Optional pre-filled check-in date (YYYY-MM-DD). Used by the Listing
   * Detail booking panel so the dates the guest already picked carry into
   * the dialog.
   */
  initialCheckIn?: string;
  initialCheckOut?: string;
  /**
   * Phase 17 — when set, the dialog runs in controlled mode and the
   * default trigger button is hidden. Used by surfaces that already
   * have their own CTA (e.g. the "Continue to Apply" button on the
   * pre-booking inquiry page).
   */
  controlledOpen?: boolean;
  onOpenChange?: (open: boolean) => void;
};

/**
 * Phase 17.1 — apply flow simplified to dates-only.
 *
 * Earlier (Phase 16.9) this dialog also captured a card off-session via
 * Stripe Elements so an instant-book deal could be charged automatically
 * the moment the host approved. We pulled that step out because the
 * deposit, insurance fee, and jurisdiction warning are all decided at
 * approval time — the tenant should never feel "already paid" before
 * those numbers are final. Single payment surface lives on the deal /
 * checkout page after the Truth Surface is sealed.
 *
 * Backend still accepts an optional `stripePaymentMethodId`; we now
 * always pass null and `CardOnFileChargeService` short-circuits, leaving
 * the standard checkout flow as the only path that bills the guest.
 */
export const ApplyDialog = ({
  listing,
  initialCheckIn,
  initialCheckOut,
  controlledOpen,
  onOpenChange,
}: Props) => {
  const user = useAuthStore((s) => s.user);
  const submitMutation = useSubmitApplication();
  const navigate = useNavigate();

  const isControlled = controlledOpen !== undefined;
  const [internalOpen, setInternalOpen] = useState(false);
  const open = isControlled ? controlledOpen : internalOpen;
  const setOpen = (next: boolean) => {
    if (isControlled) {
      onOpenChange?.(next);
    } else {
      setInternalOpen(next);
      onOpenChange?.(next);
    }
  };

  const [checkIn, setCheckIn] = useState(initialCheckIn ?? "");
  const [checkOut, setCheckOut] = useState(initialCheckOut ?? "");
  // Guest count + cover note are local to the dialog — the BookingPanel
  // never pre-fills them, and resetting on close keeps the next open a
  // clean slate (matches the listing-detail "dates only" pre-fill model).
  const [guestCount, setGuestCount] = useState(1);
  const [message, setMessage] = useState("");
  const [submitErrorMessage, setSubmitErrorMessage] = useState<string | null>(null);

  // Listings without a HouseRules block fall back to a soft cap of 16
  // (Airbnb's published max). The server still rejects values above the
  // listing's actual cap, so this is purely a UX guardrail.
  const maxGuests = listing.houseRules?.maxGuests ?? 16;

  useEffect(() => {
    if (open) {
      if (initialCheckIn) setCheckIn(initialCheckIn);
      if (initialCheckOut) setCheckOut(initialCheckOut);
      setSubmitErrorMessage(null);
    }
  }, [open, initialCheckIn, initialCheckOut]);

  // If the host edits the listing's max guests after the dialog mounts
  // (or it loads in lazily), make sure we never display a value above
  // the new cap — otherwise the "Book" button could submit data the
  // server would immediately reject.
  useEffect(() => {
    setGuestCount((prev) => Math.min(Math.max(1, prev), maxGuests));
  }, [maxGuests]);

  const stayDays =
    checkIn && checkOut
      ? Math.max(
          0,
          Math.round(
            (new Date(checkOut).getTime() - new Date(checkIn).getTime()) / 86_400_000,
          ),
        )
      : 0;

  const minStay = listing.minStayDays ?? 30;
  const maxStay = listing.maxStayDays ?? 365;
  const isValidStay = stayDays >= minStay && stayDays <= maxStay;
  const datesValid = Boolean(checkIn && checkOut && stayDays > 0 && isValidStay);

  const messageLength = message.trim().length;
  const messageTooLong = messageLength > MESSAGE_MAX_LENGTH;
  const guestsValid = guestCount >= 1 && guestCount <= maxGuests;
  const canSubmit = datesValid && guestsValid && !messageTooLong;

  const canDecrementGuests = guestCount > 1;
  const canIncrementGuests = guestCount < maxGuests;

  const handleSubmit = async () => {
    if (!user || !canSubmit) return;

    setSubmitErrorMessage(null);

    try {
      await privacyApi.ensureRequiredConsents(user.userId);

      const trimmedMessage = message.trim();
      const result = await submitMutation.mutateAsync({
        listingId: listing.id,
        requestedCheckIn: checkIn,
        requestedCheckOut: checkOut,
        guestCount,
        message: trimmedMessage.length > 0 ? trimmedMessage : null,
        stripePaymentMethodId: null,
      });

      setOpen(false);
      setCheckIn("");
      setCheckOut("");
      setGuestCount(1);
      setMessage("");

      if (result?.nextPath) {
        navigate(result.nextPath);
      }
    } catch (error) {
      if (isAxiosError(error) && error.response?.status === 451) {
        setSubmitErrorMessage(
          "Please complete KYC and data-processing consent before submitting an application.",
        );
        return;
      }

      setSubmitErrorMessage(
        getApiErrorMessage(error, "Failed to submit application. Please try again."),
      );
    }
  };

  const guestLabel = useMemo(
    () => (guestCount === 1 ? "1 guest" : `${guestCount} guests`),
    [guestCount],
  );

  const submitting = submitMutation.isPending;
  const ctaLabel = listing.instantBookingEnabled ? "Book instantly" : "Request to book";

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      {!isControlled && (
        <DialogTrigger asChild>
          <Button variant="accent" size="lg" className="w-full">
            <Send className="h-4 w-4" />
            {ctaLabel}
          </Button>
        </DialogTrigger>
      )}
      <DialogContent className="sm:max-w-xl">
        <DialogHeader>
          <DialogTitle>
            {listing.instantBookingEnabled
              ? "Confirm your dates"
              : "Request these dates"}
          </DialogTitle>
        </DialogHeader>

        {submitting ? (
          <div className="py-12">
            <Loader label="Submitting application…" />
          </div>
        ) : (
          <form
            onSubmit={(e) => {
              e.preventDefault();
              void handleSubmit();
            }}
            className="space-y-4"
          >
            <div className="rounded-lg bg-secondary p-3 space-y-1">
              <p className="text-sm font-medium">{listing.title}</p>
              <p className="text-sm text-muted-foreground">
                {formatMoney(listing.monthlyRentCents)} / month
              </p>
              <p className="text-xs text-muted-foreground">
                Stay range: {minStay}–{maxStay} days
              </p>
            </div>

            <DateRangeField
              value={{ checkIn, checkOut }}
              onChange={(next) => {
                setCheckIn(next.checkIn);
                setCheckOut(next.checkOut);
              }}
              minStayDays={minStay}
              maxStayDays={maxStay}
            />

            {stayDays > 0 && (
              <p
                className={`text-sm ${
                  isValidStay ? "text-muted-foreground" : "text-destructive"
                }`}
              >
                {stayDays} day{stayDays !== 1 ? "s" : ""}
                {!isValidStay && ` (must be ${minStay}–${maxStay} days)`}
              </p>
            )}

            {/*
             * Guest stepper. Airbnb-style: bounded by the listing's
             * `houseRules.maxGuests` so the tenant can't request more
             * heads than the host advertised. Decrement is disabled at 1
             * (a booking with 0 guests is meaningless); increment is
             * disabled once we reach the listed maximum.
             */}
            <div className="space-y-2">
              <div className="flex items-center justify-between gap-2">
                <Label
                  htmlFor="apply-dialog-guest-count"
                  className="flex items-center gap-2 text-sm font-medium"
                >
                  <Users className="h-4 w-4 text-muted-foreground" />
                  Guests
                </Label>
                <p className="text-xs text-muted-foreground">
                  Max {maxGuests} {maxGuests === 1 ? "guest" : "guests"}
                </p>
              </div>
              <div className="flex items-center justify-between rounded-lg border bg-background px-3 py-2">
                <div>
                  <p
                    id="apply-dialog-guest-count"
                    className="text-sm font-medium"
                  >
                    {guestLabel}
                  </p>
                  <p className="text-[11px] text-muted-foreground">
                    Include yourself in the headcount.
                  </p>
                </div>
                <div className="flex items-center gap-1">
                  <Button
                    type="button"
                    size="icon"
                    variant="outline"
                    className="h-8 w-8 rounded-full"
                    onClick={() =>
                      setGuestCount((prev) => Math.max(1, prev - 1))
                    }
                    disabled={!canDecrementGuests}
                    aria-label="Decrease guest count"
                  >
                    <Minus className="h-3.5 w-3.5" />
                  </Button>
                  <span
                    className={cn(
                      "min-w-[2rem] text-center text-sm font-semibold tabular-nums",
                      !guestsValid && "text-destructive",
                    )}
                    aria-live="polite"
                  >
                    {guestCount}
                  </span>
                  <Button
                    type="button"
                    size="icon"
                    variant="outline"
                    className="h-8 w-8 rounded-full"
                    onClick={() =>
                      setGuestCount((prev) => Math.min(maxGuests, prev + 1))
                    }
                    disabled={!canIncrementGuests}
                    aria-label="Increase guest count"
                  >
                    <Plus className="h-3.5 w-3.5" />
                  </Button>
                </div>
              </div>
            </div>

            {/*
             * Cover note. Optional. We trim whitespace on submit and
             * send `null` when empty so the host doesn't see a blank
             * "Message from <guest>" section on the detail dialog.
             */}
            <div className="space-y-2">
              <Label
                htmlFor="apply-dialog-message"
                className="text-sm font-medium"
              >
                Message to the host{" "}
                <span className="text-muted-foreground font-normal">
                  (optional)
                </span>
              </Label>
              <Textarea
                id="apply-dialog-message"
                value={message}
                onChange={(e) => setMessage(e.target.value)}
                placeholder="Introduce yourself, share why you're booking, who's coming, and anything else the host should know."
                rows={4}
                maxLength={MESSAGE_MAX_LENGTH + 1}
                className={cn(messageTooLong && "border-destructive")}
              />
              <div className="flex items-center justify-between text-[11px]">
                <span
                  className={cn(
                    "text-muted-foreground",
                    messageTooLong && "text-destructive",
                  )}
                >
                  Hosts read these to decide who they accept.
                </span>
                <span
                  className={cn(
                    "tabular-nums",
                    messageTooLong
                      ? "text-destructive font-medium"
                      : "text-muted-foreground",
                  )}
                >
                  {messageLength}/{MESSAGE_MAX_LENGTH}
                </span>
              </div>
            </div>

            <div className="flex items-start gap-2 rounded-md bg-muted/40 p-2 text-[11px] text-muted-foreground">
              <ShieldCheck className="h-3.5 w-3.5 mt-0.5 shrink-0" />
              <span>
                {listing.instantBookingEnabled
                  ? "You won't be charged here. After the host confirms the Truth Surface, you'll review the final terms (deposit + insurance) and pay on the deal page."
                  : "You won't be charged here. The host has 72 hours to accept. Once they do, you'll review the final terms and pay on the deal page."}
              </span>
            </div>

            {submitErrorMessage && (
              <Alert variant="destructive">{submitErrorMessage}</Alert>
            )}

            <Button
              type="submit"
              className="w-full gap-2"
              disabled={!canSubmit}
            >
              <Send className="h-4 w-4" />
              {ctaLabel}
            </Button>

            <p className="text-[11px] text-muted-foreground">
              Submitting binds you to the listing's house rules and cancellation
              policy if the host accepts.
            </p>
          </form>
        )}
      </DialogContent>
    </Dialog>
  );
};
