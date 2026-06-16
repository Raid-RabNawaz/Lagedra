import { useMemo, useState } from "react";
import { Send, Loader2, Plus, Trash2, DollarSign, CheckCircle2 } from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Select } from "@/components/ui/select";
import { formatDate, formatMoney } from "@/utils/format";
import { useIssueDecision } from "@/features/arbitration/hooks/useArbitration";
import type {
  CaseDto,
  DecisionOutcome,
  DecisionSeverity,
  DecisionPenaltyDto,
  PenaltyType,
} from "@/api/types";
import {
  defaultPenaltyParty,
  outcomeLabels,
  PENALTY_TYPES,
  penaltyRequiresAmount,
  penaltyTypeLabels,
  severityGuidance,
  severityLabels,
} from "@/features/arbitration/lib/verdictLabels";

type PenaltyDraft = {
  id: string;
  partyUserId: string;
  penaltyType: PenaltyType;
  amountDollars: string;
  description: string;
};

const newPenaltyRow = (partyUserId = ""): PenaltyDraft => ({
  id: crypto.randomUUID(),
  partyUserId,
  penaltyType: "AccountWarning",
  amountDollars: "",
  description: "",
});

function partyLabel(userId: string, c: CaseDto) {
  if (c.landlordUserId && userId === c.landlordUserId) return "Host";
  if (c.tenantUserId && userId === c.tenantUserId) return "Guest";
  return "Party";
}

function DecisionDisplay({
  c,
  decision,
  title = "Decision",
}: {
  c: CaseDto;
  decision: NonNullable<CaseDto["decision"]>;
  title?: string;
}) {
  const d = decision;
  return (
    <Card>
      <CardHeader className="pb-3">
        <CardTitle className="text-base flex items-center gap-2">
          <CheckCircle2 className="h-4 w-4 text-green-600" />
          {title}
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-3">
        {d.isStructured && d.outcome && d.severity && (
          <div className="flex flex-wrap gap-2">
            <Badge variant="default">{outcomeLabels[d.outcome]}</Badge>
            <Badge variant="secondary">{severityLabels[d.severity]} severity</Badge>
          </div>
        )}
        {!d.isStructured && <Badge variant="outline">Narrative verdict</Badge>}
        <p className="text-sm whitespace-pre-wrap">{d.summary}</p>
        {d.awardAmount != null && (
          <div className="flex items-center gap-2 text-sm">
            <DollarSign className="h-4 w-4 text-muted-foreground" />
            <span className="font-medium">Award: ${d.awardAmount.toLocaleString()}</span>
          </div>
        )}
        {d.penalties.length > 0 && (
          <div className="space-y-2 pt-2 border-t">
            <p className="text-xs font-medium text-muted-foreground uppercase tracking-wide">
              Penalties
            </p>
            <ul className="space-y-2">
              {d.penalties.map((p) => (
                <PenaltyLine key={p.penaltyId} p={p} c={c} />
              ))}
            </ul>
          </div>
        )}
        <p className="text-xs text-muted-foreground">Decided {formatDate(d.decidedAt)}</p>
      </CardContent>
    </Card>
  );
}

function PenaltyLine({ p, c }: { p: DecisionPenaltyDto; c: CaseDto }) {
  return (
    <li className="text-sm rounded-md border px-3 py-2">
      <span className="font-medium">{partyLabel(p.partyUserId, c)}</span>
      {" · "}
      {penaltyTypeLabels[p.penaltyType]}
      {p.amountCents != null && p.amountCents > 0 && (
        <> · {formatMoney(p.amountCents)}</>
      )}
      {p.description && <p className="text-muted-foreground mt-1">{p.description}</p>}
    </li>
  );
}

type Props = {
  c: CaseDto;
  canDecide: boolean;
};

export function VerdictForm({ c, canDecide }: Props) {
  const issueDecision = useIssueDecision();
  const [mode, setMode] = useState<"structured" | "narrative">("structured");
  const [summary, setSummary] = useState("");
  const [awardAmount, setAwardAmount] = useState("");
  const [outcome, setOutcome] = useState<DecisionOutcome>("TenantFavored");
  const [severity, setSeverity] = useState<DecisionSeverity>("Medium");
  const [penalties, setPenalties] = useState<PenaltyDraft[]>([newPenaltyRow()]);

  const guidance = useMemo(
    () => (mode === "structured" ? severityGuidance(severity, outcome) : null),
    [mode, severity, outcome],
  );

  const isFinalStatus = c.status === "Decided" || c.status === "Closed";
  const canIssueNewVerdict = canDecide && c.status === "UnderReview";

  if (isFinalStatus && c.decision) {
    return <DecisionDisplay c={c} decision={c.decision} />;
  }

  if (!canIssueNewVerdict) {
    if (c.priorDecision) {
      return (
        <DecisionDisplay
          c={c}
          decision={c.priorDecision}
          title="Prior verdict (under appeal review)"
        />
      );
    }
    return null;
  }

  const partiesAvailable = Boolean(c.landlordUserId && c.tenantUserId);

  const addPenaltyForOutcome = () => {
    const party = defaultPenaltyParty(outcome, c.landlordUserId!, c.tenantUserId!);
    setPenalties((rows) => [...rows, newPenaltyRow(party ?? "")]);
  };

  const buildPayload = () => {
    const narrativeSummary =
      mode === "narrative"
        ? summary.trim()
        : summary.trim() ||
          `${outcomeLabels[outcome]} — ${severityLabels[severity]} severity.`;

    return {
      caseId: c.caseId,
      decisionSummary: narrativeSummary,
      awardAmount:
        c.tier === "BindingArbitration" && awardAmount
          ? Number(awardAmount)
          : null,
      isStructured: mode === "structured",
      outcome: mode === "structured" ? outcome : null,
      severity: mode === "structured" ? severity : null,
      penalties:
        mode === "structured"
          ? penalties
              .filter((p) => p.partyUserId)
              .map((p) => ({
                partyUserId: p.partyUserId,
                penaltyType: p.penaltyType,
                amountCents: p.amountDollars
                  ? Math.round(Number(p.amountDollars) * 100)
                  : null,
                description: p.description.trim() || null,
              }))
          : [],
    };
  };

  const canSubmit =
    mode === "narrative"
      ? summary.trim().length > 0
      : partiesAvailable && (summary.trim().length > 0 || outcome !== "Dismissed");

  return (
    <div className="space-y-4">
      {c.priorDecision && (
        <DecisionDisplay
          c={c}
          decision={c.priorDecision}
          title="Prior verdict (appealed)"
        />
      )}
      <Card>
      <CardHeader className="pb-3">
        <CardTitle className="text-base">
          {c.priorDecision ? "Issue new verdict" : "Issue verdict"}
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="flex gap-2">
          <Button
            type="button"
            size="sm"
            variant={mode === "structured" ? "default" : "outline"}
            onClick={() => setMode("structured")}
          >
            Structured
          </Button>
          <Button
            type="button"
            size="sm"
            variant={mode === "narrative" ? "default" : "outline"}
            onClick={() => setMode("narrative")}
          >
            Narrative only
          </Button>
        </div>

        {mode === "structured" ? (
          <>
            {!partiesAvailable && (
              <p className="text-sm text-destructive">
                Cannot load host/guest for this deal. Use narrative only or refresh the case.
              </p>
            )}
            <div className="grid gap-3 sm:grid-cols-2">
              <div>
                <label className="text-xs font-medium text-muted-foreground">Outcome</label>
                <Select
                  className="mt-1 w-full"
                  value={outcome}
                  onChange={(e) => setOutcome(e.target.value as DecisionOutcome)}
                >
                  {(Object.keys(outcomeLabels) as DecisionOutcome[]).map((o) => (
                    <option key={o} value={o}>
                      {outcomeLabels[o]}
                    </option>
                  ))}
                </Select>
              </div>
              <div>
                <label className="text-xs font-medium text-muted-foreground">Severity</label>
                <Select
                  className="mt-1 w-full"
                  value={severity}
                  onChange={(e) => setSeverity(e.target.value as DecisionSeverity)}
                >
                  {(Object.keys(severityLabels) as DecisionSeverity[]).map((s) => (
                    <option key={s} value={s}>
                      {severityLabels[s]}
                    </option>
                  ))}
                </Select>
              </div>
            </div>
            {guidance && (
              <p className="text-xs text-muted-foreground rounded-md bg-muted px-3 py-2">
                {guidance}
              </p>
            )}
            <div>
              <label className="text-xs font-medium text-muted-foreground">
                Additional notes (optional)
              </label>
              <textarea
                className="mt-1 w-full rounded-lg border bg-background px-3 py-2 text-sm min-h-[60px] resize-y"
                placeholder="Supplemental explanation for the parties..."
                value={summary}
                onChange={(e) => setSummary(e.target.value)}
              />
            </div>
            <div className="space-y-2">
              <div className="flex items-center justify-between">
                <span className="text-xs font-medium text-muted-foreground">Penalties</span>
                <Button type="button" size="sm" variant="outline" onClick={addPenaltyForOutcome}>
                  <Plus className="h-3.5 w-3.5 mr-1" />
                  Add penalty
                </Button>
              </div>
              {penalties.map((row, index) => (
                <div key={row.id} className="rounded-lg border p-3 space-y-2">
                  <div className="flex justify-between items-center">
                    <span className="text-xs font-medium">Penalty {index + 1}</span>
                    {penalties.length > 1 && (
                      <Button
                        type="button"
                        variant="ghost"
                        size="sm"
                        onClick={() =>
                          setPenalties((rows) => rows.filter((r) => r.id !== row.id))
                        }
                      >
                        <Trash2 className="h-3.5 w-3.5" />
                      </Button>
                    )}
                  </div>
                  <div className="grid gap-2 sm:grid-cols-2">
                    <Select
                      value={row.partyUserId}
                      onChange={(e) =>
                        setPenalties((rows) =>
                          rows.map((r) =>
                            r.id === row.id ? { ...r, partyUserId: e.target.value } : r,
                          ),
                        )
                      }
                    >
                      <option value="">Select party…</option>
                      {c.landlordUserId && (
                        <option value={c.landlordUserId}>Host</option>
                      )}
                      {c.tenantUserId && (
                        <option value={c.tenantUserId}>Guest</option>
                      )}
                    </Select>
                    <Select
                      value={row.penaltyType}
                      onChange={(e) =>
                        setPenalties((rows) =>
                          rows.map((r) =>
                            r.id === row.id
                              ? { ...r, penaltyType: e.target.value as PenaltyType }
                              : r,
                          ),
                        )
                      }
                    >
                      {PENALTY_TYPES.map((t) => (
                        <option key={t} value={t}>
                          {penaltyTypeLabels[t]}
                        </option>
                      ))}
                    </Select>
                  </div>
                  {penaltyRequiresAmount(row.penaltyType) && (
                    <input
                      type="number"
                      min="0"
                      step="0.01"
                      className="w-full rounded-lg border bg-background px-3 py-2 text-sm"
                      placeholder="Amount (USD)"
                      value={row.amountDollars}
                      onChange={(e) =>
                        setPenalties((rows) =>
                          rows.map((r) =>
                            r.id === row.id ? { ...r, amountDollars: e.target.value } : r,
                          ),
                        )
                      }
                    />
                  )}
                  <input
                    type="text"
                    className="w-full rounded-lg border bg-background px-3 py-2 text-sm"
                    placeholder="Description (optional)"
                    value={row.description}
                    onChange={(e) =>
                      setPenalties((rows) =>
                        rows.map((r) =>
                          r.id === row.id ? { ...r, description: e.target.value } : r,
                        ),
                      )
                    }
                  />
                </div>
              ))}
            </div>
          </>
        ) : (
          <textarea
            className="w-full rounded-lg border bg-background px-3 py-2 text-sm min-h-[100px] resize-y"
            placeholder="Full narrative verdict when structured outcome does not apply..."
            value={summary}
            onChange={(e) => setSummary(e.target.value)}
          />
        )}

        {c.tier === "BindingArbitration" && (
          <div>
            <label className="text-xs font-medium text-muted-foreground">
              Binding award amount (optional, USD)
            </label>
            <input
              type="number"
              className="mt-1 w-full rounded-lg border bg-background px-3 py-2 text-sm"
              placeholder="Total award amount"
              value={awardAmount}
              onChange={(e) => setAwardAmount(e.target.value)}
            />
          </div>
        )}

        <Button
          size="sm"
          disabled={!canSubmit || issueDecision.isPending}
          onClick={() => issueDecision.mutate(buildPayload())}
        >
          {issueDecision.isPending ? (
            <Loader2 className="h-4 w-4 animate-spin mr-2" />
          ) : (
            <Send className="h-4 w-4 mr-2" />
          )}
          Issue verdict
        </Button>
      </CardContent>
    </Card>
    </div>
  );
}
