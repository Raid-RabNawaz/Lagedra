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
  useDealInsurance,
  useRejectApplication,
  useRescreenDealInsurance,
} from "@/features/applications/hooks/useApplications";
import { useListingDetail } from "@/features/listings/hooks/useListings";
import { ApplicationStatusBadge } from "@/features/applications/components/ApplicationStatusBadge";
import { ApplicationProfilePanel } from "@/features/applications/components/ApplicationProfilePanel";
import { TrustLevelBadge } from "@/features/applications/components/TrustLevelBadge";
import { CompletePartnerRequestPanel } from "@/features/applications/components/CompletePartnerRequestPanel";
import { HostPayoutReadinessNotice } from "@/components/shared/HostPayoutReadinessNotice";
import { BackLink } from "@/components/shared/BackLink";
import { useAuthStore } from "@/app/auth/authStore";
import { isAdmin } from "@/app/auth/permissions";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { buttonVariants } from "@/components/ui/button-variants";
import { Badge } from "@/components/ui/badge";
import { ConsentTickButton } from "@/features/applications/components/ConsentTickButton";
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
import {
  BOOKING_CONSENT_VERSION,
  isAwaitingOwnerConsent,
  tierLabel,
} from "@/features/applications/lib/bookingConsent";
import { OwnerConsentPanel } from "@/features/applications/components/OwnerConsentPanel";
import { formatDate, formatMoney } from "@/utils/format";
import { STAY_PROTECTION_LABEL } from "@/features/listings/lib/stayProtection";
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
  const { data: stayProtection } = useDealInsurance(application?.dealId);
  const rescreenProtection = useRescreenDealInsurance(application?.dealId);
  const approveMutation = useApproveApplication();
  const rejectMutation = useRejectApplication();
  const user = useAuthStore((s) => s.user);

  const [consentChecked, setConsentChecked] = useState(false);
  const [approveOpen, setApproveOpen] = useState(false);

  if (isLoading) return <Loader fullPage label="Loading application..." />;

  if (isForbiddenError(error)) {
    return (
      <div className="mx-auto max-w-3xl px-4 py-16 sm:px-6 lg:px-8">
        <BackLink
          fallbackTo="/app/applications"
          variant="button"
          label="Back to applications"
          className="mb-6"
        />
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
        <div className="mt-4 flex justify-center">
          <BackLink
            fallbackTo="/app/applications"
            variant="button"
            label="Back to applications"
          />
        </div>
      </div>
    );
  }

  // Tenant and Landlord were merged into a single "Member" role, so we cannot
  // gate UI by role alone. Authorize per-application: only the host of the
  // listing (or a platform admin) sees the approve/reject controls.
  const isApplicationLandlord = !!user && user.userId === application.landlordUserId;
  const isApplicationTenant = !!user && user.userId === application.tenantUserId;
  const isApplicationOwner = !!user && user.userId === application.homeOwnerUserId;
  const isPlatformAdmin = !!user && isAdmin(user.role);
  const canDecide = isApplicationLandlord || isPlatformAdmin;
  const canRescreenFlagged =
    (isApplicationLandlord || isApplicationOwner || isPlatformAdmin)
    && stayProtection?.screeningStatus === "Flagged"
    && Boolean(application.dealId);

  const isPending = application.status === "Pending";
  const awaitingOwnerConsent = isAwaitingOwnerConsent(application);
  const isPartnerDirect =
    application.source === "PartnerDirectReservation" || Boolean(application.partnerOrganizationId);
  const paymentReady = application.isPaymentReady === true;
  const needsTenantAction =
    isPending &&
    isApplicationTenant &&
    isPartnerDirect &&
    (!application.tenantConsentGiven ||
      (application.payerType !== "PartnerOrganization" && !application.hasPaymentMethod));

  const handleApprove = async () => {
    if (!consentChecked) return;
    try {
      await approveMutation.mutateAsync({
        id: application.applicationId,
        payload: {
          truthSurfaceConsentGiven: true,
          consentVersion: BOOKING_CONSENT_VERSION,
        },
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
      <BackLink fallbackTo="/app/applications" className="mb-6" />

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

      {canDecide && (
        <div className="mb-6">
          <TrustLevelBadge
            tier={application.tenantVerificationTier}
            detailed
            depositReason={application.depositReason}
          />
        </div>
      )}

      <Card className="mb-6">
        <CardHeader className="pb-3">
          <CardTitle className="text-base">
            {isApplicationLandlord || isApplicationOwner || isPlatformAdmin ? "Guest" : "Host"}
          </CardTitle>
        </CardHeader>
        <CardContent>
          <ApplicationProfilePanel
            userId={
              isApplicationLandlord || isApplicationOwner || isPlatformAdmin
                ? application.tenantUserId
                : application.landlordUserId
            }
            roleLabel={
              isApplicationLandlord || isApplicationOwner || isPlatformAdmin ? "Guest" : "Host"
            }
            profileLink={`/app/users/${
              isApplicationLandlord || isApplicationOwner || isPlatformAdmin
                ? application.tenantUserId
                : application.landlordUserId
            }`}
            showReputation
          />
        </CardContent>
      </Card>

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
              <div className="space-y-0.5">
                <div className="flex items-center gap-2 text-sm">
                  <DollarSign className="h-4 w-4 text-muted-foreground" />
                  <span className="font-medium">Deposit:</span>
                  {formatMoney(application.depositAmountCents)}
                  {application.tenantVerificationTier && (
                    <span className="text-xs text-muted-foreground">
                      ({tierLabel(application.tenantVerificationTier)} tenant)
                    </span>
                  )}
                </div>
                {application.depositReason && (
                  <p className="pl-6 text-xs text-muted-foreground">
                    {application.depositReason}
                  </p>
                )}
              </div>
            )}
            {application.insuranceFeeCents != null && (
              <div className="flex items-center gap-2 text-sm">
                <Shield className="h-4 w-4 text-muted-foreground" />
                <span className="font-medium">{STAY_PROTECTION_LABEL}:</span>
                {formatMoney(application.insuranceFeeCents)}
              </div>
            )}
            {stayProtection?.screeningStatus && (
              <div className="space-y-0.5 pl-6 text-xs text-muted-foreground">
                <p>
                  Screening: {stayProtection.screeningStatus}
                  {stayProtection.verificationId
                    ? ` · ${stayProtection.verificationId}`
                    : ""}
                </p>
                {stayProtection.flaggedReason && (
                  <p>{stayProtection.flaggedReason}</p>
                )}
                {canRescreenFlagged && (
                  <div className="space-y-1.5 pt-1">
                    <p>
                      Modify cannot change guest email or phone. After the guest
                      updates contact details, send a new screening.
                    </p>
                    <Button
                      type="button"
                      size="sm"
                      variant="outline"
                      disabled={rescreenProtection.isPending}
                      onClick={() => rescreenProtection.mutate()}
                    >
                      {rescreenProtection.isPending ? "Rescreening…" : "Rescreen guest"}
                    </Button>
                    {rescreenProtection.isError && (
                      <p className="text-destructive">
                        {getApiErrorMessage(
                          rescreenProtection.error,
                          "Could not rescreen this guest.",
                        )}
                      </p>
                    )}
                  </div>
                )}
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

      {/* Partner attribution */}
      {isPartnerDirect && (
        <>
          <Separator className="my-6" />
          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="text-base">Partner booking</CardTitle>
            </CardHeader>
            <CardContent className="space-y-2 text-sm">
              <div className="flex flex-wrap gap-2">
                <Badge variant="outline">
                  {application.source === "PartnerDirectReservation"
                    ? "Partner direct reservation"
                    : "Partner referred"}
                </Badge>
                {application.payerType === "PartnerOrganization" ? (
                  <Badge variant="secondary">Company pays</Badge>
                ) : (
                  <Badge variant="secondary">Member pays</Badge>
                )}
                {isPending && (
                  <Badge variant={paymentReady ? "success" : "secondary"}>
                    {paymentReady ? "Ready to approve" : "Waiting for member"}
                  </Badge>
                )}
              </div>
              {application.partnerOrganizationName && (
                <p>
                  <span className="text-muted-foreground">Organization: </span>
                  <span className="font-medium">{application.partnerOrganizationName}</span>
                </p>
              )}
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
        {application.isPartnerReferred && !isPartnerDirect && (
          <span className="text-accent font-medium">Partner referred</span>
        )}
      </div>

      {/* Tenant completes partner-created request */}
      {needsTenantAction && (
        <>
          <Separator className="my-6" />
          <CompletePartnerRequestPanel application={application} />
        </>
      )}

      {/* Viewer perspective hint for tenants */}
      {!canDecide && isApplicationTenant && isPending && !needsTenantAction && (
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

      {isPending && awaitingOwnerConsent && isApplicationTenant && (
        <>
          <Separator className="my-6" />
          <Alert className="border-amber-300 bg-amber-50 text-amber-900">
            <Clock className="h-4 w-4" />
            <span className="ml-2">
              Waiting for the home owner to consent before the property manager
              can accept this request.
            </span>
          </Alert>
        </>
      )}

      {isPending && awaitingOwnerConsent && isApplicationOwner && (
        <>
          <Separator className="my-6" />
          <OwnerConsentPanel application={application} />
        </>
      )}

      {isPending && canDecide && awaitingOwnerConsent && (
        <>
          <Separator className="my-6" />
          <Alert className="border-amber-300 bg-amber-50 text-amber-900">
            <Clock className="h-4 w-4" />
            <span className="ml-2">
              Awaiting home-owner consent. You can accept after the owner
              approves this tenancy.
            </span>
          </Alert>
        </>
      )}

      {/* Host waiting for payment readiness */}
      {isPending && canDecide && isPartnerDirect && !paymentReady && (
        <>
          <Separator className="my-6" />
          <Alert>
            <Clock className="h-4 w-4" />
            <span className="ml-2">
              Waiting for the member to complete payment authorization and Truth Surface
              consent before you can accept this request.
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
                <Button
                  variant="accent"
                  className="gap-2"
                  disabled={(isPartnerDirect && !paymentReady) || awaitingOwnerConsent}
                >
                  <CheckCircle2 className="h-4 w-4" />
                  Approve
                </Button>
              </DialogTrigger>
              <DialogContent className="sm:max-w-md">
                <DialogHeader>
                  <DialogTitle>Accept booking request</DialogTitle>
                </DialogHeader>
                <div className="space-y-4">
                  <div className="space-y-1 rounded-md border bg-muted/30 p-3">
                    <div className="flex items-center justify-between">
                      <span className="text-sm text-muted-foreground">
                        Security deposit
                        {application.tenantVerificationTier
                          ? ` (${tierLabel(application.tenantVerificationTier)} tenant)`
                          : ""}
                      </span>
                      <span className="text-sm font-semibold">
                        {application.depositAmountCents != null
                          ? formatMoney(application.depositAmountCents)
                          : "—"}
                      </span>
                    </div>
                    {application.depositReason && (
                      <p className="text-xs text-muted-foreground">
                        {application.depositReason}
                      </p>
                    )}
                    <p className="pt-1 text-xs text-muted-foreground">
                      The deposit is predetermined by your listing's
                      verification-tier rules and can't be changed here.
                    </p>
                  </div>

                  <HostPayoutReadinessNotice />

                  <ConsentTickButton
                    className="text-xs"
                    checked={consentChecked}
                    onCheckedChange={setConsentChecked}
                  >
                    I agree to the Truth Surface agreement for this booking.
                    Accepting seals an immutable signed record and automatically
                    charges the guest&apos;s saved card (deposit + first month&apos;s rent
                    + fees) and activates the booking. The rent and deposit are
                    paid directly to my Stripe account; Lagedra only deducts its
                    service fee and stay protection. I return the deposit to
                    the guest directly after move-out, and the booking only
                    completes once I confirm the return and the guest confirms
                    receipt.
                  </ConsentTickButton>

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
                    disabled={!consentChecked || approveMutation.isPending}
                  >
                    {approveMutation.isPending ? "Sealing…" : "Accept & seal"}
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
