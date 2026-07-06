import { Badge } from "@/components/ui/badge";
import type { PaymentConfirmationStatus } from "@/api/types";

const config: Record<
  PaymentConfirmationStatus,
  { label: string; variant: "default" | "secondary" | "destructive" | "accent" }
> = {
  Pending: { label: "Pending", variant: "secondary" },
  PaymentMethodProvided: { label: "Card on file", variant: "secondary" },
  CapturePending: { label: "Charging", variant: "secondary" },
  Confirmed: { label: "Confirmed", variant: "accent" },
  Disputed: { label: "Disputed", variant: "destructive" },
  Failed: { label: "Payment failed", variant: "destructive" },
  Refunded: { label: "Refunded", variant: "default" },
  Rejected: { label: "Rejected", variant: "destructive" },
  Cancelled: { label: "Cancelled", variant: "default" },
};

type Props = {
  status: PaymentConfirmationStatus;
  className?: string;
};

export const PaymentStatusBadge = ({ status, className }: Props) => {
  const { label, variant } = config[status] ?? config.Pending;
  return (
    <Badge variant={variant} className={className}>
      {label}
    </Badge>
  );
};
