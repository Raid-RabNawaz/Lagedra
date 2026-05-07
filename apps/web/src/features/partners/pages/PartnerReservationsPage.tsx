import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { Plus, CalendarCheck, Loader2, ExternalLink } from "lucide-react";
import { partnerApi } from "@/features/partners/services/partnerApi";
import { usePartnerMembership } from "@/features/partners/hooks/usePartnerMembership";
import { extractErrorMessage } from "@/lib/errors";
import type { DirectReservationDto } from "@/api/types";
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
            Bookings you've made on behalf of guests. Each reservation creates a real deal
            application that the host then approves.
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
                  ? "When you book on behalf of a guest, the reservation will appear here."
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
                  <TableHead>Guest</TableHead>
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
  const [guestName, setGuestName] = useState("");
  const [guestEmail, setGuestEmail] = useState("");
  const [listingId, setListingId] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const reset = () => {
    setGuestName("");
    setGuestEmail("");
    setListingId("");
    setError(null);
    setSubmitting(false);
  };

  const handleClose = (next: boolean) => {
    if (!next) reset();
    onOpenChange(next);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!guestName.trim() || !guestEmail.trim() || !listingId.trim()) return;
    setSubmitting(true);
    setError(null);
    try {
      await partnerApi.createReservation(orgId, {
        guestName: guestName.trim(),
        guestEmail: guestEmail.trim(),
        listingId: listingId.trim(),
      });
      onSuccess();
      handleClose(false);
    } catch (err) {
      setError(extractErrorMessage(err));
      setSubmitting(false);
    }
  };

  return (
    <Dialog open={open} onOpenChange={handleClose}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>New direct reservation</DialogTitle>
          <DialogDescription>
            The guest must already have a Lagedra account. If they don't, use the{" "}
            <strong>Guests</strong> page first to invite them — the invite flow can also create the
            reservation in one step.
          </DialogDescription>
        </DialogHeader>
        <form onSubmit={(e) => void handleSubmit(e)} className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="resv-guest-name">Guest name</Label>
            <Input
              id="resv-guest-name"
              value={guestName}
              onChange={(e) => setGuestName(e.target.value)}
              placeholder="Jane Doe"
              required
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="resv-guest-email">Guest email</Label>
            <Input
              id="resv-guest-email"
              type="email"
              value={guestEmail}
              onChange={(e) => setGuestEmail(e.target.value)}
              placeholder="jane@example.com"
              required
            />
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
              You can copy a listing's ID from its detail page URL.
            </p>
          </div>
          {error && <FormError message={error} />}
          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => handleClose(false)}>
              Cancel
            </Button>
            <Button type="submit" disabled={submitting}>
              {submitting && <Loader2 className="h-4 w-4 animate-spin" />}
              Submit
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
