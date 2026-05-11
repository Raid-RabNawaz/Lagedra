import { Badge } from "@/components/ui/badge";
import type { TruthSurfaceStatus } from "@/api/types";

const config: Record<
  TruthSurfaceStatus,
  { label: string; variant: "default" | "secondary" | "destructive" | "accent" }
> = {
  Draft: { label: "Draft", variant: "secondary" },
  PendingBothConfirmations: { label: "Pending Both", variant: "secondary" },
  PendingLandlordConfirmation: { label: "Pending Landlord", variant: "secondary" },
  PendingTenantConfirmation: { label: "Pending Tenant", variant: "secondary" },
  Confirmed: { label: "Confirmed", variant: "accent" },
  Superseded: { label: "Superseded", variant: "default" },
};

type Props = {
  status: TruthSurfaceStatus;
  className?: string;
};

export const TruthSurfaceStatusBadge = ({ status, className }: Props) => {
  const { label, variant } = config[status] ?? config.Draft;
  return (
    <Badge variant={variant} className={className}>
      {label}
    </Badge>
  );
};
