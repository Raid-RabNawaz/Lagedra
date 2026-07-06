import { type FormEvent, useEffect, useMemo, useState } from "react";
import { isAxiosError } from "axios";
import { useNavigate } from "react-router-dom";
import {
  ArrowLeft,
  CreditCard,
  Lock,
  Minus,
  Plus,
  Send,
  ShieldCheck,
  Users,
} from "lucide-react";
import { loadStripe } from "@stripe/stripe-js";
import {
  Elements,
  PaymentElement,
  useElements,
  useStripe,
} from "@stripe/react-stripe-js";
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
import { Checkbox } from "@/components/ui/checkbox";
import { Loader } from "@/components/shared/Loader";
import { appConfig } from "@/app/config";
import { useAuthStore } from "@/app/auth/authStore";
import {
  useCreateBookingSetupIntent,
  useReservationPreview,
  useSubmitApplication,
} from "@/features/applications/hooks/useApplications";
import {
  BOOKING_CONSENT_VERSION,
  tierLabel,
} from "@/features/applications/lib/bookingConsent";
import { DateRangeField } from "@/features/listings/components/DateRangeField";
import { privacyApi } from "@/features/privacy/services/privacyApi";
import { formatMoney } from "@/utils/format";
import { cn } from "@/lib/utils";
import type { ListingDetailsDto, ReservationPreviewDto } from "@/api/types";
import { getApiErrorMessage } from "@/api/errors";

/** Server-side hard cap on the cover note. Mirrors `DealApplication.MessageMaxLength`. */
const MESSAGE_MAX_LENGTH = 1000;

const stripePromise = appConfig.stripePublishableKey
  ? loadStripe(appConfig.stripePublishableKey)
  : null;

type Props = {
  listing: ListingDetailsDto;
  initialCheckIn?: string;
  initialCheckOut?: string;
  controlledOpen?: boolean;
  onOpenChange?: (open: boolean) => void;
};

type Step = "details" | "payment";

/**
 * Predetermined-deposit apply flow:
 *  1. Tenant picks dates / guests / note and sees the full price breakdown
 *     (predetermined deposit for their verification tier + rent + fees + total).
 *  2. Tenant saves a card (Stripe SetupIntent, off-session) and agrees to the
 *     Truth Surface terms.
 *  3. On submit we confirm the SetupIntent to obtain a `pm_…` token and send it
 *     with the consent. No money moves now — the host's approval seals the
 *     Truth Surface and charges the saved card off-session.
 */
export const ApplyDialog = ({
  listing,
  initialCheckIn,
  initialCheckOut,
  controlledOpen,
  onOpenChange,
}: Props) => {
  const user = useAuthStore((s) => s.user);
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

  const [step, setStep] = useState<Step>("details");
  const [checkIn, setCheckIn] = useState(initialCheckIn ?? "");
  const [checkOut, setCheckOut] = useState(initialCheckOut ?? "");
  const [guestCount, setGuestCount] = useState(1);
  const [message, setMessage] = useState("");
  const [submitErrorMessage, setSubmitErrorMessage] = useState<string | null>(null);
  const [submitAttempted, setSubmitAttempted] = useState(false);
  const [clientSecret, setClientSecret] = useState<string | null>(null);

  const setupIntentMutation = useCreateBookingSetupIntent();

  const maxGuests = listing.houseRules?.maxGuests ?? 16;

  useEffect(() => {
    if (open) {
      if (initialCheckIn) setCheckIn(initialCheckIn);
      if (initialCheckOut) setCheckOut(initialCheckOut);
      setSubmitErrorMessage(null);
      setSubmitAttempted(false);
      setStep("details");
      setClientSecret(null);
    }
  }, [open, initialCheckIn, initialCheckOut]);

  useEffect(() => {
    setGuestCount((prev) => Math.min(Math.max(1, prev), maxGuests));
  }, [maxGuests]);

  const rawDayDelta =
    checkIn && checkOut
      ? Math.round(
          (new Date(checkOut).getTime() - new Date(checkIn).getTime()) / 86_400_000,
        )
      : 0;
  const stayDays = Math.max(0, rawDayDelta);

  const minStay = listing.minStayDays ?? 30;
  const maxStay = listing.maxStayDays ?? 365;
  const isValidStay = stayDays >= minStay && stayDays <= maxStay;
  const datesValid = Boolean(checkIn && checkOut && stayDays > 0 && isValidStay);

  const messageLength = message.trim().length;
  const messageTooLong = messageLength > MESSAGE_MAX_LENGTH;
  const guestsValid = guestCount >= 1 && guestCount <= maxGuests;
  const detailsValid = datesValid && guestsValid && !messageTooLong;

  const preview = useReservationPreview(listing.id, checkIn, checkOut, datesValid);

  const dateErrorMessage = !checkIn && !checkOut
    ? "Select check-in and check-out dates."
    : !checkIn
      ? "Select a check-in date."
      : !checkOut
        ? "Select a check-out date."
        : rawDayDelta <= 0
          ? "Check-out must be after check-in."
          : !isValidStay
            ? `Stay must be between ${minStay} and ${maxStay} days.`
            : "";
  const showDateError = submitAttempted && Boolean(dateErrorMessage);

  const canDecrementGuests = guestCount > 1;
  const canIncrementGuests = guestCount < maxGuests;

  const guestLabel = useMemo(
    () => (guestCount === 1 ? "1 guest" : `${guestCount} guests`),
    [guestCount],
  );

  const ctaLabel = listing.instantBookingEnabled ? "Book instantly" : "Request to book";

  const resetAndClose = () => {
    setOpen(false);
    setCheckIn("");
    setCheckOut("");
    setGuestCount(1);
    setMessage("");
    setStep("details");
    setClientSecret(null);
  };

  const handleContinueToPayment = async () => {
    setSubmitAttempted(true);
    if (!detailsValid || !user) return;

    setSubmitErrorMessage(null);
    try {
      const result = await setupIntentMutation.mutateAsync(listing.id);
      setClientSecret(result.clientSecret);
      setStep("payment");
    } catch (error) {
      setSubmitErrorMessage(
        getApiErrorMessage(error, "Couldn't start secure payment setup. Please try again."),
      );
    }
  };

  const stripeOptions = useMemo(
    () =>
      clientSecret
        ? {
            clientSecret,
            appearance: { theme: "stripe" as const, variables: { borderRadius: "8px" } },
          }
        : null,
    [clientSecret],
  );

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
            {step === "payment"
              ? "Review & confirm"
              : listing.instantBookingEnabled
                ? "Confirm your dates"
                : "Request these dates"}
          </DialogTitle>
        </DialogHeader>

        {step === "details" && (
          <form
            onSubmit={(e) => {
              e.preventDefault();
              void handleContinueToPayment();
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
                if (submitAttempted) setSubmitAttempted(false);
              }}
              minStayDays={minStay}
              maxStayDays={maxStay}
              error={showDateError}
              errorMessage={showDateError ? dateErrorMessage : undefined}
              id="apply-dialog-dates"
            />

            {stayDays > 0 && isValidStay && (
              <p className="text-sm text-muted-foreground">
                {stayDays} day{stayDays !== 1 ? "s" : ""}
              </p>
            )}

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
                  <p id="apply-dialog-guest-count" className="text-sm font-medium">
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
                    onClick={() => setGuestCount((prev) => Math.max(1, prev - 1))}
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
                    onClick={() => setGuestCount((prev) => Math.min(maxGuests, prev + 1))}
                    disabled={!canIncrementGuests}
                    aria-label="Increase guest count"
                  >
                    <Plus className="h-3.5 w-3.5" />
                  </Button>
                </div>
              </div>
            </div>

            <div className="space-y-2">
              <Label htmlFor="apply-dialog-message" className="text-sm font-medium">
                Message to the host{" "}
                <span className="text-muted-foreground font-normal">(optional)</span>
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
                <span className={cn("text-muted-foreground", messageTooLong && "text-destructive")}>
                  Hosts read these to decide who they accept.
                </span>
                <span
                  className={cn(
                    "tabular-nums",
                    messageTooLong ? "text-destructive font-medium" : "text-muted-foreground",
                  )}
                >
                  {messageLength}/{MESSAGE_MAX_LENGTH}
                </span>
              </div>
            </div>

            {datesValid && (
              <PriceBreakdown
                preview={preview.data}
                loading={preview.isLoading}
                error={preview.isError}
              />
            )}

            {submitErrorMessage && <Alert variant="destructive">{submitErrorMessage}</Alert>}

            <Button
              type="submit"
              className={cn("w-full gap-2", !detailsValid && "opacity-60")}
              aria-disabled={!detailsValid}
              disabled={messageTooLong || setupIntentMutation.isPending}
              title={!detailsValid ? "Pick valid check-in and check-out dates first." : undefined}
            >
              <CreditCard className="h-4 w-4" />
              {setupIntentMutation.isPending ? "Preparing…" : "Continue to payment"}
            </Button>

            <p className="text-[11px] text-muted-foreground">
              You won't be charged now. Your card is charged only if the host accepts.
            </p>
          </form>
        )}

        {step === "payment" && (
          <div className="space-y-4">
            <PriceBreakdown
              preview={preview.data}
              loading={preview.isLoading}
              error={preview.isError}
            />

            {!stripePromise ? (
              <Alert variant="destructive">
                Stripe is not configured. Please set VITE_STRIPE_PUBLISHABLE_KEY.
              </Alert>
            ) : stripeOptions ? (
              <Elements stripe={stripePromise} options={stripeOptions}>
                <PaymentConsentStep
                  listingId={listing.id}
                  instantBooking={listing.instantBookingEnabled}
                  totalCents={preview.data?.totalPayableCents ?? null}
                  checkIn={checkIn}
                  checkOut={checkOut}
                  guestCount={guestCount}
                  message={message}
                  userId={user?.userId ?? null}
                  ctaLabel={ctaLabel}
                  onBack={() => setStep("details")}
                  onDone={(nextPath) => {
                    resetAndClose();
                    if (nextPath) navigate(nextPath);
                  }}
                />
              </Elements>
            ) : (
              <div className="py-8">
                <Loader label="Preparing secure payment…" />
              </div>
            )}
          </div>
        )}
      </DialogContent>
    </Dialog>
  );
};

function PriceBreakdown({
  preview,
  loading,
  error,
}: {
  preview: ReservationPreviewDto | undefined;
  loading: boolean;
  error: boolean;
}) {
  if (loading) {
    return (
      <div className="rounded-lg border p-4">
        <Loader label="Calculating your price…" />
      </div>
    );
  }

  if (error || !preview) {
    return (
      <Alert variant="destructive" className="text-sm">
        Couldn't load the price breakdown. Please re-check your dates.
      </Alert>
    );
  }

  return (
    <div className="rounded-lg border p-4 space-y-3">
      <div className="flex items-center justify-between">
        <h3 className="text-sm font-medium">Price breakdown</h3>
        <span className="rounded-full bg-secondary px-2 py-0.5 text-[11px] font-medium">
          {tierLabel(preview.tier)} tenant
        </span>
      </div>
      <div className="space-y-2 text-sm">
        <div className="flex justify-between">
          <span className="text-muted-foreground">First month's rent</span>
          <span>{formatMoney(preview.firstMonthRentCents)}</span>
        </div>
        <div className="flex justify-between">
          <span className="text-muted-foreground">Security deposit</span>
          <span>{formatMoney(preview.depositCents)}</span>
        </div>
        {preview.insuranceFeeCents > 0 && (
          <div className="flex justify-between">
            <span className="text-muted-foreground">Insurance premium</span>
            <span>{formatMoney(preview.insuranceFeeCents)}</span>
          </div>
        )}
        {preview.serviceFeeCents > 0 && (
          <div className="flex justify-between">
            <span className="text-muted-foreground">Service fee</span>
            <span>{formatMoney(preview.serviceFeeCents)}</span>
          </div>
        )}
        <div className="border-t pt-2 flex justify-between font-semibold">
          <span>Total charged on approval</span>
          <span>{formatMoney(preview.totalPayableCents)}</span>
        </div>
      </div>
      {preview.depositReason && (
        <p className="flex items-start gap-1.5 text-[11px] text-muted-foreground">
          <ShieldCheck className="h-3.5 w-3.5 mt-0.5 shrink-0 text-emerald-600" />
          {preview.depositReason}
        </p>
      )}
      <p className="flex items-start gap-1.5 border-t pt-2 text-[11px] text-muted-foreground">
        <Lock className="h-3.5 w-3.5 mt-0.5 shrink-0" />
        Your first month's rent and deposit are paid directly to the host through
        Stripe. Lagedra only collects its service fee and the insurance premium —
        we never hold your funds. The host returns your deposit directly after
        move-out.
      </p>
    </div>
  );
}

function PaymentConsentStep({
  listingId,
  instantBooking,
  totalCents,
  checkIn,
  checkOut,
  guestCount,
  message,
  userId,
  ctaLabel,
  onBack,
  onDone,
}: {
  listingId: string;
  instantBooking: boolean;
  totalCents: number | null;
  checkIn: string;
  checkOut: string;
  guestCount: number;
  message: string;
  userId: string | null;
  ctaLabel: string;
  onBack: () => void;
  onDone: (nextPath: string | null) => void;
}) {
  const stripe = useStripe();
  const elements = useElements();
  const submitMutation = useSubmitApplication();
  const [consentChecked, setConsentChecked] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [processing, setProcessing] = useState(false);

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    if (!stripe || !elements || !userId) return;
    if (!consentChecked) {
      setError("Please agree to the Truth Surface terms to continue.");
      return;
    }

    setProcessing(true);
    setError(null);

    try {
      const { error: submitError } = await elements.submit();
      if (submitError) {
        setError(submitError.message ?? "Please check your card details.");
        setProcessing(false);
        return;
      }

      // Confirm the SetupIntent off-session so we get a reusable pm_ token.
      // No money moves here — the host's approval charges it later.
      const { error: confirmError, setupIntent } = await stripe.confirmSetup({
        elements,
        redirect: "if_required",
      });

      if (confirmError) {
        setError(confirmError.message ?? "Couldn't save your card. Please try again.");
        setProcessing(false);
        return;
      }

      const paymentMethodId =
        typeof setupIntent?.payment_method === "string"
          ? setupIntent.payment_method
          : (setupIntent?.payment_method?.id ?? null);

      if (!paymentMethodId) {
        setError("Couldn't confirm your saved card. Please try again.");
        setProcessing(false);
        return;
      }

      await privacyApi.ensureRequiredConsents(userId);

      const trimmedMessage = message.trim();
      const result = await submitMutation.mutateAsync({
        listingId,
        requestedCheckIn: checkIn,
        requestedCheckOut: checkOut,
        guestCount,
        message: trimmedMessage.length > 0 ? trimmedMessage : null,
        stripePaymentMethodId: paymentMethodId,
        truthSurfaceConsentGiven: true,
        consentVersion: BOOKING_CONSENT_VERSION,
      });

      onDone(result?.nextPath ?? null);
    } catch (err) {
      if (isAxiosError(err) && err.response?.status === 451) {
        setError(
          "Please complete KYC and data-processing consent before submitting a request.",
        );
        setProcessing(false);
        return;
      }
      setError(getApiErrorMessage(err, "Failed to submit your request. Please try again."));
      setProcessing(false);
    }
  };

  const payLabel =
    totalCents != null ? `${ctaLabel} · ${formatMoney(totalCents)} on approval` : ctaLabel;

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      <div className="space-y-2">
        <Label className="flex items-center gap-2 text-sm font-medium">
          <CreditCard className="h-4 w-4 text-muted-foreground" />
          Payment method
        </Label>
        <PaymentElement />
        <p className="flex items-start gap-1.5 text-[11px] text-muted-foreground">
          <Lock className="h-3.5 w-3.5 mt-0.5 shrink-0" />
          Your card is securely saved with Stripe. We never store card details and
          you're only charged if the host accepts.
        </p>
      </div>

      <label className="flex items-start gap-2 rounded-md border bg-muted/30 p-3 text-xs text-muted-foreground cursor-pointer">
        <Checkbox
          checked={consentChecked}
          onCheckedChange={(checked) => {
            setConsentChecked(checked);
            if (checked) setError(null);
          }}
          className="mt-0.5"
        />
        <span>
          I agree to the Truth Surface agreement and the listing's house rules and
          cancellation policy. When the host accepts, this seals an immutable,
          signed record of the deal and authorises the deposit + first month's
          rent + fees to be charged to my saved card. My rent and deposit are paid
          directly to the host via Stripe (Lagedra never holds your funds). After
          move-out the host returns my deposit directly, less any agreed or
          arbitrated deductions; the booking is only marked complete once the host
          confirms the deposit was returned and I confirm I received it. If it
          isn't returned, I can raise an arbitration case.
        </span>
      </label>

      {error && <Alert variant="destructive" className="text-sm">{error}</Alert>}

      <div className="flex items-center gap-2">
        <Button type="button" variant="outline" className="gap-2" onClick={onBack} disabled={processing}>
          <ArrowLeft className="h-4 w-4" />
          Back
        </Button>
        <Button
          type="submit"
          className="flex-1 gap-2"
          disabled={!stripe || !elements || processing || !consentChecked}
        >
          <Send className="h-4 w-4" />
          {processing ? "Submitting…" : payLabel}
        </Button>
      </div>

      <p className="text-[11px] text-muted-foreground">
        {instantBooking
          ? "Instant book: your card is charged immediately once the agreement seals."
          : "The host has 72 hours to accept. Your card is charged only when they do."}
      </p>
    </form>
  );
}
