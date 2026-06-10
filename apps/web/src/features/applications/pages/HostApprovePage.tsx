import { useEffect, useMemo, useState } from "react";
import { Link, useNavigate, useSearchParams } from "react-router-dom";
import {
  ShieldCheck,
  CheckCircle2,
  AlertCircle,
  ArrowRight,
  Loader2,
} from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Alert } from "@/components/ui/alert";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { http } from "@/api/http";
import { getApiErrorMessage } from "@/api/errors";
import { formatMoney } from "@/utils/format";
import type { DealApplicationDto } from "@/api/types";

type ApproveResponse = DealApplicationDto;

/**
 * Phase 16.10 — destination for the host's "Approve" deep link in the
 * application-submitted email. Reads `token`, `applicationId`,
 * `depositCents`, and `listingTitle` (optional) from the URL, then
 * lets the host one-tap-approve without going through the in-app inbox.
 *
 * The endpoint is anonymous (auth via the HMAC token), so the page is
 * mounted outside the `RequireAuth` tree.
 */
export const HostApprovePage = () => {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();

  const token = searchParams.get("token") ?? "";
  const applicationId = searchParams.get("applicationId") ?? "";
  const queryDeposit = searchParams.get("depositCents");
  const listingTitle = searchParams.get("listingTitle");

  const initialDeposit = useMemo(() => {
    const parsed = Number(queryDeposit);
    return Number.isFinite(parsed) && parsed > 0 ? parsed : 0;
  }, [queryDeposit]);

  const [depositDollars, setDepositDollars] = useState<string>(
    initialDeposit > 0 ? (initialDeposit / 100).toFixed(2) : "",
  );
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [approved, setApproved] = useState<ApproveResponse | null>(null);

  // Token presence is the bare minimum precondition; everything else
  // is optional context for the UI.
  useEffect(() => {
    if (!token) {
      setError(
        "This approval link is missing its security token. Open the latest email from Lagedra and try again.",
      );
    }
  }, [token]);

  const handleApprove = async () => {
    setError(null);
    if (!token) return;

    const depositCents = Math.round(parseFloat(depositDollars || "0") * 100);
    if (!Number.isFinite(depositCents) || depositCents <= 0) {
      setError("Enter the security deposit you'd like to charge (in dollars).");
      return;
    }

    setSubmitting(true);
    try {
      const response = await http.post<ApproveResponse>(
        "/v1/actions/approve-application",
        { token, depositAmountCents: depositCents },
      );
      setApproved(response.data);
    } catch (err) {
      setError(getApiErrorMessage(err, "Failed to approve. The link may have expired."));
    } finally {
      setSubmitting(false);
    }
  };

  if (approved) {
    return (
      <div className="mx-auto max-w-lg px-4 py-16 sm:px-6 lg:px-8">
        <Card className="border-emerald-200">
          <CardHeader className="pb-3">
            <CardTitle className="flex items-center gap-2">
              <CheckCircle2 className="h-5 w-5 text-emerald-600" />
              Application approved
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-4 text-sm">
            <p className="text-muted-foreground">
              You approved the booking for{" "}
              <span className="font-medium text-foreground">
                {listingTitle ?? approved.listingId.slice(0, 8) + "…"}
              </span>
              . The Truth Surface has been auto-confirmed on your behalf and
              the tenant has been notified to confirm and pay.
            </p>
            <div className="flex flex-col gap-2 sm:flex-row">
              {approved.dealId && (
                <Button
                  className="gap-2"
                  onClick={() => navigate(`/app/deals/${approved.dealId}`)}
                >
                  Open the deal
                  <ArrowRight className="h-4 w-4" />
                </Button>
              )}
              <Link to="/app/applications">
                <Button variant="outline">Back to inbox</Button>
              </Link>
            </div>
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-lg px-4 py-16 sm:px-6 lg:px-8">
      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="flex items-center gap-2">
            <ShieldCheck className="h-5 w-5 text-primary" />
            One-tap approve
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-4 text-sm">
          <p className="text-muted-foreground">
            You're approving the booking application
            {applicationId
              ? ` ${applicationId.slice(0, 8)}…`
              : ""} for{" "}
            <span className="font-medium text-foreground">
              {listingTitle ?? "your listing"}
            </span>
            . Once approved, Lagedra automatically confirms the Truth
            Surface on your behalf and prompts the tenant to complete
            payment.
          </p>

          <div className="space-y-1.5">
            <Label htmlFor="deposit">Security deposit (USD)</Label>
            <Input
              id="deposit"
              type="number"
              inputMode="decimal"
              min="0"
              step="0.01"
              value={depositDollars}
              onChange={(e) => setDepositDollars(e.target.value)}
              placeholder="e.g. 1500.00"
            />
            {initialDeposit > 0 && (
              <p className="text-[11px] text-muted-foreground">
                Default from listing: {formatMoney(initialDeposit)}
              </p>
            )}
          </div>

          {error && (
            <Alert variant="destructive" className="text-sm">
              <AlertCircle className="h-4 w-4" />
              <span className="ml-2">{error}</span>
            </Alert>
          )}

          <Button
            onClick={handleApprove}
            disabled={submitting || !token}
            className="w-full gap-2"
            size="lg"
          >
            {submitting ? (
              <>
                <Loader2 className="h-4 w-4 animate-spin" />
                Approving…
              </>
            ) : (
              <>
                <CheckCircle2 className="h-4 w-4" />
                Approve booking
              </>
            )}
          </Button>

          <p className="text-[11px] text-muted-foreground">
            Approving here is the same as clicking Approve in the Lagedra
            app — it's signed by the secure token in this email link and
            triggers all the same notifications and downstream steps.
          </p>
        </CardContent>
      </Card>
    </div>
  );
};
