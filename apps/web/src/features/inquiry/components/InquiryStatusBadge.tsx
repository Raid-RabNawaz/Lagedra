import { Badge } from "@/components/ui/badge";
import type { InquirySessionStatus } from "@/api/types";

const config: Record<
  InquirySessionStatus,
  { label: string; variant: "default" | "secondary" | "destructive" | "accent" }
> = {
  Locked: { label: "Locked", variant: "secondary" },
  Open: { label: "Open", variant: "accent" },
  Closed: { label: "Closed", variant: "default" },
};

type Props = {
  status: InquirySessionStatus;
  className?: string;
};

export const InquiryStatusBadge = ({ status, className }: Props) => {
  const { label, variant } = config[status] ?? config.Locked;
  return (
    <Badge variant={variant} className={className}>
      {label}
    </Badge>
  );
};
