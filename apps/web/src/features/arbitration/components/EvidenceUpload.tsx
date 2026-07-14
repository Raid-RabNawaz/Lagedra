import { useState, useCallback } from "react";
import { isAxiosError } from "axios";
import {
  Upload,
  FileCheck,
  ShieldAlert,
  Loader2,
  File,
  AlertTriangle,
  ExternalLink,
  Download,
} from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Alert, AlertDescription } from "@/components/ui/alert";
import {
  useManifest,
  useCreateManifest,
  useSealManifest,
  useDirectUpload,
  useScanStatus,
  useDownloadUrl,
} from "@/features/evidence/hooks/useEvidence";
import { useAttachEvidence } from "@/features/arbitration/hooks/useArbitration";
import { getApiErrorMessage } from "@/api/errors";
import type { ManifestType, ManifestUploadDto, ScanStatus } from "@/api/types";

const ALLOWED_MIME_PREFIXES = ["image/", "video/", "audio/"];
const ALLOWED_EXACT_MIME_TYPES = new Set([
  "application/pdf",
  "application/msword",
  "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
  "application/vnd.ms-excel",
  "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
  "text/plain",
  "text/csv",
]);

const ACCEPT_ATTRIBUTE = [
  "image/jpeg",
  "image/png",
  "image/gif",
  "image/webp",
  "image/heic",
  "image/heif",
  "video/mp4",
  "video/quicktime",
  "video/webm",
  "audio/mpeg",
  "audio/mp4",
  "audio/wav",
  "application/pdf",
  ".doc",
  ".docx",
  ".xls",
  ".xlsx",
  "text/plain",
  "text/csv",
].join(",");

const MAX_FILE_BYTES = 50 * 1024 * 1024;

function isAcceptableMime(mime: string): boolean {
  if (!mime) return false;
  if (ALLOWED_EXACT_MIME_TYPES.has(mime)) return true;
  return ALLOWED_MIME_PREFIXES.some((p) => mime.startsWith(p));
}

function scanBadge(status: ScanStatus | undefined) {
  switch (status) {
    case "Clean":
      return <Badge variant="success" className="text-[10px]">Clean</Badge>;
    case "Infected":
      return <Badge variant="destructive" className="text-[10px]">Infected</Badge>;
    case "Pending":
      return <Badge variant="secondary" className="text-[10px]">Scanning...</Badge>;
    default:
      return null;
  }
}

function UploadRow({
  upload,
  canViewFiles,
}: {
  upload: ManifestUploadDto;
  canViewFiles: boolean;
}) {
  const { data: scan } = useScanStatus(upload.uploadId);
  const downloadUrl = useDownloadUrl();
  const [fileError, setFileError] = useState<string | null>(null);

  const blockedByScan = scan?.status === "Infected";
  const showActions = canViewFiles && !blockedByScan;

  const openFile = async (downloadOnly: boolean) => {
    setFileError(null);
    try {
      const { presignedUrl, originalFileName } = await downloadUrl.mutateAsync(
        upload.uploadId,
      );
      if (downloadOnly) {
        const link = document.createElement("a");
        link.href = presignedUrl;
        link.download = originalFileName;
        link.rel = "noopener noreferrer";
        link.target = "_blank";
        document.body.appendChild(link);
        link.click();
        link.remove();
      } else {
        window.open(presignedUrl, "_blank", "noopener,noreferrer");
      }
    } catch (err) {
      setFileError(
        getApiErrorMessage(err, "Could not open this evidence file."),
      );
    }
  };

  return (
    <div className="rounded-lg border p-3 text-sm space-y-2">
      <div className="flex items-center gap-3">
        <File className="h-4 w-4 text-muted-foreground shrink-0" />
        <div className="flex-1 min-w-0">
          <p className="font-medium truncate">{upload.originalFileName}</p>
          {upload.fileHash && (
            <p className="text-[10px] text-muted-foreground font-mono truncate">
              SHA-256: {upload.fileHash}
            </p>
          )}
        </div>
        {scanBadge(scan?.status)}
        <span className="text-xs text-muted-foreground hidden sm:inline">
          {upload.mimeType}
        </span>
      </div>
      {blockedByScan && (
        <p className="text-xs text-destructive">File blocked — malware detected.</p>
      )}
      {showActions && (
        <div className="flex flex-wrap gap-2">
          <Button
            type="button"
            size="sm"
            variant="outline"
            disabled={downloadUrl.isPending}
            onClick={() => void openFile(false)}
          >
            {downloadUrl.isPending ? (
              <Loader2 className="h-3.5 w-3.5 animate-spin mr-1.5" />
            ) : (
              <ExternalLink className="h-3.5 w-3.5 mr-1.5" />
            )}
            Open
          </Button>
          <Button
            type="button"
            size="sm"
            variant="ghost"
            disabled={downloadUrl.isPending}
            onClick={() => void openFile(true)}
          >
            <Download className="h-3.5 w-3.5 mr-1.5" />
            Download
          </Button>
        </div>
      )}
      {scan?.status === "Pending" && showActions && (
        <p className="text-[10px] text-muted-foreground">
          Malware scan still running — file can be opened for review.
        </p>
      )}
      {fileError && (
        <p className="text-xs text-destructive">{fileError}</p>
      )}
    </div>
  );
}

type EvidenceUploadProps = {
  dealId: string;
  caseId?: string;
  slotType?: string;
  manifestId?: string;
  /** Defaults to Arbitration. Use Damage for deposit-deduction photos. */
  manifestType?: ManifestType;
  onManifestCreated?: (manifestId: string) => void;
  onAttached?: () => void;
  /** Fired after a successful seal when there is no arbitration case to attach to. */
  onSealed?: (manifestId: string) => void;
  readOnly?: boolean;
  /** When true, show Open/Download for each uploaded file (reviewers). */
  canViewFiles?: boolean;
  /** Override the file input accept attribute (e.g. images only). */
  accept?: string;
  title?: string;
};

export function EvidenceUpload({
  dealId,
  caseId,
  slotType,
  manifestId,
  manifestType = "Arbitration",
  onManifestCreated,
  onAttached,
  onSealed,
  readOnly = false,
  canViewFiles = false,
  accept = ACCEPT_ATTRIBUTE,
  title = "Evidence Files",
}: EvidenceUploadProps) {
  const { data: manifest, isLoading } = useManifest(manifestId);
  const createManifest = useCreateManifest();
  const sealManifest = useSealManifest();
  const attachEvidence = useAttachEvidence();
  const directUpload = useDirectUpload();
  const [uploading, setUploading] = useState(false);
  const [uploadError, setUploadError] = useState<string | null>(null);
  const [attachError, setAttachError] = useState<string | null>(null);
  const [createError, setCreateError] = useState<string | null>(null);

  const handleCreateManifest = useCallback(async () => {
    setCreateError(null);
    setAttachError(null);
    try {
      const result = await createManifest.mutateAsync({
        dealId,
        manifestType,
      });
      onManifestCreated?.(result.manifestId);
    } catch (err) {
      setCreateError(
        getApiErrorMessage(err, "Could not create evidence manifest. Check deal access and try again."),
      );
    }
  }, [dealId, manifestType, createManifest, onManifestCreated]);

  const handleFileSelect = useCallback(
    async (e: React.ChangeEvent<HTMLInputElement>) => {
      const file = e.target.files?.[0];
      const inputEl = e.target;
      if (!file || !manifestId) {
        inputEl.value = "";
        return;
      }

      setUploadError(null);

      if (file.size > MAX_FILE_BYTES) {
        setUploadError(
          `"${file.name}" is ${(file.size / (1024 * 1024)).toFixed(1)} MB. The maximum is ${MAX_FILE_BYTES / (1024 * 1024)} MB.`,
        );
        inputEl.value = "";
        return;
      }

      const mime = file.type || "application/octet-stream";
      if (!isAcceptableMime(mime)) {
        setUploadError(
          `"${file.name}" (${mime || "unknown type"}) is not a supported evidence file type. Allowed: images, video, audio, PDF, Office documents, plain text.`,
        );
        inputEl.value = "";
        return;
      }

      setUploading(true);
      try {
        await directUpload.mutateAsync({ manifestId, file });
      } catch (err) {
        const detail =
          isAxiosError(err) && err.response?.data && typeof err.response.data === "object"
            ? (err.response.data as { detail?: string; error?: string }).detail
            : null;
        setUploadError(detail ?? (err instanceof Error ? err.message : "Upload failed."));
      } finally {
        setUploading(false);
        inputEl.value = "";
      }
    },
    [manifestId, directUpload],
  );

  if (!manifestId) {
    return (
      <Card>
        <CardContent className="flex flex-col items-center gap-3 py-8">
          <Upload className="h-8 w-8 text-muted-foreground" />
          <p className="text-sm text-muted-foreground text-center">
            {manifestType === "Damage"
              ? "Create a photo set to upload damage images."
              : "Create an evidence manifest to start uploading files."}
          </p>
          {!readOnly && (
            <Button
              size="sm"
              onClick={() => void handleCreateManifest()}
              disabled={createManifest.isPending}
            >
              {createManifest.isPending ? (
                <Loader2 className="h-4 w-4 animate-spin mr-2" />
              ) : (
                <Upload className="h-4 w-4 mr-2" />
              )}
              {manifestType === "Damage" ? "Start photo upload" : "Create manifest"}
            </Button>
          )}
          {createError && (
            <Alert variant="destructive" className="mt-3 text-left">
              <AlertTriangle className="h-4 w-4" />
              <AlertDescription>{createError}</AlertDescription>
            </Alert>
          )}
        </CardContent>
      </Card>
    );
  }

  if (isLoading) {
    return (
      <Card>
        <CardContent className="flex items-center justify-center py-8">
          <Loader2 className="h-5 w-5 animate-spin text-muted-foreground" />
        </CardContent>
      </Card>
    );
  }

  const isSealed = manifest?.status === "Sealed";

  return (
    <Card>
      <CardHeader className="pb-3">
        <div className="flex items-center justify-between">
          <CardTitle className="text-base">{title}</CardTitle>
          <div className="flex items-center gap-2">
            {isSealed ? (
              <Badge variant="success" className="gap-1">
                <FileCheck className="h-3 w-3" />
                Sealed
              </Badge>
            ) : (
              <Badge variant="secondary">Open</Badge>
            )}
          </div>
        </div>
        {manifest?.hashOfAllFiles && (
          <p className="text-[10px] text-muted-foreground font-mono">
            Composite: {manifest.hashOfAllFiles}
          </p>
        )}
      </CardHeader>
      <CardContent className="space-y-3">
        {manifest?.uploads.length === 0 && (
          <p className="text-sm text-muted-foreground text-center py-4">
            No files uploaded yet.
          </p>
        )}
        {manifest?.uploads.map((u) => (
          <UploadRow key={u.uploadId} upload={u} canViewFiles={canViewFiles || readOnly} />
        ))}

        {(uploadError || attachError) && (
          <Alert variant="destructive">
            <AlertTriangle className="h-4 w-4" />
            <AlertDescription>{uploadError ?? attachError}</AlertDescription>
          </Alert>
        )}

        {!readOnly && (
          <div className="space-y-2 pt-2">
            {!isSealed && (
              <div className="flex gap-2">
                <Button
                  size="sm"
                  variant="outline"
                  disabled={uploading}
                  className="relative"
                >
                  <label className="cursor-pointer flex items-center">
                    {uploading ? (
                      <Loader2 className="h-4 w-4 animate-spin mr-2" />
                    ) : (
                      <Upload className="h-4 w-4 mr-2" />
                    )}
                    {uploading ? "Uploading..." : "Upload File"}
                    <input
                      type="file"
                      accept={accept}
                      className="absolute inset-0 opacity-0 cursor-pointer"
                      onChange={handleFileSelect}
                      disabled={uploading}
                    />
                  </label>
                </Button>

                {(manifest?.uploads.length ?? 0) > 0 && (
                  <Button
                    size="sm"
                    onClick={async () => {
                      setAttachError(null);
                      try {
                        await sealManifest.mutateAsync(manifestId);
                        if (caseId && slotType) {
                          await attachEvidence.mutateAsync({
                            caseId,
                            slotType,
                            evidenceManifestId: manifestId,
                          });
                          onAttached?.();
                        } else {
                          onSealed?.(manifestId);
                        }
                      } catch (err) {
                        setAttachError(
                          getApiErrorMessage(err, "Could not seal or submit evidence to the case."),
                        );
                      }
                    }}
                    disabled={sealManifest.isPending || attachEvidence.isPending}
                  >
                    {sealManifest.isPending || attachEvidence.isPending ? (
                      <Loader2 className="h-4 w-4 animate-spin mr-2" />
                    ) : (
                      <ShieldAlert className="h-4 w-4 mr-2" />
                    )}
                    {caseId ? "Seal & submit to case" : "Seal photos"}
                  </Button>
                )}
              </div>
            )}
            {isSealed && caseId && slotType && (
              <Button
                size="sm"
                variant="outline"
                onClick={async () => {
                  setAttachError(null);
                  try {
                    await attachEvidence.mutateAsync({
                      caseId,
                      slotType,
                      evidenceManifestId: manifestId,
                    });
                    onAttached?.();
                  } catch (err) {
                    setAttachError(
                      err instanceof Error
                        ? err.message
                        : "Could not submit evidence to the case.",
                    );
                  }
                }}
                disabled={attachEvidence.isPending}
              >
                {attachEvidence.isPending ? (
                  <Loader2 className="h-4 w-4 animate-spin mr-2" />
                ) : (
                  <ShieldAlert className="h-4 w-4 mr-2" />
                )}
                Submit sealed evidence to case
              </Button>
            )}
            <p className="text-[11px] text-muted-foreground">
              Allowed: images, video, audio, PDF, Office documents, plain text. Max{" "}
              {MAX_FILE_BYTES / (1024 * 1024)} MB.
            </p>
          </div>
        )}
      </CardContent>
    </Card>
  );
}
