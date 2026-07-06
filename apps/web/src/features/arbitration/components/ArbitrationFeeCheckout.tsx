import { useCallback, useEffect, useState } from "react";
import {
  CreditCard,
  ShieldCheck,
  AlertCircle,
  Lock,
  Scale,
} from "lucide-react";
import { loadStripe } from "@stripe/stripe-js";
import {
  Elements,
  PaymentElement,
  useStripe,
  useElements,
} from "@stripe/react-stripe-js";
import { appConfig } from "@/app/config";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Alert } from "@/components/ui/alert";
import { Loader } from "@/components/shared/Loader";
import { useCreateArbitrationFeeCheckout } from "@/features/arbitration/hooks/useArbitration";
import { formatMoney } from "@/utils/format";
import { getApiErrorMessage } from "@/api/errors";
import type { ArbitrationFeeCheckoutDto } from "@/api/types";

// Load Stripe.js once at module scope (the recommended pattern — avoids
// recreating the instance on every render).
const stripePromise = appConfig.stripePublishableKey
  ? loadStripe(appConfig.stripePublishableKey)
  : null;

function FeePaymentForm({
  checkout,
  caseId,
  onPaid,
}: {
  checkout: ArbitrationFeeCheckoutDto;
  caseId: string;
  onPaid: () => void;
}) {
  const stripe = useStripe();
  const elements = useElements();
  const reconcile = useCreateArbitrationFeeCheckout();
  const [error, setError] = useState<string | null>(null);
  const [processing, setProcessing] = useState(false);
  const [succeeded, setSucceeded] = useState(false);

  const handleSubmit = useCallback(
    async (e: React.FormEvent) => {
      e.preventDefault();
      if (!stripe || !elements) return;

      setProcessing(true);
      setError(null);

      const { error: submitError } = await elements.submit();
      if (submitError) {
        setError(submitError.message ?? "Submission failed.");
        setProcessing(false);
        return;
      }

      const { error: confirmError } = await stripe.confirmPayment({
        elements,
        confirmParams: {
          return_url: `${window.location.origin}/app/arbitration/${caseId}`,
        },
        redirect: "if_required",
      });

      if (confirmError) {
        setError(confirmError.message ?? "Payment failed.");
        setProcessing(false);
        return;
      }

      // Re-hit the checkout endpoint so the case activates immediately even if
      // the Stripe webhook is delayed or not configured locally; the backend
      // re-checks the PaymentIntent and marks the fee paid when it succeeded.
      try {
        await reconcile.mutateAsync(caseId);
      } catch {
        // The webhook is the backstop — the case will activate when it lands.
      }

      setSucceeded(true);
      setProcessing(false);
      onPaid();
    },
    [stripe, elements, caseId, reconcile, onPaid],
  );

  if (succeeded) {
    return (
      <div className="text-center py-6">
        <ShieldCheck className="h-10 w-10 text-emerald-600 mx-auto mb-3" />
        <h3 className="font-semibold">Filing fee paid</h3>
        <p className="text-sm text-muted-foreground mt-1 max-w-sm mx-auto">
          Thanks — your filing fee has been received. Your case is now open and
          moving into evidence collection and review.
        </p>
      </div>
    );
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      <PaymentElement />

      {error && (
        <Alert variant="destructive" className="text-sm">
          {error}
        </Alert>
      )}

      <Button
        type="submit"
        disabled={!stripe || !elements || processing}
        className="w-full gap-2"
        size="lg"
      >
        <CreditCard className="h-4 w-4" />
        {processing
          ? "Processing..."
          : `Pay ${formatMoney(checkout.amountCents)}`}
      </Button>
    </form>
  );
}

export function ArbitrationFeeCheckout({
  caseId,
  filingFeeCents,
  onPaid,
}: {
  caseId: string;
  filingFeeCents: number;
  onPaid: () => void;
}) {
  const createCheckout = useCreateArbitrationFeeCheckout();
  const [checkout, setCheckout] = useState<ArbitrationFeeCheckoutDto | null>(
    null,
  );
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let active = true;
    createCheckout
      .mutateAsync(caseId)
      .then((dto) => {
        if (!active) return;
        setCheckout(dto);
        // Already paid (e.g. a duplicate open) — let the parent refresh.
        if (dto.caseStatus !== "PendingPayment") onPaid();
      })
      .catch((e) => {
        if (active)
          setError(
            getApiErrorMessage(e, "Could not start the filing-fee payment."),
          );
      });
    return () => {
      active = false;
    };
    // Start the checkout exactly once for this case.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [caseId]);

  const options = checkout?.clientSecret
    ? {
        clientSecret: checkout.clientSecret,
        appearance: {
          theme: "stripe" as const,
          variables: { borderRadius: "8px" },
        },
      }
    : null;

  return (
    <Card className="border-amber-200">
      <CardHeader className="pb-3">
        <CardTitle className="text-base flex items-center gap-2">
          <Scale className="h-4 w-4 text-amber-600" />
          Pay the filing fee to open your case
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="rounded-lg border bg-muted/40 p-3 text-sm flex items-center justify-between">
          <span className="text-muted-foreground">Arbitration filing fee</span>
          <span className="font-semibold">{formatMoney(filingFeeCents)}</span>
        </div>

        {!appConfig.stripePublishableKey ? (
          <Alert variant="destructive" className="text-sm">
            Stripe is not configured. Please set VITE_STRIPE_PUBLISHABLE_KEY.
          </Alert>
        ) : error ? (
          <Alert variant="destructive" className="text-sm">
            <AlertCircle className="h-4 w-4" />
            <span className="ml-2">{error}</span>
          </Alert>
        ) : !checkout?.clientSecret || !options ? (
          <Loader label="Preparing secure payment..." />
        ) : (
          <Elements stripe={stripePromise} options={options}>
            <FeePaymentForm checkout={checkout} caseId={caseId} onPaid={onPaid} />
          </Elements>
        )}

        <div className="rounded-lg bg-muted/50 p-3 text-xs text-muted-foreground flex items-start gap-2">
          <Lock className="h-4 w-4 shrink-0 mt-0.5" />
          <p>
            Securely processed by Stripe. The filing fee is paid to Lagedra for
            adjudicating your dispute and is non-refundable. Your case stays on
            hold until the fee is paid.
          </p>
        </div>
      </CardContent>
    </Card>
  );
}
