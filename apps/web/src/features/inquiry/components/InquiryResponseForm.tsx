import { useState } from "react";
import { Send } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select } from "@/components/ui/select";
import { Alert } from "@/components/ui/alert";
import { useSubmitAnswer } from "@/features/inquiry/hooks/useInquiry";
import type { ResponseType } from "@/api/types";

type Props = {
  dealId: string;
  questionId: string;
  expectedResponseType?: ResponseType;
};

export const InquiryResponseForm = ({
  dealId,
  questionId,
  expectedResponseType,
}: Props) => {
  const [responseType, setResponseType] = useState<ResponseType>(
    expectedResponseType ?? "YesNo",
  );
  const [answerValue, setAnswerValue] = useState("");
  const submitMutation = useSubmitAnswer();

  const handleSubmit = async () => {
    if (!answerValue.trim()) {
      return;
    }
    await submitMutation.mutateAsync({
      dealId,
      payload: {
        questionId,
        responseType,
        answerValue: answerValue.trim(),
      },
    });
    setAnswerValue("");
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
          {(submitMutation.error as Error)?.message ?? "Failed to submit answer."}
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
