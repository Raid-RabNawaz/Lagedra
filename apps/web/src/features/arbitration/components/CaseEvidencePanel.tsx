import { FileText } from "lucide-react";
import { formatDate } from "@/utils/format";
import type { CaseDto, EvidenceSlotDto } from "@/api/types";
import { EvidenceUpload } from "@/features/arbitration/components/EvidenceUpload";
import { EmptyState } from "@/components/shared/EmptyState";

function slotLabel(slot: EvidenceSlotDto, c: CaseDto) {
  if (c.landlordUserId && slot.submittedBy === c.landlordUserId) return "Host evidence";
  if (c.tenantUserId && slot.submittedBy === c.tenantUserId) return "Guest evidence";
  return slot.slotType;
}

type CaseEvidencePanelProps = {
  c: CaseDto;
};

export function CaseEvidencePanel({ c }: CaseEvidencePanelProps) {
  const slots = c.evidenceSlots ?? [];

  if (slots.length === 0) {
    return (
      <EmptyState
        title="No evidence on record"
        description="Parties must seal their uploads and submit them to this case before review. Evidence manifests that were only created locally are not visible here until linked."
      >
        <FileText className="h-10 w-10 text-muted-foreground/40" />
      </EmptyState>
    );
  }

  return (
    <div className="space-y-4">
      {slots.map((slot) => (
        <div key={slot.slotId} className="space-y-1">
          <p className="text-xs font-medium text-muted-foreground px-1">
            {slotLabel(slot, c)} · submitted {formatDate(slot.submittedAt)}
          </p>
          <EvidenceUpload
            dealId={c.dealId}
            manifestId={slot.evidenceManifestId}
            readOnly
            canViewFiles
          />
        </div>
      ))}
    </div>
  );
}
