import { Badge } from "@/components/ui/badge";
import { Clock, CheckCircle2, XCircle, AlertTriangle } from "lucide-react";
import type { PartnerEndorsementStatus } from "@/api/types";

const config: Record<
  PartnerEndorsementStatus,
  { label: string; variant: "secondary" | "success" | "destructive" | "accent"; icon: typeof Clock }
> = {
  Requested: { label: "Requested", variant: "secondary", icon: Clock },
  Approved: { label: "Approved", variant: "success", icon: CheckCircle2 },
  Revoked: { label: "Revoked", variant: "destructive", icon: XCircle },
  Expired: { label: "Expired", variant: "accent", icon: AlertTriangle },
};

export function EndorsementStatusBadge({ status }: { status: PartnerEndorsementStatus }) {
  const { label, variant, icon: Icon } = config[status];
  return (
    <Badge variant={variant} className="gap-1">
      <Icon className="h-3 w-3" />
      {label}
    </Badge>
  );
}
