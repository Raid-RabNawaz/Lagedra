import { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { CalendarCheck, Search } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Tabs, TabsList, TabsTrigger, TabsContent } from "@/components/ui/tabs";
import { DealCard } from "@/features/deals/components/DealCard";
import {
  DealIssueBanner,
  EndingSoonBanner,
} from "@/features/deals/components/BookingAttentionBanner";
import { PageHeader } from "@/components/shared/PageHeader";
import { ListRowsSkeleton } from "@/components/shared/ListSkeleton";
import { EmptyState } from "@/components/shared/EmptyState";
import { ErrorState } from "@/components/shared/ErrorState";
import { useMyDeals } from "@/features/deals/hooks/useDeals";
import { useAuthStore } from "@/app/auth/authStore";
import { useModeStore } from "@/app/auth/modeStore";
import {
  dealNeedsAttention,
  getDealIssue,
  getEndingSoon,
  sortDealsByAttention,
} from "@/features/deals/utils/bookingAttention";
import type { DealPhaseFilter, DealSummaryDto } from "@/api/types";
import { cn } from "@/lib/utils";

type ListFilter = DealPhaseFilter | "attention";

const tabs: { value: ListFilter; label: string }[] = [
  { value: "attention", label: "Needs attention" },
  { value: "active", label: "Active" },
  { value: "past", label: "Past" },
  { value: "all", label: "All" },
];

function isPast(deal: DealSummaryDto): boolean {
  return deal.dealPhase === "Closed" || deal.dealPhase === "Cancelled";
}

function matchesFilter(deal: DealSummaryDto, filter: ListFilter): boolean {
  if (filter === "attention") return dealNeedsAttention(deal);
  if (filter === "all") return true;
  if (filter === "past") return isPast(deal);
  return !isPast(deal);
}

export function MyDealsPage() {
  const [phase, setPhase] = useState<ListFilter>("active");
  const user = useAuthStore((s) => s.user);
  const mode = useModeStore((s) => s.mode);

  const { data, isLoading, error, refetch } = useMyDeals("all");

  const isHostContext = mode === "host";
  const perspective = isHostContext ? "host" : "guest";

  const scoped = useMemo(() => {
    const all = data ?? [];
    if (!user) return all;
    return all.filter((d) =>
      isHostContext
        ? d.landlordUserId === user.userId
        : d.tenantUserId === user.userId,
    );
  }, [data, user, isHostContext]);

  const counts = useMemo(() => {
    return {
      attention: scoped.filter((d) => dealNeedsAttention(d)).length,
      active: scoped.filter((d) => !isPast(d)).length,
      past: scoped.filter(isPast).length,
      all: scoped.length,
    } satisfies Record<ListFilter, number>;
  }, [scoped]);

  const visible = useMemo(() => {
    const filtered = scoped.filter((d) => matchesFilter(d, phase));
    return phase === "attention" || phase === "active"
      ? sortDealsByAttention(filtered)
      : filtered;
  }, [scoped, phase]);

  const attentionDeals = useMemo(
    () => sortDealsByAttention(scoped.filter((d) => dealNeedsAttention(d))),
    [scoped],
  );

  const headerTitle = isHostContext ? "Bookings" : "My reservations";
  const headerDescription = isHostContext
    ? "Approved bookings on your listings."
    : "Trips you've booked.";

  return (
    <div className="mx-auto max-w-4xl space-y-6">
      <PageHeader
        icon={CalendarCheck}
        title={headerTitle}
        description={headerDescription}
      />

      {attentionDeals.length > 0 && (
        <div className="space-y-3">
          {attentionDeals.slice(0, 3).map((deal) => {
            const issue = getDealIssue(deal, perspective);
            if (issue) {
              return (
                <DealIssueBanner
                  key={deal.dealId}
                  issue={issue}
                  showContactGuest={isHostContext && issue.kind === "PaymentFailed"}
                />
              );
            }
            const ending = getEndingSoon(deal);
            if (ending) {
              return (
                <EndingSoonBanner
                  key={deal.dealId}
                  listingTitle={deal.listingTitle}
                  ending={ending}
                  href={`/app/deals/${deal.dealId}`}
                />
              );
            }
            return null;
          })}
        </div>
      )}

      <Tabs value={phase} onValueChange={(v) => setPhase(v as ListFilter)}>
        <TabsList className="flex h-auto flex-wrap gap-1">
          {tabs.map((t) => {
            if (t.value === "attention" && counts.attention === 0) return null;
            return (
              <TabsTrigger key={t.value} value={t.value} className="gap-1.5">
                {t.label}
                <span
                  className={cn(
                    "rounded-full px-1.5 text-[10px] font-semibold tabular-nums",
                    phase === t.value
                      ? "bg-foreground text-background"
                      : t.value === "attention"
                        ? "bg-destructive/15 text-destructive"
                        : "bg-muted text-muted-foreground",
                  )}
                >
                  {counts[t.value]}
                </span>
              </TabsTrigger>
            );
          })}
        </TabsList>

        {tabs.map((t) => (
          <TabsContent key={t.value} value={t.value} className="mt-4">
            {isLoading ? (
              <ListRowsSkeleton rows={3} />
            ) : error ? (
              <ErrorState
                title="Couldn't load reservations"
                message="Something went wrong while loading your reservations."
                onRetry={() => void refetch()}
              />
            ) : visible.length === 0 ? (
              <EmptyState
                title={
                  t.value === "attention"
                    ? "Nothing needs attention"
                    : t.value === "active"
                      ? isHostContext
                        ? "No active bookings"
                        : "No active reservations"
                      : t.value === "past"
                        ? isHostContext
                          ? "No past bookings"
                          : "No past reservations"
                        : isHostContext
                          ? "No bookings yet"
                          : "No reservations yet"
                }
                description={
                  t.value === "attention"
                    ? "Payment issues and stays ending within 15 days will show up here."
                    : t.value === "past"
                      ? "Completed and cancelled bookings will be archived here."
                      : isHostContext
                        ? "Bookings appear here once you approve a guest application."
                        : "Reservations appear here once an application is approved. Browse listings to get started."
                }
              >
                {t.value !== "past" && t.value !== "attention" && !isHostContext && (
                  <Link to="/listings">
                    <Button variant="accent" size="sm">
                      <Search className="h-4 w-4" />
                      Browse listings
                    </Button>
                  </Link>
                )}
                {t.value !== "past" && t.value !== "attention" && isHostContext && (
                  <Link to="/app/applications">
                    <Button variant="accent" size="sm">
                      <Search className="h-4 w-4" />
                      Review booking requests
                    </Button>
                  </Link>
                )}
              </EmptyState>
            ) : (
              <div className="grid gap-4">
                {visible.map((deal) => (
                  <DealCard key={deal.dealId} deal={deal} />
                ))}
              </div>
            )}
          </TabsContent>
        ))}
      </Tabs>
    </div>
  );
}
