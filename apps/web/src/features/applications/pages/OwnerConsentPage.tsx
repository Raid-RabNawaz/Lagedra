import { useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import {
  AlertCircle,
  ArrowRight,
  CheckCircle2,
  Loader2,
  ShieldCheck,
  XCircle,
} from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Alert } from "@/components/ui/alert";
import { BackLink } from "@/components/shared/BackLink";
import { applicationApi } from "@/features/applications/services/applicationApi";
import { getApiErrorMessage } from "@/api/errors";
import type { DealApplicationDto } from "@/api/types";

/**
 * Destination for the home owner's consent deep link. Auth is the HMAC
 * token, so this page is mounted outside RequireAuth — same pattern as
 * `/host/approve`.
 */
export const OwnerConsentPage = () => {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();

  const token = searchParams.get("token") ?? "";
  const declineToken = searchParams.get("declineToken") ?? "";
  const applicationId = searchParams.get("applicationId") ?? "";
  const listingTitle = searchParams.get("listingTitle");

  const [submitting, setSubmitting] = useState<"consent" | "decline" | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [result, setResult] = useState<"consented" | "declined" | null>(null);
  const [application, setApplication] = useState<DealApplicationDto | null>(null);

  const tokenError = !token && !declineToken
    ? "This link is missing its security token. Open the latest email from Lagedra and try again."
    : null;
  const displayError = error ?? tokenError;

  const runAction = async (kind: "consent" | "decline") => {
    const actionToken = kind === "consent" ? token : declineToken || token;
    if (!actionToken) return;

    setError(null);
    setSubmitting(kind);
    try {
      const data = kind === "consent"
        ? await applicationApi.consentOwnerTenancyByToken(actionToken)
        : await applicationApi.declineOwnerTenancyByToken(actionToken);
      setApplication(data);
      setResult(kind === "consent" ? "consented" : "declined");
    } catch (err) {
      setError(getApiErrorMessage(
        err,
        kind === "consent"
          ? "Failed to record consent. The link may have expired."
          : "Failed to decline. The link may have expired.",
      ));
    } finally {
      setSubmitting(null);
    }
  };

  if (result) {
    const consented = result === "consented";
    return (
      <div className="mx-auto max-w-lg px-4 py-16 sm:px-6 lg:px-8">
        <Card className={consented ? "border-emerald-200" : "border-destructive/30"}>
          <CardHeader className="pb-3">
            <CardTitle className="flex items-center gap-2">
              {consented ? (
                <CheckCircle2 className="h-5 w-5 text-emerald-600" />
              ) : (
                <XCircle className="h-5 w-5 text-destructive" />
              )}
              {consented ? "Consent recorded" : "Stay declined"}
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-4 text-sm">
            <p className="text-muted-foreground">
              {consented
                ? `You consented to this tenancy for ${listingTitle ?? "the listing"}. The property manager can now accept the booking. You will be named as Landlord on the lease.`
                : `You declined this tenancy for ${listingTitle ?? "the listing"}. The guest and property manager have been notified.`}
            </p>
            <div className="flex flex-col gap-2 sm:flex-row">
              {application?.applicationId && (
                <Button
                  className="gap-2"
                  onClick={() => navigate(`/app/applications/${application.applicationId}`)}
                >
                  Open the request
                  <ArrowRight className="h-4 w-4" />
                </Button>
              )}
              <BackLink
                fallbackTo="/app/owner-consents"
                variant="button"
                label="Owner consent inbox"
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
            Owner consent
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-4 text-sm">
          <p className="text-muted-foreground">
            A guest requested a stay of more than 30 days
            {applicationId ? ` (${applicationId.slice(0, 8)}…)` : ""} at{" "}
            <span className="font-medium text-foreground">
              {listingTitle ?? "your property"}
            </span>
            . California law requires the home owner&apos;s consent when a
            property manager lists the home.
          </p>
          <p className="text-muted-foreground">
            Consenting authorizes the property manager to enter the lease on
            your behalf. You will be named as Landlord on the lease. Declining
            closes the request.
          </p>

          {displayError && (
            <Alert variant="destructive" className="text-sm">
              <AlertCircle className="h-4 w-4" />
              <span className="ml-2">{displayError}</span>
            </Alert>
          )}

          <Button
            onClick={() => void runAction("consent")}
            disabled={submitting !== null || !token}
            className="w-full gap-2"
            size="lg"
          >
            {submitting === "consent" ? (
              <>
                <Loader2 className="h-4 w-4 animate-spin" />
                Recording consent…
              </>
            ) : (
              <>
                <CheckCircle2 className="h-4 w-4" />
                I consent to this stay
              </>
            )}
          </Button>
          <Button
            onClick={() => void runAction("decline")}
            disabled={submitting !== null || !declineToken}
            variant="outline"
            className="w-full gap-2 text-red-600 hover:bg-red-50 hover:text-red-700"
          >
            {submitting === "decline" ? (
              <>
                <Loader2 className="h-4 w-4 animate-spin" />
                Declining…
              </>
            ) : (
              <>
                <XCircle className="h-4 w-4" />
                Decline this stay
              </>
            )}
          </Button>
        </CardContent>
      </Card>
    </div>
  );
};
