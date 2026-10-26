import { useEffect, useState } from "react";
import { Loader2 } from "lucide-react";
import type { KycDocumentType, ManualVerificationDocumentDto } from "@/api/types";
import { adminApi } from "@/features/admin/services/adminApi";
import { prepareKycPreviewBlob } from "@/features/admin/lib/kycPreview";

const documentLabels: Record<KycDocumentType, string> = {
  IdFront: "ID — front",
  IdBack: "ID — back",
  Selfie: "Live selfie",
  BusinessRegistration: "Business registration",
  ProofOfAuthority: "Proof of authority",
};

export function ReviewDocumentImage({
  profileId,
  doc,
}: {
  profileId: string;
  doc: ManualVerificationDocumentDto;
}) {
  const label = documentLabels[doc.documentType] ?? doc.documentType;
  const isPdf =
    doc.mimeType.toLowerCase().includes("pdf") || doc.fileName.toLowerCase().endsWith(".pdf");
  const [src, setSrc] = useState<string | null>(null);
  const [failed, setFailed] = useState(false);

  useEffect(() => {
    const controller = new AbortController();
    let objectUrl: string | null = null;
    let cancelled = false;

    void (async () => {
      try {
        const blob = await adminApi.getManualVerificationDocument(
          profileId,
          doc.documentType,
          controller.signal,
        );
        const preview = isPdf
          ? blob
          : await prepareKycPreviewBlob(blob, doc.mimeType, doc.fileName);
        objectUrl = URL.createObjectURL(preview);
        if (cancelled) {
          URL.revokeObjectURL(objectUrl);
          return;
        }
        setSrc(objectUrl);
        setFailed(false);
      } catch {
        if (cancelled || controller.signal.aborted) return;
        setFailed(true);
      }
    })();

    return () => {
      cancelled = true;
      controller.abort();
      if (objectUrl) URL.revokeObjectURL(objectUrl);
    };
  }, [profileId, doc.documentType, doc.fileName, doc.mimeType, doc.uploadedAt, isPdf]);

  return (
    <figure className="space-y-2">
      <figcaption className="text-sm font-medium">
        {label}
        <span className="ml-2 text-xs font-normal text-muted-foreground">
          {new Date(doc.uploadedAt).toLocaleString()}
        </span>
      </figcaption>
      <div className="flex min-h-64 items-center justify-center overflow-hidden rounded-lg border bg-muted">
        {src && isPdf ? (
          <a
            href={src}
            target="_blank"
            rel="noreferrer"
            className="px-4 py-8 text-sm font-medium text-primary underline"
          >
            Open {label}
          </a>
        ) : src ? (
          <button
            type="button"
            className="block w-full cursor-zoom-in"
            aria-label={`Open ${label} full size`}
            onClick={() => {
              window.open(src, "_blank");
            }}
          >
            <img
              src={src}
              alt={label}
              className="max-h-[28rem] w-full object-contain"
              onError={() => {
                setSrc(null);
                setFailed(true);
              }}
            />
          </button>
        ) : failed ? (
          <p className="px-4 py-6 text-center text-sm text-muted-foreground">
            Could not load this photo.
          </p>
        ) : (
          <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" aria-label="Loading photo" />
        )}
      </div>
    </figure>
  );
}
