import { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import {
  ArrowRight,
  Clock,
  Home,
  ImageOff,
  MapPin,
  MessageCircle,
  Search,
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
import { useMyInquiries } from "@/features/inquiry/hooks/useInquiry";
import { InquiryStatusBadge } from "@/features/inquiry/components/InquiryStatusBadge";
import { inquiryThreadHref } from "@/features/inquiry/utils/inquiryThreadHref";
import { formatDate } from "@/utils/format";
import type { InquirySessionStatus, TenantInquirySummaryDto } from "@/api/types";
import { cn } from "@/lib/utils";

type StatusFilter = InquirySessionStatus | "All" | "Awaiting";

const statusTabs: { value: StatusFilter; label: string }[] = [
  { value: "All", label: "All" },
  { value: "Awaiting", label: "Awaiting reply" },
  { value: "Open", label: "Open" },
  { value: "Locked", label: "Locked" },
  { value: "Closed", label: "Closed" },
];

/**
 * Phase 17 — tenant-side counterpart of the host inquiries inbox. Lives
 * at <code>/app/my-inquiries</code> and mirrors the layout of
 * <code>HostInquiriesPage</code> so the sent / received split is
 * obvious from the sidebar group ("Bookings" vs. "Hosting") rather
 * than from any role gating.
 */
export const MyInquiriesPage = () => {
  const { data, isLoading, isError, refetch } = useMyInquiries();
  const [statusFilter, setStatusFilter] = useState<StatusFilter>("All");
  const [search, setSearch] = useState("");

  const counts = useMemo(() => {
    const base: Record<StatusFilter, number> = {
      All: data?.length ?? 0,
      Awaiting: 0,
      Open: 0,
      Locked: 0,
      Closed: 0,
    };
    for (const r of data ?? []) {
      if (r.unansweredByHostCount > 0) base.Awaiting += 1;
      if (r.status in base) base[r.status as StatusFilter] += 1;
    }
    return base;
  }, [data]);

  const filtered = useMemo<TenantInquirySummaryDto[]>(() => {
    if (!data) return [];
    const q = search.trim().toLowerCase();
    return data.filter((row) => {
      if (statusFilter === "Awaiting" && row.unansweredByHostCount === 0) {
        return false;
      }
      if (
        statusFilter !== "All" &&
        statusFilter !== "Awaiting" &&
        row.status !== statusFilter
      ) {
        return false;
      }
      if (!q) return true;
      const haystack = [row.listingTitle, row.listingCity, row.landlordDisplayName]
        .filter(Boolean)
        .join(" ")
        .toLowerCase();
      return haystack.includes(q);
    });
  }, [data, statusFilter, search]);

  const totalAwaiting = counts.Awaiting;

  return (
    <div className="mx-auto max-w-4xl space-y-6">
      <PageHeader
        icon={MessageCircle}
        title="My conversations"
        description={
          <>
            Inquiries you've started with hosts before applying.
            {totalAwaiting > 0 && (
              <span className="font-medium text-foreground">
                {" "}
                {totalAwaiting} still awaiting a host reply.
              </span>
            )}
          </>
        }
      />

      {isError ? (
        <ErrorState
          title="Couldn't load conversations"
          message="Something went wrong while loading your conversations."
          onRetry={() => void refetch()}
        />
      ) : isLoading ? (
        <ListRowsSkeleton rows={4} />
      ) : (data?.length ?? 0) === 0 ? (
        <EmptyState
          title="No conversations yet"
          description="When you ask a host a question on a listing, the thread will land here so you can pick up where you left off."
        >
          <Link to="/listings">
            <Button variant="accent" size="sm">
              <Search className="h-4 w-4" />
              Browse listings
            </Button>
          </Link>
        </EmptyState>
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
                placeholder="Search by listing, city, or host"
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
              title="No matching conversations"
              description="Try clearing the filters or search to see all conversations."
            />
          ) : (
            <div className="space-y-3">
              {filtered.map((row) => (
                <ConversationRow key={row.sessionId} row={row} />
              ))}
            </div>
          )}
        </>
      )}
    </div>
  );
};

const ConversationRow = ({ row }: { row: TenantInquirySummaryDto }) => {
  const threadHref = inquiryThreadHref(row);

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
                  <Home className="h-3 w-3" />
                  Host: {row.landlordDisplayName ?? "—"}
                </span>
                <span className="flex items-center gap-1">
                  <Clock className="h-3 w-3" />
                  {formatDate(row.lastActivityAt)}
                </span>
              </div>
            </div>
            <div className="flex shrink-0 flex-col items-end gap-1.5">
              <InquiryStatusBadge status={row.status} />
              {row.unansweredByHostCount > 0 && (
                <Badge variant="secondary" className="text-xs">
                  {row.unansweredByHostCount} awaiting host
                </Badge>
              )}
              {row.dealId && (
                <Badge variant="accent" className="text-xs">
                  Linked to deal
                </Badge>
              )}
              {row.partnerOrganizationId && (
                <Badge variant="outline" className="text-xs">
                  Partner on thread
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
