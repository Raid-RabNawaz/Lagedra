import { useState, useCallback, useEffect } from "react";
import { isAxiosError } from "axios";
import { useParams, Link } from "react-router-dom";
import { ArrowLeft, ArrowRight, CreditCard, ShieldCheck, AlertCircle, Lock, Receipt } from "lucide-react";
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
import {
  useCheckoutStatus,
  useCreateCheckout,
  useConfirmCheckout,
} from "@/features/activation-billing/hooks/useCheckout";
import type { CheckoutDto, ErrorResponse } from "@/api/types";
import { formatMoney } from "@/utils/format";

function PaymentBreakdown({ checkout }: { checkout: CheckoutDto }) {
  const hostReceives =
    checkout.totalAmountCents - checkout.applicationFeeCents;

  return (
    <div className="rounded-lg border p-4 space-y-3">
      <h3 className="font-medium text-sm">Payment Breakdown</h3>
      <div className="space-y-2 text-sm">
        <div className="flex justify-between">
          <span className="text-muted-foreground">First month's rent</span>
          <span>{formatMoney(checkout.firstMonthRentCents)}</span>
        </div>
        <div className="flex justify-between">
          <span className="text-muted-foreground">Security deposit</span>
          <span>{formatMoney(checkout.depositAmountCents)}</span>
        </div>
        <div className="flex justify-between">
          <span className="text-muted-foreground">Insurance premium</span>
          <span>{formatMoney(checkout.insuranceFeeCents)}</span>
        </div>
        <div className="border-t pt-2 flex justify-between font-semibold">
          <span>Total</span>
          <span>{formatMoney(checkout.totalAmountCents)}</span>
        </div>
      </div>
      <div className="pt-2 border-t text-xs text-muted-foreground space-y-1">
        <p>
          Platform fee ({formatMoney(checkout.applicationFeeCents)}) is deducted
          from the total. The host receives {formatMoney(hostReceives)}.
        </p>
      </div>
    </div>
  );
}

function CheckoutForm({
  checkout,
  dealId,
}: {
  checkout: CheckoutDto;
  dealId: string;
}) {
  const stripe = useStripe();
  const elements = useElements();
  const confirmCheckout = useConfirmCheckout();
  const [error, setError] = useState<string | null>(null);
  const [processing, setProcessing] = useState(false);
  const [succeeded, setSucceeded] = useState(checkout.status === "succeeded");

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
          return_url: `${window.location.origin}/app/deals/${dealId}/billing`,
        },
        redirect: "if_required",
      });

      if (confirmError) {
        setError(confirmError.message ?? "Payment failed.");
        setProcessing(false);
      } else {
        try {
          await confirmCheckout.mutateAsync(dealId);
        } catch {
          // Deal activation runs async; status page will reflect it
        }
        setSucceeded(true);
        setProcessing(false);
      }
    },
    [stripe, elements, dealId, confirmCheckout],
  );

  if (succeeded) {
    return (
      <div className="text-center py-8">
        <ShieldCheck className="h-12 w-12 text-emerald-600 mx-auto mb-3" />
        <h2 className="text-lg font-semibold">Payment Successful</h2>
        <p className="text-sm text-muted-foreground mt-1 max-w-sm mx-auto">
          Your payment has been processed successfully. The deal is being activated
          and the booking will be confirmed shortly.
        </p>
        <div className="mt-6 flex flex-col sm:flex-row items-center justify-center gap-3">
          <Link to={`/app/deals/${dealId}`}>
            <Button size="lg" className="gap-2">
              Go to My Deal
              <ArrowRight className="h-4 w-4" />
            </Button>
          </Link>
          <Link to={`/app/deals/${dealId}/billing`}>
            <Button variant="outline" size="lg" className="gap-2">
              <Receipt className="h-4 w-4" />
              View Billing
            </Button>
          </Link>
        </div>
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
        {processing ? "Processing..." : `Pay ${formatMoney(checkout.totalAmountCents)}`}
      </Button>
    </form>
  );
}

export default function CheckoutPage() {
  const { dealId } = useParams<{ dealId: string }>();
  const { data: checkoutStatus, isLoading: statusLoading } = useCheckoutStatus(dealId);
  const createCheckout = useCreateCheckout();
  const [checkout, setCheckout] = useState<CheckoutDto | null>(null);
  const [stripePromise, setStripePromise] = useState<ReturnType<typeof loadStripe> | null>(null);

  useEffect(() => {
    if (checkoutStatus && checkoutStatus.clientSecret) {
      setCheckout(checkoutStatus);
    }
  }, [checkoutStatus]);

  useEffect(() => {
    if (!checkout?.clientSecret || !appConfig.stripePublishableKey || stripePromise) {
      return;
    }

    setStripePromise(loadStripe(appConfig.stripePublishableKey));
  }, [checkout?.clientSecret, stripePromise]);

  const handleStartCheckout = useCallback(() => {
    if (!dealId) return;
    createCheckout.mutate(dealId, {
      onSuccess: (data) => setCheckout(data),
    });
  }, [dealId, createCheckout]);

  if (!appConfig.stripePublishableKey) {
    return (
      <div className="mx-auto max-w-lg px-4 py-16 text-center">
        <Alert variant="destructive">
          Stripe is not configured. Please set VITE_STRIPE_PUBLISHABLE_KEY.
        </Alert>
      </div>
    );
  }

  if (statusLoading) {
    return <Loader label="Loading checkout..." />;
  }

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
    <div className="mx-auto max-w-lg px-4 py-8 sm:px-6 lg:px-8">
      <Link
        to={dealId ? `/app/deals/${dealId}` : "/app/deals"}
        className="inline-flex items-center gap-1.5 text-sm text-muted-foreground hover:text-foreground transition-colors mb-6"
      >
        <ArrowLeft className="h-4 w-4" />
        Back to deal
      </Link>

      <h1 className="text-2xl font-bold tracking-tight mb-2">Checkout</h1>
      <p className="text-sm text-muted-foreground mb-6">
        Complete your payment to activate the deal. Funds are securely processed
        by Stripe.
      </p>

      {checkoutStatus && (
        <div className="mb-6">
          <PaymentBreakdown checkout={checkoutStatus} />
        </div>
      )}

      {checkoutStatus?.status === "succeeded" && (
        <div className="rounded-lg border border-emerald-200 bg-emerald-50 p-4 text-center">
          <ShieldCheck className="h-8 w-8 text-emerald-600 mx-auto mb-2" />
          <p className="font-medium text-emerald-800">
            Payment already completed
          </p>
          <p className="text-sm text-emerald-700 mt-1">
            Your booking is confirmed. The deal is active.
          </p>
          <div className="mt-4 flex flex-col sm:flex-row items-center justify-center gap-3">
            <Link to={dealId ? `/app/deals/${dealId}` : "/app/deals"}>
              <Button size="sm" className="gap-2">
                Go to My Deal
                <ArrowRight className="h-4 w-4" />
              </Button>
            </Link>
            <Link to={`/app/deals/${dealId}/billing`}>
              <Button variant="outline" size="sm" className="gap-2">
                <Receipt className="h-4 w-4" />
                View Billing
              </Button>
            </Link>
          </div>
        </div>
      )}

      {checkoutStatus?.status !== "succeeded" && !checkout?.clientSecret && (
        <Card>
          <CardContent className="py-8 text-center">
            <Lock className="h-8 w-8 text-muted-foreground mx-auto mb-3" />
            <p className="text-sm text-muted-foreground mb-4">
              Ready to proceed? Click below to start the secure payment process.
            </p>
            <Button
              onClick={handleStartCheckout}
              disabled={createCheckout.isPending}
              size="lg"
              className="gap-2"
            >
              <CreditCard className="h-4 w-4" />
              {createCheckout.isPending
                ? "Preparing..."
                : "Proceed to Payment"}
            </Button>
            {createCheckout.isError && (
              <div className="mt-4 flex items-center gap-2 text-sm text-destructive justify-center">
                <AlertCircle className="h-4 w-4" />
                <span>
                  {isAxiosError<ErrorResponse>(createCheckout.error)
                    ? (createCheckout.error.response?.data?.detail
                      ?? createCheckout.error.response?.data?.message
                      ?? createCheckout.error.response?.data?.error
                      ?? createCheckout.error.message)
                    : ((createCheckout.error as Error)?.message
                      ?? "Failed to create checkout. Please try again.")}
                </span>
              </div>
            )}
          </CardContent>
        </Card>
      )}

      {checkout?.clientSecret && options && checkoutStatus?.status !== "succeeded" && (
        <Card>
          <CardHeader className="pb-3">
            <CardTitle className="text-base flex items-center gap-2">
              <CreditCard className="h-4 w-4" />
              Payment Details
            </CardTitle>
          </CardHeader>
          <CardContent>
            <Elements stripe={stripePromise} options={options}>
              <CheckoutForm checkout={checkout} dealId={dealId!} />
            </Elements>
          </CardContent>
        </Card>
      )}

      <div className="mt-6 rounded-lg bg-muted/50 p-4 text-xs text-muted-foreground flex items-start gap-2">
        <Lock className="h-4 w-4 shrink-0 mt-0.5" />
        <p>
          Your payment is securely processed by Stripe. Lagedra collects the
          total amount, deducts the platform fee and insurance premium, and
          transfers the remainder directly to the host's account.
        </p>
      </div>
    </div>
  );
}
