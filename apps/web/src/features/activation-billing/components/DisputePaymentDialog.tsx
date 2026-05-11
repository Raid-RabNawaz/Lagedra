import { useState } from "react";
import { AlertTriangle } from "lucide-react";
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
import { useDisputePayment } from "@/features/activation-billing/hooks/useBilling";
import { getApiErrorMessage } from "@/api/errors";

type Props = {
  dealId: string;
  open: boolean;
  onOpenChange: (open: boolean) => void;
};

export const DisputePaymentDialog = ({ dealId, open, onOpenChange }: Props) => {
  const [reason, setReason] = useState("");
  const [error, setError] = useState<string | null>(null);
  const dispute = useDisputePayment();

  const handleSubmit = async () => {
    if (!reason.trim()) {
      setError("Please provide a reason for the dispute.");
      return;
    }
    setError(null);
    try {
      await dispute.mutateAsync({ dealId, payload: { reason: reason.trim() } });
      setReason("");
      onOpenChange(false);
    } catch (e) {
      setError(getApiErrorMessage(e, "Failed to submit dispute."));
    }
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <AlertTriangle className="h-5 w-5 text-amber-500" />
            Dispute Payment
          </DialogTitle>
          <DialogDescription>
            If you believe the payment was not received correctly, describe the
            issue below. Our team will review the dispute and contact both
            parties.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-3">
          <Textarea
            placeholder="Describe the issue with this payment..."
            value={reason}
            onChange={(e) => setReason(e.target.value)}
            rows={4}
            maxLength={2000}
          />
          <p className="text-xs text-muted-foreground text-right">
            {reason.length}/2000
          </p>
        </div>

        {error && (
          <Alert variant="destructive" className="text-sm">
            {error}
          </Alert>
        )}

        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            Cancel
          </Button>
          <Button
            variant="destructive"
            onClick={handleSubmit}
            disabled={dispute.isPending || !reason.trim()}
          >
            {dispute.isPending ? "Submitting..." : "Submit Dispute"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
};
