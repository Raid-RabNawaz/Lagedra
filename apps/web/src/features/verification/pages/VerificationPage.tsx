import { useEffect, useMemo, useState } from "react";
import { isAxiosError } from "axios";
import { Link, useNavigate } from "react-router-dom";
import {
  Shield,
  CheckCircle2,
  Clock,
  AlertTriangle,
  XCircle,
  UserCheck,
  FileSearch,
  ShieldCheck,
  ChevronRight,
  Loader2,
  Mail,
  Phone,
  Send,
  Sparkles,
  RefreshCw,
} from "lucide-react";
import { useAuthStore } from "@/app/auth/authStore";
import { authApi } from "@/features/auth/services/authApi";
import { privacyApi } from "@/features/privacy/services/privacyApi";
import {
  useVerificationStatus,
  useStartKyc,
  useCompleteKyc,
  useSubmitBackgroundCheckConsent,
  useRiskView,
} from "@/features/verification/hooks/useVerification";
import type { VerificationStatus, VerificationClassLevel } from "@/api/types";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Separator } from "@/components/ui/separator";
import { BackLink } from "@/components/shared/BackLink";
import { Loader } from "@/components/shared/Loader";
import { Checkbox } from "@/components/ui/checkbox";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { formatMoney } from "@/utils/format";
import { getApiErrorMessage } from "@/api/errors";
import { ProtectionTierBadge } from "@/features/partners/components/ProtectionTierBadge";
import { TenantEndorsementsPanel } from "@/features/partners/components/TenantEndorsementsPanel";

function statusConfig(status: VerificationStatus) {
  const map: Record<
    VerificationStatus,
    { label: string; icon: typeof CheckCircle2; color: string; badgeVariant: "default" | "secondary" | "destructive" | "accent" }
  > = {
    NotStarted: { label: "Not started", icon: Clock, color: "text-muted-foreground", badgeVariant: "secondary" },
    Pending: { label: "In progress", icon: Loader2, color: "text-amber-500", badgeVariant: "default" },
    Verified: { label: "Verified", icon: CheckCircle2, color: "text-emerald-600", badgeVariant: "accent" },
    Failed: { label: "Failed", icon: XCircle, color: "text-red-500", badgeVariant: "destructive" },
    ManualReviewRequired: { label: "Under review", icon: FileSearch, color: "text-amber-500", badgeVariant: "default" },
  };
  return map[status] ?? map.NotStarted;
}

function classConfig(level: VerificationClassLevel) {
  const map: Record<
    VerificationClassLevel,
    { label: string; color: string; description: string }
  > = {
    High: { label: "High", color: "text-emerald-600", description: "Fully verified — eligible for the lowest deposit bands." },
    Medium: { label: "Medium", color: "text-amber-500", description: "Partially verified — standard deposit range applies." },
    Low: { label: "Low", color: "text-red-500", description: "Minimal verification — higher deposit requirements apply." },
  };
  return map[level] ?? map.Low;
}

const AUTO_DISMISS_MS = 5_000;

export const VerificationPage = () => {
  const navigate = useNavigate();
  const user = useAuthStore((s) => s.user);
  const userId = user?.userId;

  const { data: verificationStatus, isLoading, error: statusError } = useVerificationStatus(userId);
  const riskUserId = verificationStatus?.status === "Verified" ? userId : undefined;
  const { data: riskView } = useRiskView(riskUserId);

  const startKyc = useStartKyc();
  const completeKyc = useCompleteKyc();
  const bgConsent = useSubmitBackgroundCheckConsent();

  const [consentChecked, setConsentChecked] = useState(false);
  const [actionError, setActionError] = useState<string | null>(null);
  const [actionSuccess, setActionSuccess] = useState<string | null>(null);

  // Inline DOB capture when starting KYC and the auth profile has none.
  const initialDob = user?.dateOfBirth ?? "";
  const [dobInput, setDobInput] = useState<string>(initialDob);
  useEffect(() => {
    setDobInput(initialDob);
  }, [initialDob]);

  // Resend verification email (one-shot, with cooldown so users don't spam).
  const [resendingEmail, setResendingEmail] = useState(false);
  const [emailCooldownSec, setEmailCooldownSec] = useState(0);
  useEffect(() => {
    if (emailCooldownSec <= 0) return;
    const t = window.setTimeout(() => setEmailCooldownSec((n) => n - 1), 1_000);
    return () => window.clearTimeout(t);
  }, [emailCooldownSec]);

  // Phone OTP verification
  const [phoneCode, setPhoneCode] = useState("");
  const [sendingPhoneCode, setSendingPhoneCode] = useState(false);
  const [confirmingPhoneCode, setConfirmingPhoneCode] = useState(false);
  const [phoneCodeSent, setPhoneCodeSent] = useState(false);
  const [phoneCooldownSec, setPhoneCooldownSec] = useState(0);
  useEffect(() => {
    if (phoneCooldownSec <= 0) return;
    const t = window.setTimeout(() => setPhoneCooldownSec((n) => n - 1), 1_000);
    return () => window.clearTimeout(t);
  }, [phoneCooldownSec]);

  // Auto-dismiss success/error toasts inline.
  useEffect(() => {
    if (!actionSuccess) return;
    const t = window.setTimeout(() => setActionSuccess(null), AUTO_DISMISS_MS);
    return () => window.clearTimeout(t);
  }, [actionSuccess]);
  useEffect(() => {
    if (!actionError) return;
    const t = window.setTimeout(() => setActionError(null), AUTO_DISMISS_MS * 2);
    return () => window.clearTimeout(t);
  }, [actionError]);

  const kycStatus = verificationStatus?.status ?? "NotStarted";
  const kycConfig = statusConfig(kycStatus);
  const KycIcon = kycConfig.icon;

  const isVerified = kycStatus === "Verified";
  const canStartKyc = kycStatus === "NotStarted" || kycStatus === "Failed";
  const isPending = kycStatus === "Pending";

  const emailVerified = Boolean(user?.emailConfirmed ?? user?.isActive);
  const phoneVerified = Boolean(user?.isPhoneVerified);
  const govIdVerified = Boolean(user?.isGovernmentIdVerified) || isVerified;

  const phoneStepEnabled = true;
  const hasPhoneNumber = Boolean(user?.phoneNumber?.trim());
  const totalSteps = 3;
  const completedSteps =
    (emailVerified ? 1 : 0) + (govIdVerified ? 1 : 0) + (phoneVerified ? 1 : 0);

  const verClass = verificationStatus?.verificationClass ?? riskView?.verificationClass;
  const classInfo = verClass ? classConfig(verClass) : null;

  const nextStep = useMemo(() => {
    if (!emailVerified) {
      return {
        title: "Verify your email",
        body: `We sent a verification link to ${user?.email ?? "your inbox"}. Open it to confirm this address.`,
        href: "#email",
        cta: "Resend verification email",
      };
    }
    if (!phoneVerified) {
      return {
        title: hasPhoneNumber ? "Verify your phone" : "Add a phone number",
        body: hasPhoneNumber
          ? `We'll text a 6-digit code to ${user?.phoneNumber}. Enter it below to confirm this number.`
          : "Add a phone number in your profile, then return here to verify it by SMS.",
        href: hasPhoneNumber ? "#phone" : "/app/profile",
        cta: hasPhoneNumber ? "Send verification code" : "Open profile",
      };
    }
    if (canStartKyc) {
      return {
        title: "Verify your identity",
        body: "Confirm a government-issued ID to unlock the lowest deposit bands and faster booking.",
        href: "#kyc",
        cta: kycStatus === "Failed" ? "Retry verification" : "Start verification",
      };
    }
    if (isPending) {
      return {
        title: "Verification in progress",
        body: "Hang tight — we'll update this page automatically the moment your check completes.",
        href: "#kyc",
        cta: null,
      };
    }
    if (kycStatus === "ManualReviewRequired") {
      return {
        title: "Under manual review",
        body: "Our team is reviewing your submission. We'll notify you within 1–2 business days.",
        href: "#kyc",
        cta: null,
      };
    }
    if (isVerified && classInfo?.label !== "High") {
      return {
        title: "Add a background check",
        body: "Optional — but it raises your trust class and may lower your deposit further.",
        href: "#background",
        cta: "Open background check",
      };
    }
    return null;
  }, [
    emailVerified,
    phoneVerified,
    hasPhoneNumber,
    canStartKyc,
    isPending,
    kycStatus,
    isVerified,
    classInfo,
    user?.email,
    user?.phoneNumber,
  ]);

  if (isLoading) {
    return <Loader fullPage label="Loading verification status..." />;
  }

  const scrollToAnchor = (hash: string) => {
    const id = hash.replace(/^#/, "");
    const el = document.getElementById(id);
    if (el) el.scrollIntoView({ behavior: "smooth", block: "start" });
  };

  const handleStartKyc = async () => {
    if (!userId) return;
    setActionError(null);
    setActionSuccess(null);

    const dob = dobInput.trim() || user?.dateOfBirth || null;
    if (!dob) {
      setActionError("Please enter your date of birth before starting verification.");
      return;
    }

    try {
      await privacyApi.ensureRequiredConsents(userId);
      await startKyc.mutateAsync({
        userId,
        firstName: user?.firstName,
        lastName: user?.lastName,
        dateOfBirth: dob,
      });
      setActionSuccess(
        "Identity verification started. With no KYC partner configured we've auto-approved it for you.",
      );
    } catch (e) {
      if (isAxiosError(e) && e.response?.status === 451) {
        setActionError("Consent is required before verification. Please accept legal consents and try again.");
        return;
      }
      setActionError((e as Error)?.message ?? "Failed to start identity verification.");
    }
  };

  const handleCompleteKyc = async () => {
    if (!userId) return;
    setActionError(null);
    setActionSuccess(null);
    try {
      await completeKyc.mutateAsync({ userId });
      setActionSuccess("Identity verification completed successfully.");
    } catch (e) {
      setActionError((e as Error)?.message ?? "Failed to complete verification.");
    }
  };

  const handleBackgroundCheck = async () => {
    if (!userId) return;
    setActionError(null);
    setActionSuccess(null);
    try {
      await bgConsent.mutateAsync({ userId });
      setActionSuccess(
        "Background check consent submitted. With no provider configured we've auto-cleared it for you.",
      );
    } catch (e) {
      setActionError((e as Error)?.message ?? "Failed to submit background check consent.");
    }
  };

  const handleResendVerification = async () => {
    if (!user?.email || resendingEmail || emailCooldownSec > 0) return;
    setResendingEmail(true);
    setActionError(null);
    setActionSuccess(null);
    try {
      await authApi.resendVerification(user.email);
      setActionSuccess(`If an unverified account matches ${user.email}, a fresh link is on its way.`);
      setEmailCooldownSec(45);
    } catch (e) {
      setActionError((e as Error)?.message ?? "Could not resend the verification email.");
    } finally {
      setResendingEmail(false);
    }
  };

  const handleSendPhoneCode = async () => {
    if (!hasPhoneNumber || sendingPhoneCode || phoneCooldownSec > 0 || phoneVerified) return;
    setSendingPhoneCode(true);
    setActionError(null);
    setActionSuccess(null);
    try {
      await authApi.sendPhoneVerificationCode();
      setPhoneCodeSent(true);
      setPhoneCooldownSec(60);
      setActionSuccess(`We sent a verification code to ${user?.phoneNumber}.`);
    } catch (e) {
      setActionError(getApiErrorMessage(e) || "Could not send the verification code.");
    } finally {
      setSendingPhoneCode(false);
    }
  };

  const handleConfirmPhoneCode = async () => {
    const code = phoneCode.trim();
    if (!code || confirmingPhoneCode || phoneVerified) return;
    setConfirmingPhoneCode(true);
    setActionError(null);
    setActionSuccess(null);
    try {
      await authApi.confirmPhoneVerificationCode(code);
      const me = await authApi.getCurrentUser();
      useAuthStore.getState().setUser(me);
      setPhoneCode("");
      setPhoneCodeSent(false);
      setActionSuccess("Phone number verified.");
    } catch (e) {
      setActionError(getApiErrorMessage(e) || "Could not verify the code.");
    } finally {
      setConfirmingPhoneCode(false);
    }
  };

  return (
    <div className="space-y-8">
      {/* Header */}
      <div>
        <BackLink fallbackTo="/app/profile" className="mb-4" />
        <h1 className="text-3xl font-bold tracking-tight">Verification</h1>
        <p className="mt-1 text-muted-foreground">
          Complete the steps below to raise your trust class and unlock lower deposits.
        </p>
      </div>

      {/* Inline alerts */}
      {actionSuccess && (
        <Alert variant="success">
          <CheckCircle2 className="h-4 w-4" />
          <AlertDescription>{actionSuccess}</AlertDescription>
        </Alert>
      )}
      {actionError && (
        <Alert variant="destructive">
          <AlertTriangle className="h-4 w-4" />
          <AlertDescription>{actionError}</AlertDescription>
        </Alert>
      )}
      {statusError && !verificationStatus && (
        <Alert>
          <AlertDescription className="text-muted-foreground">
            No verification profile found yet. Start identity verification below to create one.
          </AlertDescription>
        </Alert>
      )}

      {/* Next-step callout */}
      {nextStep && (
        <Card className="border-primary/30 bg-primary/5">
          <CardContent className="flex flex-col gap-3 p-5 sm:flex-row sm:items-center sm:justify-between">
            <div className="flex items-start gap-3">
              <div className="rounded-full bg-primary/10 p-2 text-primary">
                <Sparkles className="h-5 w-5" />
              </div>
              <div>
                <p className="text-xs font-medium uppercase tracking-wide text-primary/80">Next step</p>
                <p className="mt-0.5 font-semibold">{nextStep.title}</p>
                <p className="mt-1 text-sm text-muted-foreground">{nextStep.body}</p>
              </div>
            </div>
            {nextStep.cta && (
              <Button
                variant="default"
                onClick={() => {
                  if (nextStep.href.startsWith("/")) {
                    void navigate(nextStep.href);
                    return;
                  }
                  if (nextStep.cta === "Send verification code") {
                    void handleSendPhoneCode();
                    scrollToAnchor("#phone");
                    return;
                  }
                  if (nextStep.cta === "Resend verification email") {
                    void handleResendVerification();
                    scrollToAnchor("#email");
                    return;
                  }
                  scrollToAnchor(nextStep.href);
                }}
                className="shrink-0 gap-2"
              >
                {nextStep.cta}
                <ChevronRight className="h-4 w-4" />
              </Button>
            )}
          </CardContent>
        </Card>
      )}

      {/* Progress overview */}
      <Card>
        <CardHeader className="pb-3">
          <div className="flex items-center justify-between">
            <CardTitle className="text-base flex items-center gap-2">
              <Shield className="h-4 w-4" />
              Verification progress
            </CardTitle>
            {classInfo && (
              <Badge variant="secondary" className={classInfo.color}>
                {classInfo.label} trust
              </Badge>
            )}
          </div>
        </CardHeader>
        <CardContent>
          <div className="flex items-center gap-2 mb-4">
            <div className="flex-1 h-2 bg-muted rounded-full overflow-hidden">
              <div
                className="h-full bg-emerald-500 rounded-full transition-all duration-500"
                style={{ width: `${(completedSteps / totalSteps) * 100}%` }}
              />
            </div>
            <span className="text-sm font-medium text-muted-foreground">
              {completedSteps}/{totalSteps}
            </span>
          </div>

          <div className={`grid gap-3 ${phoneStepEnabled ? "sm:grid-cols-3" : "sm:grid-cols-2"}`}>
            <StepIndicator
              label="Email"
              done={emailVerified}
              description={emailVerified ? "Verified" : "Check your inbox"}
              onClick={() => scrollToAnchor("#email")}
            />
            <StepIndicator
              label="Government ID"
              done={govIdVerified}
              description={govIdVerified ? "Verified" : "Complete KYC below"}
              onClick={() => scrollToAnchor("#kyc")}
            />
            {phoneStepEnabled && (
              <StepIndicator
                label="Phone"
                done={phoneVerified}
                description={
                  phoneVerified
                    ? "Verified"
                    : hasPhoneNumber
                      ? "Confirm via SMS"
                      : "Add in profile settings"
                }
                onClick={() => scrollToAnchor("#phone")}
              />
            )}
          </div>

          {classInfo && (
            <>
              <Separator className="my-4" />
              <p className="text-sm text-muted-foreground">{classInfo.description}</p>
            </>
          )}

          {riskView && (
            <div className="mt-3 space-y-3">
              <div className="grid grid-cols-2 gap-3 text-sm">
                <div>
                  <span className="text-muted-foreground">Deposit range</span>
                  <p className="font-medium">
                    {formatMoney(riskView.depositBandLowCents)} – {formatMoney(riskView.depositBandHighCents)}
                  </p>
                </div>
                <div>
                  <span className="text-muted-foreground">Confidence</span>
                  <p className="font-medium">
                    {riskView.confidenceLevel} — {riskView.confidenceReason}
                  </p>
                </div>
              </div>
              <div className="flex flex-wrap items-center gap-2 text-sm">
                <span className="text-muted-foreground">Protection</span>
                <ProtectionTierBadge
                  tier={riskView.protectionTier}
                  orgName={riskView.endorsedBy[0]?.organizationName}
                  expiresAt={riskView.endorsedBy[0]?.expiresAt}
                />
              </div>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Email verification */}
      <Card id="email">
        <CardHeader>
          <div className="flex items-center justify-between">
            <div>
              <CardTitle className="text-lg flex items-center gap-2">
                <Mail className="h-5 w-5" />
                Email
              </CardTitle>
              <CardDescription className="mt-1">
                Confirm we can reach you at <span className="font-medium">{user?.email}</span>.
              </CardDescription>
            </div>
            <Badge variant={emailVerified ? "accent" : "secondary"}>
              {emailVerified ? (
                <>
                  <CheckCircle2 className="mr-1 h-3 w-3 text-emerald-600" />
                  Verified
                </>
              ) : (
                <>
                  <Clock className="mr-1 h-3 w-3 text-muted-foreground" />
                  Not verified
                </>
              )}
            </Badge>
          </div>
        </CardHeader>
        <CardContent>
          {emailVerified ? (
            <p className="text-sm text-muted-foreground">
              Your email is confirmed. You're all set on this step.
            </p>
          ) : (
            <div className="space-y-3">
              <p className="text-sm text-muted-foreground">
                Open the link we emailed you at sign-up. Can't find it? Check spam, then resend below.
              </p>
              <Button
                variant="outline"
                onClick={() => void handleResendVerification()}
                disabled={resendingEmail || emailCooldownSec > 0}
                className="gap-2"
              >
                {resendingEmail ? (
                  <Loader2 className="h-4 w-4 animate-spin" />
                ) : (
                  <Send className="h-4 w-4" />
                )}
                {emailCooldownSec > 0
                  ? `Resend in ${emailCooldownSec}s`
                  : resendingEmail
                    ? "Sending..."
                    : "Resend verification email"}
              </Button>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Identity verification (KYC) */}
      <Card id="kyc">
        <CardHeader>
          <div className="flex items-center justify-between">
            <div>
              <CardTitle className="text-lg flex items-center gap-2">
                <UserCheck className="h-5 w-5" />
                Identity verification
              </CardTitle>
              <CardDescription className="mt-1">
                Verify your identity with a government-issued ID. This confirms you are who you say you are.
              </CardDescription>
            </div>
            <Badge variant={kycConfig.badgeVariant}>
              <KycIcon className={`h-3 w-3 mr-1 ${kycConfig.color} ${isPending ? "animate-spin" : ""}`} />
              {kycConfig.label}
            </Badge>
          </div>
        </CardHeader>
        <CardContent>
          {isVerified && (
            <div className="flex items-center gap-3 rounded-lg border border-emerald-200 bg-emerald-50 p-4">
              <ShieldCheck className="h-8 w-8 text-emerald-600 shrink-0" />
              <div>
                <p className="font-medium text-emerald-800">Identity verified</p>
                <p className="text-sm text-emerald-700 mt-0.5">
                  Your government ID has been verified. This contributes to your trust level and may lower
                  your deposit requirements.
                </p>
              </div>
            </div>
          )}

          {canStartKyc && (
            <div className="space-y-4">
              <div className="rounded-lg border p-4 bg-muted/30 space-y-3">
                <h4 className="font-medium text-sm">What you'll need</h4>
                <ul className="text-sm text-muted-foreground space-y-1.5">
                  <li className="flex items-center gap-2">
                    <ChevronRight className="h-3 w-3 shrink-0" />
                    A valid government-issued photo ID (driver's license, passport, or state ID)
                  </li>
                  <li className="flex items-center gap-2">
                    <ChevronRight className="h-3 w-3 shrink-0" />
                    A device with a camera for a quick selfie
                  </li>
                  <li className="flex items-center gap-2">
                    <ChevronRight className="h-3 w-3 shrink-0" />
                    Takes about 2–3 minutes to complete
                  </li>
                </ul>
              </div>

              {!user?.dateOfBirth && (
                <div className="space-y-2">
                  <Label htmlFor="kyc-dob">Date of birth</Label>
                  <Input
                    id="kyc-dob"
                    type="date"
                    value={dobInput}
                    onChange={(e) => setDobInput(e.target.value)}
                    className="max-w-xs"
                  />
                  <p className="text-xs text-muted-foreground">
                    Used only to match the document you'll upload. We never share your DOB publicly.
                  </p>
                </div>
              )}

              {kycStatus === "Failed" && (
                <Alert variant="destructive">
                  <XCircle className="h-4 w-4" />
                  <AlertDescription>
                    Your previous verification attempt was unsuccessful. You can try again below.
                  </AlertDescription>
                </Alert>
              )}

              <Button
                onClick={() => void handleStartKyc()}
                disabled={startKyc.isPending}
                className="gap-2"
              >
                <UserCheck className="h-4 w-4" />
                {startKyc.isPending
                  ? "Starting..."
                  : kycStatus === "Failed"
                    ? "Retry verification"
                    : "Start identity verification"}
              </Button>
            </div>
          )}

          {isPending && (
            <div className="space-y-4">
              <div className="flex items-center gap-3 rounded-lg border border-amber-200 bg-amber-50 p-4">
                <Loader2 className="h-6 w-6 text-amber-500 animate-spin shrink-0" />
                <div>
                  <p className="font-medium text-amber-800">Verification in progress</p>
                  <p className="text-sm text-amber-700 mt-0.5">
                    Your identity is being verified. This page refreshes automatically every few seconds —
                    you don't need to do anything.
                  </p>
                </div>
              </div>
              <Button
                variant="outline"
                onClick={() => void handleCompleteKyc()}
                disabled={completeKyc.isPending}
                className="gap-2"
              >
                <RefreshCw className={`h-4 w-4 ${completeKyc.isPending ? "animate-spin" : ""}`} />
                {completeKyc.isPending ? "Checking..." : "Check status now"}
              </Button>
            </div>
          )}

          {kycStatus === "ManualReviewRequired" && (
            <div className="flex items-center gap-3 rounded-lg border border-amber-200 bg-amber-50 p-4">
              <FileSearch className="h-6 w-6 text-amber-500 shrink-0" />
              <div>
                <p className="font-medium text-amber-800">Under review</p>
                <p className="text-sm text-amber-700 mt-0.5">
                  Your verification requires manual review by our team. We'll notify you once it's complete —
                  this typically takes 1–2 business days.
                </p>
              </div>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Phone OTP */}
      <Card id="phone">
        <CardHeader>
          <div className="flex items-center justify-between">
            <div>
              <CardTitle className="text-lg flex items-center gap-2">
                <Phone className="h-5 w-5" />
                Phone verification
              </CardTitle>
              <CardDescription className="mt-1">
                Confirm your number with a one-time SMS code. Verified phones receive booking alerts.
              </CardDescription>
            </div>
            {phoneVerified ? (
              <Badge variant="default" className="gap-1">
                <CheckCircle2 className="h-3 w-3" />
                Verified
              </Badge>
            ) : (
              <Badge variant="secondary">Not verified</Badge>
            )}
          </div>
        </CardHeader>
        <CardContent className="space-y-4">
          {phoneVerified ? (
            <p className="text-sm text-muted-foreground">
              {user?.phoneNumber} is verified. Change the number in your{" "}
              <Link to="/app/profile" className="text-primary hover:underline">
                profile
              </Link>{" "}
              to re-verify.
            </p>
          ) : !hasPhoneNumber ? (
            <div className="space-y-3">
              <p className="text-sm text-muted-foreground">
                Add a phone number in E.164 format (e.g. +14155552671) on your profile first.
              </p>
              <Button type="button" onClick={() => navigate("/app/profile")}>
                Open profile
              </Button>
            </div>
          ) : (
            <div className="space-y-4">
              <p className="text-sm text-muted-foreground">
                Number on file: <span className="font-medium text-foreground">{user?.phoneNumber}</span>
              </p>
              <div className="flex flex-wrap gap-2">
                <Button
                  onClick={() => void handleSendPhoneCode()}
                  disabled={sendingPhoneCode || phoneCooldownSec > 0}
                >
                  {sendingPhoneCode ? (
                    <>
                      <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                      Sending…
                    </>
                  ) : phoneCooldownSec > 0 ? (
                    `Resend in ${phoneCooldownSec}s`
                  ) : phoneCodeSent ? (
                    <>
                      <RefreshCw className="mr-2 h-4 w-4" />
                      Resend code
                    </>
                  ) : (
                    <>
                      <Send className="mr-2 h-4 w-4" />
                      Send verification code
                    </>
                  )}
                </Button>
              </div>
              {(phoneCodeSent || phoneCode.length > 0) && (
                <div className="space-y-2 max-w-xs">
                  <Label htmlFor="phone-otp">Verification code</Label>
                  <div className="flex gap-2">
                    <Input
                      id="phone-otp"
                      inputMode="numeric"
                      autoComplete="one-time-code"
                      placeholder="6-digit code"
                      value={phoneCode}
                      onChange={(e) => setPhoneCode(e.target.value.replace(/\D/g, "").slice(0, 6))}
                      maxLength={6}
                    />
                    <Button
                      onClick={() => void handleConfirmPhoneCode()}
                      disabled={phoneCode.trim().length < 4 || confirmingPhoneCode}
                    >
                      {confirmingPhoneCode ? (
                        <Loader2 className="h-4 w-4 animate-spin" />
                      ) : (
                        "Confirm"
                      )}
                    </Button>
                  </div>
                </div>
              )}
            </div>
          )}
        </CardContent>
      </Card>

      {/* Background check */}
      <Card id="background">
        <CardHeader>
          <CardTitle className="text-lg flex items-center gap-2">
            <FileSearch className="h-5 w-5" />
            Background check
          </CardTitle>
          <CardDescription>
            A background check helps build trust with landlords and can lower your deposit requirements. This
            is optional but recommended.
          </CardDescription>
        </CardHeader>
        <CardContent>
          {!isVerified ? (
            <div className="rounded-lg border p-4 bg-muted/30 flex items-start gap-3">
              <Clock className="h-4 w-4 text-muted-foreground shrink-0 mt-0.5" />
              <p className="text-sm text-muted-foreground">
                Complete identity verification above before requesting a background check.
              </p>
            </div>
          ) : (
            <div className="space-y-4">
              <div className="rounded-lg border p-4 bg-muted/30 space-y-2">
                <p className="text-sm">
                  By consenting, you authorize a consumer report under the Fair Credit Reporting Act (FCRA).
                  The report may include criminal records, eviction history, and credit information. Results
                  are kept confidential and used only for rental verification.
                </p>
              </div>

              <div className="flex items-start gap-2">
                <Checkbox
                  id="bg-consent"
                  checked={consentChecked}
                  onCheckedChange={(checked) => setConsentChecked(checked === true)}
                />
                <label htmlFor="bg-consent" className="text-sm leading-tight cursor-pointer">
                  I consent to a background check and understand my rights under the FCRA.
                </label>
              </div>

              <Button
                onClick={() => void handleBackgroundCheck()}
                disabled={bgConsent.isPending || !consentChecked}
                className="gap-2"
              >
                <FileSearch className="h-4 w-4" />
                {bgConsent.isPending ? "Submitting..." : "Submit background check consent"}
              </Button>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Institutional endorsements */}
      <TenantEndorsementsPanel />

      {/* Info footer */}
      <div className="rounded-lg bg-muted/50 p-4 text-xs text-muted-foreground flex items-start gap-2">
        <Shield className="h-4 w-4 shrink-0 mt-0.5" />
        <p>
          Your verification data is encrypted and handled in accordance with our privacy policy. Once a real
          verification partner is integrated, these steps will connect to their secure infrastructure.
          Currently running in development mode with auto-approval.
        </p>
      </div>
    </div>
  );
};

function StepIndicator({
  label,
  done,
  description,
  onClick,
}: {
  label: string;
  done: boolean;
  description: string;
  onClick?: () => void;
}) {
  const Component = onClick ? "button" : "div";
  return (
    <Component
      type={onClick ? "button" : undefined}
      onClick={onClick}
      className={`flex items-center gap-2 text-left ${
        onClick ? "rounded-md p-1 -m-1 hover:bg-muted/60 transition-colors cursor-pointer" : ""
      }`}
    >
      {done ? (
        <CheckCircle2 className="h-5 w-5 text-emerald-600 shrink-0" />
      ) : (
        <div className="h-5 w-5 rounded-full border-2 border-muted-foreground/30 shrink-0" />
      )}
      <div>
        <p className="text-sm font-medium">{label}</p>
        <p className="text-xs text-muted-foreground">{description}</p>
      </div>
    </Component>
  );
}
