import { useState } from "react";
import { Link } from "react-router-dom";
import {
  BadgeCheck,
  Calendar,
  Clock,
  User,
  Users,
  CheckCircle2,
  XCircle,
  Loader2,
  MapPin,
  MessageCircle,
  CalendarCheck,
  ChevronRight,
} from "lucide-react";
import { Card, CardContent } from "@/components/ui/card";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Alert } from "@/components/ui/alert";
import { Separator } from "@/components/ui/separator";
import { Badge } from "@/components/ui/badge";
import { ApplicationStatusBadge } from "./ApplicationStatusBadge";
import { ApplicationDetailDialog } from "./ApplicationDetailDialog";
import { usePublicProfile } from "@/features/auth/hooks/usePublicProfile";
import { formatDate, formatMoney } from "@/utils/format";
import {
  useApproveApplication,
  useRejectApplication,
} from "@/features/applications/hooks/useApplications";
import { getApiErrorMessage } from "@/api/errors";
import type { DealApplicationDto } from "@/api/types";

type Props = {
  application: DealApplicationDto;
  showListingLink?: boolean;
  /**
   * Phase 16.11 — when true, surfaces inline Approve / Reject affordances
   * for pending applications so the host can decide straight from the
   * inbox without opening the detail page. Should only be set on the
   * host's own /app/applications view.
   */
  showHostActions?: boolean;
  /**
   * Optional pre-fill for the inline approve dialog's deposit field.
   * Falls back to the application's existing deposit (rare) or 0.
   */
  defaultDepositCents?: number | null;
};

export const ApplicationCard = ({
  application,
  showListingLink = false,
  showHostActions = false,
  defaultDepositCents,
}: Props) => {
  const stayLabel = `${application.requestedCheckIn} → ${application.requestedCheckOut}`;
  const isPending = application.status === "Pending";

  const [showApproveForm, setShowApproveForm] = useState(false);
  const [showRejectConfirm, setShowRejectConfirm] = useState(false);
  const [showDetailDialog, setShowDetailDialog] = useState(false);
  const [depositDollars, setDepositDollars] = useState<string>(() => {
    const initial =
      defaultDepositCents ??
      application.depositAmountCents ??
      0;
    return initial > 0 ? (initial / 100).toFixed(2) : "";
  });
  const [actionError, setActionError] = useState<string | null>(null);

  const approveMutation = useApproveApplication();
  const rejectMutation = useRejectApplication();

  // Host-side cards always preview the applying guest so the host can
  // size up the request before opening the popup. Guest-side cards skip
  // the lookup — they already know who they are.
  const tenantProfile = usePublicProfile(
    showHostActions ? application.tenantUserId : undefined,
  );
  const tenant = tenantProfile.data;
  const tenantDisplayName = tenant?.displayName
    ?? [tenant?.firstName, tenant?.lastName].filter(Boolean).join(" ").trim();
  const tenantName =
    tenantDisplayName && tenantDisplayName.length > 0
      ? tenantDisplayName
      : null;
  const tenantInitials = tenantName
    ? tenantName
        .split(/\s+/)
        .filter(Boolean)
        .slice(0, 2)
        .map((p) => p[0]?.toUpperCase() ?? "")
        .join("")
    : "G";
  const tenantLocation = tenant
    ? [tenant.city, tenant.state, tenant.country]
        .map((p) => p?.trim())
        .filter((p): p is string => Boolean(p && p.length > 0))
        .join(", ")
    : "";

  const stop = (e: React.SyntheticEvent) => {
    e.preventDefault();
    e.stopPropagation();
  };

  const openDetailDialog = (e: React.SyntheticEvent) => {
    stop(e);
    setShowDetailDialog(true);
  };

  const handleApprove = async (e: React.SyntheticEvent) => {
    stop(e);
    setActionError(null);

    const cents = Math.round(parseFloat(depositDollars || "0") * 100);
    if (!Number.isFinite(cents) || cents <= 0) {
      setActionError("Enter a deposit amount in dollars (e.g. 1500.00).");
      return;
    }

    try {
      await approveMutation.mutateAsync({
        id: application.applicationId,
        payload: { depositAmountCents: cents },
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

  // The whole card is one click-target (`<button>`-style overlay) that
  // opens a rich detail dialog. We deliberately do NOT use a stretched
  // `<Link>` anymore — the popup is the primary affordance for hosts to
  // inspect a request, and routing away from the inbox each click would
  // make triaging painful. Inline Approve/Reject controls and the
  // "View profile" link sit above the overlay with `relative z-10` so
  // their clicks reach their own handlers.
  return (
    <>
      <Card className="relative transition-shadow hover:shadow-md focus-within:ring-2 focus-within:ring-ring">
        <button
          type="button"
          onClick={openDetailDialog}
          aria-label={`Open booking request details for ${tenantName ?? "guest"} · ${stayLabel}`}
          className="absolute inset-0 z-0 rounded-[inherit] focus:outline-none cursor-pointer"
        />
        <CardContent className="relative p-5">
          <div className="flex items-start justify-between gap-3">
            <div className="space-y-2 min-w-0 flex-1">
              <div className="flex items-center gap-2 flex-wrap">
                <ApplicationStatusBadge status={application.status} />
                {application.jurisdictionWarning && (
                  <span className="text-xs text-amber-600 font-medium">
                    {application.jurisdictionWarning}
                  </span>
                )}
              </div>

              {/* Tenant block — only shown to the host. Guest-side
                  callers (e.g. the tenant's own "my applications" view)
                  don't pass `showHostActions`, and the tenant doesn't
                  need to see their own avatar in their own inbox. */}
              {showHostActions && (
                <div className="flex items-center gap-3 pt-1">
                  <Avatar className="h-10 w-10 shrink-0">
                    {tenant?.profilePhotoUrl ? (
                      <AvatarImage
                        src={tenant.profilePhotoUrl}
                        alt={tenantName ?? "Guest"}
                      />
                    ) : null}
                    <AvatarFallback className="text-xs">
                      {tenantInitials}
                    </AvatarFallback>
                  </Avatar>
                  <div className="min-w-0">
                    <div className="flex items-center gap-1.5 flex-wrap">
                      <p className="text-sm font-semibold leading-none truncate">
                        {tenantProfile.isLoading
                          ? "Loading guest…"
                          : tenantName ?? "Guest"}
                      </p>
                      {tenant?.isGovernmentIdVerified && (
                        <Badge variant="secondary" className="gap-1">
                          <BadgeCheck className="h-3 w-3" />
                          ID
                        </Badge>
                      )}
                    </div>
                    {tenantLocation && (
                      <p className="text-xs text-muted-foreground flex items-center gap-1 mt-0.5">
                        <MapPin className="h-3 w-3" />
                        {tenantLocation}
                      </p>
                    )}
                  </div>
                </div>
              )}

              <div className="flex flex-wrap items-center gap-x-4 gap-y-1 text-sm text-muted-foreground">
                <span className="flex items-center gap-1.5">
                  <Calendar className="h-3.5 w-3.5" />
                  {stayLabel}
                </span>
                <span className="flex items-center gap-1.5">
                  <Clock className="h-3.5 w-3.5" />
                  {application.stayDurationDays} days
                </span>
                <span className="flex items-center gap-1.5">
                  <Users className="h-3.5 w-3.5" />
                  {application.guestCount}{" "}
                  {application.guestCount === 1 ? "guest" : "guests"}
                </span>
              </div>

              {/*
               * Show a one-line preview of the tenant's cover note when
               * one was provided. Helps hosts triage at a glance — full
               * text is one click away in the detail dialog.
               */}
              {application.message && application.message.trim().length > 0 && (
                <p className="flex items-start gap-1.5 text-xs text-muted-foreground italic">
                  <MessageCircle className="h-3 w-3 mt-0.5 shrink-0" />
                  <span className="line-clamp-1">
                    &ldquo;{application.message.trim()}&rdquo;
                  </span>
                </p>
              )}

              {showListingLink && (
                <p className="text-xs text-muted-foreground truncate">
                  Listing: {application.listingId}
                </p>
              )}
            </div>

            <div className="text-right shrink-0 space-y-1">
              {application.firstMonthRentCents != null && (
                <p className="text-sm font-semibold">
                  {formatMoney(application.firstMonthRentCents)}/mo
                </p>
              )}
              {application.depositAmountCents != null && (
                <p className="text-xs text-muted-foreground">
                  Deposit: {formatMoney(application.depositAmountCents)}
                </p>
              )}
            </div>
          </div>

          <div className="mt-3 flex items-center gap-2 text-xs text-muted-foreground">
            <User className="h-3 w-3 shrink-0" />
            <span>Submitted {formatDate(application.submittedAt)}</span>
            {application.decidedAt && (
              <>
                <span aria-hidden="true">·</span>
                <span>
                  {application.status === "Approved"
                    ? "Approved"
                    : application.status === "Rejected"
                      ? "Rejected"
                      : "Decided"}{" "}
                  {formatDate(application.decidedAt)}
                </span>
              </>
            )}
            <ChevronRight className="ml-auto h-3.5 w-3.5 opacity-60" />
          </div>

          {/*
           * Status-aware footer — surfaces the next-step affordance
           * without forcing the host to open the detail popup. Approved
           * applications get a "View booking" link to the linked deal
           * (when the backend has materialised one), rejected
           * applications quietly note the decision, and pending ones
           * fall through to the inline action shelf below.
           */}
          {application.status === "Approved" && application.dealId && (
            <div className="relative z-10 mt-3 flex items-center justify-between gap-2 rounded-md border border-success/20 bg-success/5 px-3 py-2">
              <div className="flex items-center gap-2 text-xs text-success-foreground/80 min-w-0">
                <CalendarCheck className="h-3.5 w-3.5 text-success shrink-0" />
                <span className="truncate">
                  Booking confirmed — guest is checked into the deal flow.
                </span>
              </div>
              <Link
                to={`/app/deals/${application.dealId}`}
                onClick={stop}
                className="shrink-0 text-xs font-medium text-primary hover:underline"
              >
                View booking →
              </Link>
            </div>
          )}

          {application.status === "Rejected" && application.decidedAt && (
            <div className="relative z-10 mt-3 flex items-center gap-2 rounded-md border border-destructive/20 bg-destructive/5 px-3 py-2 text-xs text-destructive">
              <XCircle className="h-3.5 w-3.5 shrink-0" />
              <span>
                Declined on {formatDate(application.decidedAt)}.
                {showHostActions
                  ? " The guest has been notified."
                  : " The host has notified you of the decision."}
              </span>
            </div>
          )}

        {/*
         * Phase 16.11 — inline host action shelf. Pending applications
         * pick up Approve / Reject buttons that open compact inline
         * forms. The `relative z-10` lifts the action region above the
         * stretched link overlay so its clicks don't navigate away.
         */}
        {showHostActions && isPending && (
          <div className="relative z-10">
            <Separator className="my-4" />
            {!showApproveForm && !showRejectConfirm && (
              <div className="flex items-center gap-2">
                <Button
                  size="sm"
                  onClick={(e) => {
                    stop(e);
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
                  onClick={(e) => {
                    stop(e);
                    setActionError(null);
                    setShowRejectConfirm(true);
                  }}
                  className="gap-1.5 text-red-600 hover:text-red-700 hover:bg-red-50"
                >
                  <XCircle className="h-3.5 w-3.5" />
                  Reject
                </Button>
              </div>
            )}

            {showApproveForm && (
              <div
                onClick={stop}
                className="space-y-3 rounded-md border bg-muted/30 p-3"
              >
                <div className="space-y-1.5">
                  <Label
                    htmlFor={`deposit-${application.applicationId}`}
                    className="text-xs"
                  >
                    Security deposit (USD)
                  </Label>
                  <Input
                    id={`deposit-${application.applicationId}`}
                    type="number"
                    inputMode="decimal"
                    min="0"
                    step="0.01"
                    value={depositDollars}
                    onChange={(e) => setDepositDollars(e.target.value)}
                    onClick={stop}
                    placeholder="e.g. 1500.00"
                    className="h-8"
                  />
                </div>
                {actionError && (
                  <Alert variant="destructive" className="text-xs">
                    {actionError}
                  </Alert>
                )}
                <div className="flex items-center gap-2">
                  <Button
                    size="sm"
                    onClick={handleApprove}
                    disabled={approveMutation.isPending}
                    className="gap-1.5"
                  >
                    {approveMutation.isPending ? (
                      <Loader2 className="h-3.5 w-3.5 animate-spin" />
                    ) : (
                      <CheckCircle2 className="h-3.5 w-3.5" />
                    )}
                    Confirm approve
                  </Button>
                  <Button
                    size="sm"
                    variant="ghost"
                    onClick={(e) => {
                      stop(e);
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
              <div
                onClick={stop}
                className="space-y-3 rounded-md border bg-red-50/50 p-3"
              >
                <p className="text-xs text-red-800">
                  The tenant will be notified that their application was
                  rejected. This can't be undone.
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
                    onClick={(e) => {
                      stop(e);
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
        showTenantProfileLink={showHostActions}
      />
    </>
  );
};
