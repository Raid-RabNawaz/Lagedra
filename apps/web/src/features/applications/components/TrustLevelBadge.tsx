import { Shield, ShieldCheck } from "lucide-react";
import { cn } from "@/lib/utils";
import type { TenantVerificationTier } from "@/api/types";
import {
  tierBadgeClassName,
  tierDescription,
  tierLabel,
  tierShortLabel,
} from "@/features/applications/lib/bookingConsent";

type Props = {
  tier: TenantVerificationTier | string | null | undefined;
  /** Compact inline badge for list cards. */
  compact?: boolean;
  /** Full panel for the detail modal. */
  detailed?: boolean;
  depositReason?: string | null;
  className?: string;
};

export function TrustLevelBadge({
  tier,
  compact = false,
  detailed = false,
  depositReason,
  className,
}: Props) {
  const resolved = tier ?? "Unverified";
  const label = compact ? tierShortLabel(resolved) : tierLabel(resolved);

  if (detailed) {
    return (
      <section
        className={cn(
          "rounded-lg border p-4",
          tierBadgeClassName(resolved),
          className,
        )}
      >
        <div className="flex items-start gap-3">
          <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-background/70">
            {resolved === "BackgroundVerified" || resolved === "PartnerGuaranteed" ? (
              <ShieldCheck className="h-5 w-5" />
            ) : (
              <Shield className="h-5 w-5" />
            )}
          </span>
          <div className="min-w-0 space-y-1">
            <p className="text-[11px] font-semibold uppercase tracking-wide opacity-80">
              Trust level
            </p>
            <p className="text-base font-semibold leading-tight">{label}</p>
            <p className="text-sm opacity-90">{tierDescription(resolved)}</p>
            {depositReason && (
              <p className="text-xs opacity-80 pt-1">
                Deposit rationale: {depositReason}
              </p>
            )}
          </div>
        </div>
      </section>
    );
  }

  return (
    <span
      className={cn(
        "inline-flex items-center gap-1 rounded-full border px-2 py-0.5 text-[11px] font-semibold leading-none",
        tierBadgeClassName(resolved),
        className,
      )}
    >
      <Shield className="h-3 w-3 shrink-0" />
      {label}
    </span>
  );
}
