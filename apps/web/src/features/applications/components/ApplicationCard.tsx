import { useState } from "react";
import { Link } from "react-router-dom";
import {
  BadgeCheck,
  Calendar,
  CalendarCheck,
  ChevronRight,
  Clock,
  Home,
  Loader2,
  MapPin,
  MessageCircle,
  User,
  Users,
  CheckCircle2,
  XCircle,
} from "lucide-react";
import { Card, CardContent } from "@/components/ui/card";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { Alert } from "@/components/ui/alert";
import { Badge } from "@/components/ui/badge";
import { ApplicationStatusBadge } from "./ApplicationStatusBadge";
import { TrustLevelBadge } from "./TrustLevelBadge";
import {
  ApplicationDetailDialog,
  type ApplicationPerspective,
} from "./ApplicationDetailDialog";
import { HostPayoutReadinessNotice } from "@/components/shared/HostPayoutReadinessNotice";
import { useApplicationProfilePreview } from "./ApplicationProfilePanel";
import { formatDate, formatMoney } from "@/utils/format";
import {
  useApproveApplication,
  useRejectApplication,
} from "@/features/applications/hooks/useApplications";
import {
  BOOKING_CONSENT_VERSION,
  tierLabel,
} from "@/features/applications/lib/bookingConsent";
import { getApiErrorMessage } from "@/api/errors";
import type { DealApplicationDto } from "@/api/types";

type Props = {
  application: DealApplicationDto;
  /** Host inbox passes "host"; tenant my-applications passes "tenant". */
  perspective: ApplicationPerspective;
  /** Show a compact listing row on tenant cards inside a group. */
  showListingPreview?: boolean;
};

export const ApplicationCard = ({
  application,
  perspective,
  showListingPreview = false,
}: Props) => {
  const isHostView = perspective === "host";
  const stayLabel = `${application.requestedCheckIn} → ${application.requestedCheckOut}`;
  const isPending = application.status === "Pending";

  const [showApproveForm, setShowApproveForm] = useState(false);
  const [showRejectConfirm, setShowRejectConfirm] = useState(false);
  const [showDetailDialog, setShowDetailDialog] = useState(false);
  const [consentChecked, setConsentChecked] = useState(false);
  const [actionError, setActionError] = useState<string | null>(null);

  const approveMutation = useApproveApplication();
  const rejectMutation = useRejectApplication();

  const counterpartyUserId = isHostView
    ? application.tenantUserId
    : application.landlordUserId;
  const counterpartyLabel = isHostView ? "Guest" : "Host";
  const {
    query: counterpartyQuery,
    profile: counterpartyProfile,
    name: counterpartyName,
    location: counterpartyLocation,
    initials: counterpartyInitials,
  } = useApplicationProfilePreview(
    counterpartyUserId,
    true,
    counterpartyLabel,
  );

  const stop = (e: React.SyntheticEvent) => {
    e.preventDefault();
    e.stopPropagation();
  };

  /** Keep card click from swallowing nested links/buttons. */
  const stopBubble = (e: React.SyntheticEvent) => {
    e.stopPropagation();
  };

  const openDetailDialog = () => {
    setShowDetailDialog(true);
  };

  const handleApprove = async (e: React.SyntheticEvent) => {
    stop(e);
    setActionError(null);

    if (!consentChecked) {
      setActionError("Please agree to the Truth Surface terms to accept this request.");
      return;
    }

    try {
      await approveMutation.mutateAsync({
        id: application.applicationId,
        payload: {
          truthSurfaceConsentGiven: true,
          consentVersion: BOOKING_CONSENT_VERSION,
        },
      });
      setShowApproveForm(false);
    } catch (err) {
      setActionError(getApiErrorMessage(err, "Failed to approve application."));
    }
  };

  const handleReject = async (e: React.SyntheticEvent) => {
    stop(e);
    setActionError(null);
    try {
      await rejectMutation.mutateAsync(application.applicationId);
      setShowRejectConfirm(false);
    } catch (err) {
      setActionError(getApiErrorMessage(err, "Failed to reject application."));
    }
  };

  return (
    <>
      <Card
        className="relative overflow-hidden cursor-pointer transition-all hover:border-primary/30 hover:shadow-md focus-within:ring-2 focus-within:ring-ring"
        onClick={openDetailDialog}
        onKeyDown={(e) => {
          if (e.key === "Enter" || e.key === " ") {
            e.preventDefault();
            openDetailDialog();
          }
        }}
        role="button"
        tabIndex={0}
        aria-label={`Open booking request: ${stayLabel}`}
      >
        <CardContent className="p-0">
          <div className="flex flex-col sm:flex-row">
            {/* Left: counterparty + stay summary */}
            <div className="min-w-0 flex-1 space-y-3 p-4 sm:p-5">
              {showListingPreview && application.listingTitle && (
                <div className="flex items-center gap-2 text-xs text-muted-foreground">
                  <Home className="h-3.5 w-3.5 shrink-0" />
                  <span className="truncate font-medium text-foreground">
                    {application.listingTitle}
                  </span>
                  {application.listingCity && (
                    <>
                      <span aria-hidden="true">·</span>
                      <span className="flex items-center gap-0.5 shrink-0">
                        <MapPin className="h-3 w-3" />
                        {application.listingCity}
                      </span>
                    </>
                  )}
                </div>
              )}

              {/* Guest/host preview with trust level front and center */}
              <div className="flex items-start gap-3">
                <Avatar className="h-11 w-11 shrink-0 ring-2 ring-background">
                  {counterpartyProfile?.profilePhotoUrl ? (
                    <AvatarImage
                      src={counterpartyProfile.profilePhotoUrl}
                      alt={counterpartyName}
                    />
                  ) : null}
                  <AvatarFallback className="text-xs">
                    {counterpartyInitials}
                  </AvatarFallback>
                </Avatar>
                <div className="min-w-0 flex-1">
                  <div className="flex flex-wrap items-center gap-2">
                    <span className="text-[11px] font-medium uppercase tracking-wide text-muted-foreground">
                      {counterpartyLabel}
                    </span>
                    <TrustLevelBadge
                      tier={application.tenantVerificationTier}
                      compact
                    />
                  </div>
                  <div className="mt-0.5 flex items-center gap-1.5 flex-wrap">
                    <p className="text-sm font-semibold leading-tight truncate">
                      {counterpartyQuery.isLoading
                        ? `Loading ${counterpartyLabel.toLowerCase()}…`
                        : counterpartyName}
                    </p>
                    {counterpartyProfile?.isGovernmentIdVerified && (
                      <Badge variant="secondary" className="gap-1 px-1.5 py-0">
                        <BadgeCheck className="h-3 w-3" />
                        ID
                      </Badge>
                    )}
                  </div>
                  {counterpartyLocation && (
                    <p className="text-xs text-muted-foreground flex items-center gap-1 mt-0.5">
                      <MapPin className="h-3 w-3 shrink-0" />
                      {counterpartyLocation}
                    </p>
                  )}
                </div>
                <ApplicationStatusBadge
                  status={application.status}
                  className="shrink-0"
                />
              </div>

              <div className="grid grid-cols-2 gap-2 sm:grid-cols-3">
                <InfoChip icon={Calendar} label="Dates" value={stayLabel} />
                <InfoChip
                  icon={Clock}
                  label="Duration"
                  value={`${application.stayDurationDays} days`}
                />
                <InfoChip
                  icon={Users}
                  label="Guests"
                  value={String(application.guestCount)}
                />
              </div>

              {application.message && application.message.trim().length > 0 && (
                <p className="flex items-start gap-1.5 rounded-md bg-muted/40 px-2.5 py-2 text-xs text-muted-foreground">
                  <MessageCircle className="h-3.5 w-3.5 mt-0.5 shrink-0" />
                  <span className="line-clamp-2 italic">
                    &ldquo;{application.message.trim()}&rdquo;
                  </span>
                </p>
              )}

              {application.jurisdictionWarning && (
                <p className="text-xs font-medium text-amber-600">
                  {application.jurisdictionWarning}
                </p>
              )}
            </div>

            {/* Right: pricing + meta */}
            <div className="flex flex-col justify-between border-t bg-muted/20 p-4 sm:w-44 sm:border-t-0 sm:border-l sm:p-5">
              <div className="space-y-1 text-right">
                {application.firstMonthRentCents != null && (
                  <p className="text-lg font-bold tabular-nums">
                    {formatMoney(application.firstMonthRentCents)}
                    <span className="text-xs font-normal text-muted-foreground">/mo</span>
                  </p>
                )}
                {application.depositAmountCents != null && (
                  <p className="text-xs text-muted-foreground">
                    {formatMoney(application.depositAmountCents)} deposit
                  </p>
                )}
              </div>

              <div className="mt-3 space-y-0.5 text-right text-[11px] text-muted-foreground">
                <p className="flex items-center justify-end gap-1">
                  <User className="h-3 w-3" />
                  {formatDate(application.submittedAt)}
                </p>
                {application.decidedAt && (
                  <p>
                    {application.status === "Approved"
                      ? "Approved"
                      : application.status === "Rejected"
                        ? "Declined"
                        : "Updated"}{" "}
                    {formatDate(application.decidedAt)}
                  </p>
                )}
              </div>
            </div>
          </div>

          {application.status === "Approved" && application.dealId && (
            <div
              className="relative z-10 flex items-center justify-between gap-2 rounded-b-[inherit] border-t border-emerald-200 bg-emerald-50 px-4 py-2.5"
              onClick={stopBubble}
            >
              <div className="flex items-center gap-2 text-xs font-medium text-emerald-800 min-w-0">
                <CalendarCheck className="h-3.5 w-3.5 text-emerald-600 shrink-0" />
                <span className="truncate">Booking confirmed</span>
              </div>
              <Link
                to={`/app/deals/${application.dealId}`}
                onClick={stopBubble}
                className="shrink-0 text-xs font-semibold text-primary hover:underline"
              >
                View booking →
              </Link>
            </div>
          )}

          {application.status === "Rejected" && application.decidedAt && (
            <div className="relative z-10 flex items-center gap-2 rounded-b-[inherit] border-t border-destructive/20 bg-destructive/5 px-4 py-2.5 text-xs text-destructive">
              <XCircle className="h-3.5 w-3.5 shrink-0" />
              <span>
                Declined {formatDate(application.decidedAt)}.
                {isHostView
                  ? " The guest has been notified."
                  : " The host has notified you."}
              </span>
            </div>
          )}

          {isHostView && isPending && (
            <div className="relative z-10 rounded-b-[inherit] border-t px-4 py-3" onClick={stop}>
              {!showApproveForm && !showRejectConfirm && (
                <div className="flex items-center gap-2">
                  <Button
                    size="sm"
                    onClick={() => {
                      setActionError(null);
                      setShowApproveForm(true);
                    }}
                    className="gap-1.5"
                  >
                    <CheckCircle2 className="h-3.5 w-3.5" />
                    Approve
                  </Button>
                  <Button
                    size="sm"
                    variant="outline"
                    onClick={() => {
                      setActionError(null);
                      setShowRejectConfirm(true);
                    }}
                    className="gap-1.5 text-red-600 hover:bg-red-50 hover:text-red-700"
                  >
                    <XCircle className="h-3.5 w-3.5" />
                    Reject
                  </Button>
                  <Button
                    size="sm"
                    variant="ghost"
                    className="ml-auto gap-1 text-muted-foreground"
                    onClick={openDetailDialog}
                  >
                    Details
                    <ChevronRight className="h-3.5 w-3.5" />
                  </Button>
                </div>
              )}

              {showApproveForm && (
                <div className="space-y-3 rounded-md border bg-muted/30 p-3">
                  <div className="space-y-1 rounded-md border bg-background p-3">
                    <div className="flex items-center justify-between">
                      <span className="text-xs text-muted-foreground">
                        Security deposit ({tierLabel(application.tenantVerificationTier)} tenant)
                      </span>
                      <span className="text-sm font-semibold">
                        {application.depositAmountCents != null
                          ? formatMoney(application.depositAmountCents)
                          : "—"}
                      </span>
                    </div>
                    {application.depositReason && (
                      <p className="text-[11px] text-muted-foreground">
                        {application.depositReason}
                      </p>
                    )}
                  </div>

                  <HostPayoutReadinessNotice className="text-[11px]" />

                  <label className="flex items-start gap-2 text-[11px] text-muted-foreground cursor-pointer">
                    <Checkbox
                      checked={consentChecked}
                      onCheckedChange={(checked) => {
                        setConsentChecked(checked);
                        if (checked) setActionError(null);
                      }}
                      className="mt-0.5"
                    />
                    <span>
                      I agree to the Truth Surface agreement for this booking.
                      Accepting seals an immutable signed record and automatically
                      charges the guest&apos;s saved card and activates the booking.
                      The rent and deposit are paid directly to my Stripe account;
                      I return the deposit to the guest directly after move-out, and
                      the booking only completes once I confirm the return and the
                      guest confirms receipt.
                    </span>
                  </label>

                  {actionError && (
                    <Alert variant="destructive" className="text-xs">
                      {actionError}
                    </Alert>
                  )}
                  <div className="flex items-center gap-2">
                    <Button
                      size="sm"
                      onClick={handleApprove}
                      disabled={approveMutation.isPending || !consentChecked}
                      className="gap-1.5"
                    >
                      {approveMutation.isPending ? (
                        <Loader2 className="h-3.5 w-3.5 animate-spin" />
                      ) : (
                        <CheckCircle2 className="h-3.5 w-3.5" />
                      )}
                      Accept &amp; seal
                    </Button>
                    <Button
                      size="sm"
                      variant="ghost"
                      onClick={() => {
                        setShowApproveForm(false);
                        setActionError(null);
                      }}
                      disabled={approveMutation.isPending}
                    >
                      Cancel
                    </Button>
                  </div>
                </div>
              )}

              {showRejectConfirm && (
                <div className="space-y-3 rounded-md border bg-red-50/50 p-3">
                  <p className="text-xs text-red-800">
                    The tenant will be notified that their application was
                    rejected. This can&apos;t be undone.
                  </p>
                  {actionError && (
                    <Alert variant="destructive" className="text-xs">
                      {actionError}
                    </Alert>
                  )}
                  <div className="flex items-center gap-2">
                    <Button
                      size="sm"
                      variant="destructive"
                      onClick={handleReject}
                      disabled={rejectMutation.isPending}
                      className="gap-1.5"
                    >
                      {rejectMutation.isPending ? (
                        <Loader2 className="h-3.5 w-3.5 animate-spin" />
                      ) : (
                        <XCircle className="h-3.5 w-3.5" />
                      )}
                      Confirm reject
                    </Button>
                    <Button
                      size="sm"
                      variant="ghost"
                      onClick={() => {
                        setShowRejectConfirm(false);
                        setActionError(null);
                      }}
                      disabled={rejectMutation.isPending}
                    >
                      Cancel
                    </Button>
                  </div>
                </div>
              )}
            </div>
          )}
        </CardContent>
      </Card>

      <ApplicationDetailDialog
        application={application}
        open={showDetailDialog}
        onOpenChange={setShowDetailDialog}
        perspective={perspective}
      />
    </>
  );
};

function InfoChip({
  icon: Icon,
  label,
  value,
}: {
  icon: typeof Calendar;
  label: string;
  value: string;
}) {
  return (
    <div className="rounded-lg border bg-background/60 px-2.5 py-2">
      <p className="text-[10px] font-medium uppercase tracking-wide text-muted-foreground">
        {label}
      </p>
      <p className="mt-0.5 flex items-center gap-1 text-xs font-medium truncate">
        <Icon className="h-3 w-3 shrink-0 text-muted-foreground" />
        <span className="truncate">{value}</span>
      </p>
    </div>
  );
}
