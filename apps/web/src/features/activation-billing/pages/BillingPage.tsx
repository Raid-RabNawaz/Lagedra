import { useState } from "react";
import { useParams, Navigate, Link } from "react-router-dom";
import {
  CreditCard,
  Clock,
  DollarSign,
  Receipt,
  AlertTriangle,
  CheckCircle2,
  XCircle,
  FileWarning,
  Ban,
  Shield,
  Lock,
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
import { useMyDeals } from "@/features/deals/hooks/useDeals";
import {
  getApiErrorMessage,
  isForbiddenError,
  isNotFoundError,
} from "@/api/errors";
import { BillingStatusBadge } from "@/features/activation-billing/components/BillingStatusBadge";
import { PaymentStatusBadge } from "@/features/activation-billing/components/PaymentStatusBadge";
import { InvoiceStatusBadge } from "@/features/activation-billing/components/InvoiceStatusBadge";
import { PaymentSecurityNotice } from "@/features/activation-billing/components/NonCustodialDisclaimer";
import { DisputePaymentDialog } from "@/features/activation-billing/components/DisputePaymentDialog";
import { CancelBookingDialog } from "@/features/activation-billing/components/CancelBookingDialog";
import { FileDamageClaimDialog } from "@/features/activation-billing/components/FileDamageClaimDialog";
import { ConfirmStopBillingDialog } from "@/features/activation-billing/components/ConfirmStopBillingDialog";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Alert } from "@/components/ui/alert";
import { Separator } from "@/components/ui/separator";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { BackLink } from "@/components/shared/BackLink";
import { Loader } from "@/components/shared/Loader";
import { formatDate, formatMoney } from "@/utils/format";

export const BillingPage = () => {
  const { dealId } = useParams<{ dealId: string }>();
  const user = useAuthStore((s) => s.user);
  const { data: deals, isLoading: dealsLoading } = useMyDeals("all");
  const deal = deals?.find((d) => d.dealId === dealId);
  // Tenant/Landlord were merged into a single "Member" role, so participation
  // must be checked per-deal. Authorize UI controls against the actual deal
  // participants rather than role; platform admins can read the page but do
  // not see participant-only actions.
  const isLandlord = !!user && !!deal && user.userId === deal.landlordUserId;
  const isTenant = !!user && !!deal && user.userId === deal.tenantUserId;

  const isPostActive =
    !!deal &&
    (deal.dealPhase === "Active" || deal.dealPhase === "Closed");

  const {
    data: billing,
    isLoading: billingLoading,
    error: billingError,
  } = useBillingStatus(isPostActive ? dealId : undefined);
  const {
    data: payment,
    isLoading: paymentLoading,
    error: paymentError,
  } = usePaymentStatus(isPostActive ? dealId : undefined);
  const { data: paymentDetailsData } = usePaymentDetails(
    isTenant && payment?.status === "Pending" ? dealId : undefined,
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

  const accessError = billingError ?? paymentError;
  const isAccessDenied = isForbiddenError(accessError);
  // 404 on /billing simply means the billing account hasn't been created yet
  // (deal not yet activated). That isn't an access problem, so we ignore it.
  const isOtherError =
    !!accessError && !isAccessDenied && !isNotFoundError(accessError);

  // Phase 16.6: enforce the single-money-surface rule **eagerly**. Redirect
  // pre-Active deals away from billing once the deal list has loaded.
  if (deal && dealId && !isPostActive) {
    const redirectTo =
      deal.dealPhase === "Checkout"
        ? `/app/deals/${dealId}/checkout`
        : `/app/deals/${dealId}`;
    return <Navigate to={redirectTo} replace />;
  }

  if (dealsLoading || billingLoading || paymentLoading) {
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
      setActionError(getApiErrorMessage(e, "Failed to stop billing."));
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
      setActionError(getApiErrorMessage(e, "Failed to confirm payment."));
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
      setActionError(getApiErrorMessage(e, "Failed to confirm platform payment."));
    }
  };

  if (isAccessDenied) {
    return (
      <div className="mx-auto max-w-3xl px-4 py-16 sm:px-6 lg:px-8">
        <BackLink fallbackTo="/app/deals" label="Back to deals" className="mb-6" />
        <Alert variant="destructive">
          <Lock className="h-4 w-4" />
          <span className="ml-2 text-sm">
            {getApiErrorMessage(
              accessError,
              "You do not have access to this deal's billing.",
            )}
          </span>
        </Alert>
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-3xl px-4 py-8 sm:px-6 lg:px-8">
      <BackLink
        fallbackTo={dealId ? `/app/deals/${dealId}` : "/app/deals"}
        label="Back to deal"
        className="mb-6"
      />

      <h1 className="text-2xl font-bold tracking-tight mb-6">
        Billing & Payments
      </h1>

      {isOtherError && (
        <Alert variant="destructive" className="mb-6">
          <AlertTriangle className="h-4 w-4" />
          <span className="ml-2 text-sm">
            {getApiErrorMessage(accessError, "Failed to load billing details.")}
          </span>
        </Alert>
      )}

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

            {isLandlord && (
              <>
                <Separator />
                <div>
                  <p className="text-sm font-medium mb-2">
                    Monthly platform fee history
                  </p>
                  {billing.invoices.length === 0 ? (
                    <p className="text-xs text-muted-foreground">
                      No platform-fee charges yet. Your first monthly fee will
                      appear here once Stripe bills the subscription.
                    </p>
                  ) : (
                    <Table>
                      <TableHeader>
                        <TableRow>
                          <TableHead>Period</TableHead>
                          <TableHead className="text-right">Amount</TableHead>
                          <TableHead className="text-right">Status</TableHead>
                        </TableRow>
                      </TableHeader>
                      <TableBody>
                        {billing.invoices.map((invoice) => (
                          <TableRow key={invoice.invoiceId}>
                            <TableCell className="whitespace-nowrap text-sm">
                              {formatDate(invoice.periodStart)}
                              {" – "}
                              {formatDate(invoice.periodEnd)}
                            </TableCell>
                            <TableCell className="text-right text-sm font-medium">
                              {formatMoney(invoice.amountCents)}
                            </TableCell>
                            <TableCell className="text-right">
                              <InvoiceStatusBadge status={invoice.status} />
                            </TableCell>
                          </TableRow>
                        ))}
                      </TableBody>
                    </Table>
                  )}
                  <p className="mt-2 text-xs text-muted-foreground">
                    See all your bookings' fees on the{" "}
                    <Link to="/app/billing" className="text-primary hover:underline">
                      platform fees statement
                    </Link>
                    .
                  </p>
                </div>
              </>
            )}

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

            {isTenant &&
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

              {isTenant &&
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

              {(isTenant || isLandlord) &&
                payment.status !== "Cancelled" &&
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
