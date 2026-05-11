import { Link, useNavigate } from "react-router-dom";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { ArrowLeft, AlertTriangle } from "lucide-react";
import { useAuthStore } from "@/app/auth/authStore";
import { listingApi } from "@/features/listings/services/listingApi";
import { ListingWizard } from "@/features/listings/components/ListingWizard";
import { useListingDefinitions } from "@/features/listings/hooks/useListingDefinitions";
import { toCreateListingRequest } from "@/features/listings/lib/toListingRequests";
import type { ListingFormValues } from "@/features/listings/lib/listingFormSchema";
import { Loader } from "@/components/shared/Loader";
import { Alert, AlertDescription } from "@/components/ui/alert";

export const CreateListingPage = () => {
  const user = useAuthStore((s) => s.user);
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const defs = useListingDefinitions();

  const mutation = useMutation({
    mutationFn: async (values: ListingFormValues) => {
      if (!user) throw new Error("You must be signed in.");
      return listingApi.create(toCreateListingRequest(values));
    },
    onSuccess: (data) => {
      void queryClient.invalidateQueries({ queryKey: ["listings", "mine"] });
      void navigate(`/app/listings/${data.id}/edit`, { replace: true });
    },
  });

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

      {mutation.isError && (
        <Alert variant="destructive">
          <AlertTriangle className="h-4 w-4" />
          <AlertDescription>
            {(mutation.error as Error)?.message ??
              "Failed to create listing. Check all fields and try again."}
          </AlertDescription>
        </Alert>
      )}

      <ListingWizard
        definitions={defs.data}
        submitLabel="Create listing"
        onSubmit={async (values) => {
          await mutation.mutateAsync(values);
        }}
      />
    </div>
  );
};
