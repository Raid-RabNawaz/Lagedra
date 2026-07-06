import { useState } from "react";
import {
  Shield,
  CheckCircle2,
  XCircle,
  ChevronDown,
  ChevronUp,
  Fingerprint,
  Clock,
  Lock,
  Download,
} from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Separator } from "@/components/ui/separator";
import {
  useSnapshotProof,
  useDownloadSnapshotReceipt,
} from "@/features/truth-surface/hooks/useTruthSurface";
import { formatDate } from "@/utils/format";
import type { TruthSurfaceDto } from "@/api/types";

type Props = {
  snapshot: TruthSurfaceDto;
};

export const TruthSnapshotViewer = ({ snapshot }: Props) => {
  const [showProof, setShowProof] = useState(false);
  const isSealed =
    snapshot.status === "Confirmed" || snapshot.status === "Superseded";

  const { data: proof, isLoading: proofLoading } = useSnapshotProof(
    snapshot.snapshotId,
    showProof && isSealed,
  );
  const downloadReceipt = useDownloadSnapshotReceipt();

  // Embedded proof returned with the snapshot has already been re-verified by
  // the API at read time; we no longer trust it implicitly.
  const embeddedProofValid = snapshot.proof?.isValid ?? null;

  return (
    <Card>
      <CardHeader className="pb-3">
        <CardTitle className="text-base flex items-center gap-2">
          <Fingerprint className="h-4 w-4" />
          Cryptographic Proof
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-4">
        <p className="text-sm text-muted-foreground">
          Every term in this agreement is sealed with a tamper-evident
          signature. Use the controls below to verify integrity or download a
          copy for your records.
        </p>
        <div className="grid grid-cols-2 gap-3 text-sm">
          <div>
            <span className="text-muted-foreground">Landlord confirmed</span>
            <div className="mt-0.5">
              {snapshot.landlordConfirmed ? (
                <Badge variant="accent" className="text-xs gap-1">
                  <CheckCircle2 className="h-3 w-3" /> Yes
                </Badge>
              ) : (
                <Badge variant="secondary" className="text-xs gap-1">
                  <Clock className="h-3 w-3" /> Pending
                </Badge>
              )}
            </div>
          </div>
          <div>
            <span className="text-muted-foreground">Tenant confirmed</span>
            <div className="mt-0.5">
              {snapshot.tenantConfirmed ? (
                <Badge variant="accent" className="text-xs gap-1">
                  <CheckCircle2 className="h-3 w-3" /> Yes
                </Badge>
              ) : (
                <Badge variant="secondary" className="text-xs gap-1">
                  <Clock className="h-3 w-3" /> Pending
                </Badge>
              )}
            </div>
          </div>
        </div>

        {snapshot.sealedAt && (
          <div className="text-sm">
            <span className="text-muted-foreground">Sealed at</span>
            <p className="font-medium mt-0.5">{formatDate(snapshot.sealedAt)}</p>
          </div>
        )}

        {snapshot.inquiryClosed && (
          <Badge variant="outline" className="text-xs gap-1">
            <Lock className="h-3 w-3" />
            Inquiry closed (post-seal)
          </Badge>
        )}

        {isSealed && embeddedProofValid !== null && (
          <Badge
            variant={embeddedProofValid ? "accent" : "destructive"}
            className="text-xs gap-1"
          >
            {embeddedProofValid ? (
              <>
                <CheckCircle2 className="h-3 w-3" /> Server-verified at read
              </>
            ) : (
              <>
                <XCircle className="h-3 w-3" /> Tamper detected at read
              </>
            )}
          </Badge>
        )}

        {isSealed && (
          <>
            <Separator />
            <div className="grid gap-2 sm:grid-cols-2">
              <Button
                variant="outline"
                size="sm"
                onClick={() => setShowProof((v) => !v)}
                className="gap-1.5"
              >
                <Shield className="h-3.5 w-3.5" />
                {showProof ? "Hide" : "Verify"} cryptographic proof
                {showProof ? (
                  <ChevronUp className="h-3.5 w-3.5" />
                ) : (
                  <ChevronDown className="h-3.5 w-3.5" />
                )}
              </Button>
              <Button
                variant="outline"
                size="sm"
                onClick={() => downloadReceipt.mutate(snapshot.snapshotId)}
                disabled={downloadReceipt.isPending}
                className="gap-1.5"
              >
                <Download className="h-3.5 w-3.5" />
                {downloadReceipt.isPending ? "Preparing..." : "Download receipt"}
              </Button>
            </div>

            {showProof && (
              <div className="rounded-md border bg-muted/30 p-3 space-y-3">
                {proofLoading ? (
                  <p className="text-sm text-muted-foreground">Verifying...</p>
                ) : proof ? (
                  <>
                    <div className="flex items-center gap-2">
                      {proof.isValid ? (
                        <Badge variant="accent" className="gap-1">
                          <CheckCircle2 className="h-3 w-3" />
                          Integrity verified
                        </Badge>
                      ) : (
                        <Badge variant="destructive" className="gap-1">
                          <XCircle className="h-3 w-3" />
                          Verification failed
                        </Badge>
                      )}
                    </div>
                    <div className="space-y-2 text-xs">
                      <div>
                        <span className="text-muted-foreground">SHA-256 Hash</span>
                        <p className="font-mono break-all mt-0.5 select-all">
                          {proof.hash}
                        </p>
                      </div>
                      <div>
                        <span className="text-muted-foreground">
                          HMAC-SHA256 Signature
                        </span>
                        <p className="font-mono break-all mt-0.5 select-all">
                          {proof.signature}
                        </p>
                      </div>
                      <div>
                        <span className="text-muted-foreground">Signed at</span>
                        <p className="mt-0.5">{formatDate(proof.signedAt)}</p>
                      </div>
                    </div>
                  </>
                ) : (
                  <p className="text-sm text-muted-foreground">
                    Proof not available.
                  </p>
                )}
              </div>
            )}
          </>
        )}
      </CardContent>
    </Card>
  );
};
