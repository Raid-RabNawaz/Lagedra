import { useEffect, useState, type FormEvent } from "react";
import { Link } from "react-router-dom";
import {
  Plus,
  CalendarCheck,
  Loader2,
  ExternalLink,
  CreditCard,
} from "lucide-react";
import {
  Elements,
  PaymentElement,
  useElements,
  useStripe,
} from "@stripe/react-stripe-js";
import { loadStripe } from "@stripe/stripe-js";
import { partnerApi } from "@/features/partners/services/partnerApi";
import { usePartnerMembership } from "@/features/partners/hooks/usePartnerMembership";
import { extractErrorMessage } from "@/lib/errors";
import { appConfig } from "@/app/config";
import type {
  ApplicationPayerType,
  DirectReservationDto,
  EndorsedMemberDto,
} from "@/api/types";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Loader } from "@/components/shared/Loader";
import { ErrorState } from "@/components/shared/ErrorState";
import { EmptyState } from "@/components/shared/EmptyState";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { FormError } from "@/components/shared/FormError";
import {
  Select,
} from "@/components/ui/select";

const stripePromise = appConfig.stripePublishableKey
  ? loadStripe(appConfig.stripePublishableKey)
  : null;

const formatDate = (iso: string) =>
  new Date(iso).toLocaleString(undefined, { dateStyle: "medium", timeStyle: "short" });

const truncate = (id: string) => `${id.slice(0, 8)}…`;

type Tab = "all" | "linked" | "pending";

export const PartnerReservationsPage = () => {
  const { membership, isLoading: membershipLoading, error: membershipError, refresh } =
    usePartnerMembership();

  const [reservations, setReservations] = useState<DirectReservationDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<unknown>(null);
  const [tab, setTab] = useState<Tab>("all");
  const [dialogOpen, setDialogOpen] = useState(false);

  const orgId = membership?.organization.id;
  const isAdmin = membership?.memberRole === "Admin";
  const isVerified = membership?.organization.status === "Verified";

  const loadReservations = async () => {
    if (!orgId) return;
    setIsLoading(true);
    setError(null);
    try {
      const data = await partnerApi.listReservations(orgId, {
        status: tab === "all" ? undefined : tab,
      });
      setReservations(data);
    } catch (err) {
      setError(err);
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    if (orgId) void loadReservations();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [orgId, tab]);

  if (membershipLoading) return <Loader label="Loading reservations..." />;
  if (membershipError) return <ErrorState error={membershipError} onRetry={() => void refresh()} />;
  if (!membership) return null;

  return (
    <div className="space-y-6">
      <div className="flex items-start justify-between gap-3">
        <div>
          <h1 className="flex items-center gap-2 text-3xl font-bold tracking-tight">
            <CalendarCheck className="h-7 w-7 text-muted-foreground" />
            Direct reservations
          </h1>
          <p className="mt-1 text-muted-foreground">
            Book on behalf of endorsed members. Each reservation creates a deal application
            visible to the member and the host.
          </p>
        </div>
        {isAdmin && isVerified && (
          <Button onClick={() => setDialogOpen(true)}>
            <Plus className="h-4 w-4" />
            New reservation
          </Button>
        )}
      </div>

      {!isVerified && (
        <Alert>
          <AlertDescription>
            Reservations can only be created once your organization is verified.
          </AlertDescription>
        </Alert>
      )}

      <Tabs value={tab} onValueChange={(v) => setTab(v as Tab)}>
        <TabsList>
          <TabsTrigger value="all">All</TabsTrigger>
          <TabsTrigger value="pending">Pending</TabsTrigger>
          <TabsTrigger value="linked">Linked to applications</TabsTrigger>
        </TabsList>
      </Tabs>

      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="text-lg">Reservations</CardTitle>
          <CardDescription>
            {reservations.length} reservation{reservations.length === 1 ? "" : "s"}
          </CardDescription>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <Loader label="Loading reservations..." />
          ) : error ? (
            <ErrorState error={error} onRetry={() => void loadReservations()} />
          ) : reservations.length === 0 ? (
            <EmptyState
              title="No reservations"
              description={
                tab === "all"
                  ? "When you book on behalf of an endorsed member, the reservation will appear here."
                  : "Try a different filter to see more."
              }
            >
              {isAdmin && isVerified && tab === "all" && (
                <Button onClick={() => setDialogOpen(true)}>
                  <Plus className="h-4 w-4" />
                  New reservation
                </Button>
              )}
            </EmptyState>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Member</TableHead>
                  <TableHead className="hidden md:table-cell">Email</TableHead>
                  <TableHead>Listing</TableHead>
                  <TableHead>Application</TableHead>
                  <TableHead className="hidden lg:table-cell">Booked</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {reservations.map((r) => (
                  <TableRow key={r.id}>
                    <TableCell className="font-medium">{r.guestName}</TableCell>
                    <TableCell className="hidden md:table-cell text-sm text-muted-foreground">
                      {r.guestEmail}
                    </TableCell>
                    <TableCell>
                      <Link
                        to={`/listings/${r.listingId}`}
                        className="inline-flex items-center gap-1 font-mono text-xs hover:underline"
                      >
                        {truncate(r.listingId)} <ExternalLink className="h-3 w-3" />
                      </Link>
                    </TableCell>
                    <TableCell>
                      {r.dealApplicationId ? (
                        <Link
                          to={`/app/applications/${r.dealApplicationId}`}
                          className="inline-flex items-center gap-1"
                        >
                          <Badge variant="success">Linked</Badge>
                        </Link>
                      ) : (
                        <Badge variant="secondary">Pending</Badge>
                      )}
                    </TableCell>
                    <TableCell className="hidden lg:table-cell text-sm text-muted-foreground">
                      {formatDate(r.createdAt)}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>

      {orgId && (
        <NewReservationDialog
          open={dialogOpen}
          onOpenChange={setDialogOpen}
          orgId={orgId}
          onSuccess={() => void loadReservations()}
        />
      )}
    </div>
  );
};

function NewReservationDialog({
  open,
  onOpenChange,
  orgId,
  onSuccess,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  orgId: string;
  onSuccess: () => void;
}) {
  const [members, setMembers] = useState<EndorsedMemberDto[]>([]);
  const [membersLoading, setMembersLoading] = useState(false);
  const [tenantUserId, setTenantUserId] = useState("");
  const [listingId, setListingId] = useState("");
  const [checkIn, setCheckIn] = useState("");
  const [checkOut, setCheckOut] = useState("");
  const [payerType, setPayerType] = useState<ApplicationPayerType>("Tenant");
  const [clientSecret, setClientSecret] = useState<string | null>(null);
  const [setupLoading, setSetupLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const reset = () => {
    setTenantUserId("");
    setListingId("");
    setCheckIn("");
    setCheckOut("");
    setPayerType("Tenant");
    setClientSecret(null);
    setError(null);
    setSubmitting(false);
    setSetupLoading(false);
  };

  const handleClose = (next: boolean) => {
    if (!next) reset();
    onOpenChange(next);
  };

  useEffect(() => {
    if (!open) return;
    setMembersLoading(true);
    void partnerApi
      .listEndorsedMembers(orgId)
      .then(setMembers)
      .catch((err) => setError(extractErrorMessage(err)))
      .finally(() => setMembersLoading(false));
  }, [open, orgId]);

  useEffect(() => {
    if (payerType !== "PartnerOrganization" || !listingId.trim() || listingId.trim().length < 30) {
      setClientSecret(null);
      return;
    }

    let cancelled = false;
    setSetupLoading(true);
    setError(null);
    void partnerApi
      .createSetupIntent(orgId, listingId.trim())
      .then((result) => {
        if (!cancelled) setClientSecret(result.clientSecret);
      })
      .catch((err) => {
        if (!cancelled) {
          setClientSecret(null);
          setError(extractErrorMessage(err));
        }
      })
      .finally(() => {
        if (!cancelled) setSetupLoading(false);
      });

    return () => {
      cancelled = true;
    };
  }, [payerType, listingId, orgId]);

  const canSubmit = Boolean(tenantUserId) && Boolean(listingId.trim());

  const submitReservation = async (stripePaymentMethodId: string | null) => {
    setSubmitting(true);
    setError(null);
    try {
      await partnerApi.createReservation(orgId, {
        tenantUserId,
        listingId: listingId.trim(),
        payerType,
        requestedCheckIn: checkIn || null,
        requestedCheckOut: checkOut || null,
        stripePaymentMethodId,
      });
      onSuccess();
      handleClose(false);
    } catch (err) {
      setError(extractErrorMessage(err));
      setSubmitting(false);
    }
  };

  const handleTenantPaysSubmit = async (e: FormEvent) => {
    e.preventDefault();
    if (!canSubmit) {
      setError("Select an endorsed member and listing before submitting.");
      return;
    }
    await submitReservation(null);
  };

  return (
    <Dialog open={open} onOpenChange={handleClose}>
      <DialogContent className="sm:max-w-lg max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>New direct reservation</DialogTitle>
          <DialogDescription>
            Choose an endorsed member. Invite and endorse new people from the Guests page first.
          </DialogDescription>
        </DialogHeader>

        <form
          onSubmit={(e) => {
            if (payerType === "Tenant") void handleTenantPaysSubmit(e);
            else e.preventDefault();
          }}
          className="space-y-4"
        >
          <div className="space-y-2">
            <Label>Endorsed member</Label>
            {membersLoading ? (
              <p className="text-sm text-muted-foreground">Loading members…</p>
            ) : members.length === 0 ? (
              <Alert>
                <AlertDescription>
                  No endorsed members yet. Invite someone from Guests (with endorsement) first.
                </AlertDescription>
              </Alert>
            ) : (
              <Select
                value={tenantUserId}
                onChange={(e) => setTenantUserId(e.target.value)}
              >
                <option value="" disabled>
                  Select a member
                </option>
                {members.map((m) => (
                  <option key={m.tenantUserId} value={m.tenantUserId}>
                    {m.displayName} ({m.email})
                  </option>
                ))}
              </Select>
            )}
          </div>

          <div className="space-y-2">
            <Label htmlFor="resv-listing-id">Listing ID</Label>
            <Input
              id="resv-listing-id"
              value={listingId}
              onChange={(e) => setListingId(e.target.value)}
              placeholder="00000000-0000-0000-0000-000000000000"
              required
            />
            <p className="text-xs text-muted-foreground">
              Copy a listing&apos;s ID from its detail page URL.
            </p>
          </div>

          <div className="grid gap-4 sm:grid-cols-2">
            <div className="space-y-2">
              <Label htmlFor="resv-check-in">
                Check-in <span className="text-muted-foreground">(optional)</span>
              </Label>
              <Input
                id="resv-check-in"
                type="date"
                value={checkIn}
                onChange={(e) => setCheckIn(e.target.value)}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="resv-check-out">
                Check-out <span className="text-muted-foreground">(optional)</span>
              </Label>
              <Input
                id="resv-check-out"
                type="date"
                value={checkOut}
                onChange={(e) => setCheckOut(e.target.value)}
              />
            </div>
          </div>

          <div className="space-y-3 rounded-md border p-4">
            <Label className="text-sm font-medium">Who pays?</Label>
            <div className="space-y-2">
              <label className="flex items-start gap-3 cursor-pointer">
                <input
                  type="radio"
                  name="payer"
                  className="mt-1"
                  checked={payerType === "Tenant"}
                  onChange={() => setPayerType("Tenant")}
                />
                <span className="text-sm">
                  <span className="font-medium">Member pays</span>
                  <span className="block text-muted-foreground">
                    The member attaches their card and consent before the host can approve.
                  </span>
                </span>
              </label>
              <label className="flex items-start gap-3 cursor-pointer">
                <input
                  type="radio"
                  name="payer"
                  className="mt-1"
                  checked={payerType === "PartnerOrganization"}
                  onChange={() => setPayerType("PartnerOrganization")}
                />
                <span className="text-sm">
                  <span className="font-medium">Company pays</span>
                  <span className="block text-muted-foreground">
                    Attach your company card now. The member still confirms Truth Surface terms.
                  </span>
                </span>
              </label>
            </div>
          </div>

          {payerType === "PartnerOrganization" && (
            <div className="space-y-3 rounded-md border p-4">
              <div className="flex items-center gap-2 text-sm font-medium">
                <CreditCard className="h-4 w-4" />
                Company payment method
              </div>
              {!stripePromise ? (
                <Alert variant="destructive">
                  <AlertDescription>Stripe is not configured in this environment.</AlertDescription>
                </Alert>
              ) : setupLoading ? (
                <p className="text-sm text-muted-foreground">Preparing card form…</p>
              ) : clientSecret ? (
                <Elements
                  stripe={stripePromise}
                  options={{
                    clientSecret,
                    appearance: { theme: "stripe" },
                  }}
                >
                  <PartnerCardCapture
                    disabled={!canSubmit || submitting}
                    submitting={submitting}
                    onError={setError}
                    onConfirm={(pmId) => void submitReservation(pmId)}
                  />
                </Elements>
              ) : (
                <p className="text-sm text-muted-foreground">
                  Enter a listing ID to load the company card form.
                </p>
              )}
            </div>
          )}

          {error && <FormError message={error} />}

          {payerType === "Tenant" && (
            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => handleClose(false)}>
                Cancel
              </Button>
              <Button type="submit" disabled={!canSubmit || submitting || members.length === 0}>
                {submitting && <Loader2 className="h-4 w-4 animate-spin" />}
                Submit
              </Button>
            </DialogFooter>
          )}

          {payerType === "PartnerOrganization" && (
            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => handleClose(false)}>
                Cancel
              </Button>
            </DialogFooter>
          )}
        </form>
      </DialogContent>
    </Dialog>
  );
}

function PartnerCardCapture({
  disabled,
  submitting,
  onError,
  onConfirm,
}: {
  disabled: boolean;
  submitting: boolean;
  onError: (message: string) => void;
  onConfirm: (paymentMethodId: string) => void;
}) {
  const stripe = useStripe();
  const elements = useElements();
  const [processing, setProcessing] = useState(false);

  const handleConfirm = async () => {
    if (!stripe || !elements) return;
    setProcessing(true);
    onError("");

    try {
      const { error: submitError } = await elements.submit();
      if (submitError) {
        onError(submitError.message ?? "Please check your card details.");
        setProcessing(false);
        return;
      }

      const { error: confirmError, setupIntent } = await stripe.confirmSetup({
        elements,
        redirect: "if_required",
      });

      if (confirmError) {
        onError(confirmError.message ?? "Couldn't save the company card.");
        setProcessing(false);
        return;
      }

      const paymentMethodId =
        typeof setupIntent?.payment_method === "string"
          ? setupIntent.payment_method
          : (setupIntent?.payment_method?.id ?? null);

      if (!paymentMethodId) {
        onError("Couldn't confirm the company card. Please try again.");
        setProcessing(false);
        return;
      }

      onConfirm(paymentMethodId);
    } catch (err) {
      onError(extractErrorMessage(err));
      setProcessing(false);
    }
  };

  return (
    <div className="space-y-4">
      <PaymentElement />
      <Button
        type="button"
        disabled={disabled || processing || submitting || !stripe || !elements}
        onClick={() => void handleConfirm()}
      >
        {(processing || submitting) && <Loader2 className="h-4 w-4 animate-spin" />}
        Save card &amp; submit reservation
      </Button>
    </div>
  );
}
