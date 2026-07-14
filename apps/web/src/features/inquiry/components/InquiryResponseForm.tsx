import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { Send } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select } from "@/components/ui/select";
import { Textarea } from "@/components/ui/textarea";
import { Alert } from "@/components/ui/alert";
import { inquiryApi } from "@/features/inquiry/services/inquiryApi";
import type {
  InquiryDto,
  ResponseType,
  SubmitLandlordResponseRequest,
} from "@/api/types";
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
  const queryClient = useQueryClient();
  const initialResponseType: ResponseType = props.isOpenQuestion
    ? "OpenText"
    : props.expectedResponseType ?? "YesNo";

  const [responseType, setResponseType] = useState<ResponseType>(initialResponseType);
  const [answerValue, setAnswerValue] = useState("");

  const useSessionRoute = "sessionId" in props && !!props.sessionId;

  // One mutation instance per question form — never share pending/error with siblings.
  const submitMutation = useMutation({
    mutationKey: [
      "inquiry",
      "submit-answer",
      useSessionRoute ? props.sessionId : props.dealId,
      props.questionId,
    ],
    mutationFn: async (payload: SubmitLandlordResponseRequest) => {
      if (useSessionRoute && props.sessionId) {
        return inquiryApi.submitSessionAnswer(props.sessionId, payload);
      }
      if ("dealId" in props && props.dealId) {
        return inquiryApi.submitAnswer(props.dealId, payload);
      }
      throw new Error("Missing dealId or sessionId for answer submit.");
    },
    onSuccess: (answer, payload) => {
      if (useSessionRoute && props.sessionId) {
        queryClient.setQueryData<InquiryDto>(
          ["inquiry", "by-session", props.sessionId],
          (current) => {
            if (!current) return current;
            return {
              ...current,
              questions: current.questions.map((q) =>
                q.questionId === payload.questionId
                  ? {
                      ...q,
                      answer: {
                        answerId: answer.answerId,
                        responseType: answer.responseType,
                        answerValue: answer.answerValue,
                        answeredAt: answer.answeredAt,
                      },
                    }
                  : q,
              ),
            };
          },
        );
        void queryClient.invalidateQueries({
          queryKey: ["inquiry", "by-session", props.sessionId],
        });
      } else if ("dealId" in props && props.dealId) {
        void queryClient.invalidateQueries({
          queryKey: ["inquiry", props.dealId],
        });
      }
      void queryClient.invalidateQueries({
        queryKey: ["inquiry", "host-inbox"],
      });
      setAnswerValue("");
    },
  });

  const handleSubmit = () => {
    const trimmed = answerValue.trim();
    if (!trimmed) return;

    submitMutation.mutate({
      questionId: props.questionId,
      responseType,
      answerValue: trimmed.slice(0, MAX_TEXT_ANSWER),
    });
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

      {submitMutation.isError && (
        <Alert variant="destructive" className="text-xs">
          {getApiErrorMessage(submitMutation.error, "Failed to submit answer.")}
        </Alert>
      )}

      <Button
        size="sm"
        onClick={handleSubmit}
        disabled={!answerValue.trim() || submitMutation.isPending}
        className="gap-1.5"
      >
        <Send className="h-3 w-3" />
        {submitMutation.isPending ? "Sending..." : "Send Answer"}
      </Button>
    </div>
  );
};
