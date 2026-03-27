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
} from "lucide-react";
import {
  useApplicationDetail,
  useApproveApplication,
  useRejectApplication,
} from "@/features/applications/hooks/useApplications";
import { useListingDetail } from "@/features/listings/hooks/useListings";
import { ApplicationStatusBadge } from "@/features/applications/components/ApplicationStatusBadge";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button, buttonVariants } from "@/components/ui/button";
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

export const ApplicationDetailPage = () => {
  const { id } = useParams<{ id: string }>();
  const { data: application, isLoading, isError } = useApplicationDetail(id);
  const { data: listing } = useListingDetail(application?.listingId);
  const approveMutation = useApproveApplication();
  const rejectMutation = useRejectApplication();

  const [depositInput, setDepositInput] = useState("");
  const [approveOpen, setApproveOpen] = useState(false);

  if (isLoading) return <Loader fullPage label="Loading application..." />;

  if (isError || !application) {
    return (
      <div className="mx-auto max-w-3xl px-4 py-16 sm:px-6 lg:px-8 text-center">
        <p className="text-destructive font-medium">Application not found or failed to load.</p>
        <Link to="/app/applications" className={cn(buttonVariants({ variant: "outline" }), "mt-4")}>
          <ArrowLeft className="h-4 w-4" />
          Back to applications
        </Link>
      </div>
    );
  }

  const isPending = application.status === "Pending";
  const maxDeposit = listing?.maxDepositCents ?? 0;
  const depositCents = Math.round(Number(depositInput) * 100);
  const isValidDeposit = depositCents > 0 && depositCents <= maxDeposit;

  const handleApprove = async () => {
    if (!isValidDeposit) return;
    await approveMutation.mutateAsync({
      id: application.applicationId,
      payload: { depositAmountCents: depositCents },
    });
    setApproveOpen(false);
  };

  const handleReject = async () => {
    await rejectMutation.mutateAsync(application.applicationId);
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

      {/* Actions */}
      {isPending && (
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
                      {(approveMutation.error as Error)?.message ?? "Failed to approve."}
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
              {(rejectMutation.error as Error)?.message ?? "Failed to reject."}
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
              Application approved. Deal ID: <code className="text-xs">{application.dealId}</code>
            </span>
          </Alert>
        </>
      )}
    </div>
  );
};
