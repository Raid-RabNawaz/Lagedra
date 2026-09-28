import { useState } from "react";
import { PenLine } from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { getApiErrorMessage, isNotFoundError } from "@/api/errors";
import { formatDate } from "@/utils/format";
import type { DealLeasePartySignatureDto } from "@/api/types";
import {
  useDealLeaseSigning,
  useSignDealLease,
} from "@/features/deals/hooks/useDealLeaseSigning";

type Props = {
  dealId: string;
};

function PartyRow({
  label,
  signature,
}: {
  label: string;
  signature: DealLeasePartySignatureDto | null;
}) {
  return (
    <div className="flex items-start justify-between gap-3 text-sm">
      <p className="font-medium">{label}</p>
      {signature ? (
        <p className="text-right text-muted-foreground">
          {signature.typedName}
          <span className="block text-xs">{formatDate(signature.signedAtUtc)}</span>
        </p>
      ) : (
        <p className="text-muted-foreground">Waiting</p>
      )}
    </div>
  );
}

export function DealLeaseSigningCard({ dealId }: Props) {
  const status = useDealLeaseSigning(dealId);
  const sign = useSignDealLease(dealId);
  const [typedName, setTypedName] = useState("");
  const [accepted, setAccepted] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  if (status.isLoading) {
    return null;
  }

  if (status.isError) {
    if (isNotFoundError(status.error)) {
      return null;
    }
    return (
      <Card>
        <CardContent className="py-4">
          <p className="text-sm text-destructive">
            {getApiErrorMessage(status.error, "Could not load lease signatures.")}
          </p>
        </CardContent>
      </Card>
    );
  }

  const data = status.data;
  if (!data?.signingRequired) {
    return null;
  }

  const nameReady = typedName.trim().length >= 2;
  const canSubmit = data.callerCanSign && nameReady && accepted && !sign.isPending;

  const headline = data.bothSigned
    ? "Host and tenant signed this lease."
    : data.callerHasSigned
      ? "You signed. Waiting for the other party."
      : "Type your name to execute this lease in Lagedra.";

  const handleSign = async () => {
    setFormError(null);
    if (!nameReady) {
      setFormError("Type your name to sign this lease.");
      return;
    }
    if (!accepted) {
      setFormError("Confirm that you are signing this lease electronically.");
      return;
    }

    try {
      await sign.mutateAsync({ typedName: typedName.trim(), accepted: true });
    } catch (err) {
      setFormError(getApiErrorMessage(err, "Could not record your signature."));
    }
  };

  return (
    <Card>
      <CardHeader className="pb-2">
        <CardTitle className="flex items-center gap-2 text-base">
          <PenLine className="h-4 w-4 text-muted-foreground" />
          Lease signatures
          {data.bothSigned ? (
            <Badge variant="secondary" className="text-[10px]">
              Complete
            </Badge>
          ) : (
            <Badge variant="accent" className="text-[10px]">
              Action needed
            </Badge>
          )}
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-4">
        <p className="text-sm text-muted-foreground">{headline}</p>
        {!data.bothSigned && (
          <p className="text-xs text-muted-foreground">
            Checkout is not blocked. Your name and the date are stamped onto the
            lease PDF after you confirm.
          </p>
        )}
        <div className="space-y-2 rounded-lg border p-3">
          <PartyRow label="Host" signature={data.host} />
          <PartyRow label="Tenant" signature={data.tenant} />
        </div>
        {data.callerCanSign && (
          <form
            className="space-y-3"
            onSubmit={(event) => {
              event.preventDefault();
              void handleSign();
            }}
          >
            <div className="space-y-1.5">
              <Label htmlFor="lease-signature-name">Your name</Label>
              <Input
                id="lease-signature-name"
                value={typedName}
                autoComplete="name"
                onChange={(event) => setTypedName(event.target.value)}
                placeholder="Type your full name"
              />
            </div>
            <label className="flex items-start gap-2">
              <Checkbox
                checked={accepted}
                onCheckedChange={(checked) => setAccepted(checked === true)}
                aria-label="I am signing this lease electronically in Lagedra"
              />
              <span className="text-sm">
                I am signing this lease electronically in Lagedra.
              </span>
            </label>
            {(formError || sign.isError) && (
              <p className="text-sm text-destructive">
                {formError
                  ?? getApiErrorMessage(sign.error, "Could not record your signature.")}
              </p>
            )}
            <Button type="submit" size="sm" disabled={!canSubmit}>
              {sign.isPending ? "Signing…" : "Sign lease"}
            </Button>
          </form>
        )}
      </CardContent>
    </Card>
  );
}
