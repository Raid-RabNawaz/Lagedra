import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import {
  Building2,
  Users,
  Link2,
  CalendarCheck,
  ShieldCheck,
  ArrowRight,
  Mail,
  Calendar,
  AlertTriangle,
} from "lucide-react";
import { partnerApi } from "@/features/partners/services/partnerApi";
import { usePartnerMembership } from "@/features/partners/hooks/usePartnerMembership";
import { PartnerStatusBadge } from "@/features/partners/components/PartnerStatusBadge";
import { EndorsementStatusBadge } from "@/features/partners/components/EndorsementStatusBadge";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { buttonVariants } from "@/components/ui/button-variants";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Loader } from "@/components/shared/Loader";
import { ErrorState } from "@/components/shared/ErrorState";
import { cn } from "@/lib/utils";

const formatDate = (iso: string) =>
  new Date(iso).toLocaleDateString(undefined, { dateStyle: "medium" });

const truncate = (id: string) => `${id.slice(0, 8)}…`;

export const PartnerDashboardPage = () => {
  const { membership, isLoading: membershipLoading, error: membershipError, refresh } =
    usePartnerMembership();

  const orgId = membership?.organization.id;

  const activityQuery = useQuery({
    queryKey: ["partner", orgId, "dashboard-activity"],
    queryFn: async () => {
      const [reservations, endorsements, referrals] = await Promise.all([
        partnerApi.listReservations(orgId!, { take: 5 }),
        partnerApi.listEndorsements(orgId!, { take: 5 }),
        partnerApi.listReferralLinks(orgId!),
      ]);
      return { reservations, endorsements, referrals };
    },
    enabled: Boolean(orgId),
  });

  const reservations = activityQuery.data?.reservations ?? [];
  const endorsements = activityQuery.data?.endorsements ?? [];
  const referrals = activityQuery.data?.referrals ?? [];
  const activityLoading = activityQuery.isLoading;
  const activityError = activityQuery.error;

  if (membershipLoading) return <Loader label="Loading partner organization..." />;
  if (membershipError)
    return <ErrorState error={membershipError} onRetry={() => void refresh()} />;
  if (!membership) {
    return (
      <ErrorState
        title="No partner organization"
        message="Your account isn't linked to a partner organization yet."
      >
        <Link to="/app/partner/onboarding" className={cn(buttonVariants({ variant: "default" }))}>
          Register your organization
        </Link>
      </ErrorState>
    );
  }

  const { organization, memberRole, joinedAt } = membership;
  const isOrgAdmin = memberRole === "Admin";
  const isVerified = organization.status === "Verified";
  const activeReferrals = referrals.filter((r) => r.isActive).length;
  const requestedEndorsements = endorsements.filter((e) => e.status === "Requested").length;

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <h1 className="flex items-center gap-2 text-3xl font-bold tracking-tight">
            <Building2 className="h-7 w-7 text-muted-foreground" />
            {organization.name}
          </h1>
          <div className="mt-2 flex flex-wrap items-center gap-2 text-sm text-muted-foreground">
            <PartnerStatusBadge status={organization.status} />
            <span>•</span>
            <span>{organization.organizationType}</span>
            <span>•</span>
            <span>You joined {formatDate(joinedAt)} as {memberRole}</span>
          </div>
        </div>
        {!isVerified && (
          <Badge variant="secondary" className="self-start">
            Awaiting platform verification
          </Badge>
        )}
      </div>

      {organization.status === "PendingVerification" && (
        <Alert>
          <AlertTriangle className="h-4 w-4" />
          <AlertDescription>
            Your organization is pending verification by Lagedra. You can add members and view
            referral links, but you cannot generate new referral links, create reservations, or
            issue endorsements until you're verified. We'll email you when this is done.
          </AlertDescription>
        </Alert>
      )}
      {organization.status === "Suspended" && (
        <Alert variant="destructive">
          <AlertTriangle className="h-4 w-4" />
          <AlertDescription>
            Your organization is currently <strong>suspended</strong>. Reach out to Lagedra support
            to learn next steps.
          </AlertDescription>
        </Alert>
      )}

      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <StatCard
          icon={Users}
          label="Members"
          to="/app/partner/members"
          accessory="Manage"
        />
        <StatCard
          icon={Link2}
          label={`${activeReferrals} active referral${activeReferrals === 1 ? "" : "s"}`}
          to="/app/partner/referrals"
          accessory="Open"
        />
        <StatCard
          icon={CalendarCheck}
          label={`${reservations.length} recent reservation${reservations.length === 1 ? "" : "s"}`}
          to="/app/partner/reservations"
          accessory="View"
        />
        <StatCard
          icon={ShieldCheck}
          label={`${requestedEndorsements} pending endorsement${
            requestedEndorsements === 1 ? "" : "s"
          }`}
          to="/app/partner/endorsements"
          accessory="Review"
        />
      </div>

      {activityError ? <ErrorState error={activityError} /> : null}

      <div className="grid gap-6 lg:grid-cols-2">
        <Card>
          <CardHeader className="flex flex-row items-start justify-between gap-3 pb-3">
            <div>
              <CardTitle className="text-lg">Recent reservations</CardTitle>
              <CardDescription>Direct bookings made for your guests.</CardDescription>
            </div>
            <Link
              to="/app/partner/reservations"
              className={cn(buttonVariants({ variant: "ghost", size: "sm" }))}
            >
              View all <ArrowRight className="ml-1 h-3 w-3" />
            </Link>
          </CardHeader>
          <CardContent>
            {activityLoading ? (
              <Loader label="Loading reservations..." />
            ) : reservations.length === 0 ? (
              <p className="py-6 text-center text-sm text-muted-foreground">
                No reservations yet.
                {isOrgAdmin && isVerified && (
                  <>
                    {" "}
                    <Link to="/app/partner/reservations" className="font-medium underline">
                      Create your first one
                    </Link>
                    .
                  </>
                )}
              </p>
            ) : (
              <ul className="space-y-3">
                {reservations.map((r) => (
                  <li
                    key={r.id}
                    className="flex items-center justify-between rounded-md border p-3 text-sm"
                  >
                    <div className="min-w-0">
                      <p className="font-medium truncate">{r.guestName}</p>
                      <p className="text-muted-foreground text-xs flex items-center gap-1">
                        <Mail className="h-3 w-3" />
                        {r.guestEmail}
                      </p>
                    </div>
                    <div className="text-right text-xs text-muted-foreground">
                      <p className="flex items-center justify-end gap-1">
                        <Calendar className="h-3 w-3" />
                        {formatDate(r.createdAt)}
                      </p>
                      <p className="font-mono mt-1" title={r.listingId}>
                        listing {truncate(r.listingId)}
                      </p>
                    </div>
                  </li>
                ))}
              </ul>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-start justify-between gap-3 pb-3">
            <div>
              <CardTitle className="text-lg">Recent endorsements</CardTitle>
              <CardDescription>Tenants you've endorsed or who have requested endorsement.</CardDescription>
            </div>
            <Link
              to="/app/partner/endorsements"
              className={cn(buttonVariants({ variant: "ghost", size: "sm" }))}
            >
              View all <ArrowRight className="ml-1 h-3 w-3" />
            </Link>
          </CardHeader>
          <CardContent>
            {activityLoading ? (
              <Loader label="Loading endorsements..." />
            ) : endorsements.length === 0 ? (
              <p className="py-6 text-center text-sm text-muted-foreground">No endorsements yet.</p>
            ) : (
              <ul className="space-y-3">
                {endorsements.map((e) => (
                  <li
                    key={e.id}
                    className="flex items-center justify-between gap-3 rounded-md border p-3 text-sm"
                  >
                    <div className="min-w-0">
                      <p className="font-medium truncate" title={e.tenantUserId}>
                        {e.tenantDisplayName?.trim() || `tenant ${truncate(e.tenantUserId)}`}
                      </p>
                      <p className="text-muted-foreground text-xs">
                        Requested {formatDate(e.requestedAt)}
                        {e.expiresAt && ` • expires ${formatDate(e.expiresAt)}`}
                      </p>
                    </div>
                    <EndorsementStatusBadge status={e.status} />
                  </li>
                ))}
              </ul>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  );
};

function StatCard({
  icon: Icon,
  label,
  to,
  accessory,
}: {
  icon: typeof Users;
  label: string;
  to: string;
  accessory: string;
}) {
  return (
    <Link to={to} className="block">
      <Card className="h-full transition-shadow hover:shadow-md">
        <CardContent className="flex items-start gap-3 p-5">
          <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-accent/10">
            <Icon className="h-5 w-5 text-accent" />
          </div>
          <div className="flex flex-1 flex-col">
            <span className="text-sm font-medium leading-tight">{label}</span>
            <span className="mt-0.5 text-xs text-muted-foreground">{accessory}</span>
          </div>
          <ArrowRight className="h-4 w-4 text-muted-foreground" />
        </CardContent>
      </Card>
    </Link>
  );
}
