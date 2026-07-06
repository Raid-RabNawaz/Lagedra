import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import {
  Check,
  X,
  RefreshCw,
  ImageOff,
  ExternalLink,
  Loader2,
  ShieldCheck,
  Bed,
  Bath,
  CalendarClock,
  IdCard,
  Phone,
  Star,
  AlertTriangle,
} from "lucide-react";
import { adminApi } from "@/features/admin/services/adminApi";
import type { ListingReviewItemDto } from "@/api/types";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { cn } from "@/lib/utils";
import { MIN_HOST_PROFILE_COMPLETENESS } from "@/features/auth/lib/profileCompleteness";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
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
  const [approveTarget, setApproveTarget] = useState<ListingReviewItemDto | null>(null);
  const [denyTarget, setDenyTarget] = useState<ListingReviewItemDto | null>(null);

  const load = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const data = await adminApi.getPendingListingReviews();
      setItems(data);
    } catch (err) {
      setError(err);
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    void load();
  }, []);

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

      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="text-lg">Pending review</CardTitle>
          <CardDescription>
            {items.length} listing{items.length === 1 ? "" : "s"} waiting on a decision
          </CardDescription>
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
          ) : (
            <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
              {items.map((item) => (
                <ReviewCard
                  key={item.id}
                  item={item}
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
    </div>
  );
};

function ReviewCard({
  item,
  onApprove,
  onDeny,
}: {
  item: ListingReviewItemDto;
  onApprove: () => void;
  onDeny: () => void;
}) {
  return (
    <Card className="overflow-hidden">
      <Link
        to={`/listings/${item.id}`}
        target="_blank"
        rel="noopener noreferrer"
        className="block aspect-[16/10] overflow-hidden bg-muted relative"
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
        <div className="absolute right-3 top-3">
          <Badge variant="secondary" className="bg-background/90 backdrop-blur">
            {item.photoCount} photo{item.photoCount === 1 ? "" : "s"}
          </Badge>
        </div>
      </Link>

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
          </div>
          <p className="mt-2 flex items-center gap-1 text-xs text-muted-foreground">
            <CalendarClock className="h-3 w-3" />
            Submitted {formatRelative(item.submittedForReviewAt)}
            <span className="text-muted-foreground/60">
              · created {formatDate(item.createdAt)}
            </span>
          </p>
        </div>

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
