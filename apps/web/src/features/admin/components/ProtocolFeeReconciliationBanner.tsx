import { Link } from "react-router-dom";
import { AlertTriangle, CheckCircle2, ArrowRight } from "lucide-react";
import { useProtocolFeeReconciliation } from "@/features/admin/hooks/useProtocolFeeReconciliation";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import type { ProtocolFeeReconciliationDto } from "@/api/types";
import { formatMoney } from "@/utils/format";

type Props = {
  /** When true, render nothing if the fee is in sync (used on the dashboard). */
  hideWhenHealthy?: boolean;
  /** When true, show a "Review settings" link to the platform settings page. */
  showSettingsLink?: boolean;
};

/**
 * Admin-only banner that flags when the protocol fee hosts are shown (platform
 * setting) has drifted from the Stripe subscription price they're billed, or
 * when the price is missing/unverifiable.
 */
export const ProtocolFeeReconciliationBanner = ({
  hideWhenHealthy = false,
  showSettingsLink = false,
}: Props) => {
  const { data, isLoading, isError } = useProtocolFeeReconciliation();

  if (isLoading || isError || !data) {
    return null;
  }

  if (data.inSync && hideWhenHealthy) {
    return null;
  }

  const view = toView(data);

  return (
    <Alert
      variant={view.variant === "success" ? "success" : "destructive"}
      className={view.variant === "warning" ? WARNING_CLASS : undefined}
    >
      {view.variant === "success" ? (
        <CheckCircle2 className="h-4 w-4" />
      ) : (
        <AlertTriangle className="h-4 w-4" />
      )}
      <AlertTitle>{view.title}</AlertTitle>
      <AlertDescription className="flex flex-wrap items-center justify-between gap-2">
        <span>{view.message}</span>
        {showSettingsLink && (
          <Link
            to="/app/admin/settings"
            className="inline-flex items-center gap-1 font-medium hover:underline"
          >
            Review settings
            <ArrowRight className="h-3.5 w-3.5" />
          </Link>
        )}
      </AlertDescription>
    </Alert>
  );
};

const WARNING_CLASS = "border-amber-200 bg-amber-50 text-amber-800 [&>svg]:text-amber-800";

type View = {
  variant: "success" | "warning" | "destructive";
  title: string;
  message: string;
};

function toView(data: ProtocolFeeReconciliationDto): View {
  const configured = formatMoney(data.configuredMonthlyFeeCents);

  switch (data.issue) {
    case "drift": {
      const stripe =
        data.stripePriceAmountCents != null
          ? formatMoney(data.stripePriceAmountCents)
          : "an unknown amount";
      return {
        variant: "destructive",
        title: "Protocol fee mismatch",
        message:
          `Hosts are shown ${configured}/mo, but the Stripe subscription price ` +
          `charges ${stripe}/mo. Align the "Protocol fee" setting or the Stripe ` +
          `price so hosts see what they're actually billed.`,
      };
    }
    case "price_not_configured":
      return {
        variant: "warning",
        title: "Stripe platform fee price not set",
        message:
          `No Stripe subscription price is configured (Stripe Connect → platform ` +
          `fee price ID). Hosts are shown ${configured}/mo but won't be billed ` +
          `until a price is set.`,
      };
    case "stripe_error":
      return {
        variant: "warning",
        title: "Couldn't verify the Stripe price",
        message:
          `The configured Stripe price couldn't be read to confirm it matches the ` +
          `${configured}/mo shown to hosts. Check Stripe connectivity and credentials.`,
      };
    case "no_unit_amount":
      return {
        variant: "warning",
        title: "Stripe price has no fixed amount",
        message:
          `The Stripe subscription price uses tiered/variable pricing, so it can't ` +
          `be reconciled against the ${configured}/mo shown to hosts.`,
      };
    default:
      return {
        variant: "success",
        title: "Protocol fee in sync",
        message: `Hosts are shown and billed ${configured}/mo per active booking.`,
      };
  }
}
