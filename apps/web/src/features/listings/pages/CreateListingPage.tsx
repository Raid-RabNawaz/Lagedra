import { useNavigate } from "react-router-dom";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useAuthStore } from "@/app/auth/authStore";
import { listingApi } from "@/features/listings/services/listingApi";
import { ListingForm } from "@/features/listings/components/ListingForm";
import { useListingDefinitions } from "@/features/listings/hooks/useListingDefinitions";
import { toCreateListingRequest } from "@/features/listings/lib/toListingRequests";
import type { ListingFormValues } from "@/features/listings/lib/listingFormSchema";
import { Loader } from "@/components/shared/Loader";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { AlertTriangle } from "lucide-react";

export const CreateListingPage = () => {
  const user = useAuthStore((s) => s.user);
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const defs = useListingDefinitions();

  const mutation = useMutation({
    mutationFn: async (values: ListingFormValues) => {
      if (!user) throw new Error("You must be signed in.");
      return listingApi.create(toCreateListingRequest(values, user.userId));
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
        <h1 className="text-3xl font-bold tracking-tight">Create a listing</h1>
        <p className="mt-1 text-muted-foreground">
          Add details below. You can set the map location and photos on the next step before publishing.
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

      <ListingForm
        definitions={defs.data}
        submitLabel="Save & continue"
        onSubmit={async (values) => {
          await mutation.mutateAsync(values);
        }}
      />
    </div>
  );
};
