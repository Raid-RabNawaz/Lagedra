import { useEffect, useState, type FormEvent } from "react";
import { Loader2, CreditCard } from "lucide-react";
import { loadStripe } from "@stripe/stripe-js";
import {
  Elements,
  PaymentElement,
  useElements,
  useStripe,
} from "@stripe/react-stripe-js";
import { applicationApi } from "@/features/applications/services/applicationApi";
import { useAttachApplicationPayment } from "@/features/applications/hooks/useApplications";
import { BOOKING_CONSENT_VERSION } from "@/features/applications/lib/bookingConsent";
import { appConfig } from "@/app/config";
import { extractErrorMessage } from "@/lib/errors";
import type { DealApplicationDto } from "@/api/types";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { Label } from "@/components/ui/label";
import { FormError } from "@/components/shared/FormError";

const stripePromise = appConfig.stripePublishableKey
  ? loadStripe(appConfig.stripePublishableKey)
  : null;

/**
 * Tenant completes a partner-created Pending request:
 * - TenantPays: save card + Truth Surface consent
 * - PartnerPays: Truth Surface consent only (company already attached card)
 */
export function CompletePartnerRequestPanel({
  application,
}: {
  application: DealApplicationDto;
}) {
  const needsPayment =
    application.payerType !== "PartnerOrganization" && !application.hasPaymentMethod;
  const needsConsent = !application.tenantConsentGiven;

  if (application.status !== "Pending" || (!needsPayment && !needsConsent)) {
    return null;
  }

  if (needsPayment) {
    return <TenantPaysCompletion application={application} />;
  }

  return <TenantConsentOnlyCompletion application={application} />;
}

function TenantConsentOnlyCompletion({
  application,
}: {
  application: DealApplicationDto;
}) {
  const attachMutation = useAttachApplicationPayment();
  const [consentChecked, setConsentChecked] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    if (!consentChecked) {
      setError("Please agree to the Truth Surface terms to continue.");
      return;
    }
    setError(null);
    try {
      await attachMutation.mutateAsync({
        id: application.applicationId,
        payload: {
          truthSurfaceConsentGiven: true,
          consentVersion: BOOKING_CONSENT_VERSION,
        },
      });
    } catch (err) {
      setError(extractErrorMessage(err));
    }
  };

  return (
    <form onSubmit={(e) => void handleSubmit(e)} className="space-y-4 rounded-lg border p-4">
      <div>
        <h3 className="font-medium">Confirm booking terms</h3>
        <p className="mt-1 text-sm text-muted-foreground">
          {application.partnerOrganizationName
            ? `${application.partnerOrganizationName} is paying for this booking. `
            : "Your partner organization is paying for this booking. "}
          Confirm the Truth Surface terms so the host can review the request.
        </p>
      </div>
      <div className="flex items-start gap-3">
        <Checkbox
          id="partner-ts-consent"
          checked={consentChecked}
          onCheckedChange={(v) => setConsentChecked(v === true)}
        />
        <Label htmlFor="partner-ts-consent" className="text-sm font-normal leading-relaxed">
          I agree to the Truth Surface terms for this stay.
        </Label>
      </div>
      {error && <FormError message={error} />}
      <Button type="submit" disabled={!consentChecked || attachMutation.isPending}>
        {attachMutation.isPending && <Loader2 className="h-4 w-4 animate-spin" />}
        Confirm terms
      </Button>
    </form>
  );
}

function TenantPaysCompletion({
  application,
}: {
  application: DealApplicationDto;
}) {
  const [clientSecret, setClientSecret] = useState<string | null>(null);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    void applicationApi
      .createSetupIntent(application.listingId)
      .then((result) => {
        if (!cancelled) setClientSecret(result.clientSecret);
      })
      .catch((err) => {
        if (!cancelled) setLoadError(extractErrorMessage(err));
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [application.listingId]);

  if (loading) {
    return (
      <div className="rounded-lg border p-4 text-sm text-muted-foreground">
        Preparing payment form…
      </div>
    );
  }

  if (loadError || !clientSecret || !stripePromise) {
    return (
      <Alert variant="destructive">
        <AlertDescription>
          {loadError ?? "Stripe is not available. Please try again later."}
        </AlertDescription>
      </Alert>
    );
  }

  return (
    <div className="space-y-3 rounded-lg border p-4">
      <div className="flex items-center gap-2 font-medium">
        <CreditCard className="h-4 w-4" />
        Complete payment authorization
      </div>
      <p className="text-sm text-muted-foreground">
        {application.partnerOrganizationName
          ? `${application.partnerOrganizationName} created this request for you. `
          : "A partner created this request for you. "}
        Save your card and agree to the Truth Surface terms so the host can approve.
      </p>
      <Elements
        stripe={stripePromise}
        options={{ clientSecret, appearance: { theme: "stripe" } }}
      >
        <TenantCardAndConsentForm applicationId={application.applicationId} />
      </Elements>
    </div>
  );
}

function TenantCardAndConsentForm({ applicationId }: { applicationId: string }) {
  const stripe = useStripe();
  const elements = useElements();
  const attachMutation = useAttachApplicationPayment();
  const [consentChecked, setConsentChecked] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [processing, setProcessing] = useState(false);

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    if (!stripe || !elements) return;
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

      const { error: confirmError, setupIntent } = await stripe.confirmSetup({
        elements,
        redirect: "if_required",
      });

      if (confirmError) {
        setError(confirmError.message ?? "Couldn't save your card.");
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

      await attachMutation.mutateAsync({
        id: applicationId,
        payload: {
          stripePaymentMethodId: paymentMethodId,
          truthSurfaceConsentGiven: true,
          consentVersion: BOOKING_CONSENT_VERSION,
        },
      });
    } catch (err) {
      setError(extractErrorMessage(err));
      setProcessing(false);
    }
  };

  return (
    <form onSubmit={(e) => void handleSubmit(e)} className="space-y-4">
      <PaymentElement />
      <div className="flex items-start gap-3">
        <Checkbox
          id="tenant-pay-consent"
          checked={consentChecked}
          onCheckedChange={(v) => setConsentChecked(v === true)}
        />
        <Label htmlFor="tenant-pay-consent" className="text-sm font-normal leading-relaxed">
          I agree to the Truth Surface terms for this stay.
        </Label>
      </div>
      {error && <FormError message={error} />}
      <Button
        type="submit"
        disabled={!stripe || !elements || !consentChecked || processing || attachMutation.isPending}
      >
        {(processing || attachMutation.isPending) && (
          <Loader2 className="h-4 w-4 animate-spin" />
        )}
        Save card &amp; complete request
      </Button>
    </form>
  );
}
