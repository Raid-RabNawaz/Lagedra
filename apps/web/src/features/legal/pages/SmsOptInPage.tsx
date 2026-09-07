import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { CheckCircle2, AlertTriangle } from "lucide-react";
import { getApiErrorMessage } from "@/api/errors";
import { useAuthStore } from "@/app/auth/authStore";
import { JoinLogo } from "@/features/join/components/JoinLogo";
import { notificationApi } from "@/features/notifications/services/notificationApi";
import { useNotificationPreferences } from "@/features/notifications/hooks/useNotifications";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { SmsProgramDisclosures } from "../SmsProgramDisclosures";
import { SMS_OTP_PROGRAM, SMS_PROGRAM } from "../smsProgram";
import { StaticPageFooterLinks } from "../StaticPageChrome";

export const SmsOptInPage = () => {
  const user = useAuthStore((s) => s.user);
  const { data: preferences } = useNotificationPreferences(user?.userId);

  const [phone, setPhone] = useState("");
  const [consent, setConsent] = useState<boolean>(SMS_PROGRAM.defaultConsent);
  const [pending, setPending] = useState(false);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [optedIn, setOptedIn] = useState(false);

  useEffect(() => {
    const previous = window.document.title;
    window.document.title = "Text message alerts — Lagedra";
    return () => {
      window.document.title = previous;
    };
  }, []);

  useEffect(() => {
    const hash = window.location.hash.replace("#", "");
    if (!hash) return;
    window.document.getElementById(hash)?.scrollIntoView({ behavior: "smooth", block: "start" });
  }, []);

  useEffect(() => {
    const candidate = preferences?.smsPhoneE164?.trim() || user?.phoneNumber?.trim();
    if (candidate) {
      setPhone((current) => current || candidate);
    }
    if (preferences) {
      setOptedIn(preferences.smsCampaignsOptedIn === true);
    }
  }, [preferences, user?.phoneNumber]);

  const submit = async (nextOptedIn: boolean) => {
    setMessage(null);
    setError(null);
    setPending(true);
    try {
      const result = await notificationApi.recordSmsConsent({
        phoneNumber: phone,
        consent: nextOptedIn ? consent : false,
        optedIn: nextOptedIn,
        source: "WebForm",
      });
      setOptedIn(result.optedIn);
      setPhone(result.phoneE164);
      setConsent(SMS_PROGRAM.defaultConsent);
      setMessage(
        result.optedIn
          ? "You are subscribed to Lagedra automated texts."
          : "You are unsubscribed from Lagedra automated texts.",
      );
    } catch (e) {
      setError(getApiErrorMessage(e, "We could not update your text-message preference."));
    } finally {
      setPending(false);
    }
  };

  return (
    <div className="min-h-screen bg-white">
      <header className="sticky top-0 z-10 border-b border-[#E5E5EE] bg-white/90 backdrop-blur">
        <div className="mx-auto flex max-w-5xl items-center justify-between px-6 py-4">
          <Link to="/listings" className="hover:opacity-90">
            <JoinLogo />
          </Link>
          <nav className="flex items-center gap-4 text-sm font-semibold text-[#3D3D4E]">
            <Link to="/tc" className="hover:text-[#1A1A2E]">
              Terms
            </Link>
            <Link to="/privacy" className="hover:text-[#1A1A2E]">
              Privacy
            </Link>
            <Link to="/join" className="hover:text-[#1A1A2E]">
              Join
            </Link>
          </nav>
        </div>
      </header>

      <main className="mx-auto max-w-xl px-6 py-12 sm:py-16">
        <p className="text-xs font-semibold uppercase tracking-wider text-[#ABABBE]">SMS alerts</p>
        <h1 className="mt-3 text-4xl font-extrabold tracking-tight text-[#1A1A2E]">
          Lagedra text alert subscription
        </h1>
        <p className="mt-4 text-lg leading-relaxed text-[#3D3D4E]">
          Get automated texts about bookings, payments, account updates, and
          occasional offers. Consent is not required to book a stay or use
          Lagedra.
        </p>

        {optedIn && (
          <Alert variant="success" className="mt-6">
            <CheckCircle2 className="h-4 w-4" />
            <AlertDescription>
              This number is subscribed to Lagedra automated texts.
            </AlertDescription>
          </Alert>
        )}
        {message && (
          <Alert variant="success" className="mt-6">
            <CheckCircle2 className="h-4 w-4" />
            <AlertDescription>{message}</AlertDescription>
          </Alert>
        )}
        {error && (
          <Alert variant="destructive" className="mt-6">
            <AlertTriangle className="h-4 w-4" />
            <AlertDescription>{error}</AlertDescription>
          </Alert>
        )}

        <form
          className="mt-8 space-y-6 rounded-2xl border border-[#E5E5EE] p-6 sm:p-8"
          onSubmit={(event) => {
            event.preventDefault();
            void submit(true);
          }}
        >
          <div className="space-y-2">
            <Label htmlFor="sms-phone" className="text-[#1A1A2E]">
              Mobile phone number*
            </Label>
            <Input
              id="sms-phone"
              name="phone"
              type="tel"
              autoComplete="tel"
              required
              placeholder="(555) 123-4567"
              value={phone}
              onChange={(event) => setPhone(event.target.value)}
            />
          </div>

          <label className="flex items-start gap-3 cursor-pointer">
            <Checkbox
              id="sms-consent"
              name="consent"
              checked={consent}
              onCheckedChange={(checked) => setConsent(checked === true)}
              className="mt-0.5"
            />
            <span className="text-sm leading-6 text-[#3D3D4E]">{SMS_PROGRAM.checkboxLabel}</span>
          </label>

          <SmsProgramDisclosures />

          <Button
            type="submit"
            disabled={pending}
            className="h-12 w-full bg-[#1A1A2E] text-white hover:bg-[#1A1A2E]/90"
          >
            {pending ? "Saving..." : SMS_PROGRAM.submitLabel}
          </Button>
        </form>

        <div className="mt-6 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
          <button
            type="button"
            disabled={pending || !phone.trim()}
            onClick={() => void submit(false)}
            className="text-sm font-medium text-[#3D3D4E] underline underline-offset-2 hover:text-[#1A1A2E] disabled:opacity-50"
          >
            {SMS_PROGRAM.unsubscribeLabel}
          </button>
          <p className="text-sm text-[#ABABBE]">
            Or reply STOP from your phone.{" "}
            <Link to="/tc#sms" className="underline underline-offset-2 hover:text-[#1A1A2E]">
              SMS terms
            </Link>
          </p>
        </div>

        <section id="otp" className="mt-16 scroll-mt-24 space-y-6">
          <p className="text-xs font-semibold uppercase tracking-wider text-[#ABABBE]">
            Two-factor authentication
          </p>
          <h2 className="text-2xl font-extrabold tracking-tight text-[#1A1A2E]">
            Phone verification codes
          </h2>
          <p className="text-[15px] leading-7 text-[#3D3D4E]">
            Lagedra also texts a one-time passcode when you ask us to confirm
            a mobile number. That is a separate program from the alerts above.
            We send a code only after you create an account, add a number, and
            tap <strong>Send verification code</strong> on Verification.
          </p>
          <ol className="list-decimal space-y-2 pl-5 text-[15px] leading-7 text-[#3D3D4E]">
            <li>
              Create a Lagedra account at{" "}
              <Link to="/join" className="font-medium text-[#5B3FE0] underline underline-offset-2">
                www.lagedra.com/join
              </Link>
              .
            </li>
            <li>Add your mobile number in your profile.</li>
            <li>
              Open Verification and tap <strong>Send verification code</strong>.
              That tap is the opt-in for the one-time text.
            </li>
            <li>Enter the 6-digit code. It expires in 10 minutes.</li>
          </ol>
          <div className="space-y-4 rounded-2xl border border-[#E5E5EE] p-6 sm:p-8">
            <p className="text-sm font-semibold text-[#1A1A2E]">
              In-app opt-in (shown here for reviewers)
            </p>
            <div className="space-y-2">
              <Label className="text-[#1A1A2E]">Mobile phone number on file</Label>
              <Input value="[MobileNumber]" readOnly aria-readonly />
            </div>
            <p className="text-sm leading-6 text-[#3D3D4E]">{SMS_OTP_PROGRAM.optInLabel}</p>
            <Button type="button" disabled className="h-12 w-full bg-[#1A1A2E] text-white">
              Send verification code
            </Button>
            <p className="text-sm leading-6 text-[#3D3D4E]">
              Example message: {SMS_OTP_PROGRAM.sample}
            </p>
            <p className="text-sm leading-6 text-[#3D3D4E]">
              Reply <strong>HELP</strong> for help or <strong>STOP</strong> to
              cancel. Support:{" "}
              <a href="mailto:info@lagedra.com" className="font-medium text-[#5B3FE0] underline underline-offset-2">
                info@lagedra.com
              </a>{" "}
              or{" "}
              <a href="tel:+12137352362" className="font-medium text-[#5B3FE0] underline underline-offset-2">
                213-735-2362
              </a>
              .{" "}
              <Link to="/tc#sms" className="font-medium text-[#5B3FE0] underline underline-offset-2">
                Terms
              </Link>
              {" | "}
              <Link to="/privacy#sms" className="font-medium text-[#5B3FE0] underline underline-offset-2">
                Privacy
              </Link>
            </p>
          </div>
        </section>
      </main>

      <footer className="border-t border-[#E5E5EE] px-6 py-8">
        <div className="mx-auto flex max-w-5xl flex-col gap-3 text-xs text-[#ABABBE] sm:flex-row sm:items-center sm:justify-between">
          <p>&copy; {new Date().getFullYear()} Lagedra. Mid-term rental trust protocol.</p>
          <StaticPageFooterLinks />
        </div>
      </footer>
    </div>
  );
};
