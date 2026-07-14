import { useState } from "react";
import { Mail, Loader2, CheckCircle2, ShieldCheck, Copy, Check } from "lucide-react";
import { partnerApi } from "@/features/partners/services/partnerApi";
import { usePartnerMembership } from "@/features/partners/hooks/usePartnerMembership";
import { extractErrorMessage } from "@/lib/errors";
import type { PartnerGuestInviteResultDto } from "@/api/types";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { Checkbox } from "@/components/ui/checkbox";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Loader } from "@/components/shared/Loader";
import { ErrorState } from "@/components/shared/ErrorState";
import { FormError } from "@/components/shared/FormError";

export const PartnerGuestsPage = () => {
  const { membership, isLoading, error, refresh } = usePartnerMembership();

  const [email, setEmail] = useState("");
  const [fullName, setFullName] = useState("");
  const [withEndorsement, setWithEndorsement] = useState(true);
  const [endorsementNote, setEndorsementNote] = useState("");

  const [submitting, setSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [lastResult, setLastResult] = useState<PartnerGuestInviteResultDto | null>(null);
  const [copied, setCopied] = useState(false);

  if (isLoading) return <Loader label="Loading..." />;
  if (error) return <ErrorState error={error} onRetry={() => void refresh()} />;
  if (!membership) return null;

  const isAdmin = membership.memberRole === "Admin";
  const isVerified = membership.organization.status === "Verified";
  const canInvite = isAdmin && isVerified;

  const validate = () => {
    if (!email.trim() || !email.includes("@")) {
      setSubmitError("Please enter a valid email address.");
      return false;
    }
    if (!fullName.trim()) {
      setSubmitError("Please enter the member's full name.");
      return false;
    }
    return true;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSubmitError(null);
    if (!validate()) return;
    setSubmitting(true);
    try {
      const result = await partnerApi.inviteGuest(membership.organization.id, {
        email: email.trim(),
        fullName: fullName.trim(),
        withEndorsement,
        endorsementNote: withEndorsement ? endorsementNote.trim() || null : null,
      });
      setLastResult(result);
      setEmail("");
      setFullName("");
      setEndorsementNote("");
    } catch (err) {
      setSubmitError(extractErrorMessage(err));
    } finally {
      setSubmitting(false);
    }
  };

  const copySetPasswordUrl = async () => {
    if (!lastResult?.setPasswordUrl) return;
    try {
      await navigator.clipboard.writeText(lastResult.setPasswordUrl);
      setCopied(true);
      window.setTimeout(() => setCopied(false), 2000);
    } catch {
      /* ignore */
    }
  };

  return (
    <div className="space-y-6">
      <div>
        <h1 className="flex items-center gap-2 text-3xl font-bold tracking-tight">
          <Mail className="h-7 w-7 text-muted-foreground" />
          Invite a member
        </h1>
        <p className="mt-1 text-muted-foreground">
          Create a Lagedra account for someone you relocate or host. They&apos;ll receive an email
          with a link to set their password. After they&apos;re endorsed, book for them from
          Reservations.
        </p>
      </div>

      {!canInvite && (
        <Alert>
          <AlertDescription>
            {!isAdmin
              ? "Only organization admins can invite members."
              : "Your organization must be verified before you can invite members."}
          </AlertDescription>
        </Alert>
      )}

      {lastResult && (
        <Alert variant="success">
          <CheckCircle2 className="h-4 w-4" />
          <AlertDescription>
            <div className="space-y-2">
              <p className="font-medium">
                Invite sent to {lastResult.email}
                {lastResult.wasUserJustCreated
                  ? " — a new account was created."
                  : " — they already had an account."}
              </p>
              <ul className="ml-5 list-disc text-sm">
                {lastResult.endorsementId ? (
                  <li>
                    Endorsement <strong>auto-approved</strong>. You can book for them from
                    Reservations.
                  </li>
                ) : (
                  <li>
                    No endorsement was created. Endorse them before booking on their behalf.
                  </li>
                )}
              </ul>
              {lastResult.setPasswordUrl && (
                <div className="rounded-md border bg-background p-3">
                  <p className="text-xs text-muted-foreground mb-1">
                    Set-password link (also emailed)
                    {lastResult.setPasswordTokenExpiresAt &&
                      ` — expires ${new Date(
                        lastResult.setPasswordTokenExpiresAt,
                      ).toLocaleString(undefined, {
                        dateStyle: "medium",
                        timeStyle: "short",
                      })}`}
                  </p>
                  <div className="flex items-center gap-2">
                    <code className="flex-1 truncate font-mono text-xs">
                      {lastResult.setPasswordUrl}
                    </code>
                    <Button
                      type="button"
                      variant="outline"
                      size="sm"
                      onClick={() => void copySetPasswordUrl()}
                    >
                      {copied ? <Check className="h-3 w-3" /> : <Copy className="h-3 w-3" />}
                      {copied ? "Copied" : "Copy"}
                    </Button>
                  </div>
                </div>
              )}
            </div>
          </AlertDescription>
        </Alert>
      )}

      <Card>
        <CardHeader>
          <CardTitle>New member invite</CardTitle>
          <CardDescription>
            If a Lagedra account already exists for this email, we&apos;ll reuse it instead of
            creating a duplicate. Booking is done separately on the Reservations page.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={(e) => void handleSubmit(e)} className="space-y-4">
            <div className="grid gap-4 md:grid-cols-2">
              <div className="space-y-2">
                <Label htmlFor="invite-name">Full name</Label>
                <Input
                  id="invite-name"
                  value={fullName}
                  onChange={(e) => setFullName(e.target.value)}
                  placeholder="Jane Doe"
                  disabled={!canInvite}
                  required
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="invite-email">Email</Label>
                <Input
                  id="invite-email"
                  type="email"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  placeholder="jane@example.com"
                  disabled={!canInvite}
                  required
                />
              </div>
            </div>

            <div className="space-y-3 rounded-md border p-4">
              <div className="flex items-start gap-3">
                <Checkbox
                  id="invite-endorse"
                  checked={withEndorsement}
                  onCheckedChange={(value) => setWithEndorsement(value === true)}
                  disabled={!canInvite}
                />
                <Label
                  htmlFor="invite-endorse"
                  className="text-sm font-normal leading-relaxed"
                >
                  <span className="flex items-center gap-1 font-medium text-foreground">
                    <ShieldCheck className="h-4 w-4" />
                    Auto-approve a Partner-Backed Protection endorsement
                  </span>
                  <span className="mt-1 block text-muted-foreground">
                    Required before you can book on their behalf. You can revoke it anytime from
                    the Endorsements tab.
                  </span>
                </Label>
              </div>
              {withEndorsement && (
                <div className="space-y-2 pl-7">
                  <Label htmlFor="invite-note">Endorsement note (optional)</Label>
                  <Textarea
                    id="invite-note"
                    value={endorsementNote}
                    onChange={(e) => setEndorsementNote(e.target.value)}
                    placeholder="e.g. Approved by HR for Q3 relocation."
                    rows={2}
                    disabled={!canInvite}
                  />
                </div>
              )}
            </div>

            {submitError && <FormError message={submitError} />}

            <Button type="submit" disabled={!canInvite || submitting}>
              {submitting && <Loader2 className="h-4 w-4 animate-spin" />}
              Send invite
            </Button>
          </form>
        </CardContent>
      </Card>
    </div>
  );
};
