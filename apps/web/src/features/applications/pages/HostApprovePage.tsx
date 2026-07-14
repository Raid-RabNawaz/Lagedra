import { useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import {
  ShieldCheck,
  CheckCircle2,
  AlertCircle,
  ArrowRight,
  Loader2,
  Lock,
  ReceiptText,
} from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Alert } from "@/components/ui/alert";
import { BackLink } from "@/components/shared/BackLink";
import { http } from "@/api/http";
import { getApiErrorMessage } from "@/api/errors";
import type { DealApplicationDto } from "@/api/types";

type ApproveResponse = DealApplicationDto;

/**
 * Destination for the host's "Approve" deep link in the
 * application-submitted email. Reads `token`, `applicationId`, and
 * `listingTitle` (optional) from the URL, then lets the host one-tap-accept
 * without going through the in-app inbox.
 *
 * Under the predetermined-deposit flow there is no deposit input: the
 * deposit was fixed per verification tier and snapshotted at request time.
 * Accepting here seals the Truth Surface (the secure email-link click is
 * the host's recorded consent), charges the guest's saved card off-session,
 * and activates the booking automatically.
 *
 * The endpoint is anonymous (auth via the HMAC token), so the page is
 * mounted outside the `RequireAuth` tree.
 */
export const HostApprovePage = () => {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();

  const token = searchParams.get("token") ?? "";
  const applicationId = searchParams.get("applicationId") ?? "";
  const listingTitle = searchParams.get("listingTitle");

  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [approved, setApproved] = useState<ApproveResponse | null>(null);

  const tokenError = !token
    ? "This approval link is missing its security token. Open the latest email from Lagedra and try again."
    : null;
  const displayError = error ?? tokenError;

  const handleApprove = async () => {
    setError(null);
    if (!token) return;

    setSubmitting(true);
    try {
      const response = await http.post<ApproveResponse>(
        "/v1/actions/approve-application",
        { token },
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
              Booking accepted
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-4 text-sm">
            <p className="text-muted-foreground">
              You accepted the booking for{" "}
              <span className="font-medium text-foreground">
                {listingTitle ?? approved.listingId.slice(0, 8) + "…"}
              </span>
              . The Truth Surface has been sealed with both consents and the
              guest's saved card is being charged. The booking activates as soon
              as payment clears — we'll notify you both.
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
              <BackLink
                fallbackTo="/app/applications"
                variant="button"
                label="Back to inbox"
              />
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
            One-tap accept
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-4 text-sm">
          <p className="text-muted-foreground">
            You're accepting the booking request
            {applicationId ? ` ${applicationId.slice(0, 8)}…` : ""} for{" "}
            <span className="font-medium text-foreground">
              {listingTitle ?? "your listing"}
            </span>
            .
          </p>

          <div className="flex items-start gap-2 rounded-md border bg-muted/30 p-3 text-xs text-muted-foreground">
            <Lock className="mt-0.5 h-3.5 w-3.5 shrink-0" />
            <span>
              The security deposit was set automatically from your listing's
              verification-tier rules. Accepting seals an immutable, signed Truth
              Surface (your click here is your recorded consent), charges the
              guest's saved card off-session, and activates the booking. The rent
              and deposit are paid directly to your Stripe account; Lagedra only
              deducts its service fee and the insurance premium. You return the
              deposit to the guest directly after move-out, and the booking only
              completes once you confirm the return and the guest confirms receipt.
            </span>
          </div>

          <div className="flex items-start gap-2 rounded-md border bg-muted/30 p-3 text-xs text-muted-foreground">
            <ReceiptText className="mt-0.5 h-3.5 w-3.5 shrink-0" />
            <span>
              Once the booking is active, a recurring monthly platform fee
              applies for this booking and is charged automatically to your card
              on file. You can see the current rate and every deduction under{" "}
              <span className="font-medium text-foreground">Platform fees</span>{" "}
              in your Lagedra dashboard.
            </span>
          </div>

          {displayError && (
            <Alert variant="destructive" className="text-sm">
              <AlertCircle className="h-4 w-4" />
              <span className="ml-2">{displayError}</span>
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
                Accepting…
              </>
            ) : (
              <>
                <CheckCircle2 className="h-4 w-4" />
                Accept &amp; seal booking
              </>
            )}
          </Button>

          <p className="text-[11px] text-muted-foreground">
            Accepting here is the same as clicking Accept in the Lagedra app —
            it's signed by the secure token in this email link and triggers all
            the same notifications and downstream steps.
          </p>
        </CardContent>
      </Card>
    </div>
  );
};
