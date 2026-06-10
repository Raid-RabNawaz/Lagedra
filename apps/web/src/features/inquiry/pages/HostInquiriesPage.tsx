import { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import {
  ArrowRight,
  Clock,
  ImageOff,
  MapPin,
  MessageSquare,
  Search,
  User,
  X,
} from "lucide-react";
import { Card, CardContent } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { PageHeader } from "@/components/shared/PageHeader";
import { ListRowsSkeleton } from "@/components/shared/ListSkeleton";
import { EmptyState } from "@/components/shared/EmptyState";
import { ErrorState } from "@/components/shared/ErrorState";
import { useHostInquiries } from "@/features/inquiry/hooks/useInquiry";
import { InquiryStatusBadge } from "@/features/inquiry/components/InquiryStatusBadge";
import { formatDate } from "@/utils/format";
import type { HostInquirySummaryDto, InquirySessionStatus } from "@/api/types";
import { cn } from "@/lib/utils";

type StatusFilter = InquirySessionStatus | "All" | "Unanswered";

const statusTabs: { value: StatusFilter; label: string }[] = [
  { value: "All", label: "All" },
  { value: "Unanswered", label: "Awaiting reply" },
  { value: "Open", label: "Open" },
  { value: "Locked", label: "Locked" },
  { value: "Closed", label: "Closed" },
];

/**
 * Phase 17 — host inbox for pre-booking and deal-linked inquiries. Lives
 * at <code>/app/inquiries</code> so hosts can find every active thread
 * targeting one of their listings without relying on the email link from
 * the <code>inquiry_started</code> notification.
 */
export const HostInquiriesPage = () => {
  const { data, isLoading, isError, refetch } = useHostInquiries();
  const [statusFilter, setStatusFilter] = useState<StatusFilter>("All");
  const [search, setSearch] = useState("");

  const counts = useMemo(() => {
    const base: Record<StatusFilter, number> = {
      All: data?.length ?? 0,
      Unanswered: 0,
      Open: 0,
      Locked: 0,
      Closed: 0,
    };
    for (const r of data ?? []) {
      if (r.unansweredCount > 0) base.Unanswered += 1;
      if (r.status in base) base[r.status as StatusFilter] += 1;
    }
    return base;
  }, [data]);

  const filtered = useMemo<HostInquirySummaryDto[]>(() => {
    if (!data) return [];
    const q = search.trim().toLowerCase();
    return data.filter((row) => {
      if (statusFilter === "Unanswered" && row.unansweredCount === 0) {
        return false;
      }
      if (
        statusFilter !== "All" &&
        statusFilter !== "Unanswered" &&
        row.status !== statusFilter
      ) {
        return false;
      }
      if (!q) return true;
      const haystack = [row.listingTitle, row.listingCity, row.tenantDisplayName]
        .filter(Boolean)
        .join(" ")
        .toLowerCase();
      return haystack.includes(q);
    });
  }, [data, statusFilter, search]);

  const totalUnanswered = counts.Unanswered;

  return (
    <div className="mx-auto max-w-4xl space-y-6">
      <PageHeader
        icon={MessageSquare}
        title="Guest inquiries"
        description={
          <>
            Pre-booking conversations from guests interested in your listings.
            {totalUnanswered > 0 && (
              <span className="font-medium text-foreground">
                {" "}
                {totalUnanswered} awaiting your reply.
              </span>
            )}
          </>
        }
      />

      {isError ? (
        <ErrorState
          title="Couldn't load inquiries"
          message="Something went wrong while loading guest inquiries."
          onRetry={() => void refetch()}
        />
      ) : isLoading ? (
        <ListRowsSkeleton rows={4} />
      ) : (data?.length ?? 0) === 0 ? (
        <EmptyState
          title="No inquiries yet"
          description="When a guest asks a question on one of your listings, the conversation will land here."
        />
      ) : (
        <>
          <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
            <Tabs
              value={statusFilter}
              onValueChange={(v) => setStatusFilter(v as StatusFilter)}
            >
              <TabsList className="overflow-x-auto">
                {statusTabs.map((t) => (
                  <TabsTrigger key={t.value} value={t.value} className="gap-1.5">
                    {t.label}
                    <span
                      className={cn(
                        "rounded-full px-1.5 text-[10px] font-semibold tabular-nums",
                        statusFilter === t.value
                          ? "bg-foreground text-background"
                          : "bg-muted text-muted-foreground",
                      )}
                    >
                      {counts[t.value]}
                    </span>
                  </TabsTrigger>
                ))}
              </TabsList>
            </Tabs>

            <div className="relative w-full sm:max-w-xs">
              <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
              <Input
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                placeholder="Search by listing, city, or guest"
                className="pl-9"
              />
              {search && (
                <button
                  type="button"
                  onClick={() => setSearch("")}
                  aria-label="Clear search"
                  className="absolute right-2 top-1/2 -translate-y-1/2 rounded-md p-1 text-muted-foreground hover:bg-muted cursor-pointer"
                >
                  <X className="h-3.5 w-3.5" />
                </button>
              )}
            </div>
          </div>

          {filtered.length === 0 ? (
            <EmptyState
              title="No matching threads"
              description="Try clearing the filters or search to see all inquiries."
            />
          ) : (
            <div className="space-y-3">
              {filtered.map((row) => (
                <InquiryRow key={row.sessionId} row={row} />
              ))}
            </div>
          )}
        </>
      )}
    </div>
  );
};

const InquiryRow = ({ row }: { row: HostInquirySummaryDto }) => {
  const threadHref = `/app/inquiry/${row.sessionId}`;

  return (
    <Card className="overflow-hidden transition-colors hover:border-primary/40">
      <CardContent className="flex gap-4 p-4">
        <Link
          to={threadHref}
          aria-label={`Open conversation about ${row.listingTitle ?? "listing"}`}
          className="relative hidden h-20 w-20 shrink-0 overflow-hidden rounded-lg bg-muted sm:block"
        >
          {row.listingCoverPhotoUri ? (
            <img
              src={row.listingCoverPhotoUri}
              alt={row.listingTitle ?? ""}
              className="h-full w-full object-cover"
              loading="lazy"
            />
          ) : (
            <div className="flex h-full w-full items-center justify-center">
              <ImageOff className="h-6 w-6 text-muted-foreground/40" />
            </div>
          )}
        </Link>

        <div className="min-w-0 flex-1">
          <div className="flex items-start justify-between gap-3">
            <div className="min-w-0 flex-1">
              <Link
                to={threadHref}
                className="font-semibold leading-snug line-clamp-1 hover:underline"
              >
                {row.listingTitle ?? "Untitled listing"}
              </Link>
              <div className="mt-1 flex flex-wrap items-center gap-x-3 gap-y-1 text-xs text-muted-foreground">
                {row.listingCity && (
                  <span className="flex items-center gap-1">
                    <MapPin className="h-3 w-3" />
                    {row.listingCity}
                  </span>
                )}
                <span className="flex items-center gap-1">
                  <User className="h-3 w-3" />
                  {row.tenantDisplayName ?? "Guest"}
                </span>
                <span className="flex items-center gap-1">
                  <Clock className="h-3 w-3" />
                  {formatDate(row.lastActivityAt)}
                </span>
              </div>
            </div>
            <div className="flex shrink-0 flex-col items-end gap-1.5">
              <InquiryStatusBadge status={row.status} />
              {row.unansweredCount > 0 && (
                <Badge variant="accent" className="text-xs">
                  {row.unansweredCount} awaiting reply
                </Badge>
              )}
              {row.dealId && (
                <Badge variant="secondary" className="text-xs">
                  Linked to deal
                </Badge>
              )}
            </div>
          </div>

          <div className="mt-3 flex items-center justify-between gap-3">
            <p className="text-xs text-muted-foreground">
              {row.questionCount === 0
                ? "Conversation started — no questions yet"
                : `${row.questionCount} question${row.questionCount === 1 ? "" : "s"} in thread`}
            </p>
            <div className="flex items-center gap-2">
              {row.dealId && (
                <Link to={`/app/deals/${row.dealId}`}>
                  <Button variant="ghost" size="sm">
                    View deal
                  </Button>
                </Link>
              )}
              <Link to={threadHref}>
                <Button size="sm" className="gap-1.5">
                  Open thread
                  <ArrowRight className="h-3.5 w-3.5" />
                </Button>
              </Link>
            </div>
          </div>
        </div>
      </CardContent>
    </Card>
  );
};
