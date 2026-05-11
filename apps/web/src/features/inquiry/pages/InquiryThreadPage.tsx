import { useParams, Link } from "react-router-dom";
import {
  ArrowLeft,
  Lock,
  Unlock,
  CheckCircle2,
  Clock,
  ShieldAlert,
} from "lucide-react";
import { useAuthStore } from "@/app/auth/authStore";
import {
  useInquiryThread,
  usePredefinedQuestions,
  useRequestUnlock,
  useApproveUnlock,
} from "@/features/inquiry/hooks/useInquiry";
import { useMyDeals } from "@/features/deals/hooks/useDeals";
import { InquiryStatusBadge } from "@/features/inquiry/components/InquiryStatusBadge";
import { InquiryQuestion } from "@/features/inquiry/components/InquiryQuestion";
import { InquiryResponseForm } from "@/features/inquiry/components/InquiryResponseForm";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Alert } from "@/components/ui/alert";
import { Separator } from "@/components/ui/separator";
import { Badge } from "@/components/ui/badge";
import { Loader } from "@/components/shared/Loader";
import { EmptyState } from "@/components/shared/EmptyState";
import { formatDate } from "@/utils/format";
import type { InquiryQuestionDto, ResponseType } from "@/api/types";
import {
  getApiErrorMessage,
  isForbiddenError,
  isNotFoundError,
} from "@/api/errors";

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
};

export const InquiryThreadPage = () => {
  const { dealId } = useParams<{ dealId: string }>();
  const user = useAuthStore((s) => s.user);
  const { data: deals } = useMyDeals("all");
  const deal = deals?.find((d) => d.dealId === dealId);
  const isLandlord = !!user && !!deal && user.userId === deal.landlordUserId;
  const isTenant = !!user && !!deal && user.userId === deal.tenantUserId;

  const {
    data: inquiry,
    isLoading,
    isError,
    error,
  } = useInquiryThread(dealId);

  const { data: predefinedQuestions } = usePredefinedQuestions();
  const requestUnlock = useRequestUnlock();
  const approveUnlock = useApproveUnlock();

  const is404 = isNotFoundError(error);
  const isForbidden = isForbiddenError(error);
  const noSession = isError && is404;
  const isClosed = inquiry?.status === "Closed";
  const isOpen = inquiry?.status === "Open";
  const isLocked = inquiry?.status === "Locked";

  const getQuestionText = (q: InquiryQuestionDto): string => {
    if (q.customText) {
      return q.customText;
    }
    if (q.predefinedQuestionId && predefinedQuestions) {
      const pq = predefinedQuestions.find((p) => p.id === q.predefinedQuestionId);
      if (pq) {
        return pq.text;
      }
    }
    return "Question";
  };

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

  return (
    <div className="mx-auto max-w-3xl px-4 py-8 sm:px-6 lg:px-8">
      <Link
        to={dealId ? `/app/deals/${dealId}` : "/app/deals"}
        className="inline-flex items-center gap-1.5 text-sm text-muted-foreground hover:text-foreground transition-colors mb-6"
      >
        <ArrowLeft className="h-4 w-4" />
        Back to deal
      </Link>

      <div className="flex items-center gap-3 mb-6">
        <h1 className="text-2xl font-bold tracking-tight">Structured Inquiry</h1>
        {inquiry && <InquiryStatusBadge status={inquiry.status} />}
      </div>

      {/* Closed system notice */}
      {isClosed && (
        <Alert className="mb-6 border-blue-300 bg-blue-50 text-blue-800">
          <ShieldAlert className="h-4 w-4" />
          <span className="ml-2 text-sm">
            The Inquiry Service is now closed. All confirmed details are recorded
            in the Truth Surface.
          </span>
        </Alert>
      )}

      {/* No session — tenant can request */}
      {noSession && (
        <Card>
          <CardContent className="py-12">
            <EmptyState
              title="No inquiry session yet"
              description={
                isTenant
                  ? "Request to unlock the inquiry service for this deal. The landlord must approve before questions can be asked."
                  : "The tenant has not yet requested an inquiry session for this deal."
              }
            />
            {isTenant && dealId && (
              <div className="mt-6 flex justify-center">
                <Button
                  onClick={() => requestUnlock.mutate(dealId)}
                  disabled={requestUnlock.isPending}
                  className="gap-2"
                >
                  <Unlock className="h-4 w-4" />
                  {requestUnlock.isPending
                    ? "Requesting..."
                    : "Request Detail Unlock"}
                </Button>
              </div>
            )}
            {requestUnlock.isError && (
              <Alert variant="destructive" className="mt-4 max-w-md mx-auto">
                {getApiErrorMessage(
                  requestUnlock.error,
                  "Failed to request unlock.",
                )}
              </Alert>
            )}
          </CardContent>
        </Card>
      )}

      {/* Forbidden — clean access denied UI */}
      {isForbidden && (
        <Alert variant="destructive">
          <Lock className="h-4 w-4" />
          <span className="ml-2 text-sm">
            {getApiErrorMessage(
              error,
              "You do not have access to this deal's inquiry thread.",
            )}
          </span>
        </Alert>
      )}

      {/* Error (non-404, non-403) */}
      {isError && !is404 && !isForbidden && (
        <Alert variant="destructive">
          {getApiErrorMessage(error, "Failed to load inquiry session.")}
        </Alert>
      )}

      {/* Locked — waiting for landlord */}
      {isLocked && inquiry && (
        <Card>
          <CardContent className="py-12">
            <EmptyState
              title="Inquiry Locked"
              description={
                isLandlord
                  ? "The tenant has requested to unlock the inquiry. Approve to allow structured questions about this deal."
                  : "Waiting for the landlord to approve your inquiry request."
              }
            />
            {isLandlord && dealId && (
              <div className="mt-6 flex justify-center">
                <Button
                  onClick={() => approveUnlock.mutate(dealId)}
                  disabled={approveUnlock.isPending}
                  variant="accent"
                  className="gap-2"
                >
                  <Unlock className="h-4 w-4" />
                  {approveUnlock.isPending
                    ? "Approving..."
                    : "Approve Unlock"}
                </Button>
              </div>
            )}
            {approveUnlock.isError && (
              <Alert variant="destructive" className="mt-4 max-w-md mx-auto">
                {getApiErrorMessage(
                  approveUnlock.error,
                  "Failed to approve unlock.",
                )}
              </Alert>
            )}
          </CardContent>
        </Card>
      )}

      {/* Open or Closed — show thread */}
      {inquiry && (isOpen || isClosed) && (
        <>
          {/* Session meta */}
          <div className="flex flex-wrap gap-4 text-sm text-muted-foreground mb-6">
            <span className="flex items-center gap-1">
              <Clock className="h-3.5 w-3.5" />
              Created: {formatDate(inquiry.createdAt)}
            </span>
            {inquiry.unlockedByLandlordAt && (
              <span className="flex items-center gap-1">
                <Unlock className="h-3.5 w-3.5" />
                Unlocked: {formatDate(inquiry.unlockedByLandlordAt)}
              </span>
            )}
            {inquiry.closedAt && (
              <span className="flex items-center gap-1">
                <Lock className="h-3.5 w-3.5" />
                Closed: {formatDate(inquiry.closedAt)}
              </span>
            )}
          </div>

          {/* Question / answer thread */}
          {inquiry.questions.length === 0 && isOpen && (
            <Card className="mb-6">
              <CardContent className="py-8">
                <EmptyState
                  title="No questions yet"
                  description={
                    isTenant
                      ? "Select a predefined question from a category below to get started."
                      : "The tenant hasn't asked any questions yet."
                  }
                />
              </CardContent>
            </Card>
          )}

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
                      <div className="flex items-center gap-2">
                        <Badge variant="outline" className="text-xs">
                          {responseTypeLabels[q.answer.responseType]}
                        </Badge>
                        <span className="text-sm font-medium">
                          {q.answer.answerValue}
                        </span>
                      </div>
                    </div>
                  ) : isOpen && isLandlord ? (
                    <InquiryResponseForm
                      dealId={inquiry.dealId}
                      questionId={q.questionId}
                      expectedResponseType={getExpectedResponseType(q)}
                    />
                  ) : (
                    <p className="text-sm text-muted-foreground italic">
                      Awaiting landlord response
                    </p>
                  )}
                </CardContent>
              </Card>
            ))}
          </div>

          {/* Ask new question (tenant only, while open) */}
          {isOpen && isTenant && dealId && (
            <>
              <Separator className="my-6" />
              <InquiryQuestion dealId={dealId} />
            </>
          )}
        </>
      )}
    </div>
  );
};
