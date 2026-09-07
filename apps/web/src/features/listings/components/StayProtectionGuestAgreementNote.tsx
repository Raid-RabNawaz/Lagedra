import { cn } from "@/lib/utils";
import {
  STAY_PROTECTION_GUEST_AGREEMENT_URL,
  STAY_PROTECTION_LABEL,
} from "@/features/listings/lib/stayProtection";

type Props = {
  className?: string;
};

export const StayProtectionGuestAgreementNote = ({ className }: Props) => (
  <p className={cn("text-[11px] text-muted-foreground", className)}>
    {STAY_PROTECTION_LABEL} is included and is not renter&apos;s insurance. See
    the{" "}
    <a
      href={STAY_PROTECTION_GUEST_AGREEMENT_URL}
      target="_blank"
      rel="noopener noreferrer"
      className="underline underline-offset-2"
    >
      guest agreement
    </a>
    .
  </p>
);
