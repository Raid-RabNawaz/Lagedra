import { Star } from "lucide-react";
import { StarRatingDisplay } from "@/features/reviews/components/StarRating";
import { formatDate } from "@/utils/format";
import type { StayReviewDto, UserReputationDto } from "@/api/types";

const CATEGORY_LABELS: Record<string, string> = {
  cleanliness: "Cleanliness",
  accuracy: "Accuracy",
  communication: "Communication",
  location: "Location",
  checkIn: "Check-in",
  value: "Value",
  respectHouseRules: "House rules",
};

export function categoryLabel(key: string): string {
  return CATEGORY_LABELS[key] ?? key;
}

export function ReputationCategoryBars({
  reputation,
}: {
  reputation: UserReputationDto;
}) {
  const entries = Object.entries(reputation.categoryAverages ?? {});
  if (entries.length === 0 || reputation.reviewCount === 0) return null;

  return (
    <div className="grid gap-2 sm:grid-cols-2">
      {entries.map(([key, avg]) => (
        <div key={key} className="space-y-1">
          <div className="flex items-center justify-between text-xs">
            <span className="text-muted-foreground">{categoryLabel(key)}</span>
            <span className="font-medium tabular-nums">{avg.toFixed(1)}</span>
          </div>
          <div className="h-1.5 overflow-hidden rounded-full bg-muted">
            <div
              className="h-full rounded-full bg-amber-500"
              style={{ width: `${Math.min(100, (avg / 5) * 100)}%` }}
            />
          </div>
        </div>
      ))}
    </div>
  );
}

function reviewCategoryChips(review: StayReviewDto): { label: string; value: number }[] {
  const pairs: [string, number | null | undefined][] = [
    ["Cleanliness", review.cleanliness],
    ["Accuracy", review.accuracy],
    ["Communication", review.communication],
    ["Location", review.location],
    ["Check-in", review.checkIn],
    ["Value", review.value],
    ["House rules", review.respectHouseRules],
  ];
  return pairs
    .filter(([, v]) => v != null && v > 0)
    .map(([label, value]) => ({ label, value: value! }));
}

export function ReviewListItem({
  review,
  showCategories = false,
}: {
  review: StayReviewDto;
  showCategories?: boolean;
}) {
  const chips = showCategories ? reviewCategoryChips(review) : [];
  return (
    <div className="rounded-lg border p-3 space-y-2">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <span className="inline-flex items-center gap-1 text-sm font-medium">
          <Star className="h-3.5 w-3.5 fill-amber-500 text-amber-500" />
          {review.overallRating}
        </span>
        {(review.publishedAt || review.submittedAt) && (
          <span className="text-xs text-muted-foreground">
            {formatDate(review.publishedAt ?? review.submittedAt)}
          </span>
        )}
      </div>
      <p className="text-sm text-muted-foreground whitespace-pre-wrap leading-relaxed">
        {review.publicComment}
      </p>
      {chips.length > 0 && (
        <div className="flex flex-wrap gap-1.5">
          {chips.map((c) => (
            <span
              key={c.label}
              className="rounded-md bg-muted px-1.5 py-0.5 text-[10px] text-muted-foreground"
            >
              {c.label} {c.value}
            </span>
          ))}
        </div>
      )}
    </div>
  );
}

export function ReputationPreview({
  reputation,
  reviews,
  maxReviews = 3,
  emptyLabel = "No reviews yet",
  showCategories = false,
}: {
  reputation?: UserReputationDto | null;
  reviews?: StayReviewDto[] | null;
  maxReviews?: number;
  emptyLabel?: string;
  showCategories?: boolean;
}) {
  const count = reputation?.reviewCount ?? reviews?.length ?? 0;
  const average =
    reputation?.averageOverall ??
    (reviews && reviews.length > 0
      ? reviews.reduce((s, r) => s + r.overallRating, 0) / reviews.length
      : 0);

  if (count === 0) {
    return <p className="text-sm text-muted-foreground">{emptyLabel}</p>;
  }

  return (
    <div className="space-y-3">
      <div className="flex flex-wrap items-center gap-3">
        <StarRatingDisplay average={average} count={count} />
      </div>
      {reputation && showCategories && (
        <ReputationCategoryBars reputation={reputation} />
      )}
      {reviews && reviews.length > 0 && (
        <div className="space-y-2">
          {reviews.slice(0, maxReviews).map((r) => (
            <ReviewListItem
              key={r.id}
              review={r}
              showCategories={showCategories}
            />
          ))}
        </div>
      )}
    </div>
  );
}
