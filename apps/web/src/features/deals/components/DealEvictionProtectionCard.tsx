import { Shield } from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { formatMoney } from "@/utils/format";
import { EvictionProtectionTermsList } from "@/features/listings/components/EvictionProtectionTermsList";
import {
  EVICTION_PROTECTION_CAP_CENTS,
  EVICTION_PROTECTION_CLAIMS_EMAIL,
  EVICTION_PROTECTION_LABEL,
  evictionProtectionAmountLabel,
  type EvictionProtectionClaimStanding,
} from "@/features/listings/lib/evictionProtection";

type Props = {
  feeCents: number;
  perspective: "host" | "guest" | "other";
  claimStanding?: EvictionProtectionClaimStanding | null;
};

export function DealEvictionProtectionCard({
  feeCents,
  perspective,
  claimStanding,
}: Props) {
  const ended = claimStanding === "EndedNoClaim";
  const reviewable = claimStanding === "ClaimReviewable";
  const claimSentence =
    perspective === "guest"
      ? "The host sends a claim to Lagedra. Claims are not filed in this app."
      : "Email the booking id, rent ledger, legal notice, and invoices for attorney, court, sheriff, and locksmith costs.";

  return (
    <Card>
      <CardHeader className="pb-3">
        <div className="flex items-center justify-between gap-2">
          <CardTitle className="flex items-center gap-2 text-base">
            <Shield className="h-4 w-4" />
            {EVICTION_PROTECTION_LABEL}
          </CardTitle>
          <Badge
            variant={ended ? "secondary" : reviewable ? "warning" : "success"}
            className="text-[10px]"
          >
            {ended ? "Ended" : reviewable ? "Claim reviewable" : "Active"}
          </Badge>
        </div>
      </CardHeader>
      <CardContent className="space-y-2 text-sm text-muted-foreground">
        {ended ? (
          <p>
            This stay ended before check-out for a reason that is not a qualifying
            nonpayment or eviction event. Coverage ended when the booking ended, and
            no claim applies.
          </p>
        ) : (
          <>
            <p>
              {`This booking includes Lagedra Partner Eviction Protection${
                feeCents > 0 ? ` (${evictionProtectionAmountLabel(feeCents)})` : ""
              }, up to ${formatMoney(EVICTION_PROTECTION_CAP_CENTS)}. It covers qualifying unpaid rent and eviction costs. Property damage stays with Stay Protection.`}
            </p>
            {reviewable ? (
              <p>
                This early move-out is connected to documented nonpayment. A claim may
                still be reviewed under the Eviction Protection terms.
              </p>
            ) : (
              <EvictionProtectionTermsList />
            )}
            <p>
              {claimSentence}{" "}
              <a
                href={`mailto:${EVICTION_PROTECTION_CLAIMS_EMAIL}`}
                className="underline underline-offset-2"
              >
                {EVICTION_PROTECTION_CLAIMS_EMAIL}
              </a>
            </p>
          </>
        )}
      </CardContent>
    </Card>
  );
}
