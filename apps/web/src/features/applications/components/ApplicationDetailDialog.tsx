import { Link } from "react-router-dom";
import {
  Calendar,
  Clock,
  DollarSign,
  ExternalLink,
  Home,
  ImageOff,
  MapPin,
  MessageCircle,
  Receipt,
  Shield,
  ShieldAlert,
  Users,
} from "lucide-react";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Badge } from "@/components/ui/badge";
import { Separator } from "@/components/ui/separator";
import { Alert } from "@/components/ui/alert";
import { ApplicationStatusBadge } from "./ApplicationStatusBadge";
import { ApplicationProfilePanel } from "./ApplicationProfilePanel";
import { TrustLevelBadge } from "./TrustLevelBadge";
import { useHostBillingStatement } from "@/features/activation-billing/hooks/useBilling";
import { formatDate, formatMoney } from "@/utils/format";
import type { DealApplicationDto } from "@/api/types";

export type ApplicationPerspective = "host" | "tenant" | "owner";

type Props = {
  application: DealApplicationDto;
  open: boolean;
  onOpenChange: (open: boolean) => void;
  /** Host inbox shows guest profile; tenant view shows host profile. */
  perspective: ApplicationPerspective;
};

export const ApplicationDetailDialog = ({
  application,
  open,
  onOpenChange,
  perspective,
}: Props) => {
  const isHostView = perspective === "host" || perspective === "owner";
  const { data: statement } = useHostBillingStatement(isHostView && open);
  const profileUserId = isHostView
    ? application.tenantUserId
    : application.landlordUserId;
  const profileRole = isHostView ? "Guest" : "Host";
  const profileLink = `/app/users/${profileUserId}`;

  const stayLabel = `${application.requestedCheckIn} → ${application.requestedCheckOut}`;

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-h-[90vh] max-w-2xl overflow-y-auto">
        <DialogHeader>
          <div className="flex flex-wrap items-center gap-2 pr-8">
            <DialogTitle>Booking request details</DialogTitle>
            <ApplicationStatusBadge status={application.status} />
          </div>
          <p className="text-sm text-muted-foreground">
            {stayLabel} · {application.stayDurationDays} day
            {application.stayDurationDays !== 1 ? "s" : ""} ·{" "}
            {application.guestCount}{" "}
            {application.guestCount === 1 ? "guest" : "guests"}
          </p>
        </DialogHeader>

        <div className="space-y-5">
          <TrustLevelBadge
            tier={application.tenantVerificationTier}
            detailed
            depositReason={application.depositReason}
          />

          {/* Listing context — especially useful on the tenant side */}
          <section className="rounded-lg border bg-muted/20 p-4">
            <p className="mb-3 text-xs font-medium uppercase tracking-wide text-muted-foreground">
              Property
            </p>
            <Link
              to={`/listings/${application.listingId}`}
              onClick={() => onOpenChange(false)}
              className="group flex items-center gap-3"
            >
              <span className="relative h-14 w-14 shrink-0 overflow-hidden rounded-lg bg-muted">
                {application.listingCoverPhotoUri ? (
                  <img
                    src={application.listingCoverPhotoUri}
                    alt=""
                    className="h-full w-full object-cover transition-transform group-hover:scale-105"
                  />
                ) : (
                  <span className="flex h-full w-full items-center justify-center">
                    <ImageOff className="h-5 w-5 text-muted-foreground/40" />
                  </span>
                )}
              </span>
              <span className="min-w-0">
                <span className="block font-semibold leading-tight group-hover:underline">
                  {application.listingTitle ?? "View listing"}
                </span>
                {application.listingCity && (
                  <span className="mt-0.5 flex items-center gap-1 text-xs text-muted-foreground">
                    <MapPin className="h-3 w-3" />
                    {application.listingCity}
                  </span>
                )}
              </span>
              <ExternalLink className="ml-auto h-4 w-4 shrink-0 text-muted-foreground opacity-0 transition-opacity group-hover:opacity-100" />
            </Link>
          </section>

          {/* Counterparty profile */}
          <section className="rounded-lg border bg-muted/30 p-4">
            <p className="mb-3 text-xs font-medium uppercase tracking-wide text-muted-foreground">
              {isHostView ? "Guest" : "Host"}
            </p>
            <ApplicationProfilePanel
              userId={profileUserId}
              enabled={open}
              roleLabel={profileRole}
              profileLink={profileLink}
            />
          </section>

          {/* Stay + financial details */}
          <section className="grid gap-4 sm:grid-cols-2">
            <div className="rounded-lg border p-4">
              <p className="mb-3 text-xs font-medium uppercase tracking-wide text-muted-foreground">
                Stay
              </p>
              <ul className="space-y-2 text-sm">
                <li className="flex items-center gap-2">
                  <Calendar className="h-4 w-4 text-muted-foreground" />
                  <span className="font-medium">Check-in:</span>
                  {application.requestedCheckIn}
                </li>
                <li className="flex items-center gap-2">
                  <Calendar className="h-4 w-4 text-muted-foreground" />
                  <span className="font-medium">Check-out:</span>
                  {application.requestedCheckOut}
                </li>
                <li className="flex items-center gap-2">
                  <Clock className="h-4 w-4 text-muted-foreground" />
                  <span className="font-medium">Duration:</span>
                  {application.stayDurationDays} day
                  {application.stayDurationDays !== 1 ? "s" : ""}
                </li>
                <li className="flex items-center gap-2">
                  <Users className="h-4 w-4 text-muted-foreground" />
                  <span className="font-medium">Guests:</span>
                  {application.guestCount}
                </li>
              </ul>
            </div>

            <div className="rounded-lg border p-4">
              <p className="mb-3 text-xs font-medium uppercase tracking-wide text-muted-foreground">
                Financials
              </p>
              <ul className="space-y-2 text-sm">
                {application.firstMonthRentCents != null && (
                  <li className="flex items-center gap-2">
                    <DollarSign className="h-4 w-4 text-muted-foreground" />
                    <span className="font-medium">Monthly rent:</span>
                    {formatMoney(application.firstMonthRentCents)}
                  </li>
                )}
                {application.depositAmountCents != null &&
                  application.depositAmountCents > 0 && (
                    <li className="flex items-start gap-2">
                      <Shield className="mt-0.5 h-4 w-4 shrink-0 text-muted-foreground" />
                      <span>
                        <span className="font-medium">Deposit:</span>{" "}
                        {formatMoney(application.depositAmountCents)}
                        {application.depositReason && (
                          <span className="mt-0.5 block text-xs text-muted-foreground">
                            {application.depositReason}
                          </span>
                        )}
                      </span>
                    </li>
                  )}
                {application.insuranceFeeCents != null &&
                  application.insuranceFeeCents > 0 && (
                    <li className="flex items-center gap-2">
                      <Shield className="h-4 w-4 text-muted-foreground" />
                      <span className="font-medium">Insurance:</span>
                      {formatMoney(application.insuranceFeeCents)}
                    </li>
                  )}
                {application.serviceFeeCents != null &&
                  application.serviceFeeCents > 0 && (
                    <li className="flex items-center gap-2">
                      <Home className="h-4 w-4 text-muted-foreground" />
                      <span className="font-medium">Service fee:</span>
                      {formatMoney(application.serviceFeeCents)}
                    </li>
                  )}
                {application.totalPayableSnapshotCents != null &&
                  application.totalPayableSnapshotCents > 0 && (
                    <li className="flex items-center gap-2 border-t pt-2 font-semibold">
                      <DollarSign className="h-4 w-4 text-muted-foreground" />
                      <span>Total at approval:</span>
                      {formatMoney(application.totalPayableSnapshotCents)}
                    </li>
                  )}
              </ul>
              {isHostView && (
                <p className="mt-3 flex items-start gap-2 border-t pt-3 text-xs text-muted-foreground">
                  <Receipt className="mt-0.5 h-3.5 w-3.5 shrink-0" />
                  <span>
                    Rent, deposit and insurance above are paid by the guest. If
                    you accept, Lagedra charges you a monthly platform fee
                    {statement && statement.currentMonthlyFeeCents > 0
                      ? ` of ${formatMoney(statement.currentMonthlyFeeCents)}`
                      : ""}{" "}
                    for this booking while it stays active, billed automatically
                    to your card on file.
                  </span>
                </p>
              )}
            </div>
          </section>

          {application.message && application.message.trim().length > 0 && (
            <section className="rounded-lg border p-4">
              <p className="mb-2 flex items-center gap-2 text-xs font-medium uppercase tracking-wide text-muted-foreground">
                <MessageCircle className="h-3.5 w-3.5" />
                Message from {isHostView ? "guest" : "you"}
              </p>
              <p className="text-sm whitespace-pre-line leading-relaxed">
                {application.message}
              </p>
            </section>
          )}

          {(application.isPartnerReferred ||
            application.source === "PartnerDirectReservation" ||
            application.partnerOrganizationId) && (
            <div className="flex flex-wrap gap-2">
              <Badge variant="outline" className="w-fit">
                {application.source === "PartnerDirectReservation"
                  ? "Partner direct reservation"
                  : "Partner-referred booking"}
              </Badge>
              {application.partnerOrganizationName && (
                <Badge variant="secondary" className="w-fit">
                  {application.partnerOrganizationName}
                </Badge>
              )}
              {application.payerType === "PartnerOrganization" ? (
                <Badge variant="outline" className="w-fit">
                  Company pays
                </Badge>
              ) : application.partnerOrganizationId ? (
                <Badge variant="outline" className="w-fit">
                  Member pays
                </Badge>
              ) : null}
              {application.status === "Pending" &&
                application.isPaymentReady === false &&
                application.partnerOrganizationId && (
                  <Badge variant="secondary" className="w-fit">
                    Waiting for member
                  </Badge>
                )}
            </div>
          )}

          {application.jurisdictionWarning && (
            <Alert variant="destructive" className="text-sm">
              <ShieldAlert className="h-4 w-4" />
              <span className="ml-2">{application.jurisdictionWarning}</span>
            </Alert>
          )}

          <Separator />

          <section className="flex flex-wrap items-center justify-between gap-3 text-xs text-muted-foreground">
            <div className="space-y-0.5">
              <p>Submitted {formatDate(application.submittedAt)}</p>
              {application.decidedAt && (
                <p>Decided {formatDate(application.decidedAt)}</p>
              )}
            </div>
            {application.dealId && application.status === "Approved" && (
              <Link
                to={`/app/deals/${application.dealId}`}
                className="inline-flex items-center gap-1 text-primary hover:underline"
                onClick={() => onOpenChange(false)}
              >
                View booking
                <ExternalLink className="h-3 w-3" />
              </Link>
            )}
          </section>
        </div>
      </DialogContent>
    </Dialog>
  );
};
