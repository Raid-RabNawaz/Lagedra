import { Badge } from "@/components/ui/badge";
import type { DealPhase } from "@/api/types";
import { dealPhaseLabel } from "@/features/deals/utils/dealVocabulary";

const phaseVariant: Record<
  DealPhase,
  "default" | "secondary" | "destructive" | "success" | "outline" | "accent"
> = {
  TruthSurface: "accent",
  Checkout: "default",
  Active: "success",
  Closed: "secondary",
  Cancelled: "destructive",
};

export function DealPhaseBadge({ phase }: { phase: DealPhase }) {
  const variant = phaseVariant[phase] ?? ("outline" as const);
  return <Badge variant={variant}>{dealPhaseLabel(phase)}</Badge>;
}
