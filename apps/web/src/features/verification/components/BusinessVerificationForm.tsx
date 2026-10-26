import { useEffect, useRef, useState } from "react";
import { Building2, CheckCircle2, Loader2, Send, Upload, XCircle } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { getApiErrorMessage } from "@/api/errors";
import type { KycDocumentDto, KycDocumentType } from "@/api/types";
import {
  useMyKycDocuments,
  useSubmitManualKyc,
  useUploadKycDocument,
} from "@/features/verification/hooks/useVerification";

type Props = {
  firstName?: string | null;
  lastName?: string | null;
  legalBusinessName?: string | null;
  businessRegistrationNumber?: string | null;
  businessJurisdiction?: string | null;
  beforeSubmit?: () => Promise<void>;
  onSubmitted: () => void;
};

const ACCEPTED_TYPES = "image/jpeg,image/png,image/webp,image/heic,image/heif,application/pdf,.pdf";

function findDoc(docs: KycDocumentDto[] | undefined, type: KycDocumentType) {
  return docs?.find((d) => d.documentType === type);
}

/**
 * Property-management verification. The company uploads its registration
 * (and optionally a letter of authority). A personal government ID is not asked for.
 */
export function BusinessVerificationForm({
  firstName,
  lastName,
  legalBusinessName,
  businessRegistrationNumber,
  businessJurisdiction,
  beforeSubmit,
  onSubmitted,
}: Props) {
  const { data: docs } = useMyKycDocuments();
  const upload = useUploadKycDocument();
  const submit = useSubmitManualKyc();

  const [error, setError] = useState<string | null>(null);
  const [uploadingType, setUploadingType] = useState<KycDocumentType | null>(null);
  const [legalName, setLegalName] = useState(legalBusinessName ?? "");
  const [registrationNumber, setRegistrationNumber] = useState(businessRegistrationNumber ?? "");
  const [jurisdiction, setJurisdiction] = useState(businessJurisdiction ?? "");

  useEffect(() => {
    setLegalName(legalBusinessName ?? "");
  }, [legalBusinessName]);
  useEffect(() => {
    setRegistrationNumber(businessRegistrationNumber ?? "");
  }, [businessRegistrationNumber]);
  useEffect(() => {
    setJurisdiction(businessJurisdiction ?? "");
  }, [businessJurisdiction]);

  const registration = findDoc(docs, "BusinessRegistration");
  const authority = findDoc(docs, "ProofOfAuthority");
  const canSubmit = Boolean(registration) && !submit.isPending;

  const handleFile = async (documentType: KycDocumentType, file: File) => {
    setError(null);
    setUploadingType(documentType);
    try {
      await upload.mutateAsync({ documentType, file });
    } catch (e) {
      setError(getApiErrorMessage(e) || "Upload failed. Please try again.");
    } finally {
      setUploadingType(null);
    }
  };

  const handleSubmit = async () => {
    setError(null);
    if (!legalName.trim() || !registrationNumber.trim() || !jurisdiction.trim()) {
      setError("Enter the legal business name, registration number, and where the company is registered.");
      return;
    }
    try {
      await beforeSubmit?.();
      await submit.mutateAsync({
        firstName: firstName ?? null,
        lastName: lastName ?? null,
        legalBusinessName: legalName.trim(),
        businessRegistrationNumber: registrationNumber.trim(),
        businessJurisdiction: jurisdiction.trim(),
      });
      onSubmitted();
    } catch (e) {
      setError(getApiErrorMessage(e) || "Could not submit your verification.");
    }
  };

  return (
    <div className="space-y-5">
      {error && (
        <Alert variant="destructive">
          <XCircle className="h-4 w-4" />
          <AlertDescription>{error}</AlertDescription>
        </Alert>
      )}

      <div className="grid gap-4 sm:grid-cols-2">
        <div className="space-y-2 sm:col-span-2">
          <Label htmlFor="business-legal-name">Legal business name</Label>
          <Input
            id="business-legal-name"
            value={legalName}
            onChange={(e) => setLegalName(e.target.value)}
            placeholder="Northwind Property Management LLC"
            autoComplete="organization"
          />
        </div>
        <div className="space-y-2">
          <Label htmlFor="business-registration-number">Registration or license number</Label>
          <Input
            id="business-registration-number"
            value={registrationNumber}
            onChange={(e) => setRegistrationNumber(e.target.value)}
            placeholder="EIN, company number, or license ID"
          />
        </div>
        <div className="space-y-2">
          <Label htmlFor="business-jurisdiction">Registered in</Label>
          <Input
            id="business-jurisdiction"
            value={jurisdiction}
            onChange={(e) => setJurisdiction(e.target.value)}
            placeholder="California, United States"
          />
        </div>
      </div>

      <div className="grid gap-4 sm:grid-cols-2">
        <DocumentSlot
          title="Business registration"
          required
          description="Certificate of incorporation, business license, or equivalent registration. PDF or photo."
          doc={registration}
          uploading={uploadingType === "BusinessRegistration"}
          onSelect={(file) => void handleFile("BusinessRegistration", file)}
        />
        <DocumentSlot
          title="Proof of authority"
          description="Optional. A management agreement or letter showing this account may act for the company."
          doc={authority}
          uploading={uploadingType === "ProofOfAuthority"}
          onSelect={(file) => void handleFile("ProofOfAuthority", file)}
        />
      </div>

      <div className="flex items-center gap-3">
        <Button onClick={() => void handleSubmit()} disabled={!canSubmit} className="gap-2">
          {submit.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : <Send className="h-4 w-4" />}
          {submit.isPending ? "Submitting..." : "Submit for review"}
        </Button>
        {!canSubmit && !submit.isPending && (
          <p className="text-xs text-muted-foreground">
            Upload the business registration to enable submission.
          </p>
        )}
      </div>

      <p className="text-xs text-muted-foreground">
        Business documents are stored in a private vault and are only visible to the verification
        team. A personal photo ID is not required. Review typically completes within 24 hours.
      </p>
    </div>
  );
}

function DocumentSlot({
  title,
  description,
  required = false,
  doc,
  uploading,
  onSelect,
}: {
  title: string;
  description: string;
  required?: boolean;
  doc: KycDocumentDto | undefined;
  uploading: boolean;
  onSelect: (file: File) => void;
}) {
  const inputRef = useRef<HTMLInputElement>(null);

  return (
    <div className="rounded-lg border p-4 space-y-3">
      <div className="flex items-center justify-between">
        <p className="text-sm font-medium flex items-center gap-2">
          <Building2 className="h-4 w-4 text-muted-foreground" />
          {title}
          {required && <span className="text-red-500">*</span>}
        </p>
        {doc && <CheckCircle2 className="h-4 w-4 text-emerald-600" />}
      </div>
      <p className="text-xs text-muted-foreground">{description}</p>
      {doc && <p className="text-xs text-emerald-700 truncate">Uploaded: {doc.fileName}</p>}
      <input
        ref={inputRef}
        type="file"
        accept={ACCEPTED_TYPES}
        className="hidden"
        onChange={(e) => {
          const file = e.target.files?.[0];
          if (file) onSelect(file);
          e.target.value = "";
        }}
      />
      <Button
        type="button"
        variant="outline"
        size="sm"
        className="gap-2"
        disabled={uploading}
        onClick={() => inputRef.current?.click()}
      >
        {uploading ? <Loader2 className="h-4 w-4 animate-spin" /> : <Upload className="h-4 w-4" />}
        {uploading ? "Uploading..." : doc ? "Replace file" : "Upload file"}
      </Button>
    </div>
  );
}
