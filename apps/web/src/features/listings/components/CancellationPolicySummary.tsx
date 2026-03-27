import type { CancellationPolicyDto } from "@/api/types";
import { Badge } from "@/components/ui/badge";

const typeColors: Record<string, "success" | "secondary" | "accent" | "destructive"> = {
  Flexible: "success",
  Moderate: "secondary",
  Strict: "accent",
  NonRefundable: "destructive",
  Custom: "secondary",
};

type CancellationPolicySummaryProps = {
  policy: CancellationPolicyDto;
};

export function CancellationPolicySummary({ policy }: CancellationPolicySummaryProps) {
  const description = buildDescription(policy);

  return (
    <div className="space-y-2">
      <div className="flex items-center gap-2">
        <Badge variant={typeColors[policy.type] ?? "secondary"}>
          {policy.type === "NonRefundable" ? "Non-refundable" : policy.type}
        </Badge>
      </div>
      <p className="text-sm text-muted-foreground">{description}</p>
      {policy.customTerms && (
        <p className="text-sm text-muted-foreground border-t pt-2 mt-2">
          {policy.customTerms}
        </p>
      )}
    </div>
  );
}

function buildDescription(policy: CancellationPolicyDto): string {
  if (policy.type === "NonRefundable") {
    return "This booking is non-refundable.";
  }

  let desc = `Free cancellation up to ${policy.freeCancellationDays} days before check-in.`;

  if (policy.partialRefundPercent && policy.partialRefundDays) {
    desc += ` ${policy.partialRefundPercent}% refund if cancelled ${policy.partialRefundDays} days before.`;
  }

  return desc;
}
