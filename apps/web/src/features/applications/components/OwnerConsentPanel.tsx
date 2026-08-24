import { useState } from "react";
import { CheckCircle2, Loader2, XCircle } from "lucide-react";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { ConsentTickButton } from "@/features/applications/components/ConsentTickButton";
import { getApiErrorMessage } from "@/api/errors";
import type { DealApplicationDto } from "@/api/types";
import {
  useOwnerConsentApplication,
  useOwnerDeclineApplication,
} from "@/features/applications/hooks/useApplications";
import { OWNER_TENANCY_CONSENT_VERSION } from "@/features/applications/lib/bookingConsent";

type Props = {
  application: DealApplicationDto;
};

export const OwnerConsentPanel = ({ application }: Props) => {
  const [consentChecked, setConsentChecked] = useState(false);
  const [showDeclineConfirm, setShowDeclineConfirm] = useState(false);
  const [actionError, setActionError] = useState<string | null>(null);

  const consentMutation = useOwnerConsentApplication();
  const declineMutation = useOwnerDeclineApplication();
  const busy = consentMutation.isPending || declineMutation.isPending;

  const handleConsent = async () => {
    setActionError(null);
    if (!consentChecked) {
      setActionError("Please confirm you are the owner and consent to this tenancy.");
      return;
    }

    try {
      await consentMutation.mutateAsync({
        id: application.applicationId,
        payload: {
          consentGiven: true,
          consentVersion: OWNER_TENANCY_CONSENT_VERSION,
        },
      });
    } catch (err) {
      setActionError(getApiErrorMessage(err, "Failed to record owner consent."));
    }
  };

  const handleDecline = async () => {
    setActionError(null);
    try {
      await declineMutation.mutateAsync(application.applicationId);
    } catch (err) {
      setActionError(getApiErrorMessage(err, "Failed to decline this tenancy."));
    }
  };

  return (
    <div className="space-y-3">
      <p className="text-xs text-muted-foreground">
        California law requires the home owner&apos;s consent for stays over 30 days
        when a property manager lists the home. Consenting authorizes the property
        manager to enter the lease on your behalf. You will be named as Landlord
        on the lease.
      </p>

      {!showDeclineConfirm ? (
        <>
          <ConsentTickButton
            checked={consentChecked}
            onCheckedChange={(next) => {
              setConsentChecked(next);
              if (next) setActionError(null);
            }}
          >
            I am the owner of this property. I consent to this tenancy and
            authorize the property manager to enter into the lease on my behalf.
          </ConsentTickButton>

          {actionError && (
            <Alert variant="destructive" className="text-xs">
              {actionError}
            </Alert>
          )}

          <div className="flex items-center gap-2">
            <Button
              size="sm"
              onClick={() => void handleConsent()}
              disabled={busy || !consentChecked}
              className="gap-1.5"
            >
              {consentMutation.isPending ? (
                <Loader2 className="h-3.5 w-3.5 animate-spin" />
              ) : (
                <CheckCircle2 className="h-3.5 w-3.5" />
              )}
              Consent to this stay
            </Button>
            <Button
              size="sm"
              variant="outline"
              onClick={() => {
                setActionError(null);
                setShowDeclineConfirm(true);
              }}
              disabled={busy}
              className="gap-1.5 text-red-600 hover:bg-red-50 hover:text-red-700"
            >
              <XCircle className="h-3.5 w-3.5" />
              Decline
            </Button>
          </div>
        </>
      ) : (
        <div className="space-y-3 rounded-md border bg-red-50/50 p-3">
          <p className="text-xs text-red-800">
            Declining closes this booking request. The guest and property manager
            will be notified. This can&apos;t be undone.
          </p>
          {actionError && (
            <Alert variant="destructive" className="text-xs">
              {actionError}
            </Alert>
          )}
          <div className="flex items-center gap-2">
            <Button
              size="sm"
              variant="destructive"
              onClick={() => void handleDecline()}
              disabled={busy}
              className="gap-1.5"
            >
              {declineMutation.isPending ? (
                <Loader2 className="h-3.5 w-3.5 animate-spin" />
              ) : (
                <XCircle className="h-3.5 w-3.5" />
              )}
              Decline this stay
            </Button>
            <Button
              size="sm"
              variant="ghost"
              onClick={() => {
                setShowDeclineConfirm(false);
                setActionError(null);
              }}
              disabled={busy}
            >
              Cancel
            </Button>
          </div>
        </div>
      )}
    </div>
  );
};
