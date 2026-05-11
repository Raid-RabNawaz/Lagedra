import { useState } from "react";
import { MessageSquarePlus } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { Select } from "@/components/ui/select";
import { Alert } from "@/components/ui/alert";
import { usePredefinedQuestions, useSubmitQuestion } from "@/features/inquiry/hooks/useInquiry";
import type { InquiryCategory } from "@/api/types";
import { getApiErrorMessage } from "@/api/errors";

const categoryLabels: Record<InquiryCategory, string> = {
  UtilitySpecifics: "Utility Specifics",
  AccessibilityLayout: "Accessibility & Layout",
  RuleClarification: "Rule Clarification",
  Proximity: "Proximity & Location",
};

type Props = {
  dealId: string;
};

export const InquiryQuestion = ({ dealId }: Props) => {
  const [category, setCategory] = useState<InquiryCategory | "">("");
  const [selectedQuestionId, setSelectedQuestionId] = useState("");

  const { data: questions } = usePredefinedQuestions(
    category ? (category as InquiryCategory) : undefined,
  );
  const submitMutation = useSubmitQuestion();

  const filteredQuestions = category
    ? questions?.filter((q) => q.category === category)
    : questions;

  const selectedQuestion = filteredQuestions?.find((q) => q.id === selectedQuestionId);

  const handleSubmit = async () => {
    if (!selectedQuestion || !category) {
      return;
    }
    try {
      await submitMutation.mutateAsync({
        dealId,
        payload: {
          category: category as InquiryCategory,
          predefinedQuestionId: selectedQuestion.id,
        },
      });
      setSelectedQuestionId("");
    } catch {
      // Error is surfaced by the mutation state below.
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

        {category && filteredQuestions && filteredQuestions.length > 0 && (
          <div className="space-y-1.5">
            <Label>Question</Label>
            <Select
              value={selectedQuestionId}
              onChange={(e) => setSelectedQuestionId(e.target.value)}
            >
              <option value="">Select a question</option>
              {filteredQuestions.map((q) => (
                <option key={q.id} value={q.id}>
                  {q.text}
                </option>
              ))}
            </Select>
          </div>
        )}

        {submitMutation.isError && (
          <Alert variant="destructive">
            {getApiErrorMessage(
              submitMutation.error,
              "Failed to submit question.",
            )}
          </Alert>
        )}

        <Button
          onClick={handleSubmit}
          disabled={!selectedQuestion || submitMutation.isPending}
          className="w-full"
        >
          {submitMutation.isPending ? "Submitting..." : "Submit Question"}
        </Button>
      </div>
    </div>
  );
};
