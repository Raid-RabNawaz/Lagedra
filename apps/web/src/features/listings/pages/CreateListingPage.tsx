import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { AlertTriangle, Info } from "lucide-react";
import { useAuthStore } from "@/app/auth/authStore";
import { listingApi } from "@/features/listings/services/listingApi";
import { ListingWizard } from "@/features/listings/components/ListingWizard";
import {
  ImportFromUrlPanel,
  type ApplyImportPayload,
} from "@/features/listings/components/ImportFromUrlPanel";
import { ImportFromExcelDialog } from "@/features/listings/components/ImportFromExcelDialog";
import { useListingDefinitions } from "@/features/listings/hooks/useListingDefinitions";
import {
  toCreateListingRequest,
  toUpdateListingRequest,
} from "@/features/listings/lib/toListingRequests";
import { importListingPhotos } from "@/features/listings/lib/importListingPhotos";
import {
  listingFormSchema,
  defaultListingFormValues,
  type ListingFormValues,
} from "@/features/listings/lib/listingFormSchema";
import type { ImportedPhotoCandidateDto } from "@/api/types";
import { BackLink } from "@/components/shared/BackLink";
import { Loader } from "@/components/shared/Loader";
import { Alert, AlertDescription } from "@/components/ui/alert";

// Friendly labels for the required fields an import can leave incomplete, used
// to explain why "Import & review" pre-filled the form instead of creating a
// draft outright.
const REQUIRED_FIELD_LABELS: Partial<Record<keyof ListingFormValues, string>> = {
  title: "Title (at least 5 characters)",
  description: "Description (at least 50 characters)",
  monthlyRentDollars: "Monthly rent",
  maxDepositDollars: "Maximum deposit",
  bedrooms: "Bedrooms",
  bathrooms: "Bathrooms",
  checkInTime: "Check-in time",
  checkOutTime: "Check-out time",
  maxGuests: "Max guests",
};

function describeMissingFields(issues: readonly { path: PropertyKey[] }[]): string[] {
  const labels = new Set<string>();
  for (const issue of issues) {
    const key = issue.path[0];
    if (typeof key === "string") {
      labels.add(REQUIRED_FIELD_LABELS[key as keyof ListingFormValues] ?? key);
    }
  }
  return [...labels];
}

export const CreateListingPage = () => {
  const user = useAuthStore((s) => s.user);
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const defs = useListingDefinitions();

  // Optional values pre-filled from an imported listing URL. Undefined means the
  // wizard keeps its normal defaults, so existing behaviour is untouched.
  const [importedDefaults, setImportedDefaults] = useState<Partial<ListingFormValues>>();
  const [pendingPhotos, setPendingPhotos] = useState<ImportedPhotoCandidateDto[]>([]);
  // Bumping this key remounts the wizard so react-hook-form picks up the new
  // default values when an import is applied.
  const [wizardKey, setWizardKey] = useState(0);
  // Explains the outcome of an "Import & review" action when it could not create
  // a draft outright (e.g. the imported description was too short).
  const [reviewNote, setReviewNote] = useState<string | null>(null);

  // The draft created after the Basics step. The wizard's location and photos
  // steps save through listing-scoped endpoints that invalidate
  // ["listing", id], so we keep the draft in the shared query cache.
  const [draftId, setDraftId] = useState<string | null>(null);
  const draftQuery = useQuery({
    queryKey: ["listing", draftId],
    queryFn: () => listingApi.getDetail(draftId!),
    enabled: Boolean(draftId),
  });

  // Creates the draft when the host completes the Basics step. Fields the host
  // hasn't reached yet are sent with the form defaults and refined by the
  // later steps via update calls.
  const createDraftMutation = useMutation({
    mutationFn: async (values: ListingFormValues) => {
      if (!user) throw new Error("You must be signed in.");
      const created = await listingApi.create(toCreateListingRequest(values));
      // Re-upload any selected imported photos through the existing media
      // pipeline. No-op when nothing was selected.
      if (pendingPhotos.length > 0) {
        await importListingPhotos(created.id, pendingPhotos);
      }
      return created;
    },
    onSuccess: (data) => {
      queryClient.setQueryData(["listing", data.id], data);
      setDraftId(data.id);
      void queryClient.invalidateQueries({ queryKey: ["listings", "mine"] });
      if (pendingPhotos.length > 0) {
        // Imported photos were uploaded after the snapshot above.
        void queryClient.invalidateQueries({ queryKey: ["listing", data.id] });
      }
    },
  });

  // Persists progress each time the host advances past a form step.
  const saveProgressMutation = useMutation({
    mutationFn: async (values: ListingFormValues) => {
      if (!draftId) throw new Error("Draft has not been created yet.");
      return listingApi.update(draftId, toUpdateListingRequest(values));
    },
    onSuccess: (data) => {
      queryClient.setQueryData(["listing", data.id], data);
    },
  });

  // Final step: save once more, then hand off to the listing page where the
  // host can submit for review.
  const finishMutation = useMutation({
    mutationFn: async (values: ListingFormValues) => {
      if (!draftId) throw new Error("Draft has not been created yet.");
      return listingApi.update(draftId, toUpdateListingRequest(values));
    },
    onSuccess: (data) => {
      queryClient.setQueryData(["listing", data.id], data);
      void queryClient.invalidateQueries({ queryKey: ["listings", "mine"] });
      void navigate(`/app/listings/${data.id}`, { replace: true });
    },
  });

  // "Import & review": create the draft immediately from imported values merged
  // over the form defaults, then land the host on the edit/review page. The
  // create endpoint is unchanged — the listing is created in Draft status and
  // still requires an explicit publish, so nothing goes live unreviewed.
  const reviewMutation = useMutation({
    mutationFn: async (input: { values: ListingFormValues; photos: ImportedPhotoCandidateDto[] }) => {
      if (!user) throw new Error("You must be signed in.");
      const created = await listingApi.create(toCreateListingRequest(input.values));
      if (input.photos.length > 0) {
        await importListingPhotos(created.id, input.photos);
      }
      return created;
    },
    onSuccess: (data) => {
      void queryClient.invalidateQueries({ queryKey: ["listings", "mine"] });
      void navigate(`/app/listings/${data.id}/edit`, { replace: true });
    },
  });

  const handleApplyImport = (payload: ApplyImportPayload) => {
    setReviewNote(null);
    setImportedDefaults(payload.values);
    setPendingPhotos(payload.photos);
    setWizardKey((k) => k + 1);
  };

  const handleImportToReview = (payload: ApplyImportPayload) => {
    const merged = { ...defaultListingFormValues, ...payload.values };
    const parsed = listingFormSchema.safeParse(merged);
    if (parsed.success) {
      setReviewNote(null);
      reviewMutation.mutate({ values: parsed.data, photos: payload.photos });
      return;
    }

    // Not enough was imported to create a valid draft (most commonly the
    // imported description is shorter than the 50-character minimum). Pre-fill
    // the wizard so the host can finish, and explain exactly what's needed
    // instead of failing silently.
    setImportedDefaults(payload.values);
    setPendingPhotos(payload.photos);
    setWizardKey((k) => k + 1);

    const missing = describeMissingFields(parsed.error.issues);
    setReviewNote(
      missing.length > 0
        ? `We pre-filled the form below, but couldn't create a draft automatically because these still need attention: ${missing.join(", ")}. Complete them, then click "Create listing".`
        : "We pre-filled the form below. Complete the remaining details, then click \"Create listing\".",
    );
  };

  if (defs.isLoading) return <Loader fullPage label="Loading form..." />;
  if (defs.isError || !defs.data) {
    return (
      <Alert variant="destructive">
        <AlertTriangle className="h-4 w-4" />
        <AlertDescription>Could not load listing definitions.</AlertDescription>
      </Alert>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <BackLink fallbackTo="/app/listings" label="Back to my listings" />
          <h1 className="mt-2 text-3xl font-bold tracking-tight">Create a listing</h1>
          <p className="mt-1 text-muted-foreground">
            We'll walk you through everything step by step — details, location, photos and rules.
            Your draft is created after the first step and saved as you go.
          </p>
        </div>
        <ImportFromExcelDialog amenities={defs.data.amenities} />
      </div>

      <ImportFromUrlPanel
        amenities={defs.data.amenities}
        onApply={handleApplyImport}
        onImportToReview={handleImportToReview}
        creating={reviewMutation.isPending}
      />

      {(createDraftMutation.isError ||
        saveProgressMutation.isError ||
        finishMutation.isError ||
        reviewMutation.isError) && (
        <Alert variant="destructive">
          <AlertTriangle className="h-4 w-4" />
          <AlertDescription>
            {((createDraftMutation.error ??
              saveProgressMutation.error ??
              finishMutation.error ??
              reviewMutation.error) as Error)?.message ??
              "Failed to save listing. Check all fields and try again."}
          </AlertDescription>
        </Alert>
      )}

      {reviewNote && (
        <Alert variant="default" className="border-primary/30 bg-primary/5">
          <Info className="h-4 w-4" />
          <AlertDescription>{reviewNote}</AlertDescription>
        </Alert>
      )}

      <ListingWizard
        key={wizardKey}
        definitions={defs.data}
        defaultValues={importedDefaults}
        listing={draftQuery.data ?? null}
        submitLabel="Finish listing"
        onCreateDraft={async (values) => {
          await createDraftMutation.mutateAsync(values);
        }}
        onSaveProgress={async (values) => {
          await saveProgressMutation.mutateAsync(values);
        }}
        onFinish={async (values) => {
          await finishMutation.mutateAsync(values);
        }}
      />
    </div>
  );
};
