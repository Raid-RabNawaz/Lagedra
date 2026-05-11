import { Badge } from "@/components/ui/badge";
import { Shield, ShieldCheck, ShieldOff } from "lucide-react";
import type { ProtectionTier } from "@/api/types";

type Props = {
  tier: ProtectionTier;
  /** When tier == "PartnerBacked", the endorser org name shown after the dot. */
  orgName?: string | null;
  /** When tier == "PartnerBacked", the expiry shown in the tooltip. */
  expiresAt?: string | null;
  className?: string;
};

const tierConfig: Record<
  ProtectionTier,
  {
    label: string;
    variant: "secondary" | "success" | "accent";
    icon: typeof Shield;
  }
> = {
  Uninsured: { label: "Uninsured", variant: "secondary", icon: ShieldOff },
  ThirdPartyInsured: { label: "Insured", variant: "success", icon: ShieldCheck },
  PartnerBacked: { label: "Partner-Backed Protection", variant: "accent", icon: Shield },
};

const formatDate = (iso: string) =>
  new Date(iso).toLocaleDateString(undefined, { year: "numeric", month: "short", day: "numeric" });

export function ProtectionTierBadge({ tier, orgName, expiresAt, className }: Props) {
  const { label, variant, icon: Icon } = tierConfig[tier];
  const labelWithOrg = tier === "PartnerBacked" && orgName ? `${label} · ${orgName}` : label;

  const tooltip =
    tier === "PartnerBacked" && orgName
      ? expiresAt
        ? `${orgName}, a verified Lagedra partner, has endorsed this tenant. Endorsement is valid until ${formatDate(expiresAt)} or until ${orgName} revokes it.`
        : `${orgName}, a verified Lagedra partner, has endorsed this tenant. Endorsement remains valid until ${orgName} revokes it.`
      : tier === "ThirdPartyInsured"
        ? "Backed by a third-party insurance binding."
        : "No active insurance or endorsement on record.";

  return (
    <Badge variant={variant} className={`gap-1 ${className ?? ""}`} title={tooltip}>
      <Icon className="h-3 w-3" />
      {labelWithOrg}
    </Badge>
  );
}
