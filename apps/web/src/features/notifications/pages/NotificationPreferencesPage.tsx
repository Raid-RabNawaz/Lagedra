import { useState } from "react";
import {
  Bell,
  CheckCircle2,
  AlertTriangle,
  Save,
  Info,
  MessageSquare,
} from "lucide-react";
import { Link } from "react-router-dom";
import { SmsProgramDisclosures } from "@/features/legal/SmsProgramDisclosures";
import { SMS_PROGRAM } from "@/features/legal/smsProgram";
import { useAuthStore } from "@/app/auth/authStore";
import {
  useNotificationPreferences,
  useUpdateNotificationPreferences,
} from "@/features/notifications/hooks/useNotifications";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Separator } from "@/components/ui/separator";
import { BackLink } from "@/components/shared/BackLink";
import { Loader } from "@/components/shared/Loader";
import { Checkbox } from "@/components/ui/checkbox";

const EVENT_LABELS: Record<string, { label: string; description: string }> = {
  application_approved: {
    label: "Application approved",
    description: "When a landlord approves your rental application",
  },
  application_rejected: {
    label: "Application rejected",
    description: "When a landlord declines your rental application",
  },
  application_received: {
    label: "New application received",
    description: "When a tenant submits an application for your listing",
  },
  payment_confirmed: {
    label: "Payment confirmed",
    description: "When a payment is confirmed by the host",
  },
  payment_failed: {
    label: "Payment failed",
    description: "When a payment attempt fails",
  },
  payment_disputed: {
    label: "Payment disputed",
    description: "When a tenant disputes a payment",
  },
  deal_activated: {
    label: "Deal activated",
    description: "When a deal is activated after payment",
  },
  booking_cancelled: {
    label: "Booking cancelled",
    description: "When a booking is cancelled by either party",
  },
  identity_verified: {
    label: "Identity verified",
    description: "When your identity verification is completed",
  },
  identity_verification_failed: {
    label: "Identity verification failed",
    description: "When your identity verification attempt fails",
  },
  damage_claim_filed: {
    label: "Damage claim filed",
    description: "When a damage claim is filed against a booking",
  },
  damage_claim_approved: {
    label: "Damage claim approved",
    description: "When a damage claim is approved after review",
  },
  damage_claim_rejected: {
    label: "Damage claim rejected",
    description: "When a damage claim is rejected after review",
  },
  insurance_status_changed: {
    label: "Insurance status update",
    description: "When your insurance policy status changes",
  },
  billing_stopped: {
    label: "Billing stopped",
    description: "When billing is stopped for a deal",
  },
  host_suspended: {
    label: "Account suspended",
    description: "When your host account is suspended",
  },
  truth_surface_confirmed: {
    label: "Truth surface confirmed",
    description: "When a truth surface snapshot is confirmed",
  },
  review_due: {
    label: "Review requested",
    description: "When a completed stay opens the review window",
  },
  review_reminder: {
    label: "Review reminder",
    description: "Reminders if you have not left a review after a completed stay",
  },
};

export const NotificationPreferencesPage = () => {
  const user = useAuthStore((s) => s.user);
  const userId = user?.userId;

  const { data: preferences, isLoading } = useNotificationPreferences(userId);
  const update = useUpdateNotificationPreferences();

  const [optIns, setOptIns] = useState<Record<string, boolean>>({});
  const [smsOptedIn, setSmsOptedIn] = useState<boolean>(SMS_PROGRAM.defaultConsent);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  // Seed the form state from the loaded preferences exactly once. Updating
  // state during render (with a guard) is the React-recommended pattern for
  // "adjusting state when a prop changes" — no effect required.
  const [seeded, setSeeded] = useState(false);
  if (!seeded && preferences) {
    setSeeded(true);
    setOptIns(preferences.eventOptIns);
    setSmsOptedIn(preferences.smsCampaignsOptedIn === true);
  }

  if (isLoading || !preferences) {
    return <Loader fullPage label="Loading preferences..." />;
  }

  const toggleEvent = (eventKey: string, checked: boolean) => {
    setOptIns((prev) => ({ ...prev, [eventKey]: checked }));
  };

  const savedSms = preferences.smsCampaignsOptedIn === true;
  const isDirty =
    JSON.stringify(optIns) !== JSON.stringify(preferences.eventOptIns) ||
    smsOptedIn !== savedSms;

  const handleSave = async () => {
    if (!userId) return;
    setMessage(null);
    setError(null);
    try {
      await update.mutateAsync({
        userId,
        payload: { eventOptIns: optIns, smsCampaignsOptedIn: smsOptedIn },
      });
      setMessage("Notification preferences saved.");
    } catch (e) {
      setError(
        (e as Error)?.message ?? "Failed to save notification preferences.",
      );
    }
  };

  const allEvents = Object.keys(EVENT_LABELS);
  const knownOptIns = Object.entries(optIns).filter(
    ([key]) => key in EVENT_LABELS,
  );
  const unknownOptIns = Object.entries(optIns).filter(
    ([key]) => !(key in EVENT_LABELS),
  );
  const missingEvents = allEvents.filter((key) => !(key in optIns));

  const allToggles = [
    ...knownOptIns,
    ...missingEvents.map((key) => [key, true] as const),
    ...unknownOptIns,
  ];

  return (
    <div className="space-y-8">
      <div>
        <BackLink fallbackTo="/app/profile" className="mb-4" />
        <h1 className="text-3xl font-bold tracking-tight">
          Notification Preferences
        </h1>
        <p className="mt-1 text-muted-foreground">
          Choose which notifications you&apos;d like to receive by email and
          in-app, and whether you want optional automated texts.
        </p>
      </div>

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

      <Card>
        <CardHeader>
          <CardTitle className="text-lg flex items-center gap-2">
            <Bell className="h-5 w-5" />
            Email Notifications
          </CardTitle>
          <CardDescription>
            Toggle which event types send you an email notification.
            In-app notifications are always delivered.
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-1">
          {preferences.transactionalAlwaysSent && (
            <div className="flex items-start gap-2 rounded-lg border border-blue-200 bg-blue-50 p-3 mb-4">
              <Info className="h-4 w-4 text-blue-600 mt-0.5 shrink-0" />
              <p className="text-sm text-blue-800">
                Transactional notifications (payment confirmations, security
                alerts) are always sent regardless of your preferences.
              </p>
            </div>
          )}

          {allToggles.map(([eventKey, enabled]) => {
            const meta = EVENT_LABELS[eventKey];
            return (
              <label
                key={eventKey}
                className="flex items-start gap-3 rounded-lg px-3 py-3 hover:bg-secondary/50 transition-colors cursor-pointer"
              >
                <Checkbox
                  checked={Boolean(enabled)}
                  onCheckedChange={(checked) =>
                    toggleEvent(eventKey, checked === true)
                  }
                  className="mt-0.5"
                />
                <div>
                  <p className="text-sm font-medium">
                    {meta?.label ?? eventKey}
                  </p>
                  {meta?.description && (
                    <p className="text-xs text-muted-foreground mt-0.5">
                      {meta.description}
                    </p>
                  )}
                </div>
              </label>
            );
          })}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-lg flex items-center gap-2">
            <MessageSquare className="h-5 w-5" />
            Text messages
          </CardTitle>
          <CardDescription>
            Optional automated SMS about bookings, payments, account updates,
            and occasional offers. The box below is never pre-selected.
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          {preferences.smsPhoneE164 ? (
            <p className="text-sm text-muted-foreground">
              Messages go to{" "}
              <span className="font-medium text-foreground">{preferences.smsPhoneE164}</span>
              . Change the number in your{" "}
              <Link to="/app/profile" className="underline underline-offset-2">
                profile
              </Link>
              .
            </p>
          ) : (
            <Alert>
              <Info className="h-4 w-4" />
              <AlertDescription>
                Add a mobile number in your{" "}
                <Link to="/app/profile" className="font-medium underline underline-offset-2">
                  profile
                </Link>{" "}
                before opting in. You can also subscribe at{" "}
                <Link to="/sms" className="font-medium underline underline-offset-2">
                  lagedra.com/sms
                </Link>
                .
              </AlertDescription>
            </Alert>
          )}

          <label className="flex items-start gap-3 cursor-pointer">
            <Checkbox
              checked={smsOptedIn}
              disabled={!preferences.smsPhoneE164 && !smsOptedIn}
              onCheckedChange={(checked) => setSmsOptedIn(checked === true)}
              className="mt-0.5"
            />
            <span className="text-sm leading-6 text-muted-foreground">
              {SMS_PROGRAM.checkboxLabel}
            </span>
          </label>

          <SmsProgramDisclosures />
        </CardContent>
      </Card>

      <Separator />

      <div className="flex justify-end">
        <Button
          onClick={handleSave}
          disabled={update.isPending || !isDirty}
          className="gap-2"
        >
          <Save className="h-4 w-4" />
          {update.isPending ? "Saving..." : "Save Preferences"}
        </Button>
      </div>
    </div>
  );
};
