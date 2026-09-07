import { useRef, useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import type { UseFormReturn } from "react-hook-form";
import { FileText, Loader2, ShieldCheck, Trash2, Upload } from "lucide-react";
import type { ListingDetailsDto } from "@/api/types";
import { listingApi } from "@/features/listings/services/listingApi";
import type { ListingFormValues } from "@/features/listings/lib/listingFormSchema";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { FormError } from "@/components/shared/FormError";
import { cn } from "@/lib/utils";

const ACCEPTED_TYPES = [
  "application/pdf",
  "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
];
const MAX_BYTES = 10 * 1024 * 1024;

type ListingLeaseAgreementEditorProps = {
  form: UseFormReturn<ListingFormValues>;
  /**
   * Null inside the wizard until the Basics step creates the draft. Uploading
   * needs a listing id, so the picker stays disabled until then.
   */
  listing: ListingDetailsDto | null;
};

/**
 * Lets a host choose between Lagedra's jurisdiction lease and their own
 * uploaded document. Used by both the create wizard and the edit form; the
 * upload saves immediately through the listing-scoped endpoint, while the
 * choice itself rides along with the surrounding form save.
 */
export function ListingLeaseAgreementEditor({
  form,
  listing,
}: ListingLeaseAgreementEditorProps) {
  const queryClient = useQueryClient();
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [uploadError, setUploadError] = useState<string | null>(null);
  const [isDragging, setIsDragging] = useState(false);

  const source = form.watch("leaseAgreementSource");
  const usesOwnLease = source === "HostProvided";
  const document = listing?.customLeaseDocument ?? null;

  const uploadMutation = useMutation({
    mutationFn: (file: File) => listingApi.uploadLeaseDocument(listing!.id, file),
    onSuccess: () => {
      setUploadError(null);
      form.setValue("hasCustomLeaseDocument", true, { shouldDirty: true, shouldValidate: true });
      void queryClient.invalidateQueries({ queryKey: ["listing", listing!.id] });
    },
    onError: (error: unknown) => setUploadError(readErrorDetail(error)),
  });

  const removeMutation = useMutation({
    mutationFn: () => listingApi.removeLeaseDocument(listing!.id),
    onSuccess: () => {
      setUploadError(null);
      form.setValue("hasCustomLeaseDocument", false, { shouldDirty: true });
      // The server also falls the listing back to the standard lease, so keep
      // the form in step rather than leaving it pointing at a deleted file.
      form.setValue("leaseAgreementSource", "LagedraTemplate", { shouldDirty: true, shouldValidate: true });
      void queryClient.invalidateQueries({ queryKey: ["listing", listing!.id] });
    },
    onError: (error: unknown) => setUploadError(readErrorDetail(error)),
  });

  const handleFile = (file: File | undefined) => {
    if (!file) return;
    if (!ACCEPTED_TYPES.includes(file.type)) {
      setUploadError("Upload a PDF or Word (.docx) document.");
      return;
    }
    if (file.size > MAX_BYTES) {
      setUploadError("Lease agreements must be under 10 MB.");
      return;
    }
    setUploadError(null);
    uploadMutation.mutate(file);
  };

  const setSource = (next: ListingFormValues["leaseAgreementSource"]) => {
    form.setValue("leaseAgreementSource", next, { shouldDirty: true, shouldValidate: true });
  };

  return (
    <div className="space-y-4">
      <label className="flex items-start gap-3 rounded-lg border p-4 cursor-pointer hover:bg-muted/30 transition-colors">
        <input
          type="checkbox"
          className="mt-1 rounded border-input"
          checked={!usesOwnLease}
          onChange={(e) => setSource(e.target.checked ? "LagedraTemplate" : "HostProvided")}
        />
        <div className="space-y-1">
          <p className="text-sm font-medium flex items-center gap-2">
            <ShieldCheck className="h-4 w-4 text-muted-foreground" />
            Use Lagedra&apos;s standard lease agreement
          </p>
          <p className="text-xs text-muted-foreground">
            Our counsel-vetted lease for your property&apos;s jurisdiction. Tenant and
            landlord names, dates, rent and your lease terms are filled in
            automatically when a booking is confirmed.
          </p>
        </div>
      </label>

      {usesOwnLease && (
        <div className="space-y-3 rounded-lg border p-4">
          <div>
            <p className="text-sm font-medium">Your own lease agreement</p>
            <p className="text-xs text-muted-foreground">
              Attached to every booking exactly as you upload it. Because we
              cannot fill it in for you, leave blanks for the tenant&apos;s name,
              dates and signatures — and do not include anyone&apos;s personal
              details, since prospective tenants can read this before booking.
            </p>
          </div>

          {!listing ? (
            <p className="text-xs text-muted-foreground">
              Continue past the first step to create your draft, then come back
              to upload.
            </p>
          ) : document ? (
            <div className="flex items-center gap-3 rounded-md border bg-muted/30 p-3">
              <FileText className="h-5 w-5 shrink-0 text-muted-foreground" />
              <div className="min-w-0 flex-1">
                <p className="truncate text-sm font-medium">{document.fileName}</p>
                <p className="text-xs text-muted-foreground">
                  {formatBytes(document.sizeBytes)} &middot; uploaded{" "}
                  {new Date(document.uploadedAtUtc).toLocaleDateString()}
                </p>
              </div>
              <Badge variant="secondary">Attached</Badge>
              <Button
                type="button"
                variant="ghost"
                size="sm"
                disabled={removeMutation.isPending}
                onClick={() => removeMutation.mutate()}
              >
                {removeMutation.isPending ? (
                  <Loader2 className="h-4 w-4 animate-spin" />
                ) : (
                  <Trash2 className="h-4 w-4" />
                )}
                <span className="sr-only">Remove lease agreement</span>
              </Button>
            </div>
          ) : (
            <div
              className={cn(
                "flex flex-col items-center gap-2 rounded-md border border-dashed p-6 text-center transition-colors",
                isDragging && "border-primary bg-primary/5",
              )}
              onDragOver={(e) => {
                e.preventDefault();
                setIsDragging(true);
              }}
              onDragLeave={() => setIsDragging(false)}
              onDrop={(e) => {
                e.preventDefault();
                setIsDragging(false);
                handleFile(e.dataTransfer.files?.[0]);
              }}
            >
              <Upload className="h-6 w-6 text-muted-foreground" />
              <p className="text-sm">Drag your lease here, or</p>
              <Button
                type="button"
                variant="outline"
                size="sm"
                disabled={uploadMutation.isPending}
                onClick={() => fileInputRef.current?.click()}
              >
                {uploadMutation.isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                Choose a file
              </Button>
              <p className="text-xs text-muted-foreground">PDF or Word (.docx), up to 10 MB</p>
              <input
                ref={fileInputRef}
                type="file"
                className="hidden"
                accept=".pdf,.docx"
                onChange={(e) => {
                  handleFile(e.target.files?.[0]);
                  e.target.value = "";
                }}
              />
            </div>
          )}

          {uploadError && <FormError message={uploadError} />}
          <FormError message={form.formState.errors.hasCustomLeaseDocument?.message} />
        </div>
      )}
    </div>
  );
}

function formatBytes(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${Math.round(bytes / 1024)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

function readErrorDetail(error: unknown): string {
  return (
    (error as { response?: { data?: { detail?: string } } })?.response?.data?.detail
    ?? (error instanceof Error ? error.message : "Upload failed.")
  );
}
