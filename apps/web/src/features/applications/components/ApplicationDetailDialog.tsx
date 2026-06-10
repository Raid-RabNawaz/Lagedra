import { Link } from "react-router-dom";
import {
  ArrowRight,
  BadgeCheck,
  Briefcase,
  CalendarDays,
  Calendar,
  Clock,
  DollarSign,
  ExternalLink,
  Languages,
  Mail,
  MapPin,
  MessageCircle,
  Phone,
  Shield,
  ShieldAlert,
  User,
  Users,
} from "lucide-react";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Separator } from "@/components/ui/separator";
import { Alert } from "@/components/ui/alert";
import { Loader } from "@/components/shared/Loader";
import { ApplicationStatusBadge } from "./ApplicationStatusBadge";
import { usePublicProfile } from "@/features/auth/hooks/usePublicProfile";
import { formatDate, formatMoney } from "@/utils/format";
import { getApiErrorMessage, isNotFoundError } from "@/api/errors";
import type { DealApplicationDto } from "@/api/types";

type Props = {
  application: DealApplicationDto;
  open: boolean;
  onOpenChange: (open: boolean) => void;
  /** When true, surface the host-facing "View tenant profile" CTA. */
  showTenantProfileLink?: boolean;
};

function initialsFor(name?: string | null): string {
  if (!name) return "G";
  const parts = name.split(/\s+/).filter(Boolean).slice(0, 2);
  if (parts.length === 0) return "G";
  return parts.map((p) => p[0]?.toUpperCase() ?? "").join("");
}

function locationLabel(profile: {
  city?: string | null;
  state?: string | null;
  country?: string | null;
}): string | null {
  const parts = [profile.city, profile.state, profile.country]
    .map((p) => p?.trim())
    .filter((p): p is string => Boolean(p && p.length > 0));
  return parts.length > 0 ? parts.join(", ") : null;
}

export const ApplicationDetailDialog = ({
  application,
  open,
  onOpenChange,
  showTenantProfileLink = false,
}: Props) => {
  const tenant = usePublicProfile(open ? application.tenantUserId : undefined);
  const profile = tenant.data;
  const heading = profile?.displayName
    ?? [profile?.firstName, profile?.lastName].filter(Boolean).join(" ").trim();
  const display = heading && heading.length > 0 ? heading : "Guest";
  const location = profile ? locationLabel(profile) : null;
  const verifiedCount = profile
    ? [
        profile.isGovernmentIdVerified,
        profile.isPhoneVerified,
        profile.isEmailVerified,
      ].filter(Boolean).length
    : 0;

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-2xl">
        <DialogHeader>
          <div className="flex items-center gap-3 pr-8">
            <DialogTitle>Booking request</DialogTitle>
            <ApplicationStatusBadge status={application.status} />
          </div>
        </DialogHeader>

        <div className="space-y-5">
          {/* Tenant summary */}
          <section className="rounded-lg border bg-muted/30 p-4">
            {tenant.isLoading ? (
              <div className="py-4">
                <Loader label="Loading guest profile…" />
              </div>
            ) : tenant.isError ? (
              <Alert variant="destructive" className="text-sm">
                {isNotFoundError(tenant.error)
                  ? "Guest profile not available."
                  : getApiErrorMessage(tenant.error, "Failed to load guest profile.")}
              </Alert>
            ) : profile ? (
              <div className="flex flex-col gap-4 sm:flex-row sm:items-start">
                <Avatar className="h-16 w-16 shrink-0">
                  {profile.profilePhotoUrl ? (
                    <AvatarImage src={profile.profilePhotoUrl} alt={display} />
                  ) : null}
                  <AvatarFallback className="text-base">
                    {initialsFor(display)}
                  </AvatarFallback>
                </Avatar>
                <div className="min-w-0 flex-1 space-y-2">
                  <div className="flex flex-wrap items-center gap-2">
                    <p className="font-semibold text-base leading-none">
                      {display}
                    </p>
                    {profile.isGovernmentIdVerified && (
                      <Badge variant="secondary" className="gap-1">
                        <BadgeCheck className="h-3 w-3" />
                        ID verified
                      </Badge>
                    )}
                  </div>
                  <div className="flex flex-wrap gap-x-3 gap-y-1 text-xs text-muted-foreground">
                    {location && (
                      <span className="flex items-center gap-1">
                        <MapPin className="h-3 w-3" />
                        {location}
                      </span>
                    )}
                    {profile.occupation && (
                      <span className="flex items-center gap-1">
                        <Briefcase className="h-3 w-3" />
                        {profile.occupation}
                      </span>
                    )}
                    <span className="flex items-center gap-1">
                      <CalendarDays className="h-3 w-3" />
                      Member since {formatDate(profile.memberSince)}
                    </span>
                  </div>
                  {profile.bio && (
                    <p className="text-sm text-muted-foreground line-clamp-3">
                      {profile.bio}
                    </p>
                  )}
                  <div className="flex flex-wrap items-center gap-2 pt-1">
                    <VerificationChip
                      label="ID"
                      verified={profile.isGovernmentIdVerified}
                      icon={<BadgeCheck className="h-3 w-3" />}
                    />
                    <VerificationChip
                      label="Phone"
                      verified={profile.isPhoneVerified}
                      icon={<Phone className="h-3 w-3" />}
                    />
                    <VerificationChip
                      label="Email"
                      verified={profile.isEmailVerified}
                      icon={<Mail className="h-3 w-3" />}
                    />
                    {profile.languages && (
                      <Badge variant="outline" className="gap-1">
                        <Languages className="h-3 w-3" />
                        {profile.languages}
                      </Badge>
                    )}
                  </div>
                  <p className="pt-1 text-[11px] text-muted-foreground">
                    {verifiedCount}/3 verifications complete
                  </p>
                </div>
              </div>
            ) : (
              <p className="text-sm text-muted-foreground">
                Guest profile not available.
              </p>
            )}

            {showTenantProfileLink && (
              <div className="mt-4">
                <Link to={`/app/users/${application.tenantUserId}`}>
                  <Button variant="outline" size="sm" className="gap-1.5">
                    <User className="h-3.5 w-3.5" />
                    View full guest profile
                    <ArrowRight className="h-3.5 w-3.5" />
                  </Button>
                </Link>
              </div>
            )}
          </section>

          {/* Stay + financial details */}
          <section className="grid gap-4 sm:grid-cols-2">
            <div className="rounded-lg border p-4">
              <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground mb-3">
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
                  {application.guestCount}{" "}
                  {application.guestCount === 1 ? "guest" : "guests"}
                </li>
              </ul>
            </div>

            <div className="rounded-lg border p-4">
              <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground mb-3">
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
                    <li className="flex items-center gap-2">
                      <Shield className="h-4 w-4 text-muted-foreground" />
                      <span className="font-medium">Deposit:</span>
                      {formatMoney(application.depositAmountCents)}
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
              </ul>
            </div>
          </section>

          {/*
           * Tenant's cover note (Airbnb-style "message the host"). Only
           * rendered when populated — guests can skip this on submission
           * and we don't want to display an empty placeholder card.
           */}
          {application.message && application.message.trim().length > 0 && (
            <section className="rounded-lg border p-4">
              <p className="mb-2 flex items-center gap-2 text-xs font-medium uppercase tracking-wide text-muted-foreground">
                <MessageCircle className="h-3.5 w-3.5" />
                Message from {display}
              </p>
              <p className="text-sm whitespace-pre-line leading-relaxed">
                {application.message}
              </p>
            </section>
          )}

          {application.jurisdictionWarning && (
            <Alert variant="destructive" className="text-sm">
              <ShieldAlert className="h-4 w-4" />
              <span className="ml-2">{application.jurisdictionWarning}</span>
            </Alert>
          )}

          <Separator />

          <section className="flex flex-wrap items-center justify-between gap-3 text-xs text-muted-foreground">
            <span>Submitted {formatDate(application.submittedAt)}</span>
            {application.decidedAt && (
              <span>Decided {formatDate(application.decidedAt)}</span>
            )}
            <Link
              to={`/app/applications/${application.applicationId}`}
              className="inline-flex items-center gap-1 text-primary hover:underline"
              onClick={() => onOpenChange(false)}
            >
              <ExternalLink className="h-3 w-3" />
              Open full application page
            </Link>
          </section>
        </div>
      </DialogContent>
    </Dialog>
  );
};

function VerificationChip({
  label,
  verified,
  icon,
}: {
  label: string;
  verified: boolean;
  icon: React.ReactNode;
}) {
  if (verified) {
    return (
      <Badge variant="secondary" className="gap-1">
        {icon}
        {label}
      </Badge>
    );
  }
  return (
    <Badge variant="outline" className="gap-1 text-muted-foreground">
      {icon}
      {label}
    </Badge>
  );
}
