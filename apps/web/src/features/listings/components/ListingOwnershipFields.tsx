import { useState } from "react";
import type { UseFormReturn } from "react-hook-form";
import { useWatch } from "react-hook-form";
import { CheckCircle2, Search } from "lucide-react";
import { listingApi } from "@/features/listings/services/listingApi";
import { getApiErrorMessage } from "@/api/errors";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { FormError } from "@/components/shared/FormError";
import type { ListingFormValues } from "@/features/listings/lib/listingFormSchema";

type ListingOwnershipFieldsProps = {
  form: UseFormReturn<ListingFormValues>;
  showBrokerClause?: boolean;
};

export function ListingOwnershipFields({
  form,
  showBrokerClause = false,
}: ListingOwnershipFieldsProps) {
  const managerRole = useWatch({ control: form.control, name: "managerRole" });
  const homeOwnerUserId = useWatch({ control: form.control, name: "homeOwnerUserId" });
  const homeOwnerDisplayName = useWatch({ control: form.control, name: "homeOwnerDisplayName" });
  const homeOwnerEmail = useWatch({ control: form.control, name: "homeOwnerEmail" });
  const [lookupError, setLookupError] = useState<string | null>(null);
  const [isLookingUp, setIsLookingUp] = useState(false);

  const setRole = (role: ListingFormValues["managerRole"]) => {
    form.setValue("managerRole", role, { shouldDirty: true, shouldValidate: true });
    if (role === "Owner") {
      form.setValue("homeOwnerUserId", "", { shouldDirty: true, shouldValidate: true });
      form.setValue("homeOwnerEmail", "", { shouldDirty: true });
      form.setValue("homeOwnerDisplayName", "", { shouldDirty: true });
      setLookupError(null);
    }
  };

  const lookupOwner = async () => {
    const email = (form.getValues("homeOwnerEmail") ?? "").trim();
    if (!email) {
      setLookupError("Enter the email the owner used to create their Lagedra account.");
      return;
    }

    setIsLookingUp(true);
    setLookupError(null);
    try {
      const owner = await listingApi.lookupHomeOwner(email);
      form.setValue("homeOwnerUserId", owner.userId, { shouldDirty: true, shouldValidate: true });
      form.setValue("homeOwnerEmail", owner.email, { shouldDirty: true });
      form.setValue("homeOwnerDisplayName", owner.displayName, { shouldDirty: true });
    } catch (error) {
      form.setValue("homeOwnerUserId", "", { shouldDirty: true, shouldValidate: true });
      form.setValue("homeOwnerDisplayName", "", { shouldDirty: true });
      setLookupError(
        getApiErrorMessage(
          error,
          "No Lagedra account was found for that email. The owner needs to create an account first.",
        ),
      );
    } finally {
      setIsLookingUp(false);
    }
  };

  return (
    <div className="space-y-4">
      <div className="space-y-2">
        <Label>Your role for this property</Label>
        <div className="grid gap-2 sm:grid-cols-2">
          <RoleOption
            selected={managerRole === "Owner"}
            title="I am the home owner"
            detail="You own this property and will appear as the landlord on the lease."
            onSelect={() => setRole("Owner")}
          />
          <RoleOption
            selected={managerRole === "PropertyManager"}
            title="I am the property manager"
            detail="You manage this property. You can name the home owner now or add them later before you submit for review."
            onSelect={() => setRole("PropertyManager")}
          />
        </div>
      </div>

      {managerRole === "PropertyManager" && (
        <div className="space-y-3 rounded-lg border bg-muted/30 p-3">
          <p className="text-xs text-muted-foreground">
            Optional for now. Look up the home owner by the email on their Lagedra account.
            They will be named on the lease and asked to consent to the tenancy. You will
            need this before submitting the listing for review.
          </p>
          <div className="flex flex-col gap-2 sm:flex-row sm:items-end">
            <div className="min-w-0 flex-1 space-y-2">
              <Label htmlFor="homeOwnerEmail">Home owner account email (optional)</Label>
              <Input
                id="homeOwnerEmail"
                type="email"
                autoComplete="off"
                placeholder="owner@email.com"
                {...form.register("homeOwnerEmail")}
              />
            </div>
            <Button
              type="button"
              variant="outline"
              disabled={isLookingUp}
              onClick={() => void lookupOwner()}
            >
              <Search className="h-4 w-4" />
              {isLookingUp ? "Looking up..." : "Find account"}
            </Button>
          </div>
          {homeOwnerUserId && homeOwnerDisplayName && (
            <p className="flex items-start gap-2 text-sm text-foreground">
              <CheckCircle2 className="mt-0.5 h-4 w-4 shrink-0 text-accent" />
              <span>
                Found <span className="font-medium">{homeOwnerDisplayName}</span>
                {homeOwnerEmail ? ` (${homeOwnerEmail})` : ""}. This owner will be added to the
                lease.
              </span>
            </p>
          )}
          <FormError message={lookupError ?? form.formState.errors.homeOwnerUserId?.message} />
        </div>
      )}

      {showBrokerClause && (
        <label className="flex items-start gap-3 rounded-lg border p-3 cursor-pointer hover:bg-muted/30 transition-colors">
          <input
            type="checkbox"
            {...form.register("includeBrokerClause")}
            className="mt-1 rounded border-input"
          />
          <div>
            <p className="text-sm font-medium">Include the broker clause on this listing&apos;s lease</p>
            <p className="text-xs text-muted-foreground">
              Adds the broker disclosure addendum using the broker name and DRE license from your
              profile. Add those under Profile → Broker disclosure if they are not filled in yet.
            </p>
          </div>
        </label>
      )}
    </div>
  );
}

function RoleOption({
  selected,
  title,
  detail,
  onSelect,
}: {
  selected: boolean;
  title: string;
  detail: string;
  onSelect: () => void;
}) {
  return (
    <button
      type="button"
      onClick={onSelect}
      className={
        selected
          ? "rounded-lg border border-accent bg-accent/10 p-3 text-left ring-1 ring-accent/30"
          : "rounded-lg border p-3 text-left hover:bg-muted/50"
      }
    >
      <p className="text-sm font-medium">{title}</p>
      <p className="mt-1 text-xs text-muted-foreground">{detail}</p>
    </button>
  );
}
