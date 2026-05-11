import { CheckCircle2 } from "lucide-react";
import { Button } from "@/components/ui/button";

type Props = {
  allChecked: boolean;
  disclaimerAccepted: boolean;
  isPending: boolean;
  onClick: () => void;
  label?: string;
};

export const ConfirmButton = ({
  allChecked,
  disclaimerAccepted,
  isPending,
  onClick,
  label = "Confirm Truth Surface",
}: Props) => {
  const disabled = !allChecked || !disclaimerAccepted || isPending;

  return (
    <div className="space-y-2">
      {!allChecked && (
        <p className="text-xs text-muted-foreground">
          You must confirm every line item before submitting.
        </p>
      )}
      {allChecked && !disclaimerAccepted && (
        <p className="text-xs text-muted-foreground">
          You must accept the platform disclaimers before submitting.
        </p>
      )}
      <Button
        onClick={onClick}
        disabled={disabled}
        className="w-full gap-2"
        size="lg"
      >
        <CheckCircle2 className="h-4 w-4" />
        {isPending ? "Confirming..." : label}
      </Button>
    </div>
  );
};
