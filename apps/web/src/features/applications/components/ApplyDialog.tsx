import { useState } from "react";
import { Send } from "lucide-react";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Alert } from "@/components/ui/alert";
import { useAuthStore } from "@/app/auth/authStore";
import { useSubmitApplication } from "@/features/applications/hooks/useApplications";
import { formatMoney } from "@/utils/format";
import type { ListingDetailsDto } from "@/api/types";

type Props = {
  listing: ListingDetailsDto;
};

export const ApplyDialog = ({ listing }: Props) => {
  const user = useAuthStore((s) => s.user);
  const submitMutation = useSubmitApplication();

  const [open, setOpen] = useState(false);
  const [checkIn, setCheckIn] = useState("");
  const [checkOut, setCheckOut] = useState("");

  const stayDays =
    checkIn && checkOut
      ? Math.max(0, Math.round((new Date(checkOut).getTime() - new Date(checkIn).getTime()) / 86_400_000))
      : 0;

  const minStay = listing.minStayDays ?? 30;
  const maxStay = listing.maxStayDays ?? 365;
  const isValidStay = stayDays >= minStay && stayDays <= maxStay;
  const canSubmit = checkIn && checkOut && stayDays > 0 && isValidStay && !submitMutation.isPending;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!user || !canSubmit) return;

    await submitMutation.mutateAsync({
      listingId: listing.id,
      tenantUserId: user.userId,
      landlordUserId: listing.landlordUserId,
      requestedCheckIn: checkIn,
      requestedCheckOut: checkOut,
    });

    setOpen(false);
    setCheckIn("");
    setCheckOut("");
  };

  const today = new Date().toISOString().slice(0, 10);

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger asChild>
        <Button variant="accent" size="lg" className="w-full">
          <Send className="h-4 w-4" />
          {listing.instantBookingEnabled ? "Book now" : "Request to book"}
        </Button>
      </DialogTrigger>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>Apply for this listing</DialogTitle>
        </DialogHeader>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="rounded-lg bg-secondary p-3 space-y-1">
            <p className="text-sm font-medium">{listing.title}</p>
            <p className="text-sm text-muted-foreground">
              {formatMoney(listing.monthlyRentCents)} / month
            </p>
            <p className="text-xs text-muted-foreground">
              Stay range: {minStay}–{maxStay} days
            </p>
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1.5">
              <Label htmlFor="checkIn">Check-in date</Label>
              <Input
                id="checkIn"
                type="date"
                value={checkIn}
                onChange={(e) => setCheckIn(e.target.value)}
                min={today}
                required
              />
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="checkOut">Check-out date</Label>
              <Input
                id="checkOut"
                type="date"
                value={checkOut}
                onChange={(e) => setCheckOut(e.target.value)}
                min={checkIn || today}
                required
              />
            </div>
          </div>

          {stayDays > 0 && (
            <p className={`text-sm ${isValidStay ? "text-muted-foreground" : "text-destructive"}`}>
              {stayDays} day{stayDays !== 1 ? "s" : ""}
              {!isValidStay && ` (must be ${minStay}–${maxStay} days)`}
            </p>
          )}

          {submitMutation.isError && (
            <Alert variant="destructive">
              {(submitMutation.error as Error)?.message ?? "Failed to submit application. Please try again."}
            </Alert>
          )}

          {submitMutation.isSuccess && (
            <Alert>Application submitted successfully!</Alert>
          )}

          <Button type="submit" className="w-full" disabled={!canSubmit}>
            {submitMutation.isPending ? "Submitting..." : "Submit application"}
          </Button>
        </form>
      </DialogContent>
    </Dialog>
  );
};
