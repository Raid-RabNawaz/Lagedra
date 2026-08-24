import { useMemo, useState } from "react";
import { Download, Link2, AlertTriangle, CheckCircle2 } from "lucide-react";
import type {
  AmenityDefinitionDto,
  ImportedListingDraftDto,
  ImportedPhotoCandidateDto,
} from "@/api/types";
import type { ListingFormValues } from "@/features/listings/lib/listingFormSchema";
import { mapImportedDraftToForm } from "@/features/listings/lib/mapImportedDraftToForm";
import { useListingImport } from "@/features/listings/hooks/useListingImport";
import { MAX_IMPORT_PHOTOS } from "@/features/listings/lib/importListingPhotos";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Loader } from "@/components/shared/Loader";

export type ApplyImportPayload = {
  values: Partial<ListingFormValues>;
  photos: ImportedPhotoCandidateDto[];
  sourceHost?: string | null;
};

type ImportFromUrlPanelProps = {
  amenities: AmenityDefinitionDto[];
  onApply: (payload: ApplyImportPayload) => void;
  /**
   * Optional. When provided, shows an "Import & review" action that creates a
   * draft listing immediately and takes the host to the review page (falling
   * back to pre-filling the wizard when the import lacks required fields).
   */
  onImportToReview?: (payload: ApplyImportPayload) => void;
  creating?: boolean;
  disabled?: boolean;
};

const VERIFY_HINT = "Imported — please verify";

export function ImportFromUrlPanel({
  amenities,
  onApply,
  onImportToReview,
  creating,
  disabled,
}: ImportFromUrlPanelProps) {
  const [open, setOpen] = useState(false);
  const [url, setUrl] = useState("");
  const [attested, setAttested] = useState(false);
  const [draft, setDraft] = useState<ImportedListingDraftDto | null>(null);
  const [selectedPhotoUrls, setSelectedPhotoUrls] = useState<Set<string>>(new Set());
  const [applied, setApplied] = useState(false);

  const importMutation = useListingImport();
  const busy = importMutation.isPending || Boolean(creating);

  const mapping = useMemo(
    () => (draft ? mapImportedDraftToForm(draft, amenities) : null),
    [draft, amenities],
  );

  const canFetch = url.trim().length > 0 && attested && !importMutation.isPending;

  const handleFetch = () => {
    setApplied(false);
    importMutation.mutate(
      { url: url.trim(), hostAttestation: attested },
      {
        onSuccess: (data) => {
          setDraft(data);
          // Default-select a capped set so Airbnb galleries don't upload 50+
          // images before the host can review the draft.
          const urls = (data.photos ?? []).slice(0, MAX_IMPORT_PHOTOS).map((p) => p.url);
          setSelectedPhotoUrls(new Set(urls));
        },
      },
    );
  };

  const togglePhoto = (photoUrl: string) => {
    setSelectedPhotoUrls((prev) => {
      const next = new Set(prev);
      if (next.has(photoUrl)) {
        next.delete(photoUrl);
      } else {
        next.add(photoUrl);
      }
      return next;
    });
  };

  const reset = () => {
    setUrl("");
    setAttested(false);
    setDraft(null);
    setSelectedPhotoUrls(new Set());
    setApplied(false);
    importMutation.reset();
  };

  const handleOpenChange = (next: boolean) => {
    if (!next && busy) return;
    setOpen(next);
    if (!next) reset();
  };

  const handleApply = () => {
    if (!draft || !mapping) return;
    const photos = (draft.photos ?? [])
      .filter((p) => selectedPhotoUrls.has(p.url))
      .slice(0, MAX_IMPORT_PHOTOS);
    onApply({ values: mapping.values, photos, sourceHost: draft.sourceHost });
    handleOpenChange(false);
  };

  const handleImportToReview = () => {
    if (!draft || !mapping || !onImportToReview) return;
    const photos = (draft.photos ?? [])
      .filter((p) => selectedPhotoUrls.has(p.url))
      .slice(0, MAX_IMPORT_PHOTOS);
    onImportToReview({ values: mapping.values, photos, sourceHost: draft.sourceHost });
  };

  const handleDiscard = () => {
    setDraft(null);
    setSelectedPhotoUrls(new Set());
    setApplied(false);
    importMutation.reset();
  };

  return (
    <>
      <Button type="button" variant="outline" onClick={() => setOpen(true)} disabled={disabled}>
        <Link2 className="mr-2 h-4 w-4" />
        Import from URL
      </Button>

      <Dialog open={open} onOpenChange={handleOpenChange}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Import from a listing URL</DialogTitle>
            <DialogDescription>
              Paste a public URL for a listing you own and we'll try to pre-fill the form. You can
              review and edit everything before saving.
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="import-url">Listing URL</Label>
              <Input
                id="import-url"
                type="url"
                inputMode="url"
                placeholder="https://example.com/your-listing"
                value={url}
                onChange={(e) => setUrl(e.target.value)}
                disabled={disabled || importMutation.isPending}
              />
            </div>

            <label className="flex items-start gap-2 text-sm">
              <input
                type="checkbox"
                className="mt-0.5 h-4 w-4 rounded border-input"
                checked={attested}
                onChange={(e) => setAttested(e.target.checked)}
                disabled={disabled || importMutation.isPending}
              />
              <span className="text-muted-foreground">
                I confirm this listing belongs to me and I have rights to its content.
              </span>
            </label>

            <div>
              <Button type="button" onClick={handleFetch} disabled={!canFetch || disabled}>
                <Download className="mr-2 h-4 w-4" />
                {importMutation.isPending ? "Fetching..." : "Fetch"}
              </Button>
            </div>

            {importMutation.isPending && (
              <Loader label="Reading the page. This can take up to a minute for some sites..." />
            )}

            {importMutation.isError && (
              <Alert variant="destructive">
                <AlertTriangle className="h-4 w-4" />
                <AlertDescription>{importMutation.error.message}</AlertDescription>
              </Alert>
            )}

            {draft && mapping && !importMutation.isPending && (
              <div className="space-y-4 rounded-lg border bg-muted/30 p-4">
                <div className="flex items-center gap-2 text-sm font-medium">
                  <CheckCircle2 className="h-4 w-4 text-emerald-600" />
                  <span>
                    Found details{draft.sourceHost ? ` from ${draft.sourceHost}` : ""}. {VERIFY_HINT}.
                  </span>
                </div>

                {mapping.importedFields.length > 0 ? (
                  <div className="space-y-1 text-sm">
                    <p className="font-medium">Will be pre-filled:</p>
                    <ul className="list-disc pl-5 text-muted-foreground">
                      {mapping.importedFields.map((field) => (
                        <li key={field}>{field}</li>
                      ))}
                    </ul>
                  </div>
                ) : (
                  <p className="text-sm text-muted-foreground">
                    We couldn't read any form fields from that page. You can still enter the details
                    manually.
                  </p>
                )}

                {mapping.monthlyDerivedFromNightly && (
                  <Alert variant="default" className="border-amber-300 bg-amber-50 text-amber-900">
                    <AlertTriangle className="h-4 w-4" />
                    <AlertTitle>Estimated monthly rent</AlertTitle>
                    <AlertDescription>
                      We only found a nightly rate, so the monthly rent was estimated as nightly × 30.
                      Please confirm the amount.
                    </AlertDescription>
                  </Alert>
                )}

                {draft.currency && draft.currency !== "USD" && (
                  <Alert variant="default" className="border-amber-300 bg-amber-50 text-amber-900">
                    <AlertTriangle className="h-4 w-4" />
                    <AlertDescription>
                      Prices were detected in {draft.currency}. The form uses your account currency, so
                      double-check the amount.
                    </AlertDescription>
                  </Alert>
                )}

                {mapping.amenitiesTotal > 0 && (
                  <p className="text-sm text-muted-foreground">
                    Amenities matched: {mapping.amenitiesMatched} of {mapping.amenitiesTotal} found.
                  </p>
                )}

                {draft.approxAddress && (
                  <p className="text-sm text-muted-foreground">
                    Location detected: {draft.approxAddress}. You'll set the precise map location after
                    creating the listing.
                  </p>
                )}

                {draft.photos && draft.photos.length > 0 && (
                  <div className="space-y-2">
                    <p className="text-sm font-medium">
                      Photos ({selectedPhotoUrls.size} of {draft.photos.length} selected
                      {draft.photos.length > MAX_IMPORT_PHOTOS
                        ? ` · up to ${MAX_IMPORT_PHOTOS} upload at once`
                        : ""}
                      )
                    </p>
                    <p className="text-xs text-muted-foreground">
                      Selected photos are uploaded through Lagedra's media pipeline after the listing
                      is created (max {MAX_IMPORT_PHOTOS} at a time). Some sites may block this; any
                      that fail can be added manually.
                    </p>
                    <div className="grid grid-cols-2 gap-2 sm:grid-cols-3">
                      {draft.photos.map((photo) => (
                        <label
                          key={photo.url}
                          className="flex cursor-pointer items-center gap-2 rounded-md border bg-background p-2 text-xs"
                        >
                          <input
                            type="checkbox"
                            className="h-4 w-4 rounded border-input"
                            checked={selectedPhotoUrls.has(photo.url)}
                            onChange={() => togglePhoto(photo.url)}
                          />
                          <span className="truncate" title={photo.altText ?? photo.url}>
                            {photo.altText?.trim() || "Photo"}
                          </span>
                        </label>
                      ))}
                    </div>
                  </div>
                )}

                <div className="flex flex-wrap gap-2 pt-1">
                  {onImportToReview && (
                    <Button type="button" onClick={handleImportToReview} disabled={creating}>
                      {creating ? "Creating draft..." : "Import & review"}
                    </Button>
                  )}
                  <Button
                    type="button"
                    variant={onImportToReview ? "outline" : "default"}
                    onClick={handleApply}
                    disabled={applied || creating}
                  >
                    {applied ? "Applied" : "Apply to form"}
                  </Button>
                  <Button
                    type="button"
                    variant="outline"
                    onClick={handleDiscard}
                    disabled={creating}
                  >
                    Discard
                  </Button>
                </div>

                {onImportToReview && (
                  <p className="text-xs text-muted-foreground">
                    "Import & review" creates a draft listing and opens it for review. Nothing is
                    published until you publish it yourself.
                  </p>
                )}
              </div>
            )}
          </div>
        </DialogContent>
      </Dialog>
    </>
  );
}
