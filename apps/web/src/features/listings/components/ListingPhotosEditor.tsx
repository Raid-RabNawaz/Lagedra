import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useCallback, useEffect, useRef, useState } from "react";
import {
  ImagePlus,
  Trash2,
  Star,
  GripVertical,
  ChevronUp,
  ChevronDown,
  Upload,
  Film,
  Loader2,
  CheckCircle2,
} from "lucide-react";
import { PhotoThumbBox } from "@/features/listings/components/PhotoThumbBox";
import { listingApi } from "@/features/listings/services/listingApi";
import {
  imageFilesFromList,
  uploadListingPhotoFiles,
} from "@/features/listings/lib/uploadListingPhotoFiles";
import type { ListingDetailsDto } from "@/api/types";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { cn } from "@/lib/utils";

type ListingPhotosEditorProps = {
  listing: ListingDetailsDto;
  /** Disables uploads and photo management (e.g. platform admin inspect). */
  readOnly?: boolean;
};

/**
 * Photos & video card: device uploads, URL adds, captions, cover selection,
 * drag/button reordering and deletion. Saves through the listing photo/media
 * endpoints and invalidates ["listing", id], so it works both inside the
 * create wizard and on the edit page.
 */
export function ListingPhotosEditor({
  listing,
  readOnly = false,
}: ListingPhotosEditorProps) {
  const queryClient = useQueryClient();
  const id = listing.id;

  const [photoUrl, setPhotoUrl] = useState("");
  const [photoCaption, setPhotoCaption] = useState("");
  const [dragIdx, setDragIdx] = useState<number | null>(null);
  const [mediaError, setMediaError] = useState<string | null>(null);
  const [uploadProgress, setUploadProgress] = useState<{
    completed: number;
    total: number;
  } | null>(null);
  const [localPreviews, setLocalPreviews] = useState<
    { id: string; url: string; name: string }[]
  >([]);
  const photoCountAtUpload = useRef<number | null>(null);

  const dropPreviews = useCallback(() => {
    setLocalPreviews((current) => {
      for (const preview of current) URL.revokeObjectURL(preview.url);
      return [];
    });
  }, []);

  const hasPhotos = listing.photos.length > 0;

  const addPhotoMutation = useMutation({
    mutationFn: () => {
      const url = photoUrl.trim();
      if (!url) throw new Error("Enter an image URL.");
      try {
        new URL(url);
      } catch {
        throw new Error("Enter a valid URL.");
      }
      const storageKey = `web/${crypto.randomUUID()}`;
      return listingApi.addPhoto(id, { storageKey, url, caption: photoCaption.trim() || null });
    },
    onSuccess: () => {
      setPhotoUrl("");
      setPhotoCaption("");
      void queryClient.invalidateQueries({ queryKey: ["listing", id] });
    },
  });

  const uploadPhotosMutation = useMutation({
    mutationFn: (files: File[]) =>
      uploadListingPhotoFiles(id, files, {
        caption: photoCaption,
        onProgress: (completed, total) => setUploadProgress({ completed, total }),
      }),
    onSuccess: (result) => {
      setPhotoCaption("");
      if (result.failed > 0 && result.uploaded === 0) {
        setMediaError("None of the selected photos could be uploaded.");
      } else if (result.failed > 0) {
        setMediaError(
          `${result.uploaded} uploaded. ${result.failed} could not be uploaded.`,
        );
      } else {
        setMediaError(null);
      }
      void queryClient.invalidateQueries({ queryKey: ["listing", id] });
    },
    onError: (error: unknown) => {
      const detail =
        (error as { response?: { data?: { detail?: string } } })?.response?.data?.detail ??
        (error instanceof Error ? error.message : "Upload failed.");
      setMediaError(detail);
    },
    onSettled: (result) => {
      setUploadProgress(null);
      if (!result || result.uploaded === 0) dropPreviews();
    },
  });

  useEffect(() => {
    if (uploadPhotosMutation.isPending) return;
    if (photoCountAtUpload.current == null) return;
    if (listing.photos.length <= photoCountAtUpload.current) return;
    photoCountAtUpload.current = null;
    dropPreviews();
  }, [listing.photos.length, uploadPhotosMutation.isPending, dropPreviews]);

  const uploadVideoMutation = useMutation({
    mutationFn: (file: File) => listingApi.uploadMedia(id, file, null),
    onSuccess: () => {
      setMediaError(null);
      void queryClient.invalidateQueries({ queryKey: ["listing", id] });
    },
    onError: (error: unknown) => {
      const detail =
        (error as { response?: { data?: { detail?: string } } })?.response?.data?.detail ??
        (error instanceof Error ? error.message : "Upload failed.");
      setMediaError(detail);
    },
  });

  const mediaBusy =
    uploadPhotosMutation.isPending || uploadVideoMutation.isPending;

  const removePhotoMutation = useMutation({
    mutationFn: (photoId: string) => listingApi.removePhoto(id, photoId),
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: ["listing", id] }),
  });

  const coverMutation = useMutation({
    mutationFn: (photoId: string) => listingApi.setCoverPhoto(id, photoId),
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: ["listing", id] }),
  });

  const reorderMutation = useMutation({
    mutationFn: (photoIds: string[]) => listingApi.reorderPhotos(id, photoIds),
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: ["listing", id] }),
  });

  const sorted = listing.photos.slice().sort((a, b) => a.sortOrder - b.sortOrder);

  const movePhoto = (from: number, to: number) => {
    if (to < 0 || to >= sorted.length) return;
    const ids = sorted.map((p) => p.id);
    const [moved] = ids.splice(from, 1);
    ids.splice(to, 0, moved);
    reorderMutation.mutate(ids);
  };

  const handleDrop = (targetIdx: number) => {
    if (dragIdx === null || dragIdx === targetIdx) return;
    movePhoto(dragIdx, targetIdx);
    setDragIdx(null);
  };

  return (
    <Card id="photos" className="scroll-mt-24">
      <CardHeader>
        <CardTitle className="text-lg flex items-center gap-2">
          <ImagePlus className="h-5 w-5" />
          Photos &amp; video
          {hasPhotos ? (
            <Badge variant="secondary" className="ml-2">
              <CheckCircle2 className="h-3 w-3 mr-1" />
              {listing.photos.length} added
            </Badge>
          ) : (
            <Badge variant="outline" className="ml-2">Recommended</Badge>
          )}
        </CardTitle>
        <CardDescription>
          Upload one or more photos from your device (stored on Lagedra object storage)
          or add an existing image URL. First photo can be set as cover.
        </CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        <fieldset disabled={readOnly} className="min-w-0 space-y-4 border-0 p-0 m-0">
        <div className="space-y-1.5">
          <Label htmlFor="caption">Caption (optional, applies to this upload)</Label>
          <Input
            id="caption"
            value={photoCaption}
            onChange={(e) => setPhotoCaption(e.target.value)}
            placeholder="e.g. Living room"
          />
        </div>

        <div className="rounded-lg border border-dashed p-4 space-y-3">
          <div className="flex flex-wrap items-center gap-3">
            <Button
              type="button"
              variant="secondary"
              disabled={readOnly || mediaBusy}
              className="relative"
            >
              <label className="cursor-pointer flex items-center">
                {uploadPhotosMutation.isPending ? (
                  <Loader2 className="h-4 w-4 animate-spin mr-2" />
                ) : (
                  <Upload className="h-4 w-4 mr-2" />
                )}
                {uploadPhotosMutation.isPending && uploadProgress
                  ? `Uploading ${uploadProgress.completed} of ${uploadProgress.total}...`
                  : uploadPhotosMutation.isPending
                    ? "Uploading..."
                    : "Upload photos"}
                <input
                  type="file"
                  accept="image/jpeg,image/png,image/gif,image/webp,image/heic,image/heif"
                  multiple
                  className="absolute inset-0 opacity-0 cursor-pointer"
                  disabled={mediaBusy}
                  onChange={(e) => {
                    const files = imageFilesFromList(e.target.files);
                    const inputEl = e.target;
                    if (files.length > 0) {
                      setMediaError(null);
                      photoCountAtUpload.current = listing.photos.length;
                      const next = files.map((file) => ({
                        id: crypto.randomUUID(),
                        url: URL.createObjectURL(file),
                        name: file.name,
                      }));
                      setLocalPreviews((current) => {
                        for (const preview of current) URL.revokeObjectURL(preview.url);
                        return next;
                      });
                      uploadPhotosMutation.mutate(files);
                    } else if (e.target.files && e.target.files.length > 0) {
                      setMediaError("Select JPEG, PNG, GIF, WebP, or HEIC images.");
                    }
                    inputEl.value = "";
                  }}
                />
              </label>
            </Button>

            <Button
              type="button"
              variant="outline"
              disabled={mediaBusy}
              className="relative"
            >
              <label className="cursor-pointer flex items-center">
                {uploadVideoMutation.isPending ? (
                  <Loader2 className="h-4 w-4 animate-spin mr-2" />
                ) : (
                  <Film className="h-4 w-4 mr-2" />
                )}
                {uploadVideoMutation.isPending
                  ? "Uploading..."
                  : "Upload virtual tour video"}
                <input
                  type="file"
                  accept="video/mp4,video/quicktime,video/webm"
                  className="absolute inset-0 opacity-0 cursor-pointer"
                  disabled={mediaBusy}
                  onChange={(e) => {
                    const file = e.target.files?.[0];
                    const inputEl = e.target;
                    if (file) {
                      setMediaError(null);
                      uploadVideoMutation.mutate(file);
                    }
                    inputEl.value = "";
                  }}
                />
              </label>
            </Button>
          </div>
          <p className="text-[11px] text-muted-foreground">
            Select multiple photos at once. JPEG, PNG, GIF, WebP, HEIC up to 15 MB each.
            Videos: MP4, MOV, WebM up to 100 MB. A video replaces the listing&apos;s virtual
            tour URL.
          </p>
          {mediaError && <p className="text-sm text-destructive">{mediaError}</p>}
        </div>

        <div className="space-y-1.5">
          <Label htmlFor="photoUrl">Or add an existing image URL</Label>
          <Input
            id="photoUrl"
            value={photoUrl}
            onChange={(e) => setPhotoUrl(e.target.value)}
            placeholder="https://..."
          />
        </div>
        {addPhotoMutation.isError && (
          <p className="text-sm text-destructive">{(addPhotoMutation.error as Error).message}</p>
        )}
        <Button
          type="button"
          variant="ghost"
          disabled={addPhotoMutation.isPending || !photoUrl.trim()}
          onClick={() => addPhotoMutation.mutate()}
        >
          {addPhotoMutation.isPending ? "Adding..." : "Add photo from URL"}
        </Button>

        <div className="space-y-3 border-t pt-4">
          {sorted.length === 0 && localPreviews.length === 0 ? (
            <p className="text-sm text-muted-foreground">No photos yet.</p>
          ) : (
            <ul className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4">
              {localPreviews.map((preview) => (
                <li
                  key={preview.id}
                  className="overflow-hidden rounded-lg border bg-background"
                >
                  <PhotoThumbBox src={preview.url} alt={preview.name} className="rounded-none" />
                  <p className="truncate px-2 py-1.5 text-xs text-muted-foreground">
                    Uploading…
                  </p>
                </li>
              ))}
              {sorted.map((p, idx) => (
                <li
                  key={p.id}
                  draggable={!readOnly}
                  onDragStart={() => setDragIdx(idx)}
                  onDragEnd={() => setDragIdx(null)}
                  onDragOver={(e) => e.preventDefault()}
                  onDrop={() => handleDrop(idx)}
                  className={cn(
                    "overflow-hidden rounded-lg border bg-background text-sm transition-colors",
                    dragIdx === idx && "opacity-50",
                    dragIdx !== null && dragIdx !== idx && "border-dashed border-accent/40",
                  )}
                >
                  <div className="relative">
                    <PhotoThumbBox
                      src={p.url}
                      alt={p.caption ?? `Photo ${idx + 1}`}
                      className="rounded-none"
                    />
                    {p.isCover && (
                      <Badge variant="accent" className="absolute left-2 top-2 text-[10px]">
                        Cover
                      </Badge>
                    )}
                  </div>
                  <div className="flex items-center gap-0.5 p-1">
                    <GripVertical className="h-4 w-4 shrink-0 cursor-grab text-muted-foreground" />
                    <span className="min-w-0 flex-1 truncate text-xs">
                      {p.caption || `Photo ${idx + 1}`}
                    </span>
                    <Button
                      type="button"
                      variant="ghost"
                      size="icon"
                      className="h-7 w-7"
                      disabled={idx === 0 || reorderMutation.isPending}
                      onClick={() => movePhoto(idx, idx - 1)}
                      aria-label="Move photo earlier"
                    >
                      <ChevronUp className="h-3.5 w-3.5" />
                    </Button>
                    <Button
                      type="button"
                      variant="ghost"
                      size="icon"
                      className="h-7 w-7"
                      disabled={idx === sorted.length - 1 || reorderMutation.isPending}
                      onClick={() => movePhoto(idx, idx + 1)}
                      aria-label="Move photo later"
                    >
                      <ChevronDown className="h-3.5 w-3.5" />
                    </Button>
                    {!p.isCover && (
                      <Button
                        type="button"
                        variant="ghost"
                        size="icon"
                        className="h-7 w-7"
                        onClick={() => coverMutation.mutate(p.id)}
                        disabled={coverMutation.isPending}
                        aria-label="Set as cover"
                      >
                        <Star className="h-3.5 w-3.5" />
                      </Button>
                    )}
                    <Button
                      type="button"
                      variant="ghost"
                      size="icon"
                      className="h-7 w-7 text-destructive"
                      onClick={() => removePhotoMutation.mutate(p.id)}
                      disabled={removePhotoMutation.isPending}
                      aria-label="Remove photo"
                    >
                      <Trash2 className="h-3.5 w-3.5" />
                    </Button>
                  </div>
                </li>
              ))}
            </ul>
          )}
        </div>
        </fieldset>
      </CardContent>
    </Card>
  );
}
