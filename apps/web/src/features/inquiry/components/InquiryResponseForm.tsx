import { useState } from "react";
import { Send } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select } from "@/components/ui/select";
import { Textarea } from "@/components/ui/textarea";
import { Alert } from "@/components/ui/alert";
import {
  useSubmitAnswer,
  useSubmitSessionAnswer,
} from "@/features/inquiry/hooks/useInquiry";
import type { ResponseType } from "@/api/types";
import { getApiErrorMessage } from "@/api/errors";

/**
 * Phase 17 — props now allow either a deal-id (legacy in-deal Q&A
 * surface) or a session-id (pre-booking + new session-id-based routes).
 * Exactly one must be supplied.
 */
type Props = {
  questionId: string;
  expectedResponseType?: ResponseType;
  /**
   * If true, the question was a tenant-typed open-text ask. The form
   * defaults the response type to <code>OpenText</code> and shows a
   * textarea so the host can reply in prose.
   */
  isOpenQuestion?: boolean;
} & (
  | { dealId: string; sessionId?: never }
  | { dealId?: never; sessionId: string }
);

const MAX_TEXT_ANSWER = 2000;

export const InquiryResponseForm = (props: Props) => {
  const initialResponseType: ResponseType = props.isOpenQuestion
    ? "OpenText"
    : props.expectedResponseType ?? "YesNo";

  const [responseType, setResponseType] = useState<ResponseType>(initialResponseType);
  const [answerValue, setAnswerValue] = useState("");

  const submitDealMutation = useSubmitAnswer();
  const submitSessionMutation = useSubmitSessionAnswer();
  const isPending = submitDealMutation.isPending || submitSessionMutation.isPending;
  const submitError = submitDealMutation.error ?? submitSessionMutation.error;
  const isError = submitDealMutation.isError || submitSessionMutation.isError;

  const handleSubmit = async () => {
    const trimmed = answerValue.trim();
    if (!trimmed) return;

    const payload = {
      questionId: props.questionId,
      responseType,
      answerValue: trimmed.slice(0, MAX_TEXT_ANSWER),
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
      setAnswerValue("");
    } catch {
      // Error surfaced via the alert below.
    }
  };

  return (
    <div className="mt-3 space-y-3 rounded-md border border-dashed bg-muted/30 p-3">
      <div className="space-y-1.5">
        <Label className="text-xs">Response type</Label>
        <Select
          value={responseType}
          onChange={(e) => {
            setResponseType(e.target.value as ResponseType);
            setAnswerValue("");
          }}
          className="h-8 text-xs"
        >
          <option value="YesNo">Yes / No</option>
          <option value="MultipleChoice">Multiple Choice</option>
          <option value="Numeric">Numeric</option>
          <option value="OpenText">Open response</option>
        </Select>
      </div>

      <div className="space-y-1.5">
        <Label className="text-xs">Answer</Label>
        {responseType === "YesNo" ? (
          <Select
            value={answerValue}
            onChange={(e) => setAnswerValue(e.target.value)}
            className="h-8 text-xs"
          >
            <option value="">Select...</option>
            <option value="Yes">Yes</option>
            <option value="No">No</option>
          </Select>
        ) : responseType === "OpenText" ? (
          <Textarea
            value={answerValue}
            onChange={(e) => setAnswerValue(e.target.value.slice(0, MAX_TEXT_ANSWER))}
            placeholder="Type your response…"
            maxLength={MAX_TEXT_ANSWER}
            rows={3}
            className="text-xs"
          />
        ) : (
          <Input
            type={responseType === "Numeric" ? "number" : "text"}
            value={answerValue}
            onChange={(e) => setAnswerValue(e.target.value)}
            placeholder={
              responseType === "Numeric"
                ? "Enter a number"
                : "Enter your answer"
            }
            className="h-8 text-xs"
          />
        )}
      </div>

      {isError && (
        <Alert variant="destructive" className="text-xs">
          {getApiErrorMessage(submitError, "Failed to submit answer.")}
        </Alert>
      )}

      <Button
        size="sm"
        onClick={handleSubmit}
        disabled={!answerValue.trim() || isPending}
        className="gap-1.5"
      >
        <Send className="h-3 w-3" />
        {isPending ? "Sending..." : "Send Answer"}
      </Button>
    </div>
  );
};
