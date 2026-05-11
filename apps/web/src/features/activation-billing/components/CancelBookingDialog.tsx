import { useState } from "react";
import { XCircle } from "lucide-react";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Textarea } from "@/components/ui/textarea";
import { Alert } from "@/components/ui/alert";
import { useCancelBooking } from "@/features/activation-billing/hooks/useBilling";
import { formatMoney } from "@/utils/format";
import { getApiErrorMessage } from "@/api/errors";
import type { CancellationResultDto } from "@/api/types";

type Props = {
  dealId: string;
  open: boolean;
  onOpenChange: (open: boolean) => void;
};

export const CancelBookingDialog = ({ dealId, open, onOpenChange }: Props) => {
  const [reason, setReason] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [result, setResult] = useState<CancellationResultDto | null>(null);
  const cancel = useCancelBooking();

  const handleSubmit = async () => {
    if (!reason.trim()) {
      setError("Please provide a reason for cancellation.");
      return;
    }
    setError(null);
    try {
      const res = await cancel.mutateAsync({
        dealId,
        payload: { reason: reason.trim() },
      });
      setResult(res);
    } catch (e) {
      setError(getApiErrorMessage(e, "Failed to cancel booking."));
    }
  };

  const handleClose = () => {
    setReason("");
    setError(null);
    setResult(null);
    onOpenChange(false);
  };

  return (
    <Dialog open={open} onOpenChange={handleClose}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <XCircle className="h-5 w-5 text-red-500" />
            Cancel Booking
          </DialogTitle>
          <DialogDescription>
            This action cannot be undone. The refund amount depends on the
            listing's cancellation policy and how far out the check-in date is.
          </DialogDescription>
        </DialogHeader>

        {result ? (
          <div className="space-y-3">
            <div className="rounded-md border p-4 space-y-2 bg-muted/50">
              <p className="text-sm font-medium">Booking cancelled</p>
              <div className="flex justify-between text-sm">
                <span className="text-muted-foreground">Tenant refund</span>
                <span className="font-medium">
                  {formatMoney(result.tenantRefundCents)}
                </span>
              </div>
              <div className="flex justify-between text-sm">
                <span className="text-muted-foreground">Insurance refund</span>
                <span className="font-medium">
                  {formatMoney(result.insuranceRefundCents)}
                </span>
              </div>
              <p className="text-xs text-muted-foreground mt-2">
                {result.policyApplied}
              </p>
            </div>
            <DialogFooter>
              <Button onClick={handleClose}>Close</Button>
            </DialogFooter>
          </div>
        ) : (
          <>
            <div className="space-y-3">
              <Textarea
                placeholder="Reason for cancellation..."
                value={reason}
                onChange={(e) => setReason(e.target.value)}
                rows={3}
                maxLength={1000}
              />
            </div>

            {error && (
              <Alert variant="destructive" className="text-sm">
                {error}
              </Alert>
            )}

            <DialogFooter>
              <Button variant="outline" onClick={handleClose}>
                Keep Booking
              </Button>
              <Button
                variant="destructive"
                onClick={handleSubmit}
                disabled={cancel.isPending || !reason.trim()}
              >
                {cancel.isPending ? "Cancelling..." : "Confirm Cancellation"}
              </Button>
            </DialogFooter>
          </>
        )}
      </DialogContent>
    </Dialog>
  );
};
