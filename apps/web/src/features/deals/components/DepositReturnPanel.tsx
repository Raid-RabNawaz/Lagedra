import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { useQueryClient } from "@tanstack/react-query";
import { HandCoins, Check, Clock, DoorOpen, Scale } from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { Select } from "@/components/ui/select";
import { Checkbox } from "@/components/ui/checkbox";
import { Alert } from "@/components/ui/alert";
import { Loader } from "@/components/shared/Loader";
import { EvidenceUpload } from "@/features/arbitration/components/EvidenceUpload";
import { useManifest } from "@/features/evidence/hooks/useEvidence";
import {
  usePaymentStatus,
  useBeginMoveOut,
  useConfirmDepositReturnedByHost,
  useConfirmDepositReceivedByTenant,
} from "@/features/activation-billing/hooks/useBilling";
import { MY_DEALS_KEY } from "@/features/deals/hooks/useDeals";
import { formatDate, formatMoney } from "@/utils/format";
import type { DealSummaryDto } from "@/api/types";

type Props = {
  deal: DealSummaryDto;
  isLandlord: boolean;
  isTenant: boolean;
};

const DEPOSIT_RETURN_WINDOW_DAYS = 21;

const returnMethods = [
  "Bank transfer",
  "Zelle",
  "Venmo",
  "PayPal",
  "Cash",
  "Check",
  "Other",
];

function apiError(e: unknown): string {
  return (
    (e as { response?: { data?: { detail?: string } } })?.response?.data
      ?.detail ??
    (e as Error)?.message ??
    "Something went wrong. Please try again."
  );
}

/**
 * Non-custodial deposit return. On an Active deal it offers the "end stay" CTA
 * that opens the handshake; on an AwaitingDepositReturn deal it drives the
 * two-sided confirmation (host returned / tenant received) with an arbitration
 * escape hatch.
 */
export function DepositReturnPanel({ deal, isLandlord, isTenant }: Props) {
  if (deal.dealPhase === "Active") {
    return <EndStayCard deal={deal} canAct={isLandlord || isTenant} />;
  }
  if (deal.dealPhase === "AwaitingDepositReturn") {
    return (
      <HandshakeCard deal={deal} isLandlord={isLandlord} isTenant={isTenant} />
    );
  }
  return null;
}

function EndStayCard({
  deal,
  canAct,
}: {
  deal: DealSummaryDto;
  canAct: boolean;
}) {
  const [confirming, setConfirming] = useState(false);
  const beginMoveOut = useBeginMoveOut();
  const hasDeposit = (deal.depositAmountCents ?? 0) > 0;

  if (!canAct) return null;

  return (
    <Card>
      <CardHeader className="pb-3">
        <CardTitle className="flex items-center gap-2 text-base">
          <DoorOpen className="h-4 w-4" />
          End stay {hasDeposit ? "& return deposit" : ""}
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-3">
        <p className="text-sm text-muted-foreground">
          Ending the stay stops the recurring platform fee
          {hasDeposit
            ? " and opens the deposit-return step. The host returns the deposit to the guest directly, then both parties confirm to complete the deal."
            : ". This deal has no deposit, so it completes right away."}
        </p>

        {beginMoveOut.isError && (
          <Alert variant="destructive" className="text-sm">
            {apiError(beginMoveOut.error)}
          </Alert>
        )}

        {!confirming ? (
          <Button variant="outline" onClick={() => setConfirming(true)}>
            End stay{hasDeposit ? " & return deposit" : ""}
          </Button>
        ) : (
          <div className="flex flex-wrap items-center gap-2">
            <span className="text-sm font-medium">
              End the stay now? This closes billing.
            </span>
            <Button
              size="sm"
              disabled={beginMoveOut.isPending}
              onClick={() => beginMoveOut.mutate(deal.dealId)}
            >
              {beginMoveOut.isPending ? "Ending…" : "Yes, end stay"}
            </Button>
            <Button
              size="sm"
              variant="ghost"
              disabled={beginMoveOut.isPending}
              onClick={() => setConfirming(false)}
            >
              Cancel
            </Button>
          </div>
        )}
      </CardContent>
    </Card>
  );
}

function StepRow({ done, label }: { done: boolean; label: string }) {
  return (
    <div className="flex items-center gap-2 text-sm">
      <span
        className={
          done
            ? "flex h-5 w-5 items-center justify-center rounded-full bg-primary text-primary-foreground"
            : "flex h-5 w-5 items-center justify-center rounded-full border border-muted-foreground/40 text-muted-foreground"
        }
      >
        {done ? <Check className="h-3 w-3" /> : <Clock className="h-3 w-3" />}
      </span>
      <span className={done ? "font-medium" : "text-muted-foreground"}>
        {label}
      </span>
    </div>
  );
}

function DisputeLink({ dealId }: { dealId: string }) {
  return (
    <Link
      to={`/app/arbitration?dealId=${dealId}&category=DepositReturn`}
      className="inline-flex items-center gap-1 text-sm font-medium text-blue-600 hover:underline"
    >
      <Scale className="h-4 w-4" />
      Didn't get your deposit? Open a dispute
    </Link>
  );
}

function HandshakeCard({
  deal,
  isLandlord,
  isTenant,
}: {
  deal: DealSummaryDto;
  isLandlord: boolean;
  isTenant: boolean;
}) {
  const queryClient = useQueryClient();
  // Prefer deal-summary handshake fields so a stale payment cache can't claim
  // the guest never confirmed after they already settled server-side.
  const summarySettled = Boolean(deal.depositReturnSettledAt);
  const summaryTenantReceived = Boolean(deal.tenantConfirmedDepositReceivedAt);
  const summaryHostReturned = Boolean(deal.hostConfirmedDepositReturnedAt);

  const { data: payment, isLoading } = usePaymentStatus(deal.dealId, {
    // Keep polling while the handshake is open so the host sees guest
    // confirmation without a manual refresh.
    refetchInterval: (query) => {
      if (summarySettled || query.state.data?.depositReturnSettledAt) {
        return false;
      }
      return 5_000;
    },
  });
  const confirmReturned = useConfirmDepositReturnedByHost();
  const confirmReceived = useConfirmDepositReceivedByTenant();

  const [amountInput, setAmountInput] = useState<string | null>(null);
  const [method, setMethod] = useState(returnMethods[0]);
  const [note, setNote] = useState("");
  const [confirmChecked, setConfirmChecked] = useState(false);
  const [evidenceManifestId, setEvidenceManifestId] = useState<string | undefined>();
  const [clientError, setClientError] = useState<string | null>(null);

  const { data: evidenceManifest } = useManifest(evidenceManifestId);
  const evidenceSealed = evidenceManifest?.status === "Sealed";

  const hostReturned =
    Boolean(payment?.hostConfirmedDepositReturnedAt) || summaryHostReturned;
  const tenantReceived =
    Boolean(payment?.tenantConfirmedDepositReceivedAt) || summaryTenantReceived;
  const settled =
    Boolean(payment?.depositReturnSettledAt) || summarySettled;

  // Payment status can settle before the deals list refetches phase → Closed.
  useEffect(() => {
    if (!payment?.depositReturnSettledAt) return;
    if (deal.dealPhase === "Closed" && deal.depositReturnSettledAt) return;
    void queryClient.invalidateQueries({ queryKey: [MY_DEALS_KEY] });
  }, [
    payment?.depositReturnSettledAt,
    deal.dealPhase,
    deal.depositReturnSettledAt,
    queryClient,
  ]);

  if (isLoading || !payment) {
    return (
      <Card>
        <CardContent className="py-6">
          <Loader label="Loading deposit status…" />
        </CardContent>
      </Card>
    );
  }

  const depositCents = payment.depositAmountCents;
  const netCents =
    payment.netReturnableDepositCents ?? payment.depositAmountCents;
  const netDollars = (netCents / 100).toFixed(2);
  const amountValue = amountInput ?? netDollars;
  const dollars = Number.parseFloat(amountValue);
  const returnedCents = Number.isFinite(dollars) ? Math.round(dollars * 100) : 0;
  const isPartialReturn = returnedCents < depositCents;

  const submitHostConfirm = () => {
    setClientError(null);

    if (isPartialReturn) {
      if (!note.trim()) {
        setClientError(
          "Add a valid reason for the deductions when returning less than the full deposit.",
        );
        return;
      }
      if (!evidenceManifestId || !evidenceSealed) {
        setClientError(
          "Upload and seal at least one damage photo before confirming a partial return.",
        );
        return;
      }
    }

    confirmReturned.mutate({
      dealId: deal.dealId,
      payload: {
        returnedAmountCents: returnedCents,
        method,
        note: note.trim() ? note.trim() : null,
        evidenceManifestId: isPartialReturn ? evidenceManifestId! : null,
      },
    });
  };

  const canSubmitHost =
    confirmChecked &&
    !confirmReturned.isPending &&
    (!isPartialReturn ||
      (Boolean(note.trim()) && Boolean(evidenceManifestId) && evidenceSealed));

  return (
    <Card>
      <CardHeader className="pb-3">
        <CardTitle className="flex items-center gap-2 text-base">
          <HandCoins className="h-4 w-4" />
          Deposit return
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="space-y-1.5">
          <StepRow done={hostReturned} label="Host returned the deposit" />
          <StepRow done={tenantReceived} label="Guest confirmed receipt" />
        </div>

        {netCents !== payment.depositAmountCents && (
          <p className="text-xs text-muted-foreground">
            Deposit {formatMoney(payment.depositAmountCents)} · returnable after
            approved deductions:{" "}
            <span className="font-medium text-foreground">
              {formatMoney(netCents)}
            </span>
          </p>
        )}

        {settled && (
          <Alert className="text-sm">
            Deposit return complete — this deal is now finished.
            {payment.depositReturnAmountCents != null && (
              <> Returned {formatMoney(payment.depositReturnAmountCents)}.</>
            )}
          </Alert>
        )}

        {/* Host, still needs to confirm they returned the deposit */}
        {!settled && isLandlord && !hostReturned && (
          <div className="space-y-3 rounded-lg border p-3">
            <p className="text-sm text-muted-foreground">
              Return the deposit to your guest directly (Lagedra never holds it),
              then record it here. By law you must return the deposit — or provide
              an itemized statement of deductions — within{" "}
              {DEPOSIT_RETURN_WINDOW_DAYS} days of move-out. The deal completes
              once the guest confirms receipt.
            </p>
            <div className="grid gap-3 sm:grid-cols-2">
              <div>
                <label className="mb-1 block text-sm font-medium">
                  Amount returned
                </label>
                <Input
                  type="number"
                  min="0"
                  step="0.01"
                  inputMode="decimal"
                  value={amountValue}
                  onChange={(e) => setAmountInput(e.target.value)}
                />
              </div>
              <div>
                <label className="mb-1 block text-sm font-medium">Method</label>
                <Select
                  value={method}
                  onChange={(e) => setMethod(e.target.value)}
                >
                  {returnMethods.map((m) => (
                    <option key={m} value={m}>
                      {m}
                    </option>
                  ))}
                </Select>
              </div>
            </div>

            {isPartialReturn ? (
              <>
                <Alert className="text-sm">
                  You are returning less than the{" "}
                  {formatMoney(depositCents)} deposit paid. Provide a valid
                  reason for the deductions and attach a photo of the damage.
                </Alert>
                <div>
                  <label className="mb-1 block text-sm font-medium">
                    Reason for deductions
                  </label>
                  <Textarea
                    value={note}
                    onChange={(e) => setNote(e.target.value)}
                    placeholder="Describe the damage or other lawful deductions…"
                    required
                  />
                </div>
                <div className="space-y-2">
                  <label className="block text-sm font-medium">
                    Damage photo
                  </label>
                  <EvidenceUpload
                    dealId={deal.dealId}
                    manifestId={evidenceManifestId}
                    manifestType="Damage"
                    accept="image/jpeg,image/png,image/gif,image/webp,image/heic,image/heif"
                    title="Damage photos"
                    canViewFiles
                    onManifestCreated={setEvidenceManifestId}
                    onSealed={setEvidenceManifestId}
                  />
                  <p className="text-xs text-muted-foreground">
                    Upload at least one photo, then seal it before confirming.
                  </p>
                </div>
              </>
            ) : (
              <div>
                <label className="mb-1 block text-sm font-medium">
                  Note{" "}
                  <span className="text-muted-foreground">(optional)</span>
                </label>
                <Textarea
                  value={note}
                  onChange={(e) => setNote(e.target.value)}
                  placeholder="Reference number, etc."
                />
              </div>
            )}

            <label className="flex items-start gap-2 text-sm">
              <Checkbox
                checked={confirmChecked}
                onCheckedChange={setConfirmChecked}
                className="mt-0.5"
              />
              <span>
                I confirm I returned this deposit to the guest directly
                {isPartialReturn
                  ? " and that the deduction reason and damage photo are accurate."
                  : "."}
              </span>
            </label>

            {(clientError || confirmReturned.isError) && (
              <Alert variant="destructive" className="text-sm">
                {clientError ?? apiError(confirmReturned.error)}
              </Alert>
            )}

            <Button disabled={!canSubmitHost} onClick={submitHostConfirm}>
              {confirmReturned.isPending
                ? "Saving…"
                : "Confirm deposit returned"}
            </Button>
          </div>
        )}

        {/* Host, waiting on the guest */}
        {!settled && isLandlord && hostReturned && !tenantReceived && (
          <p className="text-sm text-muted-foreground">
            You reported returning{" "}
            {payment.depositReturnAmountCents != null
              ? formatMoney(payment.depositReturnAmountCents)
              : "the deposit"}
            {payment.depositReturnMethod
              ? ` via ${payment.depositReturnMethod}`
              : ""}
            {payment.hostConfirmedDepositReturnedAt
              ? ` on ${formatDate(payment.hostConfirmedDepositReturnedAt)}`
              : ""}
            .
            {payment.depositReturnNote ? (
              <span className="mt-1 block">
                Deduction note: “{payment.depositReturnNote}”
              </span>
            ) : null}
            Waiting for the guest to confirm receipt.
          </p>
        )}

        {/* Host, guest confirmed but deals list not yet Closed */}
        {!settled && isLandlord && hostReturned && tenantReceived && (
          <p className="text-sm text-muted-foreground">
            The guest confirmed they received the deposit. Finishing the
            booking…
          </p>
        )}

        {/* Tenant, host has reported the return */}
        {!settled && isTenant && hostReturned && !tenantReceived && (
          <div className="space-y-3 rounded-lg border p-3">
            <p className="text-sm">
              Your host reported returning{" "}
              <span className="font-medium">
                {payment.depositReturnAmountCents != null
                  ? formatMoney(payment.depositReturnAmountCents)
                  : "your deposit"}
              </span>
              {payment.depositReturnMethod
                ? ` via ${payment.depositReturnMethod}`
                : ""}
              .
              {payment.depositReturnNote ? (
                <span className="mt-1 block text-muted-foreground">
                  “{payment.depositReturnNote}”
                </span>
              ) : null}
            </p>

            {payment.depositReturnEvidenceManifestId && (
              <EvidenceUpload
                dealId={deal.dealId}
                manifestId={payment.depositReturnEvidenceManifestId}
                manifestType="Damage"
                title="Damage photos on file"
                readOnly
                canViewFiles
              />
            )}

            {confirmReceived.isError && (
              <Alert variant="destructive" className="text-sm">
                {apiError(confirmReceived.error)}
              </Alert>
            )}

            <div className="flex flex-wrap items-center gap-3">
              <Button
                disabled={confirmReceived.isPending}
                onClick={() => confirmReceived.mutate(deal.dealId)}
              >
                {confirmReceived.isPending
                  ? "Confirming…"
                  : "I received my deposit"}
              </Button>
              <DisputeLink dealId={deal.dealId} />
            </div>
          </div>
        )}

        {/* Tenant, host hasn't returned yet */}
        {!settled && isTenant && !hostReturned && (
          <div className="space-y-2">
            <p className="text-sm text-muted-foreground">
              Your host hasn't confirmed returning your deposit yet. By law they
              should return it (or provide an itemized statement of deductions)
              within {DEPOSIT_RETURN_WINDOW_DAYS} days of move-out. We'll notify
              you when they do.
            </p>
            <DisputeLink dealId={deal.dealId} />
          </div>
        )}
      </CardContent>
    </Card>
  );
}
