import { useState } from "react";
import { MessageSquarePlus } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { Select } from "@/components/ui/select";
import { Textarea } from "@/components/ui/textarea";
import { Alert } from "@/components/ui/alert";
import {
  usePredefinedQuestions,
  useSubmitQuestion,
  useSubmitSessionQuestion,
} from "@/features/inquiry/hooks/useInquiry";
import type { InquiryCategory } from "@/api/types";
import { getApiErrorMessage } from "@/api/errors";

const categoryLabels: Record<InquiryCategory, string> = {
  UtilitySpecifics: "Utility Specifics",
  AccessibilityLayout: "Accessibility & Layout",
  RuleClarification: "Rule Clarification",
  Proximity: "Proximity & Location",
};

const OTHER_OPTION = "__other__";
const MAX_OPEN_TEXT = 1000;

/**
 * Phase 17 — props now allow either a deal-id (legacy in-deal Q&A
 * surface) or a session-id (pre-booking + new session-id-based routes).
 * Exactly one must be supplied. Behaviour is otherwise identical.
 */
type Props =
  | { dealId: string; sessionId?: never }
  | { dealId?: never; sessionId: string };

export const InquiryQuestion = (props: Props) => {
  const [category, setCategory] = useState<InquiryCategory | "">("");
  const [selectedQuestionId, setSelectedQuestionId] = useState("");
  const [openQuestionText, setOpenQuestionText] = useState("");

  const { data: questions } = usePredefinedQuestions(
    category ? (category as InquiryCategory) : undefined,
  );

  const submitDealMutation = useSubmitQuestion();
  const submitSessionMutation = useSubmitSessionQuestion();
  const isPending = submitDealMutation.isPending || submitSessionMutation.isPending;
  const submitError = submitDealMutation.error ?? submitSessionMutation.error;
  const isError = submitDealMutation.isError || submitSessionMutation.isError;

  const filteredQuestions = category
    ? questions?.filter((q) => q.category === category)
    : questions;

  const isOtherSelected = selectedQuestionId === OTHER_OPTION;
  const selectedQuestion = !isOtherSelected
    ? filteredQuestions?.find((q) => q.id === selectedQuestionId)
    : undefined;

  const canSubmit =
    !!category &&
    ((isOtherSelected && openQuestionText.trim().length > 0) ||
      !!selectedQuestion);

  const handleSubmit = async () => {
    if (!canSubmit || !category) return;

    const payload = {
      category: category as InquiryCategory,
      predefinedQuestionId: isOtherSelected ? null : selectedQuestion!.id,
      openQuestionText: isOtherSelected
        ? openQuestionText.trim().slice(0, MAX_OPEN_TEXT)
        : null,
    };

    try {
      if ("dealId" in props && props.dealId) {
        await submitDealMutation.mutateAsync({ dealId: props.dealId, payload });
      } else if ("sessionId" in props && props.sessionId) {
        await submitSessionMutation.mutateAsync({
          sessionId: props.sessionId,
          payload,
        });
      }
      setSelectedQuestionId("");
      setOpenQuestionText("");
    } catch {
      // Error surfaced via the alert below.
    }
  };

  return (
    <div className="space-y-4 rounded-lg border bg-card p-4">
      <h3 className="text-sm font-semibold flex items-center gap-2">
        <MessageSquarePlus className="h-4 w-4" />
        Ask a Question
      </h3>

      <div className="space-y-3">
        <div className="space-y-1.5">
          <Label>Category</Label>
          <Select
            value={category}
            onChange={(e) => {
              setCategory(e.target.value as InquiryCategory | "");
              setSelectedQuestionId("");
              setOpenQuestionText("");
            }}
          >
            <option value="">Select a category</option>
            {(Object.keys(categoryLabels) as InquiryCategory[]).map((key) => (
              <option key={key} value={key}>
                {categoryLabels[key]}
              </option>
            ))}
          </Select>
        </div>

        {category && (
          <div className="space-y-1.5">
            <Label>Question</Label>
            <Select
              value={selectedQuestionId}
              onChange={(e) => setSelectedQuestionId(e.target.value)}
            >
              <option value="">Select a question</option>
              {filteredQuestions?.map((q) => (
                <option key={q.id} value={q.id}>
                  {q.text}
                </option>
              ))}
              {/* Phase 17 — escape hatch for free-form questions. */}
              <option value={OTHER_OPTION}>Other (type your own)</option>
            </Select>
          </div>
        )}

        {isOtherSelected && (
          <div className="space-y-1.5">
            <Label htmlFor="iq-open-text">Your question</Label>
            <Textarea
              id="iq-open-text"
              value={openQuestionText}
              onChange={(e) =>
                setOpenQuestionText(e.target.value.slice(0, MAX_OPEN_TEXT))
              }
              placeholder="Type your question for the host…"
              maxLength={MAX_OPEN_TEXT}
              rows={3}
            />
            <p className="text-[11px] text-muted-foreground text-right">
              {openQuestionText.length}/{MAX_OPEN_TEXT}
            </p>
          </div>
        )}

        {isError && (
          <Alert variant="destructive">
            {getApiErrorMessage(submitError, "Failed to submit question.")}
          </Alert>
        )}

        <Button
          onClick={handleSubmit}
          disabled={!canSubmit || isPending}
          className="w-full"
        >
          {isPending ? "Submitting..." : "Submit Question"}
        </Button>
      </div>
    </div>
  );
};
