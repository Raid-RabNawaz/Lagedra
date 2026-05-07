import { Badge } from "@/components/ui/badge";
import type { BillingAccountStatus } from "@/api/types";

const config: Record<
  BillingAccountStatus,
  { label: string; variant: "default" | "secondary" | "destructive" | "accent" }
> = {
  Inactive: { label: "Inactive", variant: "secondary" },
  Active: { label: "Active", variant: "accent" },
  Suspended: { label: "Suspended", variant: "destructive" },
  Closed: { label: "Closed", variant: "default" },
};

type Props = {
  status: BillingAccountStatus;
  className?: string;
};

export const BillingStatusBadge = ({ status, className }: Props) => {
  const { label, variant } = config[status] ?? config.Inactive;
  return (
    <Badge variant={variant} className={className}>
      {label}
    </Badge>
  );
};
