import { Link } from "react-router-dom";
import { ArrowRight, Check, MailCheck } from "lucide-react";
import { JoinLogo } from "./JoinLogo";
import { successContent } from "../joinContent";
import type { SignupSuccessInfo } from "./SignupForm";

type SignupSuccessProps = {
  info: SignupSuccessInfo;
  onRestart: () => void;
};

export const SignupSuccess = ({ info, onRestart }: SignupSuccessProps) =>
  info.preLaunch ? (
    <PreLaunchSuccess onRestart={onRestart} />
  ) : (
    <VerifyEmailSuccess info={info} />
  );

const PreLaunchSuccess = ({ onRestart }: { onRestart: () => void }) => (
  <div className="mx-auto flex min-h-screen w-full max-w-2xl flex-col items-center px-6 py-12 text-center sm:py-16">
    <JoinLogo />

    <span className="mt-12 inline-flex items-center gap-2 rounded-full bg-emerald-50 px-4 py-1.5 text-sm font-semibold text-emerald-700">
      <Check className="h-4 w-4" strokeWidth={3} />
      {successContent.badge}
    </span>

    <h1 className="mt-6 text-4xl font-extrabold leading-tight tracking-tight text-[#1A1A2E] sm:text-5xl">
      {successContent.title}
    </h1>
    <p className="mt-4 max-w-xl text-lg text-[#3D3D4E]">{successContent.subtitle}</p>

    <div className="mt-10 w-full rounded-2xl border border-[#E5E5EE] bg-white p-6 text-left sm:p-8">
      <p className="text-xs font-semibold uppercase tracking-wider text-[#ABABBE]">
        {successContent.stepsHeading}
      </p>
      <ol className="mt-5 space-y-5">
        {successContent.steps.map((step) => (
          <li key={step.num} className="flex gap-4">
            <span className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-[#F3F0FE] text-sm font-bold text-[#5B3FE0]">
              {step.num}
            </span>
            <div>
              <p className="font-semibold text-[#1A1A2E]">{step.title}</p>
              <p className="mt-1 text-sm text-[#3D3D4E]">{step.body}</p>
            </div>
          </li>
        ))}
      </ol>
    </div>

    <p className="mt-6 max-w-xl text-sm text-[#3D3D4E]">{successContent.footnote}</p>

    <Link
      to="/how-it-works"
      className="mt-8 inline-flex items-center gap-2 rounded-xl bg-[#5B3FE0] px-6 py-3 text-[15px] font-semibold text-white transition-colors hover:bg-[#4A2FC7]"
    >
      {successContent.cta}
      <ArrowRight className="h-4 w-4" />
    </Link>

    <button
      type="button"
      onClick={onRestart}
      className="mt-4 text-sm font-medium text-[#3D3D4E] transition-colors hover:text-[#1A1A2E]"
    >
      Back to start
    </button>
  </div>
);

const VerifyEmailSuccess = ({ info }: { info: SignupSuccessInfo }) => (
  <div className="mx-auto flex min-h-screen w-full max-w-xl flex-col items-center px-6 py-12 text-center sm:py-16">
    <JoinLogo />

    <span className="mt-12 flex h-16 w-16 items-center justify-center rounded-full bg-[#F3F0FE]">
      <MailCheck className="h-8 w-8 text-[#5B3FE0]" />
    </span>

    <h1 className="mt-6 text-3xl font-extrabold tracking-tight text-[#1A1A2E]">Check your email</h1>
    <p className="mt-3 max-w-md text-[15px] text-[#3D3D4E]">
      We've sent a verification link to <span className="font-semibold text-[#1A1A2E]">{info.email}</span>.
      {info.setPasswordAfterVerify
        ? " Click it to verify your email and set your password — then you can sign in and start adding listings."
        : " Click it to activate your account, then sign in."}
    </p>

    {info.devVerificationUrl && (
      <div className="mt-6 w-full rounded-xl border border-[#E5E5EE] bg-[#F3F0FE]/40 p-4 text-left">
        <p className="text-xs font-semibold text-[#4A2FC7]">Dev verification URL</p>
        <code className="mt-1 block break-all text-xs text-[#3D3D4E]">{info.devVerificationUrl}</code>
      </div>
    )}

    <Link
      to="/auth/login"
      className="mt-8 inline-flex items-center gap-2 rounded-xl bg-[#5B3FE0] px-6 py-3 text-[15px] font-semibold text-white transition-colors hover:bg-[#4A2FC7]"
    >
      Go to sign in
      <ArrowRight className="h-4 w-4" />
    </Link>
  </div>
);
