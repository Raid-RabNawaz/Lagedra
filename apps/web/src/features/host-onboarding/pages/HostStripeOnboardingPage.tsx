import { useState, type ReactNode } from "react";
import { Link } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { Textarea } from "@/components/ui/textarea";
import { Badge, type BadgeProps } from "@/components/ui/badge";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Separator } from "@/components/ui/separator";
import { Loader } from "@/components/shared/Loader";
import {
  useHostStripeStatus,
  useHostStripeOnboard,
  useRefreshOnboardingLink,
  useHostPaymentDetails,
  useSaveHostPaymentDetails,
} from "@/features/host-onboarding/hooks/useHostStripe";
import { useHostBillingStatement } from "@/features/activation-billing/hooks/useBilling";
import { formatMoney } from "@/utils/format";
import type {
  HostAccountRequirementStatus,
  HostStripeStatusDto,
} from "@/api/types";
import {
  Wallet, Save, CheckCircle2, AlertTriangle, ExternalLink,
  Clock, Building2, FileText, CreditCard, Banknote,
} from "lucide-react";

export default function HostStripeOnboardingPage() {
  const { data: status, isLoading } = useHostStripeStatus();
  const { data: statement } = useHostBillingStatement();

  if (isLoading) {
    return <Loader label="Loading payout setup..." />;
  }

  const monthlyFeeLabel =
    statement && statement.currentMonthlyFeeCents > 0
      ? `${formatMoney(statement.currentMonthlyFeeCents)}/mo`
      : "a monthly platform fee";

  const chargesEnabled = status?.chargesEnabled === true;
  const payoutsEnabled = status?.payoutsEnabled === true;
  const ready = chargesEnabled && payoutsEnabled;
  const hasAccount = Boolean(status?.stripeAccountId);

  return (
    <div className="mx-auto max-w-3xl space-y-6 p-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Payout setup</h1>
        <p className="mt-1 text-muted-foreground">
          Guests pay through Lagedra and the rent + deposit are routed straight
          to your bank via Stripe. Connect your Stripe account once to start
          receiving payouts.
        </p>
      </div>

      <StripeConnectSection status={status} ready={ready} hasAccount={hasAccount} />

      <Separator />

      <RentInstructionsSection />

      <div className="rounded-lg bg-muted/50 p-4 text-xs text-muted-foreground space-y-2">
        <p className="font-medium text-foreground text-sm">How it works</p>
        <ul className="list-disc pl-4 space-y-1">
          <li>
            When a tenant accepts a deal, they pay the first month's rent,
            deposit, and insurance through Lagedra.
          </li>
          <li>
            Stripe pays your rent and deposit directly to your connected
            account — Lagedra never holds your funds.
          </li>
          <li>
            For months 2 onward, rent is paid directly by the tenant to you
            using the instructions below.
          </li>
          <li>
            Once a booking is active, Lagedra charges you{" "}
            <span className="font-medium text-foreground">{monthlyFeeLabel}</span>{" "}
            per active booking, billed automatically to your card on file. See{" "}
            <Link to="/app/billing" className="font-medium text-primary hover:underline">
              Platform fees
            </Link>{" "}
            for the current rate and your full deduction history.
          </li>
        </ul>
      </div>
    </div>
  );
}

function StripeConnectSection({
  status,
  ready,
  hasAccount,
}: {
  status: HostStripeStatusDto | undefined;
  ready: boolean;
  hasAccount: boolean;
}) {
  const onboard = useHostStripeOnboard();
  const refresh = useRefreshOnboardingLink();
  const [error, setError] = useState<string | null>(null);

  const busy = onboard.isPending || refresh.isPending;

  const startOnboarding = async () => {
    setError(null);
    try {
      const result = await onboard.mutateAsync();
      if (result.onboardingUrl) {
        window.location.href = result.onboardingUrl;
        return;
      }
      // No URL means the account is already fully enabled — the status query
      // refresh from onSuccess will reflect the ready state.
    } catch (e) {
      setError((e as Error)?.message ?? "Could not start Stripe onboarding.");
    }
  };

  const continueOnboarding = async () => {
    setError(null);
    try {
      const { onboardingUrl } = await refresh.mutateAsync();
      if (onboardingUrl) {
        window.location.href = onboardingUrl;
      }
    } catch (e) {
      setError((e as Error)?.message ?? "Could not refresh your Stripe link.");
    }
  };

  return (
    <Card className="border-primary/20 bg-primary/5">
      <CardHeader>
        <div className="flex items-start justify-between gap-3">
          <div>
            <CardTitle className="text-lg flex items-center gap-2">
              <Wallet className="h-5 w-5" />
              Stripe payouts
            </CardTitle>
            <CardDescription>
              {ready
                ? "Your account is connected and ready to receive payouts."
                : "Connect your Stripe account to accept bookings and get paid."}
            </CardDescription>
          </div>
          {ready ? (
            <Badge variant="success" className="gap-1">
              <CheckCircle2 className="h-3.5 w-3.5" />
              Ready
            </Badge>
          ) : hasAccount ? (
            <Badge variant="secondary" className="gap-1">
              <Clock className="h-3.5 w-3.5" />
              In progress
            </Badge>
          ) : (
            <Badge variant="outline">Not started</Badge>
          )}
        </div>
      </CardHeader>
      <CardContent className="space-y-4">
        {error && (
          <Alert variant="destructive">
            <AlertTriangle className="h-4 w-4" />
            <AlertDescription>{error}</AlertDescription>
          </Alert>
        )}

        {hasAccount && (
          <div className="space-y-2">
            <RequirementRow
              icon={<CreditCard className="h-4 w-4" />}
              title="Accept charges"
              done={status?.chargesEnabled === true}
              status={status?.chargesEnabled ? "Verified" : "Pending"}
            />
            <RequirementRow
              icon={<Banknote className="h-4 w-4" />}
              title="Receive payouts"
              done={status?.payoutsEnabled === true}
              status={status?.payoutsEnabled ? "Verified" : "Pending"}
            />
            <RequirementRow
              icon={<Building2 className="h-4 w-4" />}
              title="Bank account"
              done={status?.bankAccountStatus === "Verified"}
              status={status?.bankAccountStatus ?? "Unknown"}
            />
            <RequirementRow
              icon={<FileText className="h-4 w-4" />}
              title="Tax information (W-9 / W-8)"
              done={status?.taxStatus === "Verified"}
              status={status?.taxStatus ?? "Unknown"}
            />
          </div>
        )}

        {!ready && (
          <div className="flex flex-col gap-2 sm:flex-row">
            <Button onClick={startOnboarding} disabled={busy} className="gap-2">
              <ExternalLink className="h-4 w-4" />
              {busy
                ? "Opening Stripe..."
                : hasAccount
                  ? "Continue Stripe setup"
                  : "Set up payouts with Stripe"}
            </Button>
            {hasAccount && (
              <Button
                variant="outline"
                onClick={continueOnboarding}
                disabled={busy}
                className="gap-2"
              >
                Get a new link
              </Button>
            )}
          </div>
        )}

        {!ready && (
          <p className="text-xs text-muted-foreground">
            You'll be redirected to Stripe to enter your business/personal
            details, bank account, and tax forms. Stripe sends you back here
            when you're done — keep this app's dev server running while you
            complete Stripe (don't stop <code className="rounded bg-muted px-1">npm run dev</code>).
          </p>
        )}
      </CardContent>
    </Card>
  );
}

function requirementBadge(
  status: HostAccountRequirementStatus,
): { label: string; variant: BadgeProps["variant"] } {
  switch (status) {
    case "Verified":
      return { label: "Verified", variant: "success" };
    case "Pending":
      return { label: "Pending", variant: "secondary" };
    case "Restricted":
      return { label: "Action needed", variant: "destructive" };
    default:
      return { label: "Not started", variant: "outline" };
  }
}

function RequirementRow({
  icon,
  title,
  done,
  status,
}: {
  icon: ReactNode;
  title: string;
  done: boolean;
  status: HostAccountRequirementStatus;
}) {
  const badge = requirementBadge(status);
  return (
    <div className="flex items-center justify-between gap-3 rounded-lg border bg-background p-3">
      <div className="flex items-center gap-3">
        <span className={done ? "text-emerald-600" : "text-muted-foreground"}>
          {done ? <CheckCircle2 className="h-4 w-4" /> : icon}
        </span>
        <p className="text-sm font-medium">{title}</p>
      </div>
      <Badge variant={badge.variant}>{badge.label}</Badge>
    </div>
  );
}

function RentInstructionsSection() {
  const { data: existing, isLoading } = useHostPaymentDetails();
  const save = useSaveHostPaymentDetails();

  const [paymentInfo, setPaymentInfo] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const currentValue = paymentInfo ?? existing?.paymentInfo ?? "";
  const isDirty = paymentInfo !== null && paymentInfo !== (existing?.paymentInfo ?? "");

  const handleSave = async () => {
    if (!currentValue.trim()) {
      setError("Please enter your rent instructions.");
      return;
    }
    setMessage(null);
    setError(null);
    try {
      await save.mutateAsync({ paymentInfo: currentValue.trim() });
      setPaymentInfo(null);
      setMessage("Rent instructions saved successfully.");
    } catch (e) {
      setError((e as Error)?.message ?? "Failed to save rent instructions.");
    }
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-lg flex items-center gap-2">
          <Banknote className="h-5 w-5" />
          Months 2+ rent instructions
          <Badge variant="outline" className="ml-1 font-normal">Optional</Badge>
        </CardTitle>
        <CardDescription>
          The first payment, deposit, and insurance are handled through Stripe.
          For months 2 onward, tenants pay you directly — add your preferred
          instructions (bank transfer, Zelle, Venmo, etc.).
        </CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        {message && (
          <Alert variant="success">
            <CheckCircle2 className="h-4 w-4" />
            <AlertDescription>{message}</AlertDescription>
          </Alert>
        )}
        {error && (
          <Alert variant="destructive">
            <AlertTriangle className="h-4 w-4" />
            <AlertDescription>{error}</AlertDescription>
          </Alert>
        )}

        {isLoading ? (
          <Loader label="Loading rent instructions..." />
        ) : (
          <>
            <Textarea
              placeholder={"Example:\nBank: Chase\nRouting: 021000021\nAccount: 123456789\n\nOr: Zelle — yourname@email.com"}
              value={currentValue}
              onChange={(e) => setPaymentInfo(e.target.value)}
              rows={5}
              maxLength={2000}
            />
            <p className="text-xs text-muted-foreground">
              This information is encrypted at rest and only visible to your
              matched tenants after a deal activates.
            </p>
            <Button
              onClick={handleSave}
              disabled={save.isPending || !isDirty || !currentValue.trim()}
              className="gap-2"
            >
              <Save className="h-4 w-4" />
              {save.isPending ? "Saving..." : "Save rent instructions"}
            </Button>
          </>
        )}
      </CardContent>
    </Card>
  );
}
