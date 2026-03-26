import type { ReactNode } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useAuthStore } from "@/app/auth/authStore";
import { privacyApi } from "@/features/privacy/services/privacyApi";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Loader } from "@/components/shared/Loader";
import { ShieldAlert } from "lucide-react";

type LegalConsentGateProps = {
  children: ReactNode;
};

/**
 * Landlord flows require KYC + Data Processing consent (backend returns 451 otherwise).
 * This gate loads consent state and lets the user grant missing consents before continuing.
 */
export function LegalConsentGate({ children }: LegalConsentGateProps) {
  const user = useAuthStore((s) => s.user);
  const isInitialized = useAuthStore((s) => s.isInitialized);
  const accessToken = useAuthStore((s) => s.accessToken);
  const queryClient = useQueryClient();

  const consentsQuery = useQuery({
    queryKey: ["privacy", "consents", user?.userId],
    queryFn: () => privacyApi.getUserConsents(user!.userId),
    enabled: Boolean(user?.userId),
  });

  const recordMutation = useMutation({
    mutationFn: () => privacyApi.ensureRequiredConsents(user!.userId),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["privacy", "consents", user?.userId] });
    },
  });

  if (!isInitialized || (accessToken && !user)) {
    return <Loader fullPage label="Loading session…" />;
  }

  if (!user) {
    return <>{children}</>;
  }

  if (consentsQuery.isLoading) {
    return <Loader fullPage label="Checking legal consents…" />;
  }

  if (consentsQuery.isError) {
    return (
      <Alert variant="destructive">
        <ShieldAlert className="h-4 w-4" />
        <AlertTitle>Could not load consent status</AlertTitle>
        <AlertDescription>
          {(consentsQuery.error as Error)?.message ?? "Try again later or contact support."}
        </AlertDescription>
      </Alert>
    );
  }

  const records = consentsQuery.data ?? [];
  const active = new Set(records.filter((r) => r.withdrawnAt == null).map((r) => r.consentType));
  const hasKyc = active.has("KYCConsent");
  const hasData = active.has("DataProcessing");
  const complete = hasKyc && hasData;

  if (complete) {
    return <>{children}</>;
  }

  return (
    <div className="space-y-6">
      <Alert>
        <ShieldAlert className="h-4 w-4" />
        <AlertTitle>Legal consent required</AlertTitle>
        <AlertDescription className="space-y-3 pt-2">
          <p>
            To create or change listings, you must accept <strong>KYC</strong> and{" "}
            <strong>data processing</strong> consent. This is required by our API before any write
            operations.
          </p>
          <Button
            type="button"
            disabled={recordMutation.isPending}
            onClick={() => recordMutation.mutate()}
          >
            {recordMutation.isPending ? "Saving…" : "Accept required consents and continue"}
          </Button>
          {recordMutation.isError && (
            <p className="text-sm text-destructive">
              {(recordMutation.error as Error)?.message ?? "Could not save consents."}
            </p>
          )}
        </AlertDescription>
      </Alert>
    </div>
  );
}
