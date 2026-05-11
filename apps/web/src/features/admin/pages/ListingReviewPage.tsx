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
} from "lucide-react";
import { adminApi } from "@/features/admin/services/adminApi";
import type { ListingReviewItemDto } from "@/api/types";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
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
import { Alert, AlertDescription } from "@/components/ui/alert";
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
  const [actionError, setActionError] = useState<string | null>(null);
  const [approveInFlight, setApproveInFlight] = useState<string | null>(null);
  const [denyTarget, setDenyTarget] = useState<ListingReviewItemDto | null>(null);

  const load = async () => {
    setIsLoading(true);
    setError(null);
    setActionError(null);
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

  const handleApprove = async (item: ListingReviewItemDto) => {
    if (!window.confirm(`Approve "${item.title}" and publish it to the marketplace?`)) return;
    setApproveInFlight(item.id);
    setActionError(null);
    try {
      await adminApi.approveListing(item.id);
      await load();
    } catch (err) {
      setActionError(extractErrorMessage(err));
    } finally {
      setApproveInFlight(null);
    }
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

      {actionError && (
        <Alert variant="destructive">
          <AlertDescription>{actionError}</AlertDescription>
        </Alert>
      )}

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
                  isApproving={approveInFlight === item.id}
                  onApprove={() => void handleApprove(item)}
                  onDeny={() => setDenyTarget(item)}
                />
              ))}
            </div>
          )}
        </CardContent>
      </Card>

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
  isApproving,
  onApprove,
  onDeny,
}: {
  item: ListingReviewItemDto;
  isApproving: boolean;
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
          <p className="mt-1 font-mono text-[10px] text-muted-foreground" title={item.landlordUserId}>
            Landlord {item.landlordUserId.slice(0, 8)}…
          </p>
        </div>

        <div className="flex items-center gap-2 pt-1">
          <Button
            variant="accent"
            size="sm"
            onClick={onApprove}
            disabled={isApproving}
            className="flex-1"
          >
            {isApproving ? <Loader2 className="h-3.5 w-3.5 animate-spin" /> : <Check className="h-3.5 w-3.5" />}
            Approve
          </Button>
          <Button
            variant="outline"
            size="sm"
            onClick={onDeny}
            disabled={isApproving}
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
