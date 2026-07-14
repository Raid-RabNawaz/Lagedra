import { useState } from "react";
import { Star } from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Textarea } from "@/components/ui/textarea";
import { Alert } from "@/components/ui/alert";
import { Loader } from "@/components/shared/Loader";
import { StarRatingInput } from "@/features/reviews/components/StarRating";
import {
  useDealReviews,
  useSubmitStayReview,
} from "@/features/reviews/hooks/useReviews";
import { formatDate } from "@/utils/format";
import { getApiErrorMessage } from "@/api/errors";
import type { StayReviewDirection, StayReviewDto } from "@/api/types";

type Props = {
  dealId: string;
};

const MIN_COMMENT = 40;
const MIN_CRITICAL_COMMENT = 80;
const MIN_WORDS = 6;

const guestCategories = [
  { key: "cleanliness", label: "Cleanliness" },
  { key: "accuracy", label: "Accuracy" },
  { key: "communication", label: "Communication" },
  { key: "location", label: "Location" },
  { key: "checkIn", label: "Check-in" },
  { key: "value", label: "Value" },
] as const;

const hostCategories = [
  { key: "cleanliness", label: "Cleanliness" },
  { key: "communication", label: "Communication" },
  { key: "respectHouseRules", label: "Respect house rules" },
] as const;

function wordCount(text: string): number {
  return text.trim().split(/\s+/).filter(Boolean).length;
}

function ReviewCard({ review, title }: { review: StayReviewDto; title: string }) {
  return (
    <div className="rounded-lg border p-3 space-y-2">
      <div className="flex items-center justify-between gap-2">
        <p className="text-sm font-medium">{title}</p>
        <span className="inline-flex items-center gap-1 text-sm">
          <Star className="h-3.5 w-3.5 fill-amber-500 text-amber-500" />
          {review.overallRating}
        </span>
      </div>
      <p className="text-sm text-muted-foreground whitespace-pre-wrap">
        {review.publicComment}
      </p>
      <p className="text-xs text-muted-foreground">
        {review.publishedAt
          ? formatDate(review.publishedAt)
          : formatDate(review.submittedAt)}
      </p>
    </div>
  );
}

export function LeaveStayReviewPanel({ dealId }: Props) {
  const { data: window, isLoading, isError, error } = useDealReviews(dealId);
  const submit = useSubmitStayReview(dealId);

  const [overall, setOverall] = useState(5);
  const [comment, setComment] = useState("");
  const [privateFeedback, setPrivateFeedback] = useState("");
  const [cats, setCats] = useState<Record<string, number>>({});
  const [clientError, setClientError] = useState<string | null>(null);

  if (isLoading) {
    return (
      <Card>
        <CardContent className="py-6">
          <Loader label="Loading reviews…" />
        </CardContent>
      </Card>
    );
  }

  if (isError || !window) {
    // Window may not exist yet (deal not fully closed) — hide quietly unless unexpected.
    const msg = getApiErrorMessage(error, "");
    if (msg.toLowerCase().includes("not found") || msg.toLowerCase().includes("not open")) {
      return null;
    }
    return null;
  }

  const direction = window.callerDirection as StayReviewDirection | null | undefined;
  const categoryDefs =
    direction === "GuestToHost"
      ? guestCategories
      : direction === "HostToGuest"
        ? hostCategories
        : [];

  const minComment = overall <= 2 ? MIN_CRITICAL_COMMENT : MIN_COMMENT;

  const onSubmit = () => {
    setClientError(null);
    const trimmed = comment.trim();
    if (trimmed.length < minComment) {
      setClientError(
        overall <= 2
          ? `Low ratings need a specific explanation of at least ${MIN_CRITICAL_COMMENT} characters — what went wrong, and how it affected the stay.`
          : `Please write at least ${MIN_COMMENT} characters about your experience.`,
      );
      return;
    }
    if (wordCount(trimmed) < MIN_WORDS) {
      setClientError(
        `Please write a thoughtful review with at least ${MIN_WORDS} words — stars alone are not enough.`,
      );
      return;
    }
    for (const c of categoryDefs) {
      if (!cats[c.key]) {
        setClientError(`Please rate ${c.label}.`);
        return;
      }
    }

    submit.mutate({
      overallRating: overall,
      publicComment: trimmed,
      privateFeedback: privateFeedback.trim() || null,
      cleanliness: cats.cleanliness ?? null,
      accuracy: cats.accuracy ?? null,
      communication: cats.communication ?? null,
      location: cats.location ?? null,
      checkIn: cats.checkIn ?? null,
      value: cats.value ?? null,
      respectHouseRules: cats.respectHouseRules ?? null,
    });
  };

  return (
    <Card>
      <CardHeader className="pb-3">
        <CardTitle className="flex items-center gap-2 text-base">
          <Star className="h-4 w-4" />
          Stay reviews
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-4">
        <p className="text-sm text-muted-foreground">
          Share a fair, experience-based review — what worked, what didn&apos;t,
          and why. Reviews stay private until both sides submit or the window
          closes on {formatDate(window.closesAt)}. Insults and all-caps rants
          are not accepted.
        </p>

        {window.isPublished && (
          <div className="space-y-3">
            {window.ownReview && (
              <ReviewCard review={window.ownReview} title="Your review" />
            )}
            {window.peerReview && (
              <ReviewCard
                review={window.peerReview}
                title={
                  window.peerReview.direction === "GuestToHost"
                    ? "Guest review of host"
                    : "Host review of guest"
                }
              />
            )}
            {!window.ownReview && !window.peerReview && (
              <p className="text-sm text-muted-foreground">
                The review window closed with no submissions.
              </p>
            )}
          </div>
        )}

        {!window.isPublished && window.ownReview && (
          <Alert className="text-sm">
            Your review is submitted and waiting for the other party
            {window.guestSubmitted && window.hostSubmitted
              ? " — publishing shortly."
              : "."}
          </Alert>
        )}

        {!window.isPublished && window.canCallerSubmit && direction && (
          <div className="space-y-3 rounded-lg border p-3">
            <StarRatingInput
              label="Overall rating"
              value={overall}
              onChange={setOverall}
            />
            {overall <= 2 && (
              <Alert className="text-sm">
                Low ratings should describe specific issues (timing, cleanliness,
                communication, etc.), not personal attacks. This helps the other
                party and future guests.
              </Alert>
            )}
            {categoryDefs.map((c) => (
              <StarRatingInput
                key={c.key}
                size="sm"
                label={c.label}
                value={cats[c.key] ?? 0}
                onChange={(v) => setCats((prev) => ({ ...prev, [c.key]: v }))}
              />
            ))}
            <div>
              <label className="mb-1 block text-sm font-medium">
                Public review{" "}
                <span className="font-normal text-muted-foreground">
                  (min {minComment} characters)
                </span>
              </label>
              <Textarea
                value={comment}
                onChange={(e) => setComment(e.target.value)}
                placeholder={
                  direction === "GuestToHost"
                    ? "Describe the stay in your own words — accuracy of the listing, how check-in went, communication, and overall value…"
                    : "Describe how the guest treated the home and communication during the stay…"
                }
              />
              <p className="mt-1 text-xs text-muted-foreground">
                {comment.trim().length}/{minComment} characters · {wordCount(comment)}{" "}
                words
              </p>
            </div>
            <div>
              <label className="mb-1 block text-sm font-medium">
                Private feedback to Lagedra{" "}
                <span className="text-muted-foreground">(optional)</span>
              </label>
              <Textarea
                value={privateFeedback}
                onChange={(e) => setPrivateFeedback(e.target.value)}
                placeholder="Only visible to the platform — use this for safety or policy concerns…"
              />
            </div>
            {(clientError || submit.isError) && (
              <Alert variant="destructive" className="text-sm">
                {clientError ?? getApiErrorMessage(submit.error, "Could not submit review.")}
              </Alert>
            )}
            <Button disabled={submit.isPending} onClick={onSubmit}>
              {submit.isPending ? "Submitting…" : "Submit review"}
            </Button>
          </div>
        )}

        {!window.isPublished &&
          !window.canCallerSubmit &&
          !window.ownReview &&
          !direction && (
            <p className="text-sm text-muted-foreground">
              Only the host and guest on this stay can leave reviews.
            </p>
          )}
      </CardContent>
    </Card>
  );
}
