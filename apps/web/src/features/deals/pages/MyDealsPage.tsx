import { useState } from "react";
import { Tabs, TabsList, TabsTrigger, TabsContent } from "@/components/ui/tabs";
import { DealCard } from "@/features/deals/components/DealCard";
import { Loader } from "@/components/shared/Loader";
import { EmptyState } from "@/components/shared/EmptyState";
import { useMyDeals } from "@/features/deals/hooks/useDeals";
import type { DealPhaseFilter } from "@/api/types";

const tabs: { value: DealPhaseFilter; label: string }[] = [
  { value: "active", label: "Active" },
  { value: "past", label: "Past" },
  { value: "all", label: "All" },
];

export function MyDealsPage() {
  const [phase, setPhase] = useState<DealPhaseFilter>("active");
  const { data: deals, isLoading, error } = useMyDeals(phase);

  return (
    <div className="mx-auto max-w-4xl space-y-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">My Deals</h1>
        <p className="text-muted-foreground mt-1">
          Trips you&apos;ve booked and reservations on your listings.
        </p>
      </div>

      <Tabs value={phase} onValueChange={(v) => setPhase(v as DealPhaseFilter)}>
        <TabsList>
          {tabs.map((t) => (
            <TabsTrigger key={t.value} value={t.value}>
              {t.label}
              {deals && t.value === phase && (
                <span className="ml-1.5 text-xs text-muted-foreground">
                  ({deals.length})
                </span>
              )}
            </TabsTrigger>
          ))}
        </TabsList>

        {tabs.map((t) => (
          <TabsContent key={t.value} value={t.value}>
            {isLoading && <Loader label="Loading deals..." />}

            {error && (
              <EmptyState
                title="Something went wrong"
                description="Failed to load your deals. Please try again."
              />
            )}

            {!isLoading && !error && deals?.length === 0 && (
              <EmptyState
                title={
                  t.value === "active"
                    ? "No active deals"
                    : t.value === "past"
                      ? "No past deals"
                      : "No deals yet"
                }
                description="Deals will appear here once an application is approved."
              />
            )}

            {deals && deals.length > 0 && (
              <div className="grid gap-4">
                {deals.map((deal) => (
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
