import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { ArrowLeft, AlertTriangle, Info } from "lucide-react";
import { useAuthStore } from "@/app/auth/authStore";
import { listingApi } from "@/features/listings/services/listingApi";
import { ListingWizard } from "@/features/listings/components/ListingWizard";
import {
  ImportFromUrlPanel,
  type ApplyImportPayload,
} from "@/features/listings/components/ImportFromUrlPanel";
import { useListingDefinitions } from "@/features/listings/hooks/useListingDefinitions";
import { toCreateListingRequest } from "@/features/listings/lib/toListingRequests";
import { importListingPhotos } from "@/features/listings/lib/importListingPhotos";
import {
  listingFormSchema,
  defaultListingFormValues,
  type ListingFormValues,
} from "@/features/listings/lib/listingFormSchema";
import type { ImportedPhotoCandidateDto } from "@/api/types";
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

  const mutation = useMutation({
    mutationFn: async (values: ListingFormValues) => {
      if (!user) throw new Error("You must be signed in.");
      const created = await listingApi.create(toCreateListingRequest(values));
      // Post-create step: re-upload any selected imported photos through the
      // existing media pipeline. No-op when nothing was selected.
      if (pendingPhotos.length > 0) {
        await importListingPhotos(created.id, pendingPhotos);
      }
      return created;
    },
    onSuccess: (data) => {
      void queryClient.invalidateQueries({ queryKey: ["listings", "mine"] });
      void navigate(`/app/listings/${data.id}/edit`, { replace: true });
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
      <div>
        <Link
          to="/app/listings"
          className="inline-flex items-center gap-1.5 text-sm text-muted-foreground hover:text-foreground"
        >
          <ArrowLeft className="h-4 w-4" />
          Back to my listings
        </Link>
        <h1 className="mt-2 text-3xl font-bold tracking-tight">Create a listing</h1>
        <p className="mt-1 text-muted-foreground">
          We'll walk you through the details step by step. After you create the listing, you'll set
          the map location and upload photos before publishing.
        </p>
      </div>

      <ImportFromUrlPanel
        amenities={defs.data.amenities}
        onApply={handleApplyImport}
        onImportToReview={handleImportToReview}
        creating={reviewMutation.isPending}
      />

      {(mutation.isError || reviewMutation.isError) && (
        <Alert variant="destructive">
          <AlertTriangle className="h-4 w-4" />
          <AlertDescription>
            {((mutation.error ?? reviewMutation.error) as Error)?.message ??
              "Failed to create listing. Check all fields and try again."}
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
        submitLabel="Create listing"
        onSubmit={async (values) => {
          await mutation.mutateAsync(values);
        }}
      />
    </div>
  );
};
