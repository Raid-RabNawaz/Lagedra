import { useParams, Link, useNavigate } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useCallback, useMemo } from "react";
import {
  Send,
  Ban,
  AlertTriangle,
  Clock,
  CheckCircle2,
  Circle,
  ArrowRight,
} from "lucide-react";
import { useAuthStore } from "@/app/auth/authStore";
import {
  computeProfileCompleteness,
  MIN_HOST_PROFILE_COMPLETENESS,
} from "@/features/auth/lib/profileCompleteness";
import { roles } from "@/app/auth/roles";
import { listingApi } from "@/features/listings/services/listingApi";
import { ListingForm } from "@/features/listings/components/ListingForm";
import { ListingLocationEditor } from "@/features/listings/components/ListingLocationEditor";
import { ListingPhotosEditor } from "@/features/listings/components/ListingPhotosEditor";
import { useListingDefinitions } from "@/features/listings/hooks/useListingDefinitions";
import { listingDetailsToFormValues } from "@/features/listings/lib/mapListingToForm";
import { toUpdateListingRequest } from "@/features/listings/lib/toListingRequests";
import type { ListingFormValues } from "@/features/listings/lib/listingFormSchema";
import { BackLink } from "@/components/shared/BackLink";
import { getApiErrorMessage } from "@/api/errors";
import { Loader } from "@/components/shared/Loader";
import { HostPayoutReadinessNotice } from "@/components/shared/HostPayoutReadinessNotice";
import { useHostPayoutReadiness } from "@/features/host-onboarding/hooks/useHostStripe";
import {
  REQUIRE_PAYOUT_SETUP_TO_SUBMIT_FOR_REVIEW,
  canEditListingDetails,
  canSubmitListingForReview,
} from "@/features/listings/lib/listingSubmitGates";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Separator } from "@/components/ui/separator";
import { Badge } from "@/components/ui/badge";
import { cn } from "@/lib/utils";

export const EditListingPage = () => {
  const { id } = useParams<{ id: string }>();
  const user = useAuthStore((s) => s.user);
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const defs = useListingDefinitions();

  const listingQuery = useQuery({
    queryKey: ["listing", id],
    queryFn: () => listingApi.getDetail(id!),
    enabled: Boolean(id),
  });

  const listing = listingQuery.data;

  const updateMutation = useMutation({
    mutationFn: (values: ListingFormValues) =>
      listingApi.update(id!, toUpdateListingRequest(values)),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["listing", id] });
      void queryClient.invalidateQueries({ queryKey: ["listings", "mine"] });
    },
  });

  const submitMutation = useMutation({
    mutationFn: () => listingApi.submitForReview(id!),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["listing", id] });
      void queryClient.invalidateQueries({ queryKey: ["listings", "mine"] });
    },
  });

  const closeMutation = useMutation({
    mutationFn: () => listingApi.close(id!),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["listing", id] });
      void queryClient.invalidateQueries({ queryKey: ["listings", "mine"] });
    },
  });

  const { ready: payoutsReady, settled: payoutSettled } = useHostPayoutReadiness();

  // ── Derived state (safe before early returns; falls back when listing is null) ──
  const hasLocation = listing ? listing.latitude != null && listing.longitude != null : false;
  const hasPreciseAddress = listing ? Boolean(listing.preciseAddress) : false;
  const hasPhotos = listing ? listing.photos.length > 0 : false;
  const isPropertyManager = listing?.managerRole === "PropertyManager";
  const hasHomeOwner = Boolean(listing?.homeOwnerUserId || listing?.homeOwner?.userId);
  const isDenied = listing?.status === "Denied";
  const isInReview = listing?.status === "InReview";
  const isLive = listing?.status === "Published" || listing?.status === "Activated";
  const canResubmit = listing ? canSubmitListingForReview(listing.status) : false;
  const canEditDetails = listing ? canEditListingDetails(listing.status) : false;

  // Mirror the server-side rule: SubmitForReview() requires Draft|Denied +
  // ApproxGeoPoint + a locked precise address (so the binding agreement never
  // seals with a blank city — Listing.PreciseAddressRequired). Payout setup
  // is gated by REQUIRE_PAYOUT_SETUP_TO_SUBMIT_FOR_REVIEW (temporarily off).
  // Don't block while the payout lookup is still in flight so we never flash
  // a false "blocked" state.
  const payoutBlocks =
    REQUIRE_PAYOUT_SETUP_TO_SUBMIT_FOR_REVIEW && payoutSettled && !payoutsReady;

  // Mirror the server-side host-profile gate: a faceless host can't go live,
  // because guests need to see who they're renting from before authorising a
  // payment (Listing.HostProfileIncomplete). Computed from the signed-in user's
  // profile with the same field set the backend enforces. Memoised so the
  // derived `canSubmit` stays referentially stable for the submit callback.
  const profileCompleteness = useMemo(
    () => computeProfileCompleteness(user),
    [user],
  );
  const profileComplete = profileCompleteness.meetsListingThreshold;

  const canSubmit =
    canResubmit
    && hasLocation
    && hasPreciseAddress
    && !payoutBlocks
    && profileComplete
    && (!isPropertyManager || hasHomeOwner);
  const submitLabel = isDenied ? "Resubmit for review" : "Submit for review";
  const submitBlockedReason = !listing
    ? ""
    : !canResubmit
      ? isInReview
        ? "Listing is being reviewed by an admin."
        : `Already ${listing.status.toLowerCase()}.`
      : !hasLocation
        ? "Set the approximate location below before submitting."
        : !hasPreciseAddress
          ? "Add the full property address (including city) below before submitting."
          : payoutBlocks
            ? "Set up your payout details before submitting this listing."
            : !profileComplete
              ? `Complete at least ${MIN_HOST_PROFILE_COMPLETENESS}% of your host profile before submitting (you're at ${profileCompleteness.percent}%).`
              : isPropertyManager && !hasHomeOwner
                ? "Select the home owner (they need a Lagedra account) before submitting this property-manager listing."
                : "";

  const scrollToId = useCallback((targetId: string) => {
    const el = document.getElementById(targetId);
    if (el) el.scrollIntoView({ behavior: "smooth", block: "start" });
  }, []);

  const handleSubmitClick = useCallback(() => {
    if (!canSubmit) return;
    if (!hasPhotos) {
      const ok = window.confirm(
        "This listing has no photos yet. Listings with photos perform much better — submit for review anyway?",
      );
      if (!ok) return;
    }
    submitMutation.mutate();
  }, [canSubmit, hasPhotos, submitMutation]);

  // ── Early returns (after all hooks) ─────────────────────────────────────
  if (!id) {
    return <p className="text-destructive">Missing listing id.</p>;
  }

  if (defs.isLoading || listingQuery.isLoading) {
    return <Loader fullPage label="Loading..." />;
  }

  if (defs.isError || !defs.data || listingQuery.isError || !listing) {
    return (
      <div className="space-y-4">
        <Alert variant="destructive">
          <AlertTriangle className="h-4 w-4" />
          <AlertDescription>
            {listingQuery.isError
              ? getApiErrorMessage(listingQuery.error, "Listing not found or failed to load.")
              : defs.isError
                ? getApiErrorMessage(defs.error, "Could not load listing form definitions.")
                : "Listing not found or failed to load."}
          </AlertDescription>
        </Alert>
        <BackLink fallbackTo="/app/listings" label="Back to my listings" />
      </div>
    );
  }

  const isOwner = Boolean(user && listing.landlordUserId === user.userId);
  const isPlatformAdmin = Boolean(user && String(user.role) === roles.platformAdmin);
  // Host write APIs are owner-only; admins may inspect the page but not mutate.
  if (user && !isOwner && !isPlatformAdmin) {
    return (
      <Alert variant="destructive">
        <AlertDescription>You do not have access to edit this listing.</AlertDescription>
      </Alert>
    );
  }

  const canWrite = isOwner;
  const formReadOnly = !canEditDetails || !canWrite;
  const defaultValues = listingDetailsToFormValues(listing);

  return (
    <div className="space-y-8">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <BackLink fallbackTo="/app/listings" label="My listings" className="mb-2" />
          <h1 className="text-3xl font-bold tracking-tight">Edit listing</h1>
          <div className="mt-2 flex flex-wrap items-center gap-2">
            <Badge variant="secondary">{listing.status}</Badge>
            <Link to={`/listings/${listing.id}`} className="text-sm text-accent hover:underline">
              View public page
            </Link>
          </div>
        </div>
        {canWrite && (
          <div className="flex flex-wrap gap-2">
            {canResubmit && (
              <Button
                variant="accent"
                disabled={submitMutation.isPending || !canSubmit}
                onClick={handleSubmitClick}
                title={canSubmit ? undefined : submitBlockedReason}
              >
                <Send className="h-4 w-4" />
                {submitMutation.isPending ? "Submitting..." : submitLabel}
              </Button>
            )}
            {(listing.status === "Published" || listing.status === "Activated") && (
              <Button
                variant="outline"
                disabled={closeMutation.isPending}
                onClick={() => {
                  if (window.confirm("Close this listing? It will no longer appear in search.")) {
                    closeMutation.mutate();
                  }
                }}
              >
                <Ban className="h-4 w-4" />
                Close listing
              </Button>
            )}
          </div>
        )}
      </div>

      {!canWrite && isPlatformAdmin && (
        <Alert>
          <AlertDescription>
            You&apos;re viewing this listing as a platform admin. Only the host can save changes,
            submit for review, or close the listing.
          </AlertDescription>
        </Alert>
      )}

      {canResubmit && canWrite && REQUIRE_PAYOUT_SETUP_TO_SUBMIT_FOR_REVIEW && (
        <HostPayoutReadinessNotice
          message={
            <>
              You haven&apos;t set up payouts yet. Guests pay through Lagedra, so
              this listing can only go live once there&apos;s a payout
              destination for the rent and deposit.{" "}
              <Link to="/app/payout-setup" className="font-medium underline">
                Set up payouts
              </Link>{" "}
              first, then submit for review.
            </>
          }
        />
      )}

      {isInReview && (
        <Alert>
          <Clock className="h-4 w-4" />
          <AlertDescription>
            This listing is being reviewed by an admin and is read-only until the review completes.
            We&apos;ll notify you as soon as it&apos;s approved.
          </AlertDescription>
        </Alert>
      )}

      {isLive && canWrite && (
        <Alert>
          <AlertDescription>
            This listing is live. Changes to title, pricing, amenities, house rules, and
            location go live immediately. Bookings that are already confirmed keep the terms
            that were sealed at checkout.
          </AlertDescription>
        </Alert>
      )}

      {formReadOnly && !isInReview && canWrite && (
        <Alert>
          <AlertDescription>
            Listing details can&apos;t be edited while status is{" "}
            <span className="font-medium">{listing.status}</span>. You can still update photos.
          </AlertDescription>
        </Alert>
      )}

      {isDenied && listing.rejectionReason && (
        <Alert variant="destructive">
          <AlertTriangle className="h-4 w-4" />
          <AlertDescription>
            <p className="font-medium">An admin asked you to update this listing before it can go live.</p>
            <p className="mt-1 text-sm">
              <span className="font-medium">Reason:</span> {listing.rejectionReason}
            </p>
            <p className="mt-2 text-sm">
              Make the changes below, then click <span className="font-medium">Resubmit for review</span> at the top.
            </p>
          </AlertDescription>
        </Alert>
      )}

      {updateMutation.isError && (
        <Alert variant="destructive">
          <AlertDescription>
            {getApiErrorMessage(updateMutation.error, "Failed to save changes. Try again.")}
          </AlertDescription>
        </Alert>
      )}
      {submitMutation.isError && (
        <Alert variant="destructive">
          <AlertDescription>
            {getApiErrorMessage(submitMutation.error, "Failed to submit listing for review.")}
          </AlertDescription>
        </Alert>
      )}
      {closeMutation.isError && (
        <Alert variant="destructive">
          <AlertDescription>
            {getApiErrorMessage(closeMutation.error, "Failed to close listing.")}
          </AlertDescription>
        </Alert>
      )}

      {canResubmit && canWrite && (
        <Card className="border-accent/40 bg-accent/5">
          <CardHeader>
            <CardTitle className="text-lg flex items-center gap-2">
              <Send className="h-5 w-5 text-accent" />
              {isDenied ? "Address admin feedback and resubmit" : "Ready to submit for review?"}
            </CardTitle>
            <CardDescription>
              {isDenied
                ? "Make the changes the admin requested, then resubmit. Once approved your listing will go live in the marketplace."
                : "Tenants can\u2019t see this listing until an admin approves it. These items are required before you can submit."}
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-3">
            <ChecklistRow
              done={hasLocation}
              required
              label="Set the approximate location"
              detail="Drop a pin or look up an address so tenants can see the general area."
              jumpLabel="Add location"
              onJump={() => scrollToId("location")}
            />
            <ChecklistRow
              done={hasPreciseAddress}
              required
              label="Add the full property address"
              detail="The city becomes part of the binding booking agreement, so the address (including city) is required. It stays private and is only shared with confirmed tenants."
              jumpLabel="Add address"
              onJump={() => scrollToId("precise-address")}
            />
            {isPropertyManager && (
              <ChecklistRow
                done={hasHomeOwner}
                required
                label="Select the home owner"
                detail="California stays over 30 days need the owner's consent on the lease. Look them up by the email on their Lagedra account in Ownership & lease parties."
                jumpLabel="Add owner"
                onJump={() => scrollToId("ownership")}
              />
            )}
            <ChecklistRow
              done={profileComplete}
              required
              label={`Complete your host profile (${profileCompleteness.percent}%)`}
              detail={
                profileComplete
                  ? "Guests can see who they're renting from."
                  : `Guests need to see who they're renting from before authorising a payment. Reach at least ${MIN_HOST_PROFILE_COMPLETENESS}%${
                      profileCompleteness.missing.length > 0
                        ? ` — add: ${profileCompleteness.missing.join(", ")}.`
                        : "."
                    }`
              }
              jumpLabel="Edit profile"
              onJump={() => navigate("/app/profile")}
            />
            <ChecklistRow
              done={hasPhotos}
              required={false}
              label={`Add at least one photo${hasPhotos ? ` (${listing.photos.length} added)` : ""}`}
              detail="Listings with photos get up to 10× more applications. Strongly recommended."
              jumpLabel="Add photos"
              onJump={() => scrollToId("photos")}
            />

            <div className="flex flex-col gap-2 pt-2 sm:flex-row sm:items-center sm:justify-between">
              <p className="text-xs text-muted-foreground">
                {canSubmit
                  ? hasPhotos
                    ? "All set — submit for review whenever you're ready."
                    : "You can submit without photos, but we'll ask you to confirm."
                  : submitBlockedReason}
              </p>
              <Button
                variant="accent"
                disabled={submitMutation.isPending || !canSubmit}
                onClick={handleSubmitClick}
                title={canSubmit ? undefined : submitBlockedReason}
              >
                <Send className="h-4 w-4" />
                {submitMutation.isPending ? "Submitting..." : submitLabel}
              </Button>
            </div>
          </CardContent>
        </Card>
      )}

      <section className="space-y-3">
        <div>
          <h2 className="text-xl font-semibold tracking-tight">Location &amp; photos</h2>
          <p className="text-sm text-muted-foreground">
            Each of these sections saves on its own, separately from the main form below.
          </p>
        </div>

        <div className="grid gap-6">
          <ListingLocationEditor listing={listing} readOnly={!canWrite} />
          <ListingPhotosEditor listing={listing} readOnly={!canWrite} />
        </div>
      </section>

      <Separator />

      <section id="details" className="space-y-3 scroll-mt-24">
        <div>
          <h2 className="text-xl font-semibold tracking-tight">Listing details</h2>
          <p className="text-sm text-muted-foreground">
            Title, description, pricing, amenities, house rules and lease terms. Save changes when
            you&apos;re done editing this section.
          </p>
        </div>

        <ListingForm
          key={listing.id}
          listing={listing}
          definitions={defs.data}
          defaultValues={defaultValues}
          submitLabel="Save changes"
          readOnly={formReadOnly}
          onSubmit={async (values) => {
            await updateMutation.mutateAsync(values);
          }}
        />
      </section>
    </div>
  );
};

type ChecklistRowProps = {
  done: boolean;
  required: boolean;
  label: string;
  detail: string;
  jumpLabel: string;
  onJump: () => void;
};

const ChecklistRow = ({ done, required, label, detail, jumpLabel, onJump }: ChecklistRowProps) => {
  return (
    <div className="flex items-start gap-3 rounded-md border bg-background p-3">
      {done ? (
        <CheckCircle2 className="h-5 w-5 shrink-0 text-emerald-600 mt-0.5" />
      ) : required ? (
        <AlertTriangle className="h-5 w-5 shrink-0 text-destructive mt-0.5" />
      ) : (
        <Circle className="h-5 w-5 shrink-0 text-muted-foreground mt-0.5" />
      )}
      <div className="min-w-0 flex-1">
        <div className="flex flex-wrap items-center gap-2">
          <span className={cn("text-sm font-medium", done && "line-through text-muted-foreground")}>
            {label}
          </span>
          {!done &&
            (required ? (
              <Badge variant="destructive" className="text-[10px]">Required</Badge>
            ) : (
              <Badge variant="outline" className="text-[10px]">Recommended</Badge>
            ))}
        </div>
        <p className="text-xs text-muted-foreground mt-0.5">{detail}</p>
      </div>
      {!done && (
        <Button
          type="button"
          variant="ghost"
          size="sm"
          onClick={onJump}
          className="shrink-0 h-8 px-2 text-xs"
        >
          {jumpLabel}
          <ArrowRight className="h-3.5 w-3.5 ml-1" />
        </Button>
      )}
    </div>
  );
};
