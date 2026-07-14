import { useState, useMemo } from "react";
import { useParams } from "react-router-dom";
import { CreditCard, ShieldCheck } from "lucide-react";
import { loadStripe } from "@stripe/stripe-js";
import { Elements, PaymentElement, useStripe, useElements } from "@stripe/react-stripe-js";
import { appConfig } from "@/app/config";
import { BackLink } from "@/components/shared/BackLink";
import { NonCustodialDisclaimer } from "@/features/activation-billing/components/NonCustodialDisclaimer";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Alert } from "@/components/ui/alert";

const stripePromise = appConfig.stripePublishableKey
  ? loadStripe(appConfig.stripePublishableKey)
  : null;

function PaymentForm() {
  const stripe = useStripe();
  const elements = useElements();
  const [error, setError] = useState<string | null>(null);
  const [processing, setProcessing] = useState(false);
  const [succeeded, setSucceeded] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!stripe || !elements) {
      return;
    }

    setProcessing(true);
    setError(null);

    const { error: submitError } = await elements.submit();
    if (submitError) {
      setError(submitError.message ?? "Submission failed.");
      setProcessing(false);
      return;
    }

    const { error: confirmError } = await stripe.confirmSetup({
      elements,
      confirmParams: {
        return_url: `${window.location.origin}/app`,
      },
      redirect: "if_required",
    });

    if (confirmError) {
      setError(confirmError.message ?? "Setup failed.");
      setProcessing(false);
    } else {
      setSucceeded(true);
      setProcessing(false);
    }
  };

  if (succeeded) {
    return (
      <div className="text-center py-8">
        <ShieldCheck className="h-12 w-12 text-emerald-600 mx-auto mb-3" />
        <h2 className="text-lg font-semibold">Payment Method Saved</h2>
        <p className="text-sm text-muted-foreground mt-1">
          Your payment method has been securely stored with Stripe.
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
      >
        <CreditCard className="h-4 w-4" />
        {processing ? "Processing..." : "Save Payment Method"}
      </Button>
    </form>
  );
}

export const PaymentMethodPage = () => {
  const { dealId } = useParams<{ dealId: string }>();

  const options = useMemo(
    () => ({
      mode: "setup" as const,
      currency: "usd",
      appearance: {
        theme: "stripe" as const,
        variables: {
          borderRadius: "8px",
        },
      },
    }),
    [],
  );

  if (!stripePromise) {
    return (
      <div className="mx-auto max-w-lg px-4 py-16 sm:px-6 lg:px-8 text-center">
        <Alert variant="destructive">
          Stripe is not configured. Please set VITE_STRIPE_PUBLISHABLE_KEY.
        </Alert>
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-lg px-4 py-8 sm:px-6 lg:px-8">
      <BackLink
        fallbackTo={dealId ? `/app/deals/${dealId}/billing` : "/app"}
        label="Back to billing"
        className="mb-6"
      />

      <h1 className="text-2xl font-bold tracking-tight mb-6">
        Payment Method
      </h1>

      <div className="mb-6">
        <NonCustodialDisclaimer />
      </div>

      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="text-base flex items-center gap-2">
            <CreditCard className="h-4 w-4" />
            Add Payment Method
          </CardTitle>
        </CardHeader>
        <CardContent>
          <Elements stripe={stripePromise} options={options}>
            <PaymentForm />
          </Elements>
        </CardContent>
      </Card>
    </div>
  );
};
