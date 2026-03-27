import { Badge } from "@/components/ui/badge";
import type { DealApplicationStatus } from "@/api/types";

const config: Record<DealApplicationStatus, { label: string; variant: "default" | "secondary" | "destructive" | "accent" }> = {
  Pending: { label: "Pending", variant: "secondary" },
  Approved: { label: "Approved", variant: "accent" },
  Rejected: { label: "Rejected", variant: "destructive" },
  Cancelled: { label: "Cancelled", variant: "default" },
};

type Props = {
  status: DealApplicationStatus;
  className?: string;
};

export const ApplicationStatusBadge = ({ status, className }: Props) => {
  const { label, variant } = config[status] ?? config.Pending;
  return (
    <Badge variant={variant} className={className}>
      {label}
    </Badge>
  );
};
