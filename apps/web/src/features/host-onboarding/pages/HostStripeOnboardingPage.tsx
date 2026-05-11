import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Textarea } from "@/components/ui/textarea";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Separator } from "@/components/ui/separator";
import { Loader } from "@/components/shared/Loader";
import {
  useHostPaymentDetails,
  useSaveHostPaymentDetails,
} from "@/features/host-onboarding/hooks/useHostStripe";
import {
  Wallet, Save, CheckCircle2, AlertTriangle, ShieldCheck,
} from "lucide-react";

export default function HostStripeOnboardingPage() {
  const { data: paymentDetails, isLoading } = useHostPaymentDetails();

  if (isLoading) {
    return <Loader label="Loading payout setup..." />;
  }

  const hasDirectPaymentDetails = Boolean(paymentDetails?.paymentInfo?.trim());
  const completedSteps = hasDirectPaymentDetails ? 1 : 0;
  const progressPercent = completedSteps * 100;

  return (
    <div className="mx-auto max-w-3xl space-y-6 p-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Payout setup</h1>
        <p className="mt-1 text-muted-foreground">
          Add your payout details once so tenants know how to pay you directly after activation.
        </p>
      </div>

      <Card className="border-primary/20 bg-primary/5">
        <CardHeader>
          <CardTitle className="text-lg">Hosting setup progress</CardTitle>
          <CardDescription>
            Simple host onboarding: enter your payout details to complete setup.
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex items-center justify-between text-sm">
            <span className="text-muted-foreground">{completedSteps}/1 step complete</span>
            <span className="font-medium">{progressPercent}%</span>
          </div>
          <div className="h-2 w-full rounded-full bg-muted">
            <div
              className="h-2 rounded-full bg-primary transition-all"
              style={{ width: `${progressPercent}%` }}
            />
          </div>

          <div className="space-y-2">
            <ChecklistRow
              done={hasDirectPaymentDetails}
              title="Add direct payment instructions"
              description="Set your bank transfer, Zelle, Venmo, or other payout notes."
            />
          </div>
        </CardContent>
      </Card>

      {!hasDirectPaymentDetails && (
        <Alert>
          <AlertDescription>
            Add payout details below. No external onboarding redirect is required.
          </AlertDescription>
        </Alert>
      )}

      <Separator />

      <PaymentDetailsSection />

      <div className="rounded-lg bg-muted/50 p-4 text-xs text-muted-foreground space-y-2">
        <p className="font-medium text-foreground text-sm">How it works</p>
        <ul className="list-disc pl-4 space-y-1">
          <li>
            When a tenant accepts a deal, they pay the first month's rent,
            deposit, and insurance through Lagedra.
          </li>
          <li>
            Your payout instructions are shared only with matched tenants after
            deal activation.
          </li>
          <li>
            For months 2 onward, rent is paid directly by the tenant to you.
          </li>
        </ul>
      </div>
    </div>
  );
}

function ChecklistRow({
  done,
  title,
  description,
}: {
  done: boolean;
  title: string;
  description: string;
}) {
  return (
    <div className="flex items-start gap-3 rounded-lg border bg-background p-3">
      {done ? (
        <CheckCircle2 className="mt-0.5 h-5 w-5 shrink-0 text-emerald-600" />
      ) : (
        <ShieldCheck className="mt-0.5 h-5 w-5 shrink-0 text-muted-foreground" />
      )}
      <div>
        <p className="text-sm font-medium">{title}</p>
        <p className="text-xs text-muted-foreground">{description}</p>
      </div>
    </div>
  );
}

function PaymentDetailsSection() {
  const { data: existing, isLoading } = useHostPaymentDetails();
  const save = useSaveHostPaymentDetails();

  const [paymentInfo, setPaymentInfo] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const currentValue = paymentInfo ?? existing?.paymentInfo ?? "";
  const isDirty = paymentInfo !== null && paymentInfo !== (existing?.paymentInfo ?? "");

  const handleSave = async () => {
    if (!currentValue.trim()) {
      setError("Please enter your payment details.");
      return;
    }
    setMessage(null);
    setError(null);
    try {
      await save.mutateAsync({ paymentInfo: currentValue.trim() });
      setPaymentInfo(null);
      setMessage("Payment details saved successfully.");
    } catch (e) {
      setError((e as Error)?.message ?? "Failed to save payment details.");
    }
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-lg flex items-center gap-2">
          <Wallet className="h-5 w-5" />
          Direct payment details
        </CardTitle>
        <CardDescription>
          For months 2 onward, tenants pay you directly. Add your preferred
          instructions (bank transfer, Zelle, Venmo, etc.) for post-activation rent.
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
          <Loader label="Loading payment details..." />
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
              matched tenants.
            </p>
            <Button
              onClick={handleSave}
              disabled={save.isPending || !isDirty || !currentValue.trim()}
              className="gap-2"
            >
              <Save className="h-4 w-4" />
              {save.isPending ? "Saving..." : "Save Payment Details"}
            </Button>
          </>
        )}
      </CardContent>
    </Card>
  );
}
