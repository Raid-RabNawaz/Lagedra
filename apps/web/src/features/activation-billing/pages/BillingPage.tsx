import { useState, useEffect } from "react";
import { useParams, Link, useSearchParams } from "react-router-dom";
import {
  ArrowLeft,
  CreditCard,
  Clock,
  DollarSign,
  Receipt,
  AlertTriangle,
  CheckCircle2,
  XCircle,
  ExternalLink,
  FileWarning,
  Ban,
  Shield,
} from "lucide-react";
import { useAuthStore } from "@/app/auth/authStore";
import {
  useBillingStatus,
  usePaymentStatus,
  usePaymentDetails,
  useStopBilling,
  useConfirmPayment,
  useConfirmPlatformPayment,
} from "@/features/activation-billing/hooks/useBilling";
import { useCheckoutStatus } from "@/features/activation-billing/hooks/useCheckout";
import { useMyDeals } from "@/features/deals/hooks/useDeals";
import { BillingStatusBadge } from "@/features/activation-billing/components/BillingStatusBadge";
import { PaymentStatusBadge } from "@/features/activation-billing/components/PaymentStatusBadge";
import { PaymentSecurityNotice } from "@/features/activation-billing/components/NonCustodialDisclaimer";
import { DisputePaymentDialog } from "@/features/activation-billing/components/DisputePaymentDialog";
import { CancelBookingDialog } from "@/features/activation-billing/components/CancelBookingDialog";
import { FileDamageClaimDialog } from "@/features/activation-billing/components/FileDamageClaimDialog";
import { ConfirmStopBillingDialog } from "@/features/activation-billing/components/ConfirmStopBillingDialog";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Alert } from "@/components/ui/alert";
import { Separator } from "@/components/ui/separator";
import { Loader } from "@/components/shared/Loader";
import { formatDate, formatMoney } from "@/utils/format";

export const BillingPage = () => {
  const { dealId } = useParams<{ dealId: string }>();
  const [searchParams] = useSearchParams();
  const user = useAuthStore((s) => s.user);
  const { data: deals } = useMyDeals("all");
  const deal = deals?.find((d) => d.dealId === dealId);
  const isLandlord = !!user && !!deal && user.userId === deal.landlordUserId;

  const { data: billing, isLoading: billingLoading } = useBillingStatus(dealId);
  const { data: payment, isLoading: paymentLoading } = usePaymentStatus(dealId);
  const { data: checkout, refetch: refetchCheckout } = useCheckoutStatus(dealId);
  const { data: paymentDetailsData } = usePaymentDetails(
    !isLandlord && payment?.status === "Pending" ? dealId : undefined,
  );

  const stopBilling = useStopBilling();
  const confirmPayment = useConfirmPayment();
  const confirmPlatformPayment = useConfirmPlatformPayment();

  const [actionError, setActionError] = useState<string | null>(null);
  const [actionSuccess, setActionSuccess] = useState<string | null>(null);
  const [showDisputeDialog, setShowDisputeDialog] = useState(false);
  const [showCancelDialog, setShowCancelDialog] = useState(false);
  const [showDamageClaimDialog, setShowDamageClaimDialog] = useState(false);
  const [showStopBillingDialog, setShowStopBillingDialog] = useState(false);

  useEffect(() => {
    const redirectStatus = searchParams.get("redirect_status");
    if (redirectStatus === "succeeded") {
      setActionSuccess("Payment completed successfully!");
      refetchCheckout();
    } else if (redirectStatus === "failed") {
      setActionError("Payment failed. Please try again.");
    }
  }, [searchParams, refetchCheckout]);

  if (billingLoading || paymentLoading) {
    return <Loader fullPage label="Loading billing..." />;
  }

  const handleStopBilling = async () => {
    if (!dealId) {
      return;
    }
    setActionError(null);
    try {
      await stopBilling.mutateAsync(dealId);
      setShowStopBillingDialog(false);
      setActionSuccess("Billing stopped successfully.");
    } catch (e) {
      setActionError((e as Error)?.message ?? "Failed to stop billing.");
    }
  };

  const handleConfirmPayment = async () => {
    if (!dealId) {
      return;
    }
    setActionError(null);
    try {
      await confirmPayment.mutateAsync(dealId);
      setActionSuccess("Payment confirmed successfully.");
    } catch (e) {
      setActionError((e as Error)?.message ?? "Failed to confirm payment.");
    }
  };

  const handleConfirmPlatformPayment = async () => {
    if (!dealId) {
      return;
    }
    setActionError(null);
    try {
      await confirmPlatformPayment.mutateAsync(dealId);
      setActionSuccess("Platform fee payment confirmed.");
    } catch (e) {
      setActionError(
        (e as Error)?.message ?? "Failed to confirm platform payment.",
      );
    }
  };

  const paymentCompleted = payment?.status === "Confirmed";
  const needsCheckout =
    payment && !paymentCompleted && checkout?.status !== "succeeded";

  return (
    <div className="mx-auto max-w-3xl px-4 py-8 sm:px-6 lg:px-8">
      <Link
        to={dealId ? `/app/deals/${dealId}` : "/app/deals"}
        className="inline-flex items-center gap-1.5 text-sm text-muted-foreground hover:text-foreground transition-colors mb-6"
      >
        <ArrowLeft className="h-4 w-4" />
        Back to deal
      </Link>

      <h1 className="text-2xl font-bold tracking-tight mb-6">
        Billing & Payments
      </h1>

      {actionSuccess && (
        <Alert className="border-emerald-200 bg-emerald-50 text-emerald-800 mb-6">
          <CheckCircle2 className="h-4 w-4" />
          <span className="ml-2 text-sm">{actionSuccess}</span>
        </Alert>
      )}

      {actionError && (
        <Alert variant="destructive" className="mb-6">
          <AlertTriangle className="h-4 w-4" />
          <span className="ml-2 text-sm">{actionError}</span>
        </Alert>
      )}

      {needsCheckout && !isLandlord && (
        <div className="rounded-lg border border-amber-200 bg-amber-50 p-4 mb-6">
          <div className="flex items-start gap-3">
            <CreditCard className="h-5 w-5 text-amber-600 mt-0.5" />
            <div>
              <p className="font-medium text-amber-800">
                Payment required to activate this deal
              </p>
              <p className="text-sm text-amber-700 mt-1">
                Complete the secure checkout to pay the first month's rent,
                deposit, and insurance. The deal will be activated automatically
                once payment is confirmed.
              </p>
              <Link to={`/app/deals/${dealId}/checkout`}>
                <Button size="sm" className="mt-3 gap-2">
                  <ExternalLink className="h-4 w-4" />
                  Go to Checkout
                </Button>
              </Link>
            </div>
          </div>
        </div>
      )}

      {billing && (
        <Card>
          <CardHeader className="pb-3">
            <div className="flex items-center justify-between">
              <CardTitle className="text-base flex items-center gap-2">
                <Receipt className="h-4 w-4" />
                Billing Account
              </CardTitle>
              <BillingStatusBadge status={billing.status} />
            </div>
          </CardHeader>
          <CardContent className="space-y-3">
            <div className="grid grid-cols-2 gap-3 text-sm">
              <div>
                <span className="text-muted-foreground">Start date</span>
                <p className="font-medium">{formatDate(billing.startDate)}</p>
              </div>
              {billing.endDate && (
                <div>
                  <span className="text-muted-foreground">End date</span>
                  <p className="font-medium">{formatDate(billing.endDate)}</p>
                </div>
              )}
              <div>
                <span className="text-muted-foreground">Invoices paid</span>
                <p className="font-medium">
                  {billing.paidInvoices} / {billing.totalInvoices}
                </p>
              </div>
              {billing.stripeSubscriptionId && (
                <div>
                  <span className="text-muted-foreground">
                    Platform fee subscription
                  </span>
                  <p className="font-mono text-xs mt-0.5">
                    {billing.stripeSubscriptionId.slice(0, 20)}...
                  </p>
                </div>
              )}
            </div>

            {billing.status === "Active" && isLandlord && (
              <>
                <Separator />
                <Button
                  variant="destructive"
                  size="sm"
                  onClick={() => setShowStopBillingDialog(true)}
                >
                  Stop Billing
                </Button>
              </>
            )}
          </CardContent>
        </Card>
      )}

      {payment && (
        <Card className="mt-6">
          <CardHeader className="pb-3">
            <div className="flex items-center justify-between">
              <CardTitle className="text-base flex items-center gap-2">
                <CreditCard className="h-4 w-4" />
                Payment Status
              </CardTitle>
              <PaymentStatusBadge status={payment.status} />
            </div>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="rounded-md border p-3 space-y-2">
              <div className="flex justify-between text-sm">
                <span className="text-muted-foreground">First month rent</span>
                <span className="font-medium">
                  {formatMoney(payment.firstMonthRentCents)}
                </span>
              </div>
              <div className="flex justify-between text-sm">
                <span className="text-muted-foreground">Security deposit</span>
                <span className="font-medium">
                  {formatMoney(payment.depositAmountCents)}
                </span>
              </div>
              <div className="flex justify-between text-sm">
                <span className="text-muted-foreground">Insurance premium</span>
                <span className="font-medium">
                  {formatMoney(payment.insuranceFeeCents)}
                </span>
              </div>
              <Separator />
              <div className="flex justify-between text-sm font-semibold">
                <span>Total</span>
                <span>{formatMoney(payment.totalTenantPaymentCents)}</span>
              </div>
            </div>

            <div className="flex justify-between text-sm rounded-md border p-3">
              <span className="text-muted-foreground flex items-center gap-1">
                <DollarSign className="h-3.5 w-3.5" />
                Monthly platform fee (host)
              </span>
              <span className="font-medium">
                {formatMoney(payment.monthlyProtocolFeeCents)}/mo
              </span>
            </div>

            <div className="grid grid-cols-2 gap-3 text-sm">
              <div className="flex items-center gap-2">
                {payment.hostConfirmed ? (
                  <CheckCircle2 className="h-4 w-4 text-emerald-600" />
                ) : (
                  <XCircle className="h-4 w-4 text-muted-foreground" />
                )}
                <span>
                  Payment{" "}
                  {payment.hostConfirmed ? "confirmed via Stripe" : "pending"}
                </span>
              </div>
              <div className="flex items-center gap-2">
                {payment.hostPaidPlatform ? (
                  <CheckCircle2 className="h-4 w-4 text-emerald-600" />
                ) : (
                  <Clock className="h-4 w-4 text-muted-foreground" />
                )}
                <span>
                  Platform fee{" "}
                  {payment.hostPaidPlatform ? "collected" : "pending"}
                </span>
              </div>
            </div>

            {payment.status === "Pending" && (
              <div className="flex items-center gap-2 text-sm text-muted-foreground">
                <Clock className="h-3.5 w-3.5" />
                Grace period expires:{" "}
                {formatDate(payment.gracePeriodExpiresAt)}
              </div>
            )}

            {payment.tenantDisputed && (
              <Alert className="border-red-300 bg-red-50 text-red-800">
                <AlertTriangle className="h-4 w-4" />
                <span className="ml-2 text-sm">
                  Payment disputed
                  {payment.disputeReason
                    ? `: ${payment.disputeReason}`
                    : ""}
                </span>
              </Alert>
            )}

            {!isLandlord &&
              paymentDetailsData?.paymentInfoPlain &&
              payment.status === "Pending" && (
                <div className="rounded-md border p-3 bg-muted/30">
                  <p className="text-xs font-medium text-muted-foreground mb-1">
                    Host Payment Details
                  </p>
                  <p className="text-sm font-mono whitespace-pre-wrap">
                    {paymentDetailsData.paymentInfoPlain}
                  </p>
                </div>
              )}

            <Separator />

            <div className="flex flex-wrap gap-2">
              {isLandlord &&
                payment.status === "Pending" &&
                !payment.hostConfirmed && (
                  <Button
                    size="sm"
                    className="gap-1.5"
                    onClick={handleConfirmPayment}
                    disabled={confirmPayment.isPending}
                  >
                    <CheckCircle2 className="h-4 w-4" />
                    {confirmPayment.isPending
                      ? "Confirming..."
                      : "Confirm Payment Received"}
                  </Button>
                )}

              {isLandlord &&
                payment.hostConfirmed &&
                !payment.hostPaidPlatform && (
                  <Button
                    size="sm"
                    className="gap-1.5"
                    onClick={handleConfirmPlatformPayment}
                    disabled={confirmPlatformPayment.isPending}
                  >
                    <Shield className="h-4 w-4" />
                    {confirmPlatformPayment.isPending
                      ? "Confirming..."
                      : "Confirm Platform Fee Paid"}
                  </Button>
                )}

              {!isLandlord &&
                (payment.status === "Pending" ||
                  payment.status === "Rejected") && (
                  <Button
                    variant="outline"
                    size="sm"
                    className="gap-1.5"
                    onClick={() => setShowDisputeDialog(true)}
                  >
                    <AlertTriangle className="h-4 w-4" />
                    Dispute Payment
                  </Button>
                )}

              {payment.status !== "Cancelled" &&
                payment.status !== "Confirmed" && (
                  <Button
                    variant="outline"
                    size="sm"
                    className="gap-1.5 text-red-600 hover:text-red-700 hover:bg-red-50"
                    onClick={() => setShowCancelDialog(true)}
                  >
                    <Ban className="h-4 w-4" />
                    Cancel Booking
                  </Button>
                )}

              {isLandlord && payment.status === "Confirmed" && (
                <Button
                  variant="outline"
                  size="sm"
                  className="gap-1.5"
                  onClick={() => setShowDamageClaimDialog(true)}
                >
                  <FileWarning className="h-4 w-4" />
                  File Damage Claim
                </Button>
              )}
            </div>
          </CardContent>
        </Card>
      )}

      <div className="mt-6">
        <PaymentSecurityNotice />
      </div>

      {dealId && (
        <>
          <DisputePaymentDialog
            dealId={dealId}
            open={showDisputeDialog}
            onOpenChange={setShowDisputeDialog}
          />
          <CancelBookingDialog
            dealId={dealId}
            open={showCancelDialog}
            onOpenChange={setShowCancelDialog}
          />
          {payment && (
            <FileDamageClaimDialog
              dealId={dealId}
              depositAmountCents={payment.depositAmountCents}
              open={showDamageClaimDialog}
              onOpenChange={setShowDamageClaimDialog}
            />
          )}
          <ConfirmStopBillingDialog
            open={showStopBillingDialog}
            onOpenChange={setShowStopBillingDialog}
            onConfirm={handleStopBilling}
            isPending={stopBilling.isPending}
          />
        </>
      )}
    </div>
  );
};
