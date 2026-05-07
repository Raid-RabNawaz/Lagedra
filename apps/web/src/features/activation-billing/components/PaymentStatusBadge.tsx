import { Badge } from "@/components/ui/badge";
import type { PaymentConfirmationStatus } from "@/api/types";

const config: Record<
  PaymentConfirmationStatus,
  { label: string; variant: "default" | "secondary" | "destructive" | "accent" }
> = {
  Pending: { label: "Pending", variant: "secondary" },
  Confirmed: { label: "Confirmed", variant: "accent" },
  Disputed: { label: "Disputed", variant: "destructive" },
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
