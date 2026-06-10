import { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import {
  ShieldCheck,
  CheckCircle2,
  AlertCircle,
  ExternalLink,
} from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { Label } from "@/components/ui/label";
import { Alert } from "@/components/ui/alert";
import { Loader } from "@/components/shared/Loader";
import {
  useSnapshotByDealId,
  useConfirmSnapshot,
} from "@/features/truth-surface/hooks/useTruthSurface";
import { formatMoney } from "@/utils/format";
import { getApiErrorMessage } from "@/api/errors";
import type { TruthSurfaceDto } from "@/api/types";

type Props = {
  dealId: string;
  /** Fired the moment the tenant successfully confirms. */
  onConfirmed?: () => void;
};

type CanonicalLine = { key: string; label: string; value: string };

function parseCanonicalContent(raw: string | null | undefined): CanonicalLine[] {
  if (!raw) return [];
  try {
    const obj = JSON.parse(raw) as Record<string, unknown>;
    return flattenObject(obj);
  } catch {
    return [{ key: "content", label: "Content", value: raw }];
  }
}

function flattenObject(
  obj: Record<string, unknown>,
  prefix = "",
): CanonicalLine[] {
  const lines: CanonicalLine[] = [];
  for (const [key, value] of Object.entries(obj)) {
    const fullKey = prefix ? `${prefix}.${key}` : key;
    if (value !== null && typeof value === "object" && !Array.isArray(value)) {
      lines.push(
        ...flattenObject(value as Record<string, unknown>, fullKey),
      );
    } else {
      lines.push({ key: fullKey, label: humanize(key), value: formatValue(key, value) });
    }
  }
  return lines;
}

function humanize(key: string): string {
  return key
    .replace(/([A-Z])/g, " $1")
    .replace(/[_-]/g, " ")
    .replace(/^\s/, "")
    .replace(/\b\w/g, (c) => c.toUpperCase())
    .trim();
}

function formatValue(key: string, value: unknown): string {
  if (value === null || value === undefined) return "—";
  if (typeof value === "boolean") return value ? "Yes" : "No";
  if (typeof value === "number") {
    if (key.toLowerCase().endsWith("cents")) return formatMoney(value);
    return String(value);
  }
  if (Array.isArray(value)) {
    return value.length === 0 ? "—" : value.map(String).join(", ");
  }
  return String(value);
}

/**
 * Phase 16.5 — inline Truth Surface confirmation panel rendered above
 * the Stripe payment element on the Checkout page. Removes the
 * separate Truth Surface confirmation screen for the tenant; the host
 * has already auto-confirmed at approve-time (16.4).
 *
 * Behaviour:
 * - Snapshot pending tenant confirmation → render the read-only deal
 *   terms summary, a single mandatory checkbox, and a Confirm button.
 * - Snapshot already confirmed by tenant → render nothing (caller
 *   shows the payment element).
 * - Snapshot missing or never created → render an inline error with
 *   a deep link back to the deal detail page so the host can fix it.
 */
export const InlineTruthSurfaceConfirm = ({ dealId, onConfirmed }: Props) => {
  const snapshotQuery = useSnapshotByDealId(dealId);
  const confirmMutation = useConfirmSnapshot();
  const [checked, setChecked] = useState(false);

  const lines = useMemo(
    () => parseCanonicalContent(snapshotQuery.data?.canonicalContent),
    [snapshotQuery.data?.canonicalContent],
  );

  if (snapshotQuery.isLoading) {
    return <Loader label="Loading deal terms…" />;
  }

  if (snapshotQuery.isError || !snapshotQuery.data) {
    return (
      <Alert variant="destructive" className="text-sm">
        <AlertCircle className="h-4 w-4" />
        <span className="ml-2">
          Couldn't load the Truth Surface for this deal yet. The host may not
          have created it.{" "}
          <Link
            to={`/app/deals/${dealId}`}
            className="underline underline-offset-2"
          >
            Open the deal page
          </Link>{" "}
          for the latest status.
        </span>
      </Alert>
    );
  }

  const snapshot: TruthSurfaceDto = snapshotQuery.data;

  // Already confirmed by the tenant (or sealed). Caller renders the
  // payment element; we step out of the way silently.
  if (snapshot.tenantConfirmed) {
    return null;
  }

  const handleConfirm = async () => {
    await confirmMutation.mutateAsync(
      { snapshotId: snapshot.snapshotId, party: "Tenant" },
      { onSuccess: () => onConfirmed?.() },
    );
  };

  return (
    <Card className="mb-6 border-amber-200">
      <CardHeader className="pb-3">
        <CardTitle className="text-base flex items-center gap-2">
          <ShieldCheck className="h-4 w-4 text-amber-700" />
          Confirm the deal terms before paying
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-4">
        <p className="text-sm text-muted-foreground">
          The host has already signed the Truth Surface for this booking.
          Review the terms below — once you confirm, this becomes the
          immutable source of truth and you can complete payment.
        </p>

        {lines.length > 0 && (
          <div className="rounded-md border bg-muted/30 divide-y">
            {lines.slice(0, 12).map((line) => (
              <div
                key={line.key}
                className="flex items-center justify-between gap-4 px-3 py-2 text-sm"
              >
                <span className="text-muted-foreground">{line.label}</span>
                <span className="font-medium text-right">{line.value}</span>
              </div>
            ))}
          </div>
        )}

        <div className="text-xs">
          <Link
            to={`/app/truth-surface/${snapshot.snapshotId}`}
            className="inline-flex items-center gap-1 text-muted-foreground hover:text-foreground"
            target="_blank"
            rel="noreferrer"
          >
            View full snapshot &amp; cryptographic proof
            <ExternalLink className="h-3 w-3" />
          </Link>
        </div>

        <div className="flex items-start gap-3 rounded-md border bg-background p-3">
          <Checkbox
            id="ts-tenant-confirm"
            checked={checked}
            onCheckedChange={(v) => setChecked(v === true)}
            className="mt-0.5"
          />
          <Label
            htmlFor="ts-tenant-confirm"
            className="text-sm leading-snug cursor-pointer"
          >
            I have read and agree to the deal terms above. I understand this
            confirmation seals the Truth Surface as the cryptographic record
            of this booking.
          </Label>
        </div>

        <Button
          onClick={handleConfirm}
          disabled={!checked || confirmMutation.isPending}
          className="w-full gap-2"
          size="lg"
        >
          <CheckCircle2 className="h-4 w-4" />
          {confirmMutation.isPending
            ? "Confirming…"
            : "Confirm and continue to payment"}
        </Button>

        {confirmMutation.isError && (
          <Alert variant="destructive" className="text-sm">
            {getApiErrorMessage(
              confirmMutation.error,
              "Failed to confirm. Please try again.",
            )}
          </Alert>
        )}
      </CardContent>
    </Card>
  );
};
