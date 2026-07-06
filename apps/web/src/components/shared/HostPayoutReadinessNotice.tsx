import { type ReactNode } from "react";
import { Link } from "react-router-dom";
import { AlertTriangle } from "lucide-react";
import { Alert } from "@/components/ui/alert";
import { useHostPayoutReadiness } from "@/features/host-onboarding/hooks/useHostStripe";
import { cn } from "@/lib/utils";

const DEFAULT_MESSAGE: ReactNode = (
  <>
    You haven't set up payouts yet. Accepting a request charges the guest's
    deposit and first payment right away, which needs a payout destination.{" "}
    <Link to="/app/payout-setup" className="font-medium underline">
      Set up payouts
    </Link>{" "}
    first, then accept.
  </>
);

/**
 * Warns the host that they can't be paid yet, with a link to payout setup.
 * Used wherever a host action depends on a payout destination existing
 * (accepting a booking request, taking a listing live). Renders nothing while
 * the lookup is in flight or once the host is payout-ready.
 */
export const HostPayoutReadinessNotice = ({
  className,
  message,
}: {
  className?: string;
  message?: ReactNode;
}) => {
  const { ready, settled } = useHostPayoutReadiness();

  if (!settled || ready) return null;

  return (
    <Alert variant="destructive" className={cn("text-sm", className)}>
      <AlertTriangle className="h-4 w-4" />
      <span className="ml-2">{message ?? DEFAULT_MESSAGE}</span>
    </Alert>
  );
};
