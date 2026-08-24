import { useCallback, useEffect, useRef, useState } from "react";
import {
  Camera,
  CheckCircle2,
  CreditCard,
  Loader2,
  RefreshCw,
  Send,
  Upload,
  XCircle,
} from "lucide-react";
import { Button } from "@/components/ui/button";
import { DatePicker } from "@/components/ui/date-picker";
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
  /** Auth-profile values used to prefill the submission. */
  firstName?: string | null;
  lastName?: string | null;
  dateOfBirth?: string | null;
  /** Runs before submission (e.g. required legal consents). Throw to abort. */
  beforeSubmit?: () => Promise<void>;
  /** Called after a successful submission so the parent can refresh status. */
  onSubmitted: () => void;
};

const ACCEPTED_TYPES = "image/jpeg,image/png,image/webp,image/heic,image/heif";

/**
 * Latest selectable date of birth (today minus 18 years, local timezone) —
 * users must be legal adults, matching the backend's Identity.Kyc.Underage
 * validation.
 */
function latestAdultDob(): string {
  const d = new Date();
  d.setFullYear(d.getFullYear() - 18);
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;
}

function findDoc(docs: KycDocumentDto[] | undefined, type: KycDocumentType) {
  return docs?.find((d) => d.documentType === type);
}

/**
 * Manual KYC submission: the user uploads the front (and optionally back) of
 * a government ID and captures a live selfie with their camera, then submits
 * everything for admin review.
 */
export function ManualKycUpload({
  firstName,
  lastName,
  dateOfBirth,
  beforeSubmit,
  onSubmitted,
}: Props) {
  const { data: docs } = useMyKycDocuments();
  const upload = useUploadKycDocument();
  const submit = useSubmitManualKyc();

  const [error, setError] = useState<string | null>(null);
  const [uploadingType, setUploadingType] = useState<KycDocumentType | null>(null);
  // The profile may hand us a full ISO datetime; the picker works with
  // plain YYYY-MM-DD, so keep just the date part.
  const [dobInput, setDobInput] = useState((dateOfBirth ?? "").slice(0, 10));
  useEffect(() => {
    setDobInput((dateOfBirth ?? "").slice(0, 10));
  }, [dateOfBirth]);

  const idFront = findDoc(docs, "IdFront");
  const idBack = findDoc(docs, "IdBack");
  const selfie = findDoc(docs, "Selfie");
  const canSubmit = Boolean(idFront && selfie) && !submit.isPending;

  const handleFile = async (documentType: KycDocumentType, file: File | Blob, fileName?: string) => {
    setError(null);
    setUploadingType(documentType);
    try {
      await upload.mutateAsync({ documentType, file, fileName });
    } catch (e) {
      setError(getApiErrorMessage(e) || "Upload failed. Please try again.");
    } finally {
      setUploadingType(null);
    }
  };

  const handleSubmit = async () => {
    setError(null);
    const dob = dobInput.trim();
    if (!dob) {
      setError("Please enter your date of birth before submitting.");
      return;
    }
    try {
      await beforeSubmit?.();
      await submit.mutateAsync({
        firstName: firstName ?? null,
        lastName: lastName ?? null,
        dateOfBirth: dob,
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
        <IdUploadSlot
          title="ID front"
          required
          description="Front of your driver's license, passport, or state ID."
          doc={idFront}
          uploading={uploadingType === "IdFront"}
          onSelect={(file) => void handleFile("IdFront", file)}
        />
        <IdUploadSlot
          title="ID back"
          description="Back side — skip this for passports."
          doc={idBack}
          uploading={uploadingType === "IdBack"}
          onSelect={(file) => void handleFile("IdBack", file)}
        />
      </div>

      <SelfieCapture
        doc={selfie}
        uploading={uploadingType === "Selfie"}
        onCapture={(blob) => void handleFile("Selfie", blob, "selfie.jpg")}
      />

      <div className="space-y-2 max-w-xs">
        <Label htmlFor="manual-kyc-dob">Date of birth</Label>
        <DatePicker
          id="manual-kyc-dob"
          value={dobInput}
          onChange={setDobInput}
          max={latestAdultDob()}
          placeholder="Select your date of birth"
        />
        <p className="text-xs text-muted-foreground">
          Must match the date of birth on the ID you uploaded. You must be 18 or older.
        </p>
      </div>

      <div className="flex items-center gap-3">
        <Button onClick={() => void handleSubmit()} disabled={!canSubmit} className="gap-2">
          {submit.isPending ? (
            <Loader2 className="h-4 w-4 animate-spin" />
          ) : (
            <Send className="h-4 w-4" />
          )}
          {submit.isPending ? "Submitting..." : "Submit for review"}
        </Button>
        {!canSubmit && !submit.isPending && (
          <p className="text-xs text-muted-foreground">
            Upload your ID front and capture a selfie to enable submission.
          </p>
        )}
      </div>

      <p className="text-xs text-muted-foreground">
        Your documents are stored encrypted in a private vault and are only visible to the
        verification team. Review typically completes within 24 hours.
      </p>
    </div>
  );
}

function IdUploadSlot({
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
          <CreditCard className="h-4 w-4 text-muted-foreground" />
          {title}
          {required && <span className="text-red-500">*</span>}
        </p>
        {doc && <CheckCircle2 className="h-4 w-4 text-emerald-600" />}
      </div>
      <p className="text-xs text-muted-foreground">{description}</p>
      {doc && (
        <p className="text-xs text-emerald-700 truncate">
          Uploaded: {doc.fileName}
        </p>
      )}
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
        {uploading ? (
          <Loader2 className="h-4 w-4 animate-spin" />
        ) : (
          <Upload className="h-4 w-4" />
        )}
        {uploading ? "Uploading..." : doc ? "Replace photo" : "Upload photo"}
      </Button>
    </div>
  );
}

function SelfieCapture({
  doc,
  uploading,
  onCapture,
}: {
  doc: KycDocumentDto | undefined;
  uploading: boolean;
  onCapture: (blob: Blob) => void;
}) {
  const videoRef = useRef<HTMLVideoElement>(null);
  const streamRef = useRef<MediaStream | null>(null);
  const fallbackInputRef = useRef<HTMLInputElement>(null);

  const [cameraOpen, setCameraOpen] = useState(false);
  const [cameraError, setCameraError] = useState<string | null>(null);

  const stopCamera = useCallback(() => {
    streamRef.current?.getTracks().forEach((t) => t.stop());
    streamRef.current = null;
    setCameraOpen(false);
  }, []);

  useEffect(() => stopCamera, [stopCamera]);

  const startCamera = async () => {
    setCameraError(null);
    try {
      const stream = await navigator.mediaDevices.getUserMedia({
        video: { facingMode: "user", width: { ideal: 1280 }, height: { ideal: 720 } },
        audio: false,
      });
      streamRef.current = stream;
      setCameraOpen(true);
      // The video element mounts on the next render.
      requestAnimationFrame(() => {
        if (videoRef.current) {
          videoRef.current.srcObject = stream;
          void videoRef.current.play();
        }
      });
    } catch {
      setCameraError(
        "Camera unavailable or permission denied. You can take the selfie with your device camera instead.",
      );
    }
  };

  const capture = () => {
    const video = videoRef.current;
    if (!video || video.videoWidth === 0) return;

    const canvas = document.createElement("canvas");
    canvas.width = video.videoWidth;
    canvas.height = video.videoHeight;
    canvas.getContext("2d")?.drawImage(video, 0, 0);
    canvas.toBlob(
      (blob) => {
        if (blob) {
          onCapture(blob);
          stopCamera();
        }
      },
      "image/jpeg",
      0.92,
    );
  };

  return (
    <div className="rounded-lg border p-4 space-y-3">
      <div className="flex items-center justify-between">
        <p className="text-sm font-medium flex items-center gap-2">
          <Camera className="h-4 w-4 text-muted-foreground" />
          Live selfie
          <span className="text-red-500">*</span>
        </p>
        {doc && <CheckCircle2 className="h-4 w-4 text-emerald-600" />}
      </div>
      <p className="text-xs text-muted-foreground">
        Take a selfie now so we can match your face to the ID photo. Remove hats and glasses,
        and make sure your face is well lit.
      </p>

      {doc && !cameraOpen && (
        <p className="text-xs text-emerald-700">Selfie captured. You can retake it below.</p>
      )}

      {cameraError && (
        <Alert variant="destructive">
          <AlertDescription className="text-xs">{cameraError}</AlertDescription>
        </Alert>
      )}

      {cameraOpen ? (
        <div className="space-y-3">
          {/* Mirrored preview feels natural to users, but the captured frame stays unmirrored. */}
          <video
            ref={videoRef}
            playsInline
            muted
            className="w-full max-w-sm rounded-lg border bg-black -scale-x-100"
          />
          <div className="flex gap-2">
            <Button type="button" size="sm" className="gap-2" onClick={capture} disabled={uploading}>
              {uploading ? (
                <Loader2 className="h-4 w-4 animate-spin" />
              ) : (
                <Camera className="h-4 w-4" />
              )}
              Capture selfie
            </Button>
            <Button type="button" size="sm" variant="outline" onClick={stopCamera}>
              Cancel
            </Button>
          </div>
        </div>
      ) : (
        <div className="flex flex-wrap gap-2">
          <Button
            type="button"
            variant="outline"
            size="sm"
            className="gap-2"
            disabled={uploading}
            onClick={() => void startCamera()}
          >
            {uploading ? (
              <Loader2 className="h-4 w-4 animate-spin" />
            ) : doc ? (
              <RefreshCw className="h-4 w-4" />
            ) : (
              <Camera className="h-4 w-4" />
            )}
            {uploading ? "Uploading..." : doc ? "Retake selfie" : "Open camera"}
          </Button>
          {cameraError && (
            <>
              <input
                ref={fallbackInputRef}
                type="file"
                accept="image/*"
                capture="user"
                className="hidden"
                onChange={(e) => {
                  const file = e.target.files?.[0];
                  if (file) onCapture(file);
                  e.target.value = "";
                }}
              />
              <Button
                type="button"
                variant="outline"
                size="sm"
                className="gap-2"
                disabled={uploading}
                onClick={() => fallbackInputRef.current?.click()}
              >
                <Upload className="h-4 w-4" />
                Use device camera
              </Button>
            </>
          )}
        </div>
      )}
    </div>
  );
}
