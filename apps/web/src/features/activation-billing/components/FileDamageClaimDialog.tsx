import { useState } from "react";
import { FileWarning } from "lucide-react";
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
import { Input } from "@/components/ui/input";
import { Alert } from "@/components/ui/alert";
import { useFileDamageClaim } from "@/features/activation-billing/hooks/useBilling";
import { getApiErrorMessage } from "@/api/errors";

type Props = {
  dealId: string;
  depositAmountCents: number;
  open: boolean;
  onOpenChange: (open: boolean) => void;
};

export const FileDamageClaimDialog = ({
  dealId,
  depositAmountCents,
  open,
  onOpenChange,
}: Props) => {
  const [description, setDescription] = useState("");
  const [amountEur, setAmountEur] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);
  const fileClaim = useFileDamageClaim();

  const amountCents = Math.round(parseFloat(amountEur || "0") * 100);

  const handleSubmit = async () => {
    if (!description.trim()) {
      setError("Please describe the damage.");
      return;
    }
    if (amountCents <= 0) {
      setError("Please enter a valid claim amount.");
      return;
    }
    setError(null);
    try {
      await fileClaim.mutateAsync({
        dealId,
        payload: {
          description: description.trim(),
          claimedAmountCents: amountCents,
        },
      });
      setSuccess(true);
    } catch (e) {
      setError(getApiErrorMessage(e, "Failed to file damage claim."));
    }
  };

  const handleClose = () => {
    setDescription("");
    setAmountEur("");
    setError(null);
    setSuccess(false);
    onOpenChange(false);
  };

  return (
    <Dialog open={open} onOpenChange={handleClose}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <FileWarning className="h-5 w-5 text-amber-500" />
            File Damage Claim
          </DialogTitle>
          <DialogDescription>
            Report property damage. Amounts up to the deposit (
            {new Intl.NumberFormat("en-US", {
              style: "currency",
              currency: "USD",
            }).format(depositAmountCents / 100)}
            ) are deducted from the deposit. Amounts exceeding the deposit are
            forwarded to the insurance provider.
          </DialogDescription>
        </DialogHeader>

        {success ? (
          <div className="space-y-3">
            <Alert className="border-emerald-200 bg-emerald-50 text-emerald-800">
              <span className="text-sm">
                Damage claim filed successfully. The tenant has been notified and
                our team will review the claim.
              </span>
            </Alert>
            <DialogFooter>
              <Button onClick={handleClose}>Close</Button>
            </DialogFooter>
          </div>
        ) : (
          <>
            <div className="space-y-3">
              <div>
                <label className="text-sm font-medium mb-1.5 block">
                  Description of damage
                </label>
                <Textarea
                  placeholder="Describe the damage in detail..."
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                  rows={4}
                  maxLength={2000}
                />
              </div>
              <div>
                <label className="text-sm font-medium mb-1.5 block">
                  Claimed amount (USD)
                </label>
                <Input
                  type="number"
                  min="0.01"
                  step="0.01"
                  placeholder="0.00"
                  value={amountEur}
                  onChange={(e) => setAmountEur(e.target.value)}
                />
              </div>
            </div>

            {error && (
              <Alert variant="destructive" className="text-sm">
                {error}
              </Alert>
            )}

            <DialogFooter>
              <Button variant="outline" onClick={handleClose}>
                Cancel
              </Button>
              <Button
                onClick={handleSubmit}
                disabled={
                  fileClaim.isPending || !description.trim() || amountCents <= 0
                }
              >
                {fileClaim.isPending ? "Filing..." : "File Claim"}
              </Button>
            </DialogFooter>
          </>
        )}
      </DialogContent>
    </Dialog>
  );
};
