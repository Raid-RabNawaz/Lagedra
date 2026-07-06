import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { Scale } from "lucide-react";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Select } from "@/components/ui/select";
import { Alert } from "@/components/ui/alert";
import { useFileCase } from "@/features/arbitration/hooks/useArbitration";
import type { ArbitrationTier, ArbitrationCategory } from "@/api/types";

const tierOptions: { value: ArbitrationTier; label: string; description: string }[] = [
  {
    value: "ProtocolAdjudication",
    label: "Protocol Adjudication",
    description: "Non-binding review. A filing fee applies.",
  },
  {
    value: "BindingArbitration",
    label: "Binding Arbitration",
    description: "Binding decision with monetary award. A higher filing fee applies.",
  },
];

const categoryOptions: { value: ArbitrationCategory; label: string }[] = [
  { value: "CategoryA", label: "Insurance Lapse" },
  { value: "CategoryB", label: "Payment Default" },
  { value: "CategoryC", label: "Lease Violation" },
  { value: "CategoryD", label: "Property Damage" },
  { value: "CategoryE", label: "Unauthorized Occupants" },
  { value: "CategoryF", label: "Early Termination" },
  { value: "CategoryG", label: "Rule Violation" },
  { value: "DepositReturn", label: "Deposit Not Returned" },
  { value: "Other", label: "Other" },
];

type Props = {
  dealId: string;
  open: boolean;
  onOpenChange: (open: boolean) => void;
  /** Preselects the dispute category (e.g. when deep-linked from the deposit panel). */
  defaultCategory?: ArbitrationCategory;
};

export function FileArbitrationDialog({
  dealId,
  open,
  onOpenChange,
  defaultCategory,
}: Props) {
  const navigate = useNavigate();
  const fileCase = useFileCase();
  const [tier, setTier] = useState<ArbitrationTier>("ProtocolAdjudication");
  const [category, setCategory] = useState<ArbitrationCategory>(
    defaultCategory ?? "CategoryC",
  );
  const [error, setError] = useState<string | null>(null);

  const selectedTier = tierOptions.find((t) => t.value === tier);

  const handleSubmit = async () => {
    setError(null);
    try {
      const result = await fileCase.mutateAsync({ dealId, tier, category });
      onOpenChange(false);
      navigate(`/app/arbitration/${result.caseId}`);
    } catch (e) {
      const msg =
        (e as { response?: { data?: { detail?: string } } })?.response?.data
          ?.detail ??
        (e as Error)?.message ??
        "Failed to file case.";
      setError(msg);
    }
  };

  const handleClose = () => {
    setError(null);
    onOpenChange(false);
  };

  return (
    <Dialog open={open} onOpenChange={handleClose}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <Scale className="h-5 w-5 text-blue-600" />
            File Arbitration Case
          </DialogTitle>
          <DialogDescription>
            Open a formal dispute resolution case for this deal. After filing,
            you'll be asked to pay the filing fee for the tier you select — the
            case opens once the fee is paid.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4">
          <div>
            <label className="text-sm font-medium mb-1.5 block">
              Dispute Category
            </label>
            <Select
              value={category}
              onChange={(e) =>
                setCategory(e.target.value as ArbitrationCategory)
              }
            >
              {categoryOptions.map((opt) => (
                <option key={opt.value} value={opt.value}>
                  {opt.label}
                </option>
              ))}
            </Select>
          </div>

          <div>
            <label className="text-sm font-medium mb-1.5 block">
              Arbitration Tier
            </label>
            <Select
              value={tier}
              onChange={(e) => setTier(e.target.value as ArbitrationTier)}
            >
              {tierOptions.map((opt) => (
                <option key={opt.value} value={opt.value}>
                  {opt.label}
                </option>
              ))}
            </Select>
            {selectedTier && (
              <p className="text-xs text-muted-foreground mt-1">
                {selectedTier.description}
              </p>
            )}
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
          <Button onClick={handleSubmit} disabled={fileCase.isPending}>
            {fileCase.isPending ? "Filing..." : "File & pay fee"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
