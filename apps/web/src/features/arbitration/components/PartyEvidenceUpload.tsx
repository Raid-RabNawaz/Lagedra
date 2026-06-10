import { useEffect, useState } from "react";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Info } from "lucide-react";
import type { CaseDto } from "@/api/types";
import { EvidenceUpload } from "@/features/arbitration/components/EvidenceUpload";

function partySlotType(c: CaseDto, userId: string): string {
  if (c.landlordUserId === userId) return "Landlord";
  if (c.tenantUserId === userId) return "Tenant";
  return "Party";
}

type PartyEvidenceUploadProps = {
  c: CaseDto;
  userId: string;
  onCaseUpdated: () => void;
};

export function PartyEvidenceUpload({ c, userId, onCaseUpdated }: PartyEvidenceUploadProps) {
  const [draftManifestId, setDraftManifestId] = useState<string | undefined>();

  const mySlots = (c.evidenceSlots ?? []).filter((s) => s.submittedBy === userId);
  const latestLinked = mySlots.at(-1);
  const isAppeal = c.status === "Appealed";
  const canUpload = ["Filed", "EvidencePending", "Appealed"].includes(c.status);

  useEffect(() => {
    setDraftManifestId(undefined);
  }, [c.caseId, c.status]);

  // On appeal, do not reuse pre-appeal manifest until a new slot is linked after submit.
  const manifestId =
    draftManifestId ?? (isAppeal ? undefined : latestLinked?.evidenceManifestId);

  const showLinkedNote = Boolean(latestLinked) && !isAppeal && !draftManifestId;

  if (!canUpload && !latestLinked) {
    return (
      <Alert>
        <Info className="h-4 w-4" />
        <AlertDescription>
          The evidence submission window for this case is closed.
        </AlertDescription>
      </Alert>
    );
  }

  return (
    <div className="space-y-3">
      <Alert>
        <Info className="h-4 w-4" />
        <AlertDescription>
          {isAppeal ? (
            <>
              This case was appealed. Create a <strong>new</strong> evidence manifest, upload
              files, then seal and submit for the appeal round.
            </>
          ) : (
            <>
              Upload your files, then <strong>Seal & submit to case</strong>. Sealing links your
              manifest to this case automatically.
            </>
          )}
        </AlertDescription>
      </Alert>
      <EvidenceUpload
        dealId={c.dealId}
        caseId={c.caseId}
        slotType={partySlotType(c, userId)}
        manifestId={manifestId}
        readOnly={!canUpload}
        canViewFiles={Boolean(manifestId)}
        onManifestCreated={(id) => setDraftManifestId(id)}
        onAttached={() => {
          setDraftManifestId(undefined);
          onCaseUpdated();
        }}
      />
      {showLinkedNote && (
        <p className="text-xs text-muted-foreground px-1">
          Your evidence is on file for this case. Wait for the other party or platform review.
        </p>
      )}
    </div>
  );
}
