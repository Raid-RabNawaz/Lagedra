import { useState } from "react";
import { useParams, Link } from "react-router-dom";
import {
  ArrowLeft,
  ArrowRight,
  ShieldCheck,
  ShieldAlert,
  Clock,
  CreditCard,
  FileCheck,
  CheckCircle2,
} from "lucide-react";
import { useAuthStore } from "@/app/auth/authStore";
import {
  useSnapshot,
  useConfirmSnapshot,
} from "@/features/truth-surface/hooks/useTruthSurface";
import { useMyDeals } from "@/features/deals/hooks/useDeals";
import { TruthSurfaceStatusBadge } from "@/features/truth-surface/components/TruthSurfaceStatusBadge";
import { TruthSnapshotViewer } from "@/features/truth-surface/components/TruthSnapshotViewer";
import { AgreementDocument } from "@/features/truth-surface/components/AgreementDocument";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Checkbox } from "@/components/ui/checkbox";
import { Alert } from "@/components/ui/alert";
import { Separator } from "@/components/ui/separator";
import { Label } from "@/components/ui/label";
import { Loader } from "@/components/shared/Loader";
import { formatDate } from "@/utils/format";
import type { ConfirmingParty } from "@/api/types";

export const TruthSurfaceConfirmationPage = () => {
  const { snapshotId } = useParams<{ snapshotId: string }>();
  const user = useAuthStore((s) => s.user);

  const { data: snapshot, isLoading, isError } = useSnapshot(snapshotId);
  const confirmMutation = useConfirmSnapshot();

  const { data: deals } = useMyDeals("all");
  const deal = deals?.find((d) => d.dealId === snapshot?.dealId);
  const isLandlord = !!user && !!deal && user.userId === deal.landlordUserId;
  const isTenant = !!user && !!deal && user.userId === deal.tenantUserId;

  const [termsConfirmed, setTermsConfirmed] = useState(false);
  const [platformAccepted, setPlatformAccepted] = useState(false);

  const isConfirmed = snapshot?.status === "Confirmed";
  const isSuperseded = snapshot?.status === "Superseded";
  const isSealed = isConfirmed || isSuperseded;

  const myParty: ConfirmingParty | null = isLandlord
    ? "Landlord"
    : isTenant
      ? "Tenant"
      : null;

  const alreadyConfirmedByMe =
    (isLandlord && snapshot?.landlordConfirmed) ||
    (isTenant && snapshot?.tenantConfirmed);

  const isPending =
    snapshot?.status === "PendingBothConfirmations" ||
    snapshot?.status === "PendingLandlordConfirmation" ||
    snapshot?.status === "PendingTenantConfirmation";

  const canConfirm = isPending && myParty && !alreadyConfirmedByMe;

  const canSubmit = termsConfirmed && platformAccepted;

  const handleConfirm = async () => {
    if (!snapshotId || !myParty) {
      return;
    }
    await confirmMutation.mutateAsync({
      snapshotId,
      party: myParty,
    });
  };

  if (isLoading) {
    return <Loader fullPage label="Loading Truth Surface..." />;
  }

  if (isError || !snapshot) {
    return (
      <div className="mx-auto max-w-3xl px-4 py-16 sm:px-6 lg:px-8 text-center">
        <p className="text-destructive font-medium">
          Truth Surface snapshot not found or failed to load.
        </p>
        <Link
          to="/app"
          className="inline-flex items-center gap-1.5 mt-4 text-sm text-muted-foreground hover:text-foreground"
        >
          <ArrowLeft className="h-4 w-4" />
          Back to dashboard
        </Link>
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-3xl px-4 py-8 sm:px-6 lg:px-8">
      <Link
        to={`/app/deals/${snapshot.dealId}`}
        className="inline-flex items-center gap-1.5 text-sm text-muted-foreground hover:text-foreground transition-colors mb-6"
      >
        <ArrowLeft className="h-4 w-4" />
        Back to deal
      </Link>

      <div className="flex items-center gap-3 mb-2">
        <h1 className="text-2xl font-bold tracking-tight">Booking agreement</h1>
        <TruthSurfaceStatusBadge status={snapshot.status} />
      </div>
      <p className="text-sm text-muted-foreground mb-6">
        {isSealed
          ? "This is the binding, cryptographically sealed agreement for your booking. Both parties have signed the terms below."
          : "Review the terms below carefully. Confirming seals this as the binding, cryptographically signed agreement for your booking."}
      </p>

      {/* System notice after confirmation */}
      {isSealed && (
        <Alert className="mb-6 border-blue-300 bg-blue-50 text-blue-800">
          <ShieldAlert className="h-4 w-4" />
          <span className="ml-2 text-sm">
            Inquiry Service is closed for this deal. The agreement above is
            cryptographically sealed; the seal timestamp and inquiry-closed
            flag are recorded as observed metadata alongside (not inside) the
            hashed payload.
          </span>
        </Alert>
      )}

      {/* Already confirmed by this party */}
      {alreadyConfirmedByMe && !isSealed && (
        <Alert className="mb-6 border-emerald-300 bg-emerald-50 text-emerald-800">
          <ShieldCheck className="h-4 w-4" />
          <span className="ml-2 text-sm">
            You have already confirmed this Truth Surface. Waiting for the other
            party to confirm.
          </span>
        </Alert>
      )}

      {/* Metadata */}
      <div className="flex flex-wrap gap-4 text-sm text-muted-foreground mb-6">
        <span className="flex items-center gap-1">
          <Clock className="h-3.5 w-3.5" />
          Created: {formatDate(snapshot.createdAt)}
        </span>
        {snapshot.sealedAt && (
          <span className="flex items-center gap-1">
            <FileCheck className="h-3.5 w-3.5" />
            Sealed: {formatDate(snapshot.sealedAt)}
          </span>
        )}
      </div>

      {/* Agreement terms (curated, read-only) */}
      <div className="mb-6">
        <AgreementDocument canonicalContent={snapshot.canonicalContent} />
      </div>

      {/* Cryptographic proof viewer */}
      <div className="mb-6">
        <TruthSnapshotViewer snapshot={snapshot} />
      </div>

      {/* Confirmation section */}
      {canConfirm && (
        <>
          <Card className="mb-6">
            <CardHeader className="pb-3">
              <CardTitle className="text-base">Confirm Truth Surface</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="flex items-start gap-3">
                <Checkbox
                  id="terms-confirmed"
                  checked={termsConfirmed}
                  onCheckedChange={(v) => setTermsConfirmed(v === true)}
                  className="mt-0.5"
                />
                <Label
                  htmlFor="terms-confirmed"
                  className="text-sm leading-snug cursor-pointer"
                >
                  I have reviewed all deal terms above and confirm that the
                  information is accurate to the best of my knowledge.
                </Label>
              </div>

              <Separator />

              <div className="flex items-start gap-3">
                <Checkbox
                  id="platform-accepted"
                  checked={platformAccepted}
                  onCheckedChange={(v) => setPlatformAccepted(v === true)}
                  className="mt-0.5"
                />
                <Label
                  htmlFor="platform-accepted"
                  className="text-sm leading-snug cursor-pointer text-muted-foreground"
                >
                  I understand that once confirmed, this Truth Surface becomes
                  an immutable, cryptographically signed record of the deal
                  terms and the Inquiry Service will be permanently closed.
                </Label>
              </div>
            </CardContent>
          </Card>

          <div className="space-y-2">
            {!canSubmit && (
              <p className="text-xs text-muted-foreground">
                You must accept both checkboxes above before confirming.
              </p>
            )}
            <Button
              onClick={handleConfirm}
              disabled={!canSubmit || confirmMutation.isPending}
              className="w-full gap-2"
              size="lg"
            >
              <CheckCircle2 className="h-4 w-4" />
              {confirmMutation.isPending
                ? "Confirming..."
                : `Confirm as ${myParty}`}
            </Button>
          </div>

          {confirmMutation.isError && (
            <Alert variant="destructive" className="mt-4">
              {(confirmMutation.error as Error)?.message ??
                "Failed to confirm. Please try again."}
            </Alert>
          )}
        </>
      )}

      {/* Read-only view when sealed */}
      {isSealed && (
        <>
          <Separator className="my-6" />
          <div className="text-center py-8">
            <ShieldCheck className="h-12 w-12 text-emerald-600 mx-auto mb-3" />
            <h2 className="text-lg font-semibold">
              Truth Surface Confirmed
            </h2>
            <p className="text-sm text-muted-foreground mt-1 max-w-md mx-auto">
              This snapshot has been cryptographically sealed. Both parties
              confirmed the deal terms. The record is immutable and
              tamper-evident.
            </p>

            <div className="mt-6 flex flex-col sm:flex-row items-center justify-center gap-3">
              {isTenant && (
                <Link to={`/app/deals/${snapshot.dealId}/checkout`}>
                  <Button size="lg" className="gap-2">
                    <CreditCard className="h-4 w-4" />
                    Proceed to Payment
                    <ArrowRight className="h-4 w-4" />
                  </Button>
                </Link>
              )}
              <Link to={`/app/deals/${snapshot.dealId}`}>
                <Button variant={isTenant ? "outline" : "default"} size="lg" className="gap-2">
                  View Deal
                  <ArrowRight className="h-4 w-4" />
                </Button>
              </Link>
            </div>
          </div>
        </>
      )}
    </div>
  );
};
