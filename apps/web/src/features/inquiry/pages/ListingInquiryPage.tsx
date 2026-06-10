import { useMemo, useState } from "react";
import { useParams, Link } from "react-router-dom";
import {
  ArrowLeft,
  ArrowRight,
  CheckCircle2,
  Clock,
  Lock,
  ShieldAlert,
  Unlock,
} from "lucide-react";
import { useAuthStore } from "@/app/auth/authStore";
import {
  useInquirySession,
  usePredefinedQuestions,
} from "@/features/inquiry/hooks/useInquiry";
import { useListingDetail } from "@/features/listings/hooks/useListings";
import { InquiryStatusBadge } from "@/features/inquiry/components/InquiryStatusBadge";
import { InquiryQuestion } from "@/features/inquiry/components/InquiryQuestion";
import { InquiryResponseForm } from "@/features/inquiry/components/InquiryResponseForm";
import { ApplyDialog } from "@/features/applications/components/ApplyDialog";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Alert } from "@/components/ui/alert";
import { Separator } from "@/components/ui/separator";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Loader } from "@/components/shared/Loader";
import { EmptyState } from "@/components/shared/EmptyState";
import { formatDate } from "@/utils/format";
import {
  getApiErrorMessage,
  isForbiddenError,
  isNotFoundError,
} from "@/api/errors";
import type { InquiryQuestionDto, ResponseType } from "@/api/types";

const categoryLabels: Record<string, string> = {
  UtilitySpecifics: "Utility Specifics",
  AccessibilityLayout: "Accessibility & Layout",
  RuleClarification: "Rule Clarification",
  Proximity: "Proximity & Location",
};

const responseTypeLabels: Record<ResponseType, string> = {
  YesNo: "Yes / No",
  MultipleChoice: "Multiple Choice",
  Numeric: "Numeric",
  OpenText: "Open response",
};

/**
 * Phase 17 — pre-booking inquiry page rendered by session id. Wraps the
 * existing structured Q&amp;A surface (without the deal sidebar) and adds
 * a "Continue to Apply" CTA that opens <code>ApplyDialog</code> for the
 * same listing. Once the tenant applies, the backend's
 * <code>SubmitApplicationCommand</code> links this session to the new
 * deal so the conversation persists.
 */
export const ListingInquiryPage = () => {
  const { sessionId } = useParams<{ sessionId: string }>();
  const user = useAuthStore((s) => s.user);

  const {
    data: inquiry,
    isLoading,
    isError,
    error,
  } = useInquirySession(sessionId);

  const { data: listing } = useListingDetail(inquiry?.listingId);
  const { data: predefinedQuestions } = usePredefinedQuestions();

  const isTenant = !!user && !!inquiry && user.userId === inquiry.tenantUserId;
  const isLandlord = !!user && !!listing && user.userId === listing.landlordUserId;

  const isOpen = inquiry?.status === "Open";
  const isClosed = inquiry?.status === "Closed";
  const is404 = isNotFoundError(error);
  const isForbidden = isForbiddenError(error);

  const [applyOpen, setApplyOpen] = useState(false);

  const getQuestionText = useMemo(
    () => (q: InquiryQuestionDto) => {
      if (q.openQuestionText) return q.openQuestionText;
      if (q.customText) return q.customText;
      if (q.predefinedQuestionId && predefinedQuestions) {
        const pq = predefinedQuestions.find((p) => p.id === q.predefinedQuestionId);
        if (pq) return pq.text;
      }
      return "Question";
    },
    [predefinedQuestions],
  );

  const getExpectedResponseType = (
    q: InquiryQuestionDto,
  ): ResponseType | undefined => {
    if (q.predefinedQuestionId && predefinedQuestions) {
      return predefinedQuestions.find((p) => p.id === q.predefinedQuestionId)
        ?.expectedResponseType;
    }
    return undefined;
  };

  if (isLoading) {
    return <Loader fullPage label="Loading inquiry..." />;
  }

  if (isError && is404) {
    return (
      <div className="mx-auto max-w-3xl px-4 py-8 sm:px-6 lg:px-8">
        <EmptyState
          title="Inquiry not found"
          description="This conversation may have been closed or no longer exists."
        >
          <Link to="/listings">
            <Button variant="outline" size="sm">
              <ArrowLeft className="mr-2 h-4 w-4" />
              Browse listings
            </Button>
          </Link>
        </EmptyState>
      </div>
    );
  }

  if (isError && isForbidden) {
    return (
      <div className="mx-auto max-w-3xl px-4 py-8 sm:px-6 lg:px-8">
        <Alert variant="destructive">
          <Lock className="h-4 w-4" />
          <span className="ml-2 text-sm">
            {getApiErrorMessage(error, "You do not have access to this inquiry.")}
          </span>
        </Alert>
      </div>
    );
  }

  if (!inquiry) {
    return null;
  }

  const backTo = listing
    ? `/listings/${listing.id}`
    : inquiry.dealId
      ? `/app/deals/${inquiry.dealId}`
      : "/listings";

  return (
    <div className="mx-auto max-w-3xl px-4 py-8 sm:px-6 lg:px-8">
      <Link
        to={backTo}
        className="inline-flex items-center gap-1.5 text-sm text-muted-foreground hover:text-foreground transition-colors mb-6"
      >
        <ArrowLeft className="h-4 w-4" />
        {listing ? "Back to listing" : "Back"}
      </Link>

      <div className="flex items-center gap-3 mb-6">
        <h1 className="text-2xl font-bold tracking-tight">
          {listing?.title ? `Inquiry — ${listing.title}` : "Inquiry"}
        </h1>
        <InquiryStatusBadge status={inquiry.status} />
      </div>

      {isClosed && (
        <Alert className="mb-6 border-blue-300 bg-blue-50 text-blue-800">
          <ShieldAlert className="h-4 w-4" />
          <span className="ml-2 text-sm">
            This inquiry is closed. All confirmed details are recorded in the
            Truth Surface.
          </span>
        </Alert>
      )}

      {/* Session meta */}
      <div className="flex flex-wrap items-center gap-4 text-sm text-muted-foreground mb-6">
        <span className="flex items-center gap-1">
          <Clock className="h-3.5 w-3.5" />
          Started: {formatDate(inquiry.createdAt)}
        </span>
        {inquiry.unlockedByLandlordAt && (
          <span className="flex items-center gap-1">
            <Unlock className="h-3.5 w-3.5" />
            Opened: {formatDate(inquiry.unlockedByLandlordAt)}
          </span>
        )}
        {inquiry.closedAt && (
          <span className="flex items-center gap-1">
            <Lock className="h-3.5 w-3.5" />
            Closed: {formatDate(inquiry.closedAt)}
          </span>
        )}
      </div>

      {/* Empty state — only shown for tenants with no questions yet. */}
      {inquiry.questions.length === 0 && isOpen && (
        <Card className="mb-6">
          <CardContent className="py-8">
            <EmptyState
              title="No questions yet"
              description={
                isTenant
                  ? "Pick a category below to ask the host a structured question, or choose 'Other' to type your own."
                  : "The tenant hasn't asked any questions yet."
              }
            />
          </CardContent>
        </Card>
      )}

      {/* Question / answer thread */}
      <div className="space-y-4 mb-6">
        {inquiry.questions.map((q) => (
          <Card key={q.questionId}>
            <CardHeader className="pb-2">
              <div className="flex items-start justify-between gap-2">
                <CardTitle className="text-sm font-medium leading-snug">
                  {getQuestionText(q)}
                </CardTitle>
                <Badge variant="secondary" className="shrink-0 text-xs">
                  {categoryLabels[q.category] ?? q.category}
                </Badge>
              </div>
              <p className="text-xs text-muted-foreground">
                Asked {formatDate(q.submittedAt)}
              </p>
            </CardHeader>
            <CardContent>
              {q.answer ? (
                <div className="rounded-md bg-muted/50 p-3">
                  <div className="flex items-center gap-2 mb-1">
                    <CheckCircle2 className="h-3.5 w-3.5 text-emerald-600" />
                    <span className="text-xs font-medium text-emerald-700">
                      Answered
                    </span>
                    <span className="text-xs text-muted-foreground">
                      {formatDate(q.answer.answeredAt)}
                    </span>
                  </div>
                  <div className="flex items-start gap-2">
                    <Badge variant="outline" className="text-xs shrink-0 mt-0.5">
                      {responseTypeLabels[q.answer.responseType]}
                    </Badge>
                    <span className="text-sm font-medium whitespace-pre-wrap">
                      {q.answer.answerValue}
                    </span>
                  </div>
                </div>
              ) : isOpen && isLandlord && sessionId ? (
                <InquiryResponseForm
                  sessionId={sessionId}
                  questionId={q.questionId}
                  expectedResponseType={getExpectedResponseType(q)}
                  isOpenQuestion={!!q.openQuestionText}
                />
              ) : (
                <p className="text-sm text-muted-foreground italic">
                  Awaiting host response
                </p>
              )}
            </CardContent>
          </Card>
        ))}
      </div>

      {/* Ask new question (tenant only, while open). */}
      {isOpen && isTenant && sessionId && (
        <>
          <Separator className="my-6" />
          <InquiryQuestion sessionId={sessionId} />
        </>
      )}

      {/* Continue to apply — tenant-only CTA, only meaningful before a deal exists. */}
      {isTenant && !inquiry.dealId && listing && (
        <>
          <Separator className="my-6" />
          <Card>
            <CardContent className="flex flex-col items-stretch gap-3 py-5 sm:flex-row sm:items-center sm:justify-between">
              <div>
                <p className="text-sm font-medium">Ready to book?</p>
                <p className="text-xs text-muted-foreground mt-0.5">
                  Your inquiry conversation will carry over to the deal.
                </p>
              </div>
              <Button
                className="gap-2"
                onClick={() => setApplyOpen(true)}
              >
                Continue to Apply
                <ArrowRight className="h-4 w-4" />
              </Button>
            </CardContent>
          </Card>
          <ApplyDialog
            listing={listing}
            controlledOpen={applyOpen}
            onOpenChange={setApplyOpen}
          />
        </>
      )}
    </div>
  );
};
