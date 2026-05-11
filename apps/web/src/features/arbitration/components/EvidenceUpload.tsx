import { useState, useCallback } from "react";
import { isAxiosError } from "axios";
import { Upload, FileCheck, ShieldAlert, Loader2, File, AlertTriangle } from "lucide-react";
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
} from "@/features/evidence/hooks/useEvidence";
import type { ManifestUploadDto, ScanStatus } from "@/api/types";

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

function UploadRow({ upload }: { upload: ManifestUploadDto }) {
  const { data: scan } = useScanStatus(upload.uploadId);

  return (
    <div className="flex items-center gap-3 rounded-lg border p-3 text-sm">
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
      <span className="text-xs text-muted-foreground">{upload.mimeType}</span>
    </div>
  );
}

type EvidenceUploadProps = {
  dealId: string;
  manifestId?: string;
  onManifestCreated?: (manifestId: string) => void;
  readOnly?: boolean;
};

export function EvidenceUpload({
  dealId,
  manifestId,
  onManifestCreated,
  readOnly = false,
}: EvidenceUploadProps) {
  const { data: manifest, isLoading } = useManifest(manifestId);
  const createManifest = useCreateManifest();
  const sealManifest = useSealManifest();
  const directUpload = useDirectUpload();
  const [uploading, setUploading] = useState(false);
  const [uploadError, setUploadError] = useState<string | null>(null);

  const handleCreateManifest = useCallback(async () => {
    const result = await createManifest.mutateAsync({
      dealId,
      manifestType: "Arbitration",
    });
    onManifestCreated?.(result.manifestId);
  }, [dealId, createManifest, onManifestCreated]);

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
            Create an evidence manifest to start uploading files.
          </p>
          {!readOnly && (
            <Button
              size="sm"
              onClick={handleCreateManifest}
              disabled={createManifest.isPending}
            >
              {createManifest.isPending ? (
                <Loader2 className="h-4 w-4 animate-spin mr-2" />
              ) : (
                <Upload className="h-4 w-4 mr-2" />
              )}
              Create Manifest
            </Button>
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
          <CardTitle className="text-base">Evidence Files</CardTitle>
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
          <UploadRow key={u.uploadId} upload={u} />
        ))}

        {uploadError && (
          <Alert variant="destructive">
            <AlertTriangle className="h-4 w-4" />
            <AlertDescription>{uploadError}</AlertDescription>
          </Alert>
        )}

        {!readOnly && !isSealed && (
          <div className="space-y-2 pt-2">
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
                    accept={ACCEPT_ATTRIBUTE}
                    className="absolute inset-0 opacity-0 cursor-pointer"
                    onChange={handleFileSelect}
                    disabled={uploading}
                  />
                </label>
              </Button>

              {(manifest?.uploads.length ?? 0) > 0 && (
                <Button
                  size="sm"
                  onClick={() => sealManifest.mutate(manifestId)}
                  disabled={sealManifest.isPending}
                >
                  {sealManifest.isPending ? (
                    <Loader2 className="h-4 w-4 animate-spin mr-2" />
                  ) : (
                    <ShieldAlert className="h-4 w-4 mr-2" />
                  )}
                  Seal Manifest
                </Button>
              )}
            </div>
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
