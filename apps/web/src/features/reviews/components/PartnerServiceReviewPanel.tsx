import { useState } from "react";
import { Star } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Textarea } from "@/components/ui/textarea";
import { Alert } from "@/components/ui/alert";
import { StarRatingDisplay, StarRatingInput } from "@/features/reviews/components/StarRating";
import {
  usePartnerReputation,
  usePartnerReviews,
  useSubmitPartnerReview,
} from "@/features/reviews/hooks/useReviews";
import { getApiErrorMessage } from "@/api/errors";
import { formatDate } from "@/utils/format";

type Props = {
  organizationId: string;
  organizationName: string;
};

const MIN_COMMENT = 40;
const MIN_CRITICAL_COMMENT = 80;
const MIN_WORDS = 6;

function wordCount(text: string): number {
  return text.trim().split(/\s+/).filter(Boolean).length;
}

export function PartnerServiceReviewPanel({
  organizationId,
  organizationName,
}: Props) {
  const { data: reputation } = usePartnerReputation(organizationId);
  const { data: reviews } = usePartnerReviews(organizationId);
  const submit = useSubmitPartnerReview(organizationId);
  const [open, setOpen] = useState(false);
  const [overall, setOverall] = useState(5);
  const [responsiveness, setResponsiveness] = useState(5);
  const [reliability, setReliability] = useState(5);
  const [supportQuality, setSupportQuality] = useState(5);
  const [comment, setComment] = useState("");
  const [clientError, setClientError] = useState<string | null>(null);

  if (!reputation) return null;

  const minComment = overall <= 2 ? MIN_CRITICAL_COMMENT : MIN_COMMENT;

  const onSubmit = () => {
    setClientError(null);
    const trimmed = comment.trim();
    if (trimmed.length < minComment) {
      setClientError(
        overall <= 2
          ? `Low ratings need a specific explanation of at least ${MIN_CRITICAL_COMMENT} characters.`
          : `Please write at least ${MIN_COMMENT} characters.`,
      );
      return;
    }
    if (wordCount(trimmed) < MIN_WORDS) {
      setClientError(
        `Please write a thoughtful review with at least ${MIN_WORDS} words.`,
      );
      return;
    }
    submit.mutate(
      {
        overallRating: overall,
        responsiveness,
        reliability,
        supportQuality,
        publicComment: trimmed,
      },
      { onSuccess: () => setOpen(false) },
    );
  };

  return (
    <div className="mt-2 space-y-2 border-t pt-2">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <StarRatingDisplay
          average={reputation.averageOverall}
          count={reputation.reviewCount}
        />
        {reputation.callerCanReview && !open && (
          <Button size="sm" variant="outline" onClick={() => setOpen(true)}>
            <Star className="mr-1 h-3.5 w-3.5" />
            Rate {organizationName}
          </Button>
        )}
        {reputation.callerAlreadyReviewed && (
          <span className="text-xs text-muted-foreground">You rated this partner</span>
        )}
      </div>

      {reviews && reviews.length > 0 && (
        <div className="space-y-2">
          {reviews.slice(0, 3).map((r) => (
            <div key={r.id} className="rounded-md bg-muted/40 px-2.5 py-2 space-y-1">
              <div className="flex items-center justify-between gap-2 text-xs">
                <span className="inline-flex items-center gap-1 font-medium">
                  <Star className="h-3 w-3 fill-amber-500 text-amber-500" />
                  {r.overallRating}
                </span>
                <span className="text-muted-foreground">{formatDate(r.submittedAt)}</span>
              </div>
              <p className="text-xs text-muted-foreground leading-relaxed line-clamp-3">
                {r.publicComment}
              </p>
            </div>
          ))}
        </div>
      )}

      {open && (
        <div className="space-y-3 rounded-md border p-3">
          <p className="text-xs text-muted-foreground">
            Rate the partner based on your working experience — be specific and
            fair. Insults and all-caps rants are not accepted.
          </p>
          <StarRatingInput label="Overall" value={overall} onChange={setOverall} />
          {overall <= 2 && (
            <Alert className="text-sm">
              Low ratings should describe concrete service issues, not personal attacks.
            </Alert>
          )}
          <StarRatingInput
            size="sm"
            label="Responsiveness"
            value={responsiveness}
            onChange={setResponsiveness}
          />
          <StarRatingInput
            size="sm"
            label="Reliability"
            value={reliability}
            onChange={setReliability}
          />
          <StarRatingInput
            size="sm"
            label="Support quality"
            value={supportQuality}
            onChange={setSupportQuality}
          />
          <Textarea
            value={comment}
            onChange={(e) => setComment(e.target.value)}
            placeholder="How responsive and reliable was this partner for your members?"
          />
          <p className="text-xs text-muted-foreground">
            {comment.trim().length}/{minComment} characters
          </p>
          {(clientError || submit.isError) && (
            <Alert variant="destructive" className="text-sm">
              {clientError ??
                getApiErrorMessage(submit.error, "Could not submit review.")}
            </Alert>
          )}
          <div className="flex gap-2">
            <Button size="sm" disabled={submit.isPending} onClick={onSubmit}>
              {submit.isPending ? "Submitting…" : "Submit"}
            </Button>
            <Button
              size="sm"
              variant="ghost"
              disabled={submit.isPending}
              onClick={() => setOpen(false)}
            >
              Cancel
            </Button>
          </div>
        </div>
      )}
    </div>
  );
}
