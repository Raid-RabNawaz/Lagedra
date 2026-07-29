import { useMemo } from "react";
import { Link } from "react-router-dom";
import {
  Plane,
  Clock,
  Heart,
  Bell,
  CalendarCheck,
  FileText,
  MessageCircle,
  Search,
  ImageOff,
  ChevronRight,
  MapPin,
} from "lucide-react";
import { useMyDeals } from "@/features/deals/hooks/useDeals";
import { useMyApplications } from "@/features/applications/hooks/useApplications";
import { useSavedListingIds } from "@/features/listings/hooks/useSavedListings";
import { useMyInquiries } from "@/features/inquiry/hooks/useInquiry";
import { useUnreadCount } from "@/features/notifications/hooks/useNotifications";
import { Badge } from "@/components/ui/badge";
import type { UserProfileDto } from "@/api/types";
import { ProfileHealthCard } from "./ProfileHealthCard";
import {
  StatCard,
  SectionCard,
  QuickAction,
  EmptyHint,
} from "./DashboardKit";
import { formatDayRange, dealPhaseMeta, appStatusMeta } from "./dashboardFormat";
import {
  DealIssueBanner,
  EndingSoonBanner,
  EndingSoonBadge,
} from "@/features/deals/components/BookingAttentionBanner";
import {
  getDealIssue,
  getEndingSoon,
  sortDealsByAttention,
} from "@/features/deals/utils/bookingAttention";
import { cn } from "@/lib/utils";

const ONGOING_PHASES = new Set([
  "TruthSurface",
  "Checkout",
  "Active",
  "PaymentFailed",
  "AwaitingDepositReturn",
]);

export function TravelingDashboard({ user }: { user: UserProfileDto }) {
  const userId = user.userId;
  const { data: deals } = useMyDeals("all");
  const { data: apps } = useMyApplications();
  const { data: savedIds } = useSavedListingIds();
  const { data: inquiries } = useMyInquiries();
  const { data: unreadCount } = useUnreadCount();

  const myTrips = useMemo(
    () => (deals ?? []).filter((d) => d.tenantUserId === userId),
    [deals, userId],
  );
  const myApps = useMemo(
    () => (apps ?? []).filter((a) => a.tenantUserId === userId),
    [apps, userId],
  );

  const ongoingTrips = useMemo(
    () =>
      sortDealsByAttention(
        myTrips.filter((d) => ONGOING_PHASES.has(d.dealPhase)),
      ),
    [myTrips],
  );

  const attentionTrips = useMemo(
    () =>
      ongoingTrips.filter(
        (d) => getDealIssue(d, "guest") || getEndingSoon(d),
      ),
    [ongoingTrips],
  );

  const activeCount = myTrips.filter((d) => d.dealPhase === "Active").length;
  const pendingApps = myApps.filter((a) => a.status === "Pending");
  const approvedApps = myApps.filter((a) => a.status === "Approved");
  const savedCount = savedIds?.size ?? 0;
  const openConversations = (inquiries ?? []).filter(
    (i) => i.status !== "Closed",
  ).length;

  return (
    <div className="space-y-6">
      {attentionTrips.length > 0 && (
        <div className="space-y-3">
          {attentionTrips.slice(0, 3).map((deal) => {
            const issue = getDealIssue(deal, "guest");
            if (issue) {
              return <DealIssueBanner key={deal.dealId} issue={issue} />;
            }
            const ending = getEndingSoon(deal);
            if (!ending) return null;
            return (
              <EndingSoonBanner
                key={deal.dealId}
                listingTitle={deal.listingTitle}
                ending={ending}
                href={`/app/deals/${deal.dealId}`}
              />
            );
          })}
        </div>
      )}

      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <StatCard
          label="Active trips"
          value={activeCount}
          icon={Plane}
          tone="primary"
          to="/app/deals"
          hint={ongoingTrips.length > activeCount ? `${ongoingTrips.length - activeCount} in progress` : undefined}
        />
        <StatCard
          label="Pending requests"
          value={pendingApps.length}
          icon={Clock}
          tone="warning"
          to="/app/my-applications"
          hint={approvedApps.length > 0 ? `${approvedApps.length} approved` : undefined}
        />
        <StatCard
          label="Saved listings"
          value={savedCount}
          icon={Heart}
          tone="accent"
          to="/app/saved"
        />
        <StatCard
          label="Notifications"
          value={unreadCount ?? 0}
          icon={Bell}
          to="/app/notifications"
          hint={openConversations > 0 ? `${openConversations} open chats` : undefined}
        />
      </div>

      <div className="grid gap-6 lg:grid-cols-2">
        <SectionCard
          title="Your trips"
          icon={CalendarCheck}
          action={
            <Link to="/app/deals" className="text-sm font-medium text-primary hover:underline">
              View all
            </Link>
          }
        >
          {ongoingTrips.length === 0 ? (
            <EmptyHint cta={{ to: "/listings", label: "Browse listings" }}>
              No active trips yet. Find your next mid-term stay.
            </EmptyHint>
          ) : (
            <ul className="space-y-2">
              {ongoingTrips.slice(0, 4).map((d) => {
                const phase = dealPhaseMeta(d.dealPhase);
                const ending = getEndingSoon(d);
                const failed = d.dealPhase === "PaymentFailed";
                return (
                  <li key={d.dealId}>
                    <Link
                      to={`/app/deals/${d.dealId}`}
                      className={cn(
                        "flex items-center gap-3 rounded-lg border p-3 transition-colors hover:bg-muted/50",
                        failed && "border-destructive/40 bg-destructive/5",
                        !failed && ending && "border-amber-300 bg-amber-50/60",
                      )}
                    >
                      <Thumb src={d.listingCoverPhotoUri} />
                      <div className="min-w-0 flex-1">
                        <p className="truncate text-sm font-medium">{d.listingTitle}</p>
                        <p className="flex items-center gap-2 text-xs text-muted-foreground">
                          {d.listingCity && (
                            <span className="flex items-center gap-1">
                              <MapPin className="h-3 w-3" />
                              {d.listingCity}
                            </span>
                          )}
                          <span>{formatDayRange(d.requestedCheckIn, d.requestedCheckOut)}</span>
                        </p>
                      </div>
                      <div className="flex shrink-0 flex-col items-end gap-1">
                        <Badge variant={phase.variant}>{phase.label}</Badge>
                        {ending && <EndingSoonBadge ending={ending} />}
                      </div>
                      <ChevronRight className="h-4 w-4 shrink-0 text-muted-foreground" />
                    </Link>
                  </li>
                );
              })}
            </ul>
          )}
        </SectionCard>

        <SectionCard
          title="Your requests"
          icon={FileText}
          action={
            <Link
              to="/app/my-applications"
              className="text-sm font-medium text-primary hover:underline"
            >
              View all
            </Link>
          }
        >
          {myApps.length === 0 ? (
            <EmptyHint cta={{ to: "/listings", label: "Find a place" }}>
              You haven&apos;t requested any bookings yet.
            </EmptyHint>
          ) : (
            <ul className="space-y-2">
              {myApps
                .slice()
                .sort(
                  (a, b) =>
                    new Date(b.submittedAt).getTime() -
                    new Date(a.submittedAt).getTime(),
                )
                .slice(0, 4)
                .map((a) => {
                  const meta = appStatusMeta(a.status);
                  return (
                    <li key={a.applicationId}>
                      <Link
                        to={`/app/applications/${a.applicationId}`}
                        className="flex items-center gap-3 rounded-lg border p-3 transition-colors hover:bg-muted/50"
                      >
                        <Thumb src={a.listingCoverPhotoUri} />
                        <div className="min-w-0 flex-1">
                          <p className="truncate text-sm font-medium">
                            {a.listingTitle ?? "Listing"}
                          </p>
                          <p className="text-xs text-muted-foreground">
                            {formatDayRange(a.requestedCheckIn, a.requestedCheckOut)}
                          </p>
                        </div>
                        <Badge variant={meta.variant}>{meta.label}</Badge>
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
          <QuickAction label="Browse listings" description="Find your next stay" to="/listings" icon={Search} />
          <QuickAction label="My reservations" description="Trips you've booked" to="/app/deals" icon={CalendarCheck} />
          <QuickAction label="My requests" description="Track booking requests" to="/app/my-applications" icon={FileText} />
          <QuickAction label="Saved listings" description="Places you've bookmarked" to="/app/saved" icon={Heart} />
          <QuickAction label="Conversations" description="Chat with hosts" to="/app/my-inquiries" icon={MessageCircle} />
          <QuickAction label="Notifications" description="Latest updates" to="/app/notifications" icon={Bell} />
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
