import { useState } from "react";
import { FileSearch, ExternalLink } from "lucide-react";
import { loadStripe } from "@stripe/stripe-js";
import { Elements, PaymentElement, useElements, useStripe } from "@stripe/react-stripe-js";
import { appConfig } from "@/app/config";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Alert } from "@/components/ui/alert";
import { formatMoney } from "@/utils/format";
import { getApiErrorMessage } from "@/api/errors";
import {
  formatScreenedOn,
} from "@/features/applications/lib/bookingConsent";
import {
  screeningStatusDetail,
} from "@/features/applications/lib/screeningStatus";
import { ScreeningStatusBadge } from "@/features/applications/components/ScreeningStatusBadge";
import { ScreeningReportViewer } from "@/features/applications/components/ScreeningReportViewer";
import {
  useApplicationScreening,
  useCreateScreeningCheckout,
  useScreeningReport,
} from "@/features/applications/hooks/useApplications";
import type { ScreeningCheckoutDto, TenantVerificationTier } from "@/api/types";

const stripePromise = appConfig.stripePublishableKey
  ? loadStripe(appConfig.stripePublishableKey)
  : null;

function CheckoutForm({
  checkout,
  applicationId,
  onDone,
}: {
  checkout: ScreeningCheckoutDto;
  applicationId: string;
  onDone: () => void;
}) {
  const stripe = useStripe();
  const elements = useElements();
  const reconcile = useCreateScreeningCheckout(applicationId);
  const [error, setError] = useState<string | null>(null);
  const [processing, setProcessing] = useState(false);

  if (!checkout.clientSecret) {
    return (
      <p className="text-sm text-muted-foreground">
        Screening started. Refresh if the status does not update shortly.
      </p>
    );
  }

  return (
    <form
      className="space-y-3"
      onSubmit={(e) => {
        e.preventDefault();
        if (!stripe || !elements) return;
        setProcessing(true);
        setError(null);
        void (async () => {
          const { error: submitError } = await elements.submit();
          if (submitError) {
            setError(submitError.message ?? "Submission failed.");
            setProcessing(false);
            return;
          }
          const { error: confirmError } = await stripe.confirmPayment({
            elements,
            confirmParams: {
              return_url: `${window.location.origin}/app/applications/${applicationId}`,
            },
            redirect: "if_required",
          });
          if (confirmError) {
            setError(confirmError.message ?? "Payment failed.");
            setProcessing(false);
            return;
          }
          try {
            await reconcile.mutateAsync();
            onDone();
          } catch (err) {
            setError(getApiErrorMessage(err, "Payment succeeded but screening did not start."));
            setProcessing(false);
          }
        })();
      }}
    >
      <PaymentElement />
      {error && <Alert variant="destructive">{error}</Alert>}
      <Button type="submit" disabled={!stripe || processing} className="w-full">
        {processing ? "Processing…" : `Pay ${formatMoney(checkout.amountCents)}`}
      </Button>
    </form>
  );
}

export function ApplicationScreeningPanel({
  applicationId,
  tenantTier,
  viewer = "host",
  embedded = false,
  className,
}: {
  applicationId: string;
  tenantTier?: TenantVerificationTier | string | null;
  viewer?: "host" | "guest";
  /** Match booking-request dialog sections instead of a standalone card. */
  embedded?: boolean;
  className?: string;
}) {
  const screening = useApplicationScreening(applicationId);
  const checkout = useCreateScreeningCheckout(applicationId);
  const [checkoutDto, setCheckoutDto] = useState<ScreeningCheckoutDto | null>(null);
  const [showReport, setShowReport] = useState(false);
  const report = useScreeningReport(applicationId, showReport);

  const data = screening.data;
  const screenedOn = formatScreenedOn(data?.lastScreenedAt ?? null);
  const thinHistory =
    tenantTier === "Unverified" || tenantTier === "Verified" || tenantTier == null;

  const title = (
    <>
      <FileSearch className="h-4 w-4" />
      Background &amp; credit check
    </>
  );

  const forHost = viewer === "host";
  const thisResult = data?.status === "Completed" ? data.latestResult : null;
  const statusDetail = screeningStatusDetail(
    data?.status,
    thisResult,
    forHost,
    forHost ? data?.lastFailureReason : null,
  );

  const body = (
    <div className="space-y-3 text-sm">
        {data?.status ? (
          <div className="space-y-2">
            <ScreeningStatusBadge status={data.status} result={thisResult} />
            {statusDetail && data?.status === "Failed" && forHost ? (
              <Alert variant="destructive">{statusDetail}</Alert>
            ) : statusDetail ? (
              <p className="text-muted-foreground">{statusDetail}</p>
            ) : null}
          </div>
        ) : thinHistory && data?.canRequest ? (
          <Alert>
            This guest has limited Lagedra history. A background and credit check is the
            usual way to add a second trust factor before you accept — a Pass alone still
            does not grant Elite.
          </Alert>
        ) : (
          <p className="text-muted-foreground">
            Optional. The host who requests it pays. A Pass is one factor in the guest&apos;s
            Lagedra trust profile — it never grants Elite on its own.
          </p>
        )}
        {screenedOn && (
          <p>
            Last completed check on this guest:{" "}
            <span className="font-medium">{screenedOn}</span>
            {data?.latestResult && data.status !== "Completed" ? ` · ${data.latestResult}` : null}
          </p>
        )}
        {data?.canCompleteApply && data.applicationUrl && (
          <a
            href={data.applicationUrl}
            target="_blank"
            rel="noreferrer"
            className="inline-flex items-center gap-1 text-primary underline"
          >
            Complete screening
            <ExternalLink className="h-3 w-3" />
          </a>
        )}
        {screening.isError && (
          <Alert variant="destructive">{getApiErrorMessage(screening.error, "Could not load screening.")}</Alert>
        )}
        {checkout.isError && (
          <Alert variant="destructive">{getApiErrorMessage(checkout.error, "Could not start checkout.")}</Alert>
        )}
        {data?.canRequest && !checkoutDto && (
          <Button
            variant="outline"
            disabled={checkout.isPending}
            onClick={() => {
              void checkout.mutateAsync().then((dto) => {
                if (!dto.clientSecret) {
                  void screening.refetch();
                  return;
                }
                setCheckoutDto(dto);
              });
            }}
          >
            {checkout.isPending
              ? "Starting…"
              : `Request check${data.hostFeeCents > 0 ? ` · ${formatMoney(data.hostFeeCents)}` : ""}`}
          </Button>
        )}
        {checkoutDto && stripePromise && checkoutDto.clientSecret && (
          <Elements stripe={stripePromise} options={{ clientSecret: checkoutDto.clientSecret }}>
            <CheckoutForm
              checkout={checkoutDto}
              applicationId={applicationId}
              onDone={() => {
                setCheckoutDto(null);
                void screening.refetch();
              }}
            />
          </Elements>
        )}
        {data?.canViewReport && (
          <div className="space-y-2">
            <Button variant="ghost" size="sm" onClick={() => setShowReport((v) => !v)}>
              {showReport ? "Hide report" : "View report"}
            </Button>
            {showReport && report.isError && (
              <Alert variant="destructive">
                {getApiErrorMessage(report.error, "Could not load the screening report.")}
              </Alert>
            )}
            {showReport && report.isLoading && (
              <p className="text-xs text-muted-foreground">Loading report…</p>
            )}
            {showReport && report.data && (
              <ScreeningReportViewer applicationId={applicationId} report={report.data} />
            )}
          </div>
        )}
    </div>
  );

  if (embedded) {
    return (
      <section className={className ?? "rounded-lg border bg-muted/20 p-4"}>
        <p className="mb-3 flex items-center gap-2 text-xs font-medium uppercase tracking-wide text-muted-foreground">
          {title}
        </p>
        {body}
      </section>
    );
  }

  return (
    <Card className={className ?? "mb-6"}>
      <CardHeader className="pb-3">
        <CardTitle className="flex items-center gap-2 text-base">{title}</CardTitle>
      </CardHeader>
      <CardContent>{body}</CardContent>
    </Card>
  );
}
