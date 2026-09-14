import { Link } from "react-router-dom";
import { FileWarning, Shield } from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { buttonVariants } from "@/components/ui/button-variants";
import { Loader } from "@/components/shared/Loader";
import { cn } from "@/lib/utils";
import { getApiErrorMessage, isNotFoundError } from "@/api/errors";
import {
  useDealInsurance,
  useRescreenDealInsurance,
} from "@/features/applications/hooks/useApplications";
import {
  STAY_PROTECTION_GUEST_AGREEMENT_URL,
  STAY_PROTECTION_LABEL,
  isStayProtectionClaimable,
  stayProtectionDamageClaimPath,
  stayProtectionStatus,
} from "@/features/listings/lib/stayProtection";

type Props = {
  dealId: string;
  canRescreen: boolean;
  perspective: "host" | "guest" | "other";
  canFileDamageClaim: boolean;
};

function GuestAgreementLink() {
  return (
    <a
      href={STAY_PROTECTION_GUEST_AGREEMENT_URL}
      target="_blank"
      rel="noopener noreferrer"
      className="underline underline-offset-2"
    >
      guest agreement
    </a>
  );
}

function ClaimGuidance({
  dealId,
  perspective,
  claimable,
  canFileDamageClaim,
  verificationId,
}: {
  dealId: string;
  perspective: Props["perspective"];
  claimable: boolean;
  canFileDamageClaim: boolean;
  verificationId: string | null;
}) {
  if (!claimable) {
    return (
      <p className="text-sm text-muted-foreground">
        A stay-protection claim cannot be started unless screening is Approved
        or Flagged.
      </p>
    );
  }

  if (perspective === "guest") {
    return (
      <div className="space-y-1.5">
        <p className="text-sm font-medium">How to start a claim</p>
        <p className="text-sm text-muted-foreground">
          You do not file stay-protection claims. The host starts a damage
          claim from Billing if there is damage. Amounts above the deposit go
          to Truvi under the <GuestAgreementLink />. We notify you if a claim
          is filed.
        </p>
      </div>
    );
  }

  return (
    <div className="space-y-2">
      <p className="text-sm font-medium">How to start a claim</p>
      <p className="text-sm text-muted-foreground">
        File a damage claim from Billing. Amounts up to the security deposit
        come from the deposit. Amounts above that are sent to Truvi
        {verificationId ? ` with verification ${verificationId}` : ""}.
        Coverage and exclusions follow the <GuestAgreementLink />.
      </p>
      {canFileDamageClaim ? (
        <Link
          to={stayProtectionDamageClaimPath(dealId)}
          className={cn(buttonVariants({ variant: "outline", size: "sm" }))}
        >
          <FileWarning className="h-4 w-4" />
          File a damage claim
        </Link>
      ) : (
        <p className="text-xs text-muted-foreground">
          The claim button opens on Billing after the guest&apos;s payment is
          confirmed.
        </p>
      )}
    </div>
  );
}

export function DealStayProtectionCard({
  dealId,
  canRescreen,
  perspective,
  canFileDamageClaim,
}: Props) {
  const insurance = useDealInsurance(dealId);
  const rescreen = useRescreenDealInsurance(dealId);
  const missing = isNotFoundError(insurance.error);
  const record = missing ? null : insurance.data;
  const status = stayProtectionStatus(record);
  const claimable = isStayProtectionClaimable(record);
  const showRescreen =
    canRescreen && insurance.data?.screeningStatus === "Flagged";

  return (
    <Card>
      <CardHeader className="pb-2">
        <CardTitle className="flex items-center gap-2 text-base">
          <Shield className="h-4 w-4 text-muted-foreground" />
          {STAY_PROTECTION_LABEL}
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-3">
        {insurance.isLoading ? (
          <Loader label="Loading stay protection…" />
        ) : insurance.isError && !missing ? (
          <p className="text-sm text-destructive">
            {getApiErrorMessage(
              insurance.error,
              "Could not load stay protection status.",
            )}
          </p>
        ) : (
          <>
            <div className="flex flex-wrap items-center gap-2">
              <p className="text-sm font-medium">{status.headline}</p>
              {status.screeningLabel && (
                <Badge variant={status.badgeVariant} className="text-[10px]">
                  Screening: {status.screeningLabel}
                </Badge>
              )}
            </div>
            {status.detail && (
              <p className="text-sm text-muted-foreground">{status.detail}</p>
            )}
            {insurance.data?.verificationId && (
              <p className="text-xs text-muted-foreground">
                Verification {insurance.data.verificationId}
              </p>
            )}
            <ClaimGuidance
              dealId={dealId}
              perspective={perspective}
              claimable={claimable}
              canFileDamageClaim={canFileDamageClaim}
              verificationId={insurance.data?.verificationId ?? null}
            />
            {showRescreen && (
              <div className="space-y-1.5">
                <p className="text-xs text-muted-foreground">
                  After the guest updates contact details, send a new screening.
                  Modify cannot change guest email or phone.
                </p>
                <Button
                  type="button"
                  size="sm"
                  variant="outline"
                  disabled={rescreen.isPending}
                  onClick={() => rescreen.mutate()}
                >
                  {rescreen.isPending ? "Rescreening…" : "Rescreen guest"}
                </Button>
                {rescreen.isError && (
                  <p className="text-sm text-destructive">
                    {getApiErrorMessage(
                      rescreen.error,
                      "Could not rescreen this guest.",
                    )}
                  </p>
                )}
              </div>
            )}
          </>
        )}
      </CardContent>
    </Card>
  );
}
