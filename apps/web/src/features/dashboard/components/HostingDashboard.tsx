import { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import {
  Building2,
  Inbox,
  MessageSquare,
  CalendarCheck,
  Plus,
  Wallet,
  Link2,
  ImageOff,
  ChevronRight,
  AlertTriangle,
  CheckCircle2,
  ArrowRight,
  Receipt,
} from "lucide-react";
import { useMyListings } from "@/features/listings/hooks/useMyListings";
import { useMyApplications } from "@/features/applications/hooks/useApplications";
import { useMyDeals } from "@/features/deals/hooks/useDeals";
import { useHostInquiries } from "@/features/inquiry/hooks/useInquiry";
import { useHostPayoutReadiness } from "@/features/host-onboarding/hooks/useHostStripe";
import { useHostBillingStatement } from "@/features/activation-billing/hooks/useBilling";
import { HostChannelSyncButton } from "@/features/channels/components/HostChannelSyncButton";
import { Badge } from "@/components/ui/badge";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { buttonVariants } from "@/components/ui/button-variants";
import type { ListingStatus, UserProfileDto } from "@/api/types";
import { ProfileHealthCard } from "./ProfileHealthCard";
import {
  StatCard,
  SectionCard,
  QuickAction,
  EmptyHint,
} from "./DashboardKit";
import { formatDayRange, dealPhaseMeta, listingStatusMeta } from "./dashboardFormat";
import { cn } from "@/lib/utils";
import { formatMoney } from "@/utils/format";

const LIVE_STATUSES = new Set<ListingStatus>(["Published", "Activated"]);
const ONGOING_PHASES = new Set([
  "TruthSurface",
  "Checkout",
  "Active",
  "PaymentFailed",
  "AwaitingDepositReturn",
]);

export function HostingDashboard({ user }: { user: UserProfileDto }) {
  const userId = user.userId;
  const { data: listings } = useMyListings();
  const { data: apps } = useMyApplications();
  const { data: deals } = useMyDeals("all");
  const { data: inquiries } = useHostInquiries();
  const { ready: payoutReady, settled: payoutSettled } = useHostPayoutReadiness();
  const { data: statement } = useHostBillingStatement();

  const [syncNote, setSyncNote] = useState<string | null>(null);
  const [syncError, setSyncError] = useState<string | null>(null);

  const allListings = useMemo(() => listings ?? [], [listings]);
  const liveListings = allListings.filter((l) => LIVE_STATUSES.has(l.status));

  const bookingRequests = useMemo(
    () =>
      (apps ?? [])
        .filter((a) => a.landlordUserId === userId && a.status === "Pending")
        .sort(
          (a, b) =>
            new Date(b.submittedAt).getTime() -
            new Date(a.submittedAt).getTime(),
        ),
    [apps, userId],
  );

  const hostDeals = useMemo(
    () =>
      (deals ?? [])
        .filter((d) => d.landlordUserId === userId && ONGOING_PHASES.has(d.dealPhase))
        .sort(
          (a, b) =>
            new Date(a.requestedCheckIn).getTime() -
            new Date(b.requestedCheckIn).getTime(),
        ),
    [deals, userId],
  );

  const activeBookings = hostDeals.filter((d) => d.dealPhase === "Active").length;
  const hostInquiries = inquiries ?? [];
  const unansweredInquiries = hostInquiries.reduce(
    (sum, i) => sum + i.unansweredCount,
    0,
  );

  const statusCounts = useMemo(() => {
    const c: Partial<Record<ListingStatus, number>> = {};
    for (const l of allListings) c[l.status] = (c[l.status] ?? 0) + 1;
    return c;
  }, [allListings]);

  return (
    <div className="space-y-6">
      {syncNote && (
        <Alert variant="success">
          <CheckCircle2 className="h-4 w-4" />
          <AlertDescription>{syncNote}</AlertDescription>
        </Alert>
      )}
      {syncError && (
        <Alert variant="destructive">
          <AlertTriangle className="h-4 w-4" />
          <AlertDescription>{syncError}</AlertDescription>
        </Alert>
      )}

      {payoutSettled && !payoutReady && (
        <Alert>
          <Wallet className="h-4 w-4" />
          <AlertDescription className="flex flex-wrap items-center justify-between gap-2">
            <span>
              Set up payouts to start receiving rent &amp; deposits through
              Lagedra.
            </span>
            <Link
              to="/app/payout-setup"
              className="inline-flex items-center gap-1 font-medium text-primary hover:underline"
            >
              Set up payouts
              <ArrowRight className="h-3.5 w-3.5" />
            </Link>
          </AlertDescription>
        </Alert>
      )}

      {statement && statement.currentMonthlyFeeCents > 0 && (
        <Alert>
          <Receipt className="h-4 w-4" />
          <AlertDescription className="flex flex-wrap items-center justify-between gap-2">
            <span>
              {statement.activeBookingCount > 0 ? (
                <>
                  You&apos;re billed{" "}
                  <span className="font-medium">
                    {formatMoney(statement.currentMonthlyFeeCents)}/mo
                  </span>{" "}
                  per active booking —{" "}
                  <span className="font-medium">
                    {formatMoney(statement.projectedMonthlyTotalCents)}/mo
                  </span>{" "}
                  across your {statement.activeBookingCount} active booking
                  {statement.activeBookingCount === 1 ? "" : "s"}, charged
                  automatically.
                </>
              ) : (
                <>
                  Active bookings are billed a{" "}
                  <span className="font-medium">
                    {formatMoney(statement.currentMonthlyFeeCents)}/mo
                  </span>{" "}
                  platform fee each, charged automatically once a booking goes
                  live.
                </>
              )}
            </span>
            <Link
              to="/app/billing"
              className="inline-flex items-center gap-1 font-medium text-primary hover:underline"
            >
              View statement
              <ArrowRight className="h-3.5 w-3.5" />
            </Link>
          </AlertDescription>
        </Alert>
      )}

      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <StatCard
          label="Live listings"
          value={liveListings.length}
          icon={Building2}
          tone="primary"
          to="/app/listings"
          hint={
            allListings.length > liveListings.length
              ? `${allListings.length - liveListings.length} not live`
              : undefined
          }
        />
        <StatCard
          label="Booking requests"
          value={bookingRequests.length}
          icon={Inbox}
          tone="warning"
          to="/app/applications"
          hint={bookingRequests.length > 0 ? "Awaiting your response" : undefined}
        />
        <StatCard
          label="Guest inquiries"
          value={unansweredInquiries}
          icon={MessageSquare}
          tone="accent"
          to="/app/inquiries"
          hint={unansweredInquiries > 0 ? "Questions to answer" : undefined}
        />
        <StatCard
          label="Active bookings"
          value={activeBookings}
          icon={CalendarCheck}
          tone="success"
          to="/app/deals"
        />
      </div>

      <div className="grid gap-6 lg:grid-cols-2">
        <SectionCard
          title="Booking requests"
          icon={Inbox}
          action={
            <Link
              to="/app/applications"
              className="text-sm font-medium text-primary hover:underline"
            >
              View all
            </Link>
          }
        >
          {bookingRequests.length === 0 ? (
            <EmptyHint>No pending booking requests right now.</EmptyHint>
          ) : (
            <ul className="space-y-2">
              {bookingRequests.slice(0, 4).map((a) => (
                <li key={a.applicationId}>
                  <Link
                    to="/app/applications"
                    className="flex items-center gap-3 rounded-lg border p-3 transition-colors hover:bg-muted/50"
                  >
                    <Thumb src={a.listingCoverPhotoUri} />
                    <div className="min-w-0 flex-1">
                      <p className="truncate text-sm font-medium">
                        {a.listingTitle ?? "Listing"}
                      </p>
                      <p className="text-xs text-muted-foreground">
                        {formatDayRange(a.requestedCheckIn, a.requestedCheckOut)}
                        {a.guestCount ? ` · ${a.guestCount} guest${a.guestCount === 1 ? "" : "s"}` : ""}
                      </p>
                    </div>
                    {a.totalPayableSnapshotCents != null && (
                      <span className="text-sm font-semibold">
                        {formatMoney(a.totalPayableSnapshotCents)}
                      </span>
                    )}
                    <ChevronRight className="h-4 w-4 shrink-0 text-muted-foreground" />
                  </Link>
                </li>
              ))}
            </ul>
          )}
        </SectionCard>

        <SectionCard
          title="Your listings"
          icon={Building2}
          action={
            <div className="flex items-center gap-2">
              <HostChannelSyncButton
                onSynced={(m) => {
                  setSyncError(null);
                  setSyncNote(m);
                }}
                onError={(m) => {
                  setSyncNote(null);
                  setSyncError(m);
                }}
              />
              <Link
                to="/app/listings/new"
                className={cn(buttonVariants({ variant: "accent", size: "sm" }))}
              >
                <Plus className="h-4 w-4" />
                New
              </Link>
            </div>
          }
        >
          {allListings.length === 0 ? (
            <EmptyHint cta={{ to: "/app/listings/new", label: "Create your first listing" }}>
              You don&apos;t have any listings yet.
            </EmptyHint>
          ) : (
            <>
              <div className="mb-3 flex flex-wrap gap-1.5">
                {(Object.keys(statusCounts) as ListingStatus[]).map((status) => {
                  const meta = listingStatusMeta(status);
                  return (
                    <Badge key={status} variant={meta.variant}>
                      {statusCounts[status]} {meta.label}
                    </Badge>
                  );
                })}
              </div>
              <ul className="space-y-2">
                {allListings.slice(0, 4).map((l) => {
                  const meta = listingStatusMeta(l.status);
                  return (
                    <li key={l.id}>
                      <Link
                        to={`/app/listings/${l.id}`}
                        className="flex items-center gap-3 rounded-lg border p-3 transition-colors hover:bg-muted/50"
                      >
                        <Thumb src={l.coverPhotoUrl} />
                        <div className="min-w-0 flex-1">
                          <p className="truncate text-sm font-medium">{l.title}</p>
                          <p className="text-xs text-muted-foreground">
                            {formatMoney(l.monthlyRentCents)} / mo
                          </p>
                        </div>
                        <Badge variant={meta.variant}>{meta.label}</Badge>
                        <ChevronRight className="h-4 w-4 shrink-0 text-muted-foreground" />
                      </Link>
                    </li>
                  );
                })}
              </ul>
            </>
          )}
        </SectionCard>

        <SectionCard
          title="Guest inquiries"
          icon={MessageSquare}
          action={
            <Link
              to="/app/inquiries"
              className="text-sm font-medium text-primary hover:underline"
            >
              View all
            </Link>
          }
        >
          {hostInquiries.length === 0 ? (
            <EmptyHint>No guest questions yet.</EmptyHint>
          ) : (
            <ul className="space-y-2">
              {hostInquiries
                .slice()
                .sort(
                  (a, b) =>
                    new Date(b.lastActivityAt).getTime() -
                    new Date(a.lastActivityAt).getTime(),
                )
                .slice(0, 4)
                .map((i) => (
                  <li key={i.sessionId}>
                    <Link
                      to="/app/inquiries"
                      className="flex items-center gap-3 rounded-lg border p-3 transition-colors hover:bg-muted/50"
                    >
                      <Thumb src={i.listingCoverPhotoUri} />
                      <div className="min-w-0 flex-1">
                        <p className="truncate text-sm font-medium">
                          {i.tenantDisplayName ?? "Guest"}
                        </p>
                        <p className="truncate text-xs text-muted-foreground">
                          {i.listingTitle ?? "Listing"}
                        </p>
                      </div>
                      {i.unansweredCount > 0 ? (
                        <Badge variant="destructive">{i.unansweredCount} new</Badge>
                      ) : (
                        <Badge variant="outline">Answered</Badge>
                      )}
                      <ChevronRight className="h-4 w-4 shrink-0 text-muted-foreground" />
                    </Link>
                  </li>
                ))}
            </ul>
          )}
        </SectionCard>

        <SectionCard
          title="Active bookings"
          icon={CalendarCheck}
          action={
            <Link
              to="/app/deals"
              className="text-sm font-medium text-primary hover:underline"
            >
              View all
            </Link>
          }
        >
          {hostDeals.length === 0 ? (
            <EmptyHint>No active bookings yet.</EmptyHint>
          ) : (
            <ul className="space-y-2">
              {hostDeals.slice(0, 4).map((d) => {
                const phase = dealPhaseMeta(d.dealPhase);
                return (
                  <li key={d.dealId}>
                    <Link
                      to={`/app/deals/${d.dealId}`}
                      className="flex items-center gap-3 rounded-lg border p-3 transition-colors hover:bg-muted/50"
                    >
                      <Thumb src={d.listingCoverPhotoUri} />
                      <div className="min-w-0 flex-1">
                        <p className="truncate text-sm font-medium">{d.listingTitle}</p>
                        <p className="text-xs text-muted-foreground">
                          {formatDayRange(d.requestedCheckIn, d.requestedCheckOut)}
                        </p>
                      </div>
                      <Badge variant={phase.variant}>{phase.label}</Badge>
                      <ChevronRight className="h-4 w-4 shrink-0 text-muted-foreground" />
                    </Link>
                  </li>
                );
              })}
            </ul>
          )}
        </SectionCard>
      </div>

      <ProfileHealthCard user={user} />

      <div>
        <h2 className="mb-3 text-sm font-semibold uppercase tracking-wider text-muted-foreground">
          Quick actions
        </h2>
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          <QuickAction label="Create listing" description="List a new property" to="/app/listings/new" icon={Plus} />
          <QuickAction label="My listings" description="Manage your properties" to="/app/listings" icon={Building2} />
          <QuickAction label="Import (PMS)" description="Sync from OwnerRez or Hostaway" to="/app/channels" icon={Link2} />
          <QuickAction label="Booking requests" description="Review & approve guests" to="/app/applications" icon={Inbox} />
          <QuickAction label="Guest inquiries" description="Answer questions" to="/app/inquiries" icon={MessageSquare} />
          <QuickAction label="Payout setup" description="Get paid via Stripe" to="/app/payout-setup" icon={Wallet} />
          <QuickAction label="Platform fees" description="Monthly fee statement" to="/app/billing" icon={Receipt} />
        </div>
      </div>
    </div>
  );
}

function Thumb({ src }: { src?: string | null }) {
  return (
    <span className="flex h-10 w-10 shrink-0 items-center justify-center overflow-hidden rounded-md bg-muted">
      {src ? (
        <img src={src} alt="" className="h-full w-full object-cover" loading="lazy" />
      ) : (
        <ImageOff className="h-4 w-4 text-muted-foreground/50" />
      )}
    </span>
  );
}
