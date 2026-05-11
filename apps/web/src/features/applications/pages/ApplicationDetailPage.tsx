import { useState } from "react";
import { useParams, Link } from "react-router-dom";
import {
  ArrowLeft,
  Calendar,
  Clock,
  DollarSign,
  Shield,
  CheckCircle2,
  XCircle,
  AlertTriangle,
  Lock,
} from "lucide-react";
import {
  useApplicationDetail,
  useApproveApplication,
  useRejectApplication,
} from "@/features/applications/hooks/useApplications";
import { useListingDetail } from "@/features/listings/hooks/useListings";
import { ApplicationStatusBadge } from "@/features/applications/components/ApplicationStatusBadge";
import { useAuthStore } from "@/app/auth/authStore";
import { isAdmin } from "@/app/auth/permissions";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { buttonVariants } from "@/components/ui/button-variants";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Alert } from "@/components/ui/alert";
import { Separator } from "@/components/ui/separator";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
import { Loader } from "@/components/shared/Loader";
import { formatDate, formatMoney } from "@/utils/format";
import { cn } from "@/lib/utils";
import {
  getApiErrorMessage,
  isForbiddenError,
  isNotFoundError,
} from "@/api/errors";

export const ApplicationDetailPage = () => {
  const { id } = useParams<{ id: string }>();
  const {
    data: application,
    isLoading,
    isError,
    error,
  } = useApplicationDetail(id);
  const { data: listing } = useListingDetail(application?.listingId);
  const approveMutation = useApproveApplication();
  const rejectMutation = useRejectApplication();
  const user = useAuthStore((s) => s.user);

  const [depositInput, setDepositInput] = useState("");
  const [approveOpen, setApproveOpen] = useState(false);

  if (isLoading) return <Loader fullPage label="Loading application..." />;

  if (isForbiddenError(error)) {
    return (
      <div className="mx-auto max-w-3xl px-4 py-16 sm:px-6 lg:px-8">
        <Link
          to="/app/applications"
          className={cn(buttonVariants({ variant: "outline" }), "mb-6")}
        >
          <ArrowLeft className="h-4 w-4" />
          Back to applications
        </Link>
        <Alert variant="destructive">
          <Lock className="h-4 w-4" />
          <span className="ml-2">
            {getApiErrorMessage(
              error,
              "You do not have access to this application.",
            )}
          </span>
        </Alert>
      </div>
    );
  }

  if (isError || !application) {
    return (
      <div className="mx-auto max-w-3xl px-4 py-16 sm:px-6 lg:px-8 text-center">
        <p className="text-destructive font-medium">
          {isNotFoundError(error)
            ? "Application not found."
            : getApiErrorMessage(error, "Failed to load application.")}
        </p>
        <Link to="/app/applications" className={cn(buttonVariants({ variant: "outline" }), "mt-4")}>
          <ArrowLeft className="h-4 w-4" />
          Back to applications
        </Link>
      </div>
    );
  }

  // Tenant and Landlord were merged into a single "Member" role, so we cannot
  // gate UI by role alone. Authorize per-application: only the host of the
  // listing (or a platform admin) sees the approve/reject controls.
  const isApplicationLandlord = !!user && user.userId === application.landlordUserId;
  const isApplicationTenant = !!user && user.userId === application.tenantUserId;
  const isPlatformAdmin = !!user && isAdmin(user.role);
  const canDecide = isApplicationLandlord || isPlatformAdmin;

  const isPending = application.status === "Pending";
  const maxDeposit = listing?.maxDepositCents ?? 0;
  const depositCents = Math.round(Number(depositInput) * 100);
  const isValidDeposit = depositCents > 0 && depositCents <= maxDeposit;

  const handleApprove = async () => {
    if (!isValidDeposit) return;
    try {
      await approveMutation.mutateAsync({
        id: application.applicationId,
        payload: { depositAmountCents: depositCents },
      });
      setApproveOpen(false);
    } catch {
      // Error is surfaced by mutation state in the dialog.
    }
  };

  const handleReject = async () => {
    try {
      await rejectMutation.mutateAsync(application.applicationId);
    } catch {
      // Error is surfaced by mutation state below the actions.
    }
  };

  return (
    <div className="mx-auto max-w-3xl px-4 py-8 sm:px-6 lg:px-8">
      <Link
        to="/app/applications"
        className="inline-flex items-center gap-1.5 text-sm text-muted-foreground hover:text-foreground transition-colors mb-6"
      >
        <ArrowLeft className="h-4 w-4" />
        Back to applications
      </Link>

      <div className="flex items-center gap-3 mb-6">
        <h1 className="text-2xl font-bold tracking-tight">Application</h1>
        <ApplicationStatusBadge status={application.status} />
      </div>

      {application.jurisdictionWarning && (
        <Alert className="mb-6 border-amber-300 bg-amber-50 text-amber-800">
          <AlertTriangle className="h-4 w-4" />
          <span className="ml-2 text-sm">{application.jurisdictionWarning}</span>
        </Alert>
      )}

      <div className="grid gap-6 md:grid-cols-2">
        {/* Stay details */}
        <Card>
          <CardHeader className="pb-3">
            <CardTitle className="text-base">Stay Details</CardTitle>
          </CardHeader>
          <CardContent className="space-y-3">
            <div className="flex items-center gap-2 text-sm">
              <Calendar className="h-4 w-4 text-muted-foreground" />
              <span className="font-medium">Check-in:</span>
              {application.requestedCheckIn}
            </div>
            <div className="flex items-center gap-2 text-sm">
              <Calendar className="h-4 w-4 text-muted-foreground" />
              <span className="font-medium">Check-out:</span>
              {application.requestedCheckOut}
            </div>
            <div className="flex items-center gap-2 text-sm">
              <Clock className="h-4 w-4 text-muted-foreground" />
              <span className="font-medium">Duration:</span>
              {application.stayDurationDays} days
            </div>
          </CardContent>
        </Card>

        {/* Financial details */}
        <Card>
          <CardHeader className="pb-3">
            <CardTitle className="text-base">Financials</CardTitle>
          </CardHeader>
          <CardContent className="space-y-3">
            {application.firstMonthRentCents != null && (
              <div className="flex items-center gap-2 text-sm">
                <DollarSign className="h-4 w-4 text-muted-foreground" />
                <span className="font-medium">Monthly rent:</span>
                {formatMoney(application.firstMonthRentCents)}
              </div>
            )}
            {application.depositAmountCents != null && (
              <div className="flex items-center gap-2 text-sm">
                <DollarSign className="h-4 w-4 text-muted-foreground" />
                <span className="font-medium">Deposit:</span>
                {formatMoney(application.depositAmountCents)}
              </div>
            )}
            {application.insuranceFeeCents != null && (
              <div className="flex items-center gap-2 text-sm">
                <Shield className="h-4 w-4 text-muted-foreground" />
                <span className="font-medium">Insurance fee:</span>
                {formatMoney(application.insuranceFeeCents)}
              </div>
            )}
            {listing && (
              <div className="flex items-center gap-2 text-sm text-muted-foreground">
                <span>Max deposit allowed: {formatMoney(listing.maxDepositCents)}</span>
              </div>
            )}
          </CardContent>
        </Card>
      </div>

      {/* Listing info */}
      {listing && (
        <>
          <Separator className="my-6" />
          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="text-base">Listing</CardTitle>
            </CardHeader>
            <CardContent>
              <Link
                to={`/listings/${listing.id}`}
                className="text-sm font-medium hover:underline"
              >
                {listing.title}
              </Link>
              <p className="text-sm text-muted-foreground mt-1">
                {formatMoney(listing.monthlyRentCents)} / month · {listing.bedrooms} bed · {listing.bathrooms} bath
              </p>
            </CardContent>
          </Card>
        </>
      )}

      {/* Timestamps */}
      <Separator className="my-6" />
      <div className="flex flex-wrap gap-4 text-sm text-muted-foreground">
        <span>Submitted: {formatDate(application.submittedAt)}</span>
        {application.decidedAt && (
          <span>Decided: {formatDate(application.decidedAt)}</span>
        )}
        {application.isPartnerReferred && (
          <span className="text-accent font-medium">Partner referred</span>
        )}
      </div>

      {/* Viewer perspective hint for tenants */}
      {!canDecide && isApplicationTenant && isPending && (
        <>
          <Separator className="my-6" />
          <Alert>
            <Clock className="h-4 w-4" />
            <span className="ml-2">
              Waiting for the host to review your application.
            </span>
          </Alert>
        </>
      )}

      {/* Actions */}
      {isPending && canDecide && (
        <>
          <Separator className="my-6" />
          <div className="flex gap-3">
            <Dialog open={approveOpen} onOpenChange={setApproveOpen}>
              <DialogTrigger asChild>
                <Button variant="accent" className="gap-2">
                  <CheckCircle2 className="h-4 w-4" />
                  Approve
                </Button>
              </DialogTrigger>
              <DialogContent className="sm:max-w-sm">
                <DialogHeader>
                  <DialogTitle>Approve application</DialogTitle>
                </DialogHeader>
                <div className="space-y-4">
                  <div className="space-y-1.5">
                    <Label htmlFor="deposit">Deposit amount ($)</Label>
                    <Input
                      id="deposit"
                      type="number"
                      step="0.01"
                      min="0"
                      max={maxDeposit / 100}
                      value={depositInput}
                      onChange={(e) => setDepositInput(e.target.value)}
                      placeholder={`Max: $${(maxDeposit / 100).toFixed(2)}`}
                    />
                    {depositInput && !isValidDeposit && (
                      <p className="text-xs text-destructive">
                        Must be between $0.01 and {formatMoney(maxDeposit)}
                      </p>
                    )}
                  </div>

                  {approveMutation.isError && (
                    <Alert variant="destructive">
                      {getApiErrorMessage(
                        approveMutation.error,
                        "Failed to approve.",
                      )}
                    </Alert>
                  )}

                  <Button
                    className="w-full"
                    onClick={handleApprove}
                    disabled={!isValidDeposit || approveMutation.isPending}
                  >
                    {approveMutation.isPending ? "Approving..." : "Confirm approval"}
                  </Button>
                </div>
              </DialogContent>
            </Dialog>

            <Button
              variant="destructive"
              className="gap-2"
              onClick={handleReject}
              disabled={rejectMutation.isPending}
            >
              <XCircle className="h-4 w-4" />
              {rejectMutation.isPending ? "Rejecting..." : "Reject"}
            </Button>
          </div>

          {rejectMutation.isError && (
            <Alert variant="destructive" className="mt-3">
              {getApiErrorMessage(rejectMutation.error, "Failed to reject.")}
            </Alert>
          )}
        </>
      )}

      {/* Post-decision state */}
      {application.status === "Approved" && application.dealId && (
        <>
          <Separator className="my-6" />
          <Alert>
            <CheckCircle2 className="h-4 w-4 text-success" />
            <span className="ml-2">
              Application approved.
            </span>
          </Alert>
          <Link
            to={`/app/deals/${application.dealId}`}
            className={cn(buttonVariants({ variant: "accent" }), "mt-3 gap-2")}
          >
            Go to Deal
            <ArrowLeft className="h-4 w-4 rotate-180" />
          </Link>
        </>
      )}
    </div>
  );
};
