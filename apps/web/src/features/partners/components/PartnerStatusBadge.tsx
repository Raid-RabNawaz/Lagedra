import { Badge } from "@/components/ui/badge";
import { Clock, CheckCircle2, Ban } from "lucide-react";
import type { PartnerOrganizationStatus } from "@/api/types";

const config: Record<
  PartnerOrganizationStatus,
  { label: string; variant: "secondary" | "success" | "destructive"; icon: typeof Clock }
> = {
  PendingVerification: { label: "Pending verification", variant: "secondary", icon: Clock },
  Verified: { label: "Verified", variant: "success", icon: CheckCircle2 },
  Suspended: { label: "Suspended", variant: "destructive", icon: Ban },
};

export function PartnerStatusBadge({ status }: { status: PartnerOrganizationStatus }) {
  const { label, variant, icon: Icon } = config[status];
  return (
    <Badge variant={variant} className="gap-1">
      <Icon className="h-3 w-3" />
      {label}
    </Badge>
  );
}
