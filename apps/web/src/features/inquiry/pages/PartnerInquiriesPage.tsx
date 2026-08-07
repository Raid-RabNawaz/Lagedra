import { useMemo, useState, type FormEvent } from "react";
import { Link, useNavigate } from "react-router-dom";
import {
  ArrowRight,
  Clock,
  ImageOff,
  MapPin,
  MessageSquare,
  Plus,
  Search,
  User,
  X,
} from "lucide-react";
import { Card, CardContent } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select } from "@/components/ui/select";
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs";
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { PageHeader } from "@/components/shared/PageHeader";
import { ListRowsSkeleton } from "@/components/shared/ListSkeleton";
import { EmptyState } from "@/components/shared/EmptyState";
import { ErrorState } from "@/components/shared/ErrorState";
import { Alert } from "@/components/ui/alert";
import {
  usePartnerInquiries,
  useStartPartnerListingInquiry,
} from "@/features/inquiry/hooks/useInquiry";
import { InquiryStatusBadge } from "@/features/inquiry/components/InquiryStatusBadge";
import { inquiryThreadHref } from "@/features/inquiry/utils/inquiryThreadHref";
import { usePartnerMembership } from "@/features/partners/hooks/usePartnerMembership";
import { partnerApi } from "@/features/partners/services/partnerApi";
import { formatDate } from "@/utils/format";
import { getApiErrorMessage } from "@/api/errors";
import type {
  InquirySessionStatus,
  PartnerInquirySummaryDto,
} from "@/api/types";
import { cn } from "@/lib/utils";
import { useQuery } from "@tanstack/react-query";

type StatusFilter = InquirySessionStatus | "All" | "Unanswered";

const statusTabs: { value: StatusFilter; label: string }[] = [
  { value: "All", label: "All" },
  { value: "Unanswered", label: "Awaiting host" },
  { value: "Open", label: "Open" },
  { value: "Closed", label: "Closed" },
];

/**
 * Partner staff inbox for inquiry threads where their organization is attached.
 */
export const PartnerInquiriesPage = () => {
  const navigate = useNavigate();
  const { data, isLoading, isError, refetch } = usePartnerInquiries();
  const { membership } = usePartnerMembership();
  const startInquiry = useStartPartnerListingInquiry();
  const [statusFilter, setStatusFilter] = useState<StatusFilter>("All");
  const [search, setSearch] = useState("");
  const [startOpen, setStartOpen] = useState(false);
  const [listingId, setListingId] = useState("");
  const [tenantUserId, setTenantUserId] = useState("");

  const orgId = membership?.organization.id;
  const members = useQuery({
    queryKey: ["partner", "endorsed-members", orgId],
    queryFn: () => partnerApi.listEndorsedMembers(orgId!),
    enabled: startOpen && !!orgId,
    staleTime: 60_000,
  });

  const counts = useMemo(() => {
    const base: Record<StatusFilter, number> = {
      All: data?.length ?? 0,
      Unanswered: 0,
      Open: 0,
      Locked: 0,
      Closed: 0,
    };
    for (const r of data ?? []) {
      if (r.unansweredByHostCount > 0) base.Unanswered += 1;
      if (r.status in base) base[r.status as StatusFilter] += 1;
    }
    return base;
  }, [data]);

  const filtered = useMemo<PartnerInquirySummaryDto[]>(() => {
    if (!data) return [];
    const q = search.trim().toLowerCase();
    return data.filter((row) => {
      if (statusFilter === "Unanswered" && row.unansweredByHostCount === 0) {
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
  }, [data, search, statusFilter]);

  const onStart = (e: FormEvent) => {
    e.preventDefault();
    if (!listingId.trim() || !tenantUserId) return;
    startInquiry.mutate(
      { listingId: listingId.trim(), tenantUserId },
      {
        onSuccess: (session) => {
          setStartOpen(false);
          setListingId("");
          setTenantUserId("");
          void navigate(inquiryThreadHref(session));
        },
      },
    );
  };

  return (
    <div className="mx-auto max-w-4xl space-y-6 px-4 py-8 sm:px-6 lg:px-8">
      <PageHeader
        icon={MessageSquare}
        title="Member inquiries"
        description="Conversations where your organization was invited to support a member."
      >
        <Button className="gap-1.5" onClick={() => setStartOpen(true)}>
          <Plus className="h-4 w-4" />
          Start inquiry
        </Button>
      </PageHeader>

      {isError ? (
        <ErrorState
          title="Couldn't load inquiries"
          message="Try again in a moment."
          onRetry={() => void refetch()}
        />
      ) : isLoading ? (
        <ListRowsSkeleton rows={4} />
      ) : (data?.length ?? 0) === 0 && !search && statusFilter === "All" ? (
        <EmptyState
          title="No inquiries yet"
          description="When a member invites your organization into a listing conversation — or you start one for them — it will show up here."
        >
          <Button className="gap-1.5" onClick={() => setStartOpen(true)}>
            <Plus className="h-4 w-4" />
            Start inquiry
          </Button>
        </EmptyState>
      ) : (
        <>
          <div className="mb-4 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
            <Tabs
              value={statusFilter}
              onValueChange={(v) => setStatusFilter(v as StatusFilter)}
            >
              <TabsList>
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
            <div className="relative w-full sm:w-64">
              <Search className="absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground" />
              <Input
                className="pl-8"
                placeholder="Search…"
                value={search}
                onChange={(e) => setSearch(e.target.value)}
              />
              {search && (
                <button
                  type="button"
                  className="absolute right-2.5 top-2.5 text-muted-foreground"
                  onClick={() => setSearch("")}
                >
                  <X className="h-4 w-4" />
                </button>
              )}
            </div>
          </div>

          {filtered.length === 0 ? (
            <EmptyState
              title="No matching inquiries"
              description="Try a different filter or search."
            />
          ) : (
            <div className="space-y-3">
              {filtered.map((row) => (
                <Link key={row.sessionId} to={inquiryThreadHref(row)}>
                  <Card className="transition-colors hover:bg-muted/40">
                    <CardContent className="flex gap-4 py-4">
                      <div className="h-16 w-16 shrink-0 overflow-hidden rounded-md bg-muted">
                        {row.listingCoverPhotoUri ? (
                          <img
                            src={row.listingCoverPhotoUri}
                            alt=""
                            className="h-full w-full object-cover"
                          />
                        ) : (
                          <div className="flex h-full items-center justify-center text-muted-foreground">
                            <ImageOff className="h-5 w-5" />
                          </div>
                        )}
                      </div>
                      <div className="min-w-0 flex-1 space-y-1">
                        <div className="flex flex-wrap items-center gap-2">
                          <p className="truncate font-medium">
                            {row.listingTitle ?? "Listing"}
                          </p>
                          <InquiryStatusBadge status={row.status} />
                          {row.unansweredByHostCount > 0 && (
                            <Badge variant="secondary" className="gap-1 text-xs">
                              <MessageSquare className="h-3 w-3" />
                              {row.unansweredByHostCount} awaiting host
                            </Badge>
                          )}
                        </div>
                        <div className="flex flex-wrap gap-3 text-xs text-muted-foreground">
                          {row.listingCity && (
                            <span className="flex items-center gap-1">
                              <MapPin className="h-3 w-3" />
                              {row.listingCity}
                            </span>
                          )}
                          <span className="flex items-center gap-1">
                            <User className="h-3 w-3" />
                            {row.tenantDisplayName ?? "Member"}
                          </span>
                          <span className="flex items-center gap-1">
                            <Clock className="h-3 w-3" />
                            {formatDate(row.lastActivityAt)}
                          </span>
                        </div>
                      </div>
                      <Button
                        variant="ghost"
                        size="sm"
                        className="shrink-0 gap-1 self-center"
                      >
                        Open
                        <ArrowRight className="h-3.5 w-3.5" />
                      </Button>
                    </CardContent>
                  </Card>
                </Link>
              ))}
            </div>
          )}
        </>
      )}

      <Dialog open={startOpen} onOpenChange={setStartOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Start inquiry for a member</DialogTitle>
          </DialogHeader>
          <form className="space-y-4" onSubmit={onStart}>
            <div className="space-y-1.5">
              <Label htmlFor="partner-inquiry-listing">Listing ID</Label>
              <Input
                id="partner-inquiry-listing"
                value={listingId}
                onChange={(e) => setListingId(e.target.value)}
                placeholder="Listing UUID"
                required
              />
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="partner-inquiry-member">Endorsed member</Label>
              <Select
                id="partner-inquiry-member"
                value={tenantUserId}
                onChange={(e) => setTenantUserId(e.target.value)}
                required
              >
                <option value="">Select member…</option>
                {(members.data ?? []).map((m) => (
                  <option key={m.tenantUserId} value={m.tenantUserId}>
                    {m.displayName || m.email || m.tenantUserId}
                  </option>
                ))}
              </Select>
            </div>
            {startInquiry.isError && (
              <Alert variant="destructive" className="text-sm">
                {getApiErrorMessage(
                  startInquiry.error,
                  "Could not start inquiry.",
                )}
              </Alert>
            )}
            <DialogFooter>
              <Button
                type="button"
                variant="ghost"
                onClick={() => setStartOpen(false)}
              >
                Cancel
              </Button>
              <Button
                type="submit"
                disabled={
                  !listingId.trim() || !tenantUserId || startInquiry.isPending
                }
              >
                Start
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
    </div>
  );
};
