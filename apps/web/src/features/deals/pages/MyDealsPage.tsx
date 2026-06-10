import { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { CalendarCheck, Search } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Tabs, TabsList, TabsTrigger, TabsContent } from "@/components/ui/tabs";
import { DealCard } from "@/features/deals/components/DealCard";
import { PageHeader } from "@/components/shared/PageHeader";
import { ListRowsSkeleton } from "@/components/shared/ListSkeleton";
import { EmptyState } from "@/components/shared/EmptyState";
import { ErrorState } from "@/components/shared/ErrorState";
import { useMyDeals } from "@/features/deals/hooks/useDeals";
import { useAuthStore } from "@/app/auth/authStore";
import { useModeStore } from "@/app/auth/modeStore";
import type { DealPhaseFilter, DealSummaryDto } from "@/api/types";
import { cn } from "@/lib/utils";

const tabs: { value: DealPhaseFilter; label: string }[] = [
  { value: "active", label: "Active" },
  { value: "past", label: "Past" },
  { value: "all", label: "All" },
];

function isPast(deal: DealSummaryDto): boolean {
  return deal.dealPhase === "Closed" || deal.dealPhase === "Cancelled";
}

function matchesPhase(deal: DealSummaryDto, phase: DealPhaseFilter): boolean {
  if (phase === "all") return true;
  if (phase === "past") return isPast(deal);
  return !isPast(deal);
}

export function MyDealsPage() {
  const [phase, setPhase] = useState<DealPhaseFilter>("active");
  const user = useAuthStore((s) => s.user);
  const mode = useModeStore((s) => s.mode);

  // Fetch the full set once and derive the per-tab slices client-side so
  // counts stay accurate on every tab and switching tabs doesn't refetch.
  const { data, isLoading, error, refetch } = useMyDeals("all");

  const counts = useMemo(() => {
    const all = data ?? [];
    return {
      active: all.filter((d) => !isPast(d)).length,
      past: all.filter(isPast).length,
      all: all.length,
    } satisfies Record<DealPhaseFilter, number>;
  }, [data]);

  const visible = useMemo(
    () => (data ?? []).filter((d) => matchesPhase(d, phase)),
    [data, phase],
  );

  const hostDealsCount = useMemo(
    () => (data ?? []).filter((d) => user && d.landlordUserId === user.userId).length,
    [data, user],
  );
  const guestDealsCount = useMemo(
    () => (data ?? []).filter((d) => user && d.tenantUserId === user.userId).length,
    [data, user],
  );

  // Host mode shows "Bookings" framing; guest mode keeps the traveller
  // wording. When the user has both we fall back to a hybrid label so
  // neither side feels mislabeled.
  const isHostContext =
    mode === "host" || (hostDealsCount > 0 && guestDealsCount === 0);
  const headerTitle = isHostContext ? "Bookings" : "My reservations";
  const headerDescription = isHostContext
    ? "Approved bookings on your listings and trips you've booked yourself."
    : "Trips you've booked and reservations on your listings.";

  return (
    <div className="mx-auto max-w-4xl space-y-6">
      <PageHeader
        icon={CalendarCheck}
        title={headerTitle}
        description={headerDescription}
      />

      <Tabs value={phase} onValueChange={(v) => setPhase(v as DealPhaseFilter)}>
        <TabsList>
          {tabs.map((t) => (
            <TabsTrigger key={t.value} value={t.value} className="gap-1.5">
              {t.label}
              <span
                className={cn(
                  "rounded-full px-1.5 text-[10px] font-semibold tabular-nums",
                  phase === t.value
                    ? "bg-foreground text-background"
                    : "bg-muted text-muted-foreground",
                )}
              >
                {counts[t.value]}
              </span>
            </TabsTrigger>
          ))}
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
                  t.value === "active"
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
                  t.value === "past"
                    ? "Completed and cancelled bookings will be archived here."
                    : isHostContext
                      ? "Bookings appear here once you approve a guest application."
                      : "Reservations appear here once an application is approved. Browse listings to get started."
                }
              >
                {t.value !== "past" && !isHostContext && (
                  <Link to="/listings">
                    <Button variant="accent" size="sm">
                      <Search className="h-4 w-4" />
                      Browse listings
                    </Button>
                  </Link>
                )}
                {t.value !== "past" && isHostContext && (
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
