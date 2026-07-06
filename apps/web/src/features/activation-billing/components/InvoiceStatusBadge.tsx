import { Badge, type BadgeProps } from "@/components/ui/badge";
import type { InvoiceStatus } from "@/api/types";

const config: Record<
  InvoiceStatus,
  { label: string; variant: BadgeProps["variant"] }
> = {
  Paid: { label: "Paid", variant: "success" },
  Pending: { label: "Pending", variant: "secondary" },
  Failed: { label: "Failed", variant: "destructive" },
  Disputed: { label: "Disputed", variant: "destructive" },
};

type Props = {
  status: InvoiceStatus;
  className?: string;
};

export const InvoiceStatusBadge = ({ status, className }: Props) => {
  const { label, variant } = config[status] ?? config.Pending;
  return (
    <Badge variant={variant} className={className}>
      {label}
    </Badge>
  );
};
