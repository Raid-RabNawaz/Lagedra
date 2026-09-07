import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import {
  Check,
  X,
  RefreshCw,
  ImageOff,
  ExternalLink,
  FileSignature,
  Loader2,
  ShieldCheck,
  Bed,
  Bath,
  CalendarClock,
  IdCard,
  Phone,
  Star,
  AlertTriangle,
  MapPin,
  Search,
  Zap,
} from "lucide-react";
import { adminApi } from "@/features/admin/services/adminApi";
import type { ListingReviewItemDto, PropertyType } from "@/api/types";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { Checkbox } from "@/components/ui/checkbox";
import { cn } from "@/lib/utils";
import { MIN_HOST_PROFILE_COMPLETENESS } from "@/features/auth/lib/profileCompleteness";
import { Label } from "@/components/ui/label";
import { Input } from "@/components/ui/input";
import { Select } from "@/components/ui/select";
import { Textarea } from "@/components/ui/textarea";
import { propertyTypes } from "@/features/listings/lib/listingFormSchema";
import {
  emptyListingReviewFilters,
  filterListingReviewItems,
  listingReviewHasActiveFilters,
  listingReviewLocationLabel,
  type ListingReviewFilters,
  type ListingReviewTriState,
} from "@/features/admin/lib/listingReviewFilters";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Loader } from "@/components/shared/Loader";
import { ErrorState } from "@/components/shared/ErrorState";
import { EmptyState } from "@/components/shared/EmptyState";
import { FormError } from "@/components/shared/FormError";
import { formatMoney, formatDate } from "@/utils/format";
import { extractErrorMessage } from "@/lib/errors";

const formatRelative = (iso: string | null | undefined): string => {
  if (!iso) return "—";
  const submitted = new Date(iso).getTime();
  const diffMs = Date.now() - submitted;
  const hours = Math.round(diffMs / (1000 * 60 * 60));
  if (hours < 1) return "just now";
  if (hours < 24) return `${hours}h ago`;
  const days = Math.round(hours / 24);
  return `${days}d ago`;
};

export const ListingReviewPage = () => {
  const [items, setItems] = useState<ListingReviewItemDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<unknown>(null);
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set());
  const [approveTarget, setApproveTarget] = useState<ListingReviewItemDto | null>(null);
  const [bulkApproveOpen, setBulkApproveOpen] = useState(false);
  const [denyTarget, setDenyTarget] = useState<ListingReviewItemDto | null>(null);
  const [bulkDenyOpen, setBulkDenyOpen] = useState(false);
  const [filters, setFilters] = useState<ListingReviewFilters>(emptyListingReviewFilters());
  const [bulkTargets, setBulkTargets] = useState<ListingReviewItemDto[]>([]);

  const load = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const data = await adminApi.getPendingListingReviews();
      setItems(Array.isArray(data) ? data : []);
      setSelectedIds((prev) => {
        const next = new Set<string>();
        for (const id of prev) {
          if (data.some((item) => item.id === id)) next.add(id);
        }
        return next;
      });
    } catch (err) {
      setError(err);
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    void load();
  }, []);

  const filteredItems = useMemo(
    () => filterListingReviewItems(items, filters),
    [items, filters],
  );
  const hasActiveFilters = listingReviewHasActiveFilters(filters);
  const allSelected =
    filteredItems.length > 0 && filteredItems.every((item) => selectedIds.has(item.id));
  const someSelected = filteredItems.some((item) => selectedIds.has(item.id)) && !allSelected;
  const selectedItems = useMemo(
    () => filteredItems.filter((item) => selectedIds.has(item.id)),
    [filteredItems, selectedIds],
  );

  const toggleOne = (id: string, checked: boolean) => {
    setSelectedIds((prev) => {
      const next = new Set(prev);
      if (checked) next.add(id);
      else next.delete(id);
      return next;
    });
  };

  const toggleAll = (checked: boolean) => {
    setSelectedIds((prev) => {
      const next = new Set(prev);
      if (checked) {
        for (const item of filteredItems) next.add(item.id);
      } else {
        for (const item of filteredItems) next.delete(item.id);
      }
      return next;
    });
  };

  const updateFilter = <K extends keyof ListingReviewFilters>(
    key: K,
    value: ListingReviewFilters[K],
  ) => {
    setFilters((prev) => ({ ...prev, [key]: value }));
  };

  return (
    <div className="space-y-6">
      <div className="flex items-start justify-between gap-3">
        <div>
          <h1 className="flex items-center gap-2 text-3xl font-bold tracking-tight">
            <ShieldCheck className="h-7 w-7 text-muted-foreground" />
            Listing review
          </h1>
          <p className="mt-1 text-muted-foreground">
            Approve listings that meet quality standards or send them back to the landlord with feedback.
          </p>
        </div>
        <Button variant="outline" onClick={() => void load()} disabled={isLoading}>
          <RefreshCw className={`h-4 w-4 ${isLoading ? "animate-spin" : ""}`} />
          Refresh
        </Button>
      </div>

      {items.length > 0 && (
        <Card>
          <CardHeader className="pb-3">
            <CardTitle className="text-lg">Filters</CardTitle>
            <CardDescription>
              Narrow the queue by location, host, listing type, or review flags.
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
              <div className="space-y-1.5">
                <Label htmlFor="review-filter-location">Location</Label>
                <Input
                  id="review-filter-location"
                  placeholder="City or state"
                  value={filters.location}
                  onChange={(e) => updateFilter("location", e.target.value)}
                />
              </div>
              <div className="space-y-1.5">
                <Label htmlFor="review-filter-host">Host name</Label>
                <Input
                  id="review-filter-host"
                  placeholder="Host display name"
                  value={filters.hostName}
                  onChange={(e) => updateFilter("hostName", e.target.value)}
                />
              </div>
              <div className="space-y-1.5">
                <Label htmlFor="review-filter-type">Listing type</Label>
                <Select
                  id="review-filter-type"
                  value={filters.propertyType}
                  onChange={(e) =>
                    updateFilter("propertyType", e.target.value as PropertyType | "")
                  }
                >
                  <option value="">All types</option>
                  {propertyTypes.map((type) => (
                    <option key={type} value={type}>
                      {type}
                    </option>
                  ))}
                </Select>
              </div>
              <div className="space-y-1.5">
                <Label htmlFor="review-filter-title">Title</Label>
                <Input
                  id="review-filter-title"
                  placeholder="Listing title"
                  value={filters.title}
                  onChange={(e) => updateFilter("title", e.target.value)}
                />
              </div>
              <div className="space-y-1.5">
                <Label htmlFor="review-filter-lease">Lease</Label>
                <Select
                  id="review-filter-lease"
                  value={filters.lease}
                  onChange={(e) =>
                    updateFilter("lease", e.target.value as ListingReviewFilters["lease"])
                  }
                >
                  <option value="any">Any lease</option>
                  <option value="custom">Host-provided lease</option>
                  <option value="standard">Lagedra template</option>
                </Select>
              </div>
              <div className="space-y-1.5">
                <Label htmlFor="review-filter-instant">Instant booking</Label>
                <Select
                  id="review-filter-instant"
                  value={filters.instantBooking}
                  onChange={(e) =>
                    updateFilter("instantBooking", e.target.value as ListingReviewTriState)
                  }
                >
                  <option value="any">Any</option>
                  <option value="yes">Instant book on</option>
                  <option value="no">Request to book</option>
                </Select>
              </div>
              <div className="space-y-1.5">
                <Label htmlFor="review-filter-id">Host ID</Label>
                <Select
                  id="review-filter-id"
                  value={filters.hostIdVerified}
                  onChange={(e) =>
                    updateFilter("hostIdVerified", e.target.value as ListingReviewTriState)
                  }
                >
                  <option value="any">Any</option>
                  <option value="yes">ID verified</option>
                  <option value="no">ID not verified</option>
                </Select>
              </div>
              <div className="space-y-1.5">
                <Label htmlFor="review-filter-profile">Host profile</Label>
                <Select
                  id="review-filter-profile"
                  value={filters.hostProfile}
                  onChange={(e) =>
                    updateFilter(
                      "hostProfile",
                      e.target.value as ListingReviewFilters["hostProfile"],
                    )
                  }
                >
                  <option value="any">Any completeness</option>
                  <option value="incomplete">
                    Under {MIN_HOST_PROFILE_COMPLETENESS}% complete
                  </option>
                </Select>
              </div>
            </div>
            {hasActiveFilters && (
              <Button
                variant="outline"
                size="sm"
                onClick={() => setFilters(emptyListingReviewFilters())}
              >
                <X className="h-3.5 w-3.5" />
                Clear filters
              </Button>
            )}
          </CardContent>
        </Card>
      )}

      <Card>
        <CardHeader className="pb-3">
          <div className="flex flex-wrap items-start justify-between gap-3">
            <div>
              <CardTitle className="text-lg">Pending review</CardTitle>
              <CardDescription>
                {hasActiveFilters
                  ? `${filteredItems.length} of ${items.length} listing${items.length === 1 ? "" : "s"} match the filters`
                  : `${items.length} listing${items.length === 1 ? "" : "s"} waiting on a decision`}
              </CardDescription>
            </div>
            {filteredItems.length > 0 && (
              <div className="flex flex-wrap items-center gap-2">
                <label className="flex cursor-pointer items-center gap-2 text-sm text-muted-foreground">
                  <Checkbox
                    checked={allSelected}
                    data-state={someSelected ? "indeterminate" : undefined}
                    onCheckedChange={(checked) => toggleAll(checked)}
                    aria-label="Select all visible listings"
                  />
                  Select all
                </label>
                <Button
                  variant="accent"
                  size="sm"
                  disabled={selectedItems.length === 0}
                  onClick={() => {
                    setBulkTargets(selectedItems);
                    setBulkApproveOpen(true);
                  }}
                >
                  <Check className="h-3.5 w-3.5" />
                  Approve selected ({selectedItems.length})
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  disabled={selectedItems.length === 0}
                  onClick={() => {
                    setBulkTargets(selectedItems);
                    setBulkDenyOpen(true);
                  }}
                  className="border-destructive/40 text-destructive hover:bg-destructive/10 hover:text-destructive"
                >
                  <X className="h-3.5 w-3.5" />
                  Deny selected ({selectedItems.length})
                </Button>
              </div>
            )}
          </div>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <Loader label="Loading review queue..." />
          ) : error ? (
            <ErrorState error={error} onRetry={() => void load()} />
          ) : items.length === 0 ? (
            <EmptyState
              title="Nothing to review"
              description="All caught up! New submissions will appear here."
            />
          ) : filteredItems.length === 0 ? (
            <EmptyState
              title="No listings match these filters"
              description="Clear or adjust the filters to see more of the review queue."
            >
              <Button variant="outline" onClick={() => setFilters(emptyListingReviewFilters())}>
                <Search className="h-4 w-4" />
                Clear filters
              </Button>
            </EmptyState>
          ) : (
            <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
              {filteredItems.map((item) => (
                <ReviewCard
                  key={item.id}
                  item={item}
                  selected={selectedIds.has(item.id)}
                  onSelectedChange={(checked) => toggleOne(item.id, checked)}
                  onApprove={() => setApproveTarget(item)}
                  onDeny={() => setDenyTarget(item)}
                />
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      {approveTarget && (
        <ApproveDialog
          item={approveTarget}
          onClose={() => setApproveTarget(null)}
          onSuccess={() => {
            setApproveTarget(null);
            void load();
          }}
        />
      )}

      {bulkApproveOpen && (
        <BulkApproveDialog
          items={bulkTargets}
          onClose={() => setBulkApproveOpen(false)}
          onSuccess={() => {
            setBulkApproveOpen(false);
            setSelectedIds(new Set());
            setBulkTargets([]);
            void load();
          }}
        />
      )}

      {denyTarget && (
        <DenyDialog
          item={denyTarget}
          onClose={() => setDenyTarget(null)}
          onSuccess={() => {
            setDenyTarget(null);
            void load();
          }}
        />
      )}

      {bulkDenyOpen && (
        <BulkDenyDialog
          items={bulkTargets}
          onClose={() => setBulkDenyOpen(false)}
          onSuccess={() => {
            setBulkDenyOpen(false);
            setSelectedIds(new Set());
            setBulkTargets([]);
            void load();
          }}
        />
      )}
    </div>
  );
};

function ReviewCard({
  item,
  selected,
  onSelectedChange,
  onApprove,
  onDeny,
}: {
  item: ListingReviewItemDto;
  selected: boolean;
  onSelectedChange: (checked: boolean) => void;
  onApprove: () => void;
  onDeny: () => void;
}) {
  const location = listingReviewLocationLabel(item);

  return (
    <Card className={cn("overflow-hidden", selected && "ring-2 ring-primary/40")}>
      <div className="relative">
        <Link
          to={`/listings/${item.id}`}
          target="_blank"
          rel="noopener noreferrer"
          className="block aspect-[16/10] overflow-hidden bg-muted"
        >
          {item.coverPhotoUrl ? (
            <img
              src={item.coverPhotoUrl}
              alt={item.title}
              className="h-full w-full object-cover transition-transform hover:scale-[1.02]"
              loading="lazy"
            />
          ) : (
            <div className="flex h-full w-full items-center justify-center">
              <ImageOff className="h-10 w-10 text-muted-foreground/40" />
            </div>
          )}
        </Link>
        <div
          className="absolute left-3 top-3 z-10"
          onClick={(e) => e.stopPropagation()}
          onPointerDown={(e) => e.stopPropagation()}
        >
          <Checkbox
            checked={selected}
            onCheckedChange={onSelectedChange}
            aria-label={`Select ${item.title}`}
            className="h-5 w-5 rounded-md border-2 border-background bg-background/90 shadow-sm backdrop-blur"
          />
        </div>
        <div className="absolute right-3 top-3">
          <Badge variant="secondary" className="bg-background/90 backdrop-blur">
            {item.photoCount} photo{item.photoCount === 1 ? "" : "s"}
          </Badge>
        </div>
      </div>

      <CardContent className="space-y-3 p-4">
        <div>
          <Link
            to={`/listings/${item.id}`}
            target="_blank"
            rel="noopener noreferrer"
            className="inline-flex items-center gap-1 font-semibold leading-tight hover:underline"
          >
            <span className="line-clamp-1">{item.title}</span>
            <ExternalLink className="h-3 w-3 shrink-0 text-muted-foreground" />
          </Link>
          {location && (
            <p className="mt-1 flex items-center gap-1 text-xs text-muted-foreground">
              <MapPin className="h-3 w-3 shrink-0" />
              <span className="truncate">{location}</span>
            </p>
          )}
          <div className="mt-1 flex flex-wrap items-center gap-x-3 gap-y-1 text-xs text-muted-foreground">
            <span className="font-medium text-foreground">
              {formatMoney(item.monthlyRentCents)}
              <span className="text-muted-foreground font-normal"> / mo</span>
            </span>
            <span className="flex items-center gap-1">
              <Bed className="h-3 w-3" />
              {item.bedrooms === 0 ? "Studio" : `${item.bedrooms} bd`}
            </span>
            <span className="flex items-center gap-1">
              <Bath className="h-3 w-3" />
              {item.bathrooms} ba
            </span>
            <Badge variant="outline" className="font-normal">
              {item.propertyType}
            </Badge>
            {item.instantBookingEnabled && (
              <Badge variant="secondary" className="gap-1 font-normal">
                <Zap className="h-3 w-3" />
                Instant book
              </Badge>
            )}
          </div>
          <p className="mt-2 flex items-center gap-1 text-xs text-muted-foreground">
            <CalendarClock className="h-3 w-3" />
            Submitted {formatRelative(item.submittedForReviewAt)}
            <span className="text-muted-foreground/60">
              · created {formatDate(item.createdAt)}
            </span>
          </p>
        </div>

        {item.usesCustomLeaseAgreement && (
          <div className="flex items-start gap-2 rounded-md border border-amber-500/40 bg-amber-500/10 p-3 text-xs text-amber-800">
            <FileSignature className="mt-0.5 h-4 w-4 shrink-0" />
            <span>
              This host supplied their own lease agreement
              {item.customLeaseFileName ? ` (${item.customLeaseFileName})` : ""}.
              Read it on the listing page before approving — Lagedra&apos;s
              standard lease will not apply to bookings here.
            </span>
          </div>
        )}

        <HostSummary item={item} />

        <div className="flex items-center gap-2 pt-1">
          <Button
            variant="accent"
            size="sm"
            onClick={onApprove}
            className="flex-1"
          >
            <Check className="h-3.5 w-3.5" />
            Approve
          </Button>
          <Button
            variant="outline"
            size="sm"
            onClick={onDeny}
            className="flex-1 border-destructive/40 text-destructive hover:bg-destructive/10 hover:text-destructive"
          >
            <X className="h-3.5 w-3.5" />
            Deny
          </Button>
        </div>
      </CardContent>
    </Card>
  );
}

function HostSummary({ item }: { item: ListingReviewItemDto }) {
  const name = item.hostDisplayName?.trim() || "Unnamed host";
  const initials =
    name === "Unnamed host"
      ? "?"
      : name
          .split(" ")
          .slice(0, 2)
          .map((w) => w[0]?.toUpperCase())
          .join("");
  const memberYear = item.hostMemberSince
    ? new Date(item.hostMemberSince).getFullYear()
    : null;
  const completeness = item.hostProfileCompletenessPercent;
  const profileReady = completeness >= MIN_HOST_PROFILE_COMPLETENESS;

  return (
    <div className="rounded-md border bg-muted/30 p-3">
      <div className="flex items-center gap-2">
        <Avatar className="h-9 w-9">
          {item.hostProfilePhotoUrl ? (
            <AvatarImage src={item.hostProfilePhotoUrl} alt={name} />
          ) : null}
          <AvatarFallback className="text-xs">{initials}</AvatarFallback>
        </Avatar>
        <div className="min-w-0 flex-1">
          <p className="truncate text-sm font-medium leading-tight">{name}</p>
          <p className="text-[11px] text-muted-foreground">
            Host{memberYear ? ` · Member since ${memberYear}` : ""}
          </p>
        </div>
        <Badge
          variant={profileReady ? "secondary" : "outline"}
          className={cn(
            "shrink-0 gap-1 text-[10px]",
            profileReady
              ? "border-emerald-600/30 bg-emerald-600/10 text-emerald-700"
              : "border-amber-500/40 bg-amber-500/10 text-amber-700",
          )}
          title="How complete the host's public profile is"
        >
          {profileReady ? (
            <Check className="h-3 w-3" />
          ) : (
            <AlertTriangle className="h-3 w-3" />
          )}
          Profile {completeness}%
        </Badge>
      </div>

      <div className="mt-2 flex flex-wrap items-center gap-x-3 gap-y-1 text-[11px] text-muted-foreground">
        <span
          className={cn(
            "flex items-center gap-1",
            item.hostIsGovernmentIdVerified && "text-emerald-700",
          )}
        >
          <IdCard className="h-3 w-3" />
          {item.hostIsGovernmentIdVerified ? "ID verified" : "ID not verified"}
        </span>
        <span
          className={cn(
            "flex items-center gap-1",
            item.hostIsPhoneVerified && "text-emerald-700",
          )}
        >
          <Phone className="h-3 w-3" />
          {item.hostIsPhoneVerified ? "Phone verified" : "Phone not verified"}
        </span>
        {item.hostResponseRatePercent != null && (
          <span className="flex items-center gap-1">
            <Star className="h-3 w-3" />
            {item.hostResponseRatePercent}% response
          </span>
        )}
      </div>

      <p
        className="mt-2 font-mono text-[10px] text-muted-foreground/70"
        title={item.landlordUserId}
      >
        {item.landlordUserId.slice(0, 8)}…
      </p>
    </div>
  );
}

function ApproveDialog({
  item,
  onClose,
  onSuccess,
}: {
  item: ListingReviewItemDto;
  onClose: () => void;
  onSuccess: () => void;
}) {
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const handleApprove = async () => {
    setSubmitting(true);
    setError(null);
    try {
      await adminApi.approveListing(item.id);
      onSuccess();
    } catch (err) {
      setError(extractErrorMessage(err));
      setSubmitting(false);
    }
  };

  return (
    <Dialog open onOpenChange={(next) => !next && onClose()}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Approve &ldquo;{item.title}&rdquo;</DialogTitle>
          <DialogDescription>
            This publishes the listing to the marketplace, where guests can find
            it and request to book. You can take it down later if needed.
          </DialogDescription>
        </DialogHeader>
        {item.hostProfileCompletenessPercent < MIN_HOST_PROFILE_COMPLETENESS && (
          <div className="flex items-start gap-2 rounded-md border border-amber-500/40 bg-amber-500/10 p-3 text-xs text-amber-800">
            <AlertTriangle className="mt-0.5 h-4 w-4 shrink-0" />
            <span>
              This host&apos;s profile is only {item.hostProfileCompletenessPercent}% complete.
              Guests may not be able to tell who they&apos;re renting from. Consider
              sending it back so the host can fill in their profile first.
            </span>
          </div>
        )}
        {error && <FormError message={error} />}
        <DialogFooter>
          <Button type="button" variant="outline" onClick={onClose} disabled={submitting}>
            Cancel
          </Button>
          <Button
            type="button"
            variant="accent"
            onClick={() => void handleApprove()}
            disabled={submitting}
          >
            {submitting ? (
              <Loader2 className="h-4 w-4 animate-spin" />
            ) : (
              <Check className="h-4 w-4" />
            )}
            Approve &amp; publish
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

function BulkApproveDialog({
  items,
  onClose,
  onSuccess,
}: {
  items: ListingReviewItemDto[];
  onClose: () => void;
  onSuccess: () => void;
}) {
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const incompleteHosts = items.filter(
    (item) => item.hostProfileCompletenessPercent < MIN_HOST_PROFILE_COMPLETENESS,
  ).length;

  const handleApprove = async () => {
    if (items.length === 0) return;
    setSubmitting(true);
    setError(null);
    try {
      const result = await adminApi.approveListingsBulk(items.map((item) => item.id));
      if (result.failures.length > 0 && result.approved === 0) {
        setError(
          result.failures
            .slice(0, 3)
            .map((f) => f.detail)
            .join(" · "),
        );
        setSubmitting(false);
        return;
      }
      if (result.failures.length > 0) {
        setError(
          `Approved ${result.approved} of ${result.requested}. ${result.failures.length} failed — refresh and retry those.`,
        );
        // Still refresh the queue so successes disappear.
        onSuccess();
        return;
      }
      onSuccess();
    } catch (err) {
      setError(extractErrorMessage(err));
      setSubmitting(false);
    }
  };

  return (
    <Dialog open onOpenChange={(next) => !next && onClose()}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>
            Approve {items.length} listing{items.length === 1 ? "" : "s"}
          </DialogTitle>
          <DialogDescription>
            This publishes the selected listings to the marketplace. Guests will
            be able to find them and request to book.
          </DialogDescription>
        </DialogHeader>

        <ul className="max-h-40 space-y-1 overflow-y-auto rounded-md border bg-muted/30 p-3 text-sm">
          {items.map((item) => (
            <li key={item.id} className="truncate">
              {item.title}
            </li>
          ))}
        </ul>

        {incompleteHosts > 0 && (
          <div className="flex items-start gap-2 rounded-md border border-amber-500/40 bg-amber-500/10 p-3 text-xs text-amber-800">
            <AlertTriangle className="mt-0.5 h-4 w-4 shrink-0" />
            <span>
              {incompleteHosts} of these host{incompleteHosts === 1 ? " has" : "s have"} a profile
              under {MIN_HOST_PROFILE_COMPLETENESS}% complete. Guests may not know who they&apos;re
              renting from.
            </span>
          </div>
        )}

        {error && <FormError message={error} />}

        <DialogFooter>
          <Button type="button" variant="outline" onClick={onClose} disabled={submitting}>
            Cancel
          </Button>
          <Button
            type="button"
            variant="accent"
            onClick={() => void handleApprove()}
            disabled={submitting || items.length === 0}
          >
            {submitting ? (
              <Loader2 className="h-4 w-4 animate-spin" />
            ) : (
              <Check className="h-4 w-4" />
            )}
            Approve &amp; publish {items.length}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

function DenyDialog({
  item,
  onClose,
  onSuccess,
}: {
  item: ListingReviewItemDto;
  onClose: () => void;
  onSuccess: () => void;
}) {
  const [reason, setReason] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!reason.trim()) {
      setError("Please describe what the landlord needs to fix.");
      return;
    }
    setSubmitting(true);
    setError(null);
    try {
      await adminApi.denyListing(item.id, reason.trim());
      onSuccess();
    } catch (err) {
      setError(extractErrorMessage(err));
      setSubmitting(false);
    }
  };

  return (
    <Dialog open onOpenChange={(next) => !next && onClose()}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Deny &ldquo;{item.title}&rdquo;</DialogTitle>
          <DialogDescription>
            The landlord will see this reason in their dashboard so they know what to fix.
            They can update the listing and resubmit, or delete it.
          </DialogDescription>
        </DialogHeader>
        <form onSubmit={(e) => void handleSubmit(e)} className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="deny-reason">Reason</Label>
            <Textarea
              id="deny-reason"
              value={reason}
              onChange={(e) => setReason(e.target.value)}
              rows={4}
              placeholder="e.g. The cover photo is blurry. Please add at least 3 well-lit photos and clarify the pet policy in the description."
              required
              maxLength={2000}
            />
            <p className="text-xs text-muted-foreground">{reason.length} / 2000</p>
          </div>
          {error && <FormError message={error} />}
          <DialogFooter>
            <Button type="button" variant="outline" onClick={onClose}>
              Cancel
            </Button>
            <Button type="submit" variant="destructive" disabled={submitting}>
              {submitting && <Loader2 className="h-4 w-4 animate-spin" />}
              Send back to landlord
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

function BulkDenyDialog({
  items,
  onClose,
  onSuccess,
}: {
  items: ListingReviewItemDto[];
  onClose: () => void;
  onSuccess: () => void;
}) {
  const [reason, setReason] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (items.length === 0) return;
    if (!reason.trim()) {
      setError("Please describe what the landlords need to fix.");
      return;
    }
    setSubmitting(true);
    setError(null);
    try {
      const result = await adminApi.denyListingsBulk(
        items.map((item) => item.id),
        reason.trim(),
      );
      if (result.failures.length > 0 && result.denied === 0) {
        setError(
          result.failures
            .slice(0, 3)
            .map((f) => f.detail)
            .join(" · "),
        );
        setSubmitting(false);
        return;
      }
      if (result.failures.length > 0) {
        setError(
          `Denied ${result.denied} of ${result.requested}. ${result.failures.length} failed — refresh and retry those.`,
        );
        onSuccess();
        return;
      }
      onSuccess();
    } catch (err) {
      setError(extractErrorMessage(err));
      setSubmitting(false);
    }
  };

  return (
    <Dialog open onOpenChange={(next) => !next && onClose()}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>
            Deny {items.length} listing{items.length === 1 ? "" : "s"}
          </DialogTitle>
          <DialogDescription>
            This reason is sent to every selected landlord so they know what to
            fix before resubmitting.
          </DialogDescription>
        </DialogHeader>
        <form onSubmit={(e) => void handleSubmit(e)} className="space-y-4">
          <ul className="max-h-40 space-y-1 overflow-y-auto rounded-md border bg-muted/30 p-3 text-sm">
            {items.map((item) => (
              <li key={item.id} className="truncate">
                {item.title}
              </li>
            ))}
          </ul>
          <div className="space-y-2">
            <Label htmlFor="bulk-deny-reason">Reason</Label>
            <Textarea
              id="bulk-deny-reason"
              value={reason}
              onChange={(e) => setReason(e.target.value)}
              rows={4}
              placeholder="e.g. Please add clearer photos and a complete house-rules section before resubmitting."
              required
              maxLength={2000}
            />
            <p className="text-xs text-muted-foreground">{reason.length} / 2000</p>
          </div>
          {error && <FormError message={error} />}
          <DialogFooter>
            <Button type="button" variant="outline" onClick={onClose} disabled={submitting}>
              Cancel
            </Button>
            <Button type="submit" variant="destructive" disabled={submitting || items.length === 0}>
              {submitting && <Loader2 className="h-4 w-4 animate-spin" />}
              Send back {items.length} listing{items.length === 1 ? "" : "s"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
