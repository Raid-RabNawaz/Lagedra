import { useState } from "react";
import { useForm } from "react-hook-form";
import { useNavigate } from "react-router-dom";
import { z } from "zod";
import { zodResolver } from "@hookform/resolvers/zod";
import { ArrowLeft, ArrowRight } from "lucide-react";
import { authApi } from "@/features/auth/services/authApi";
import { useAuthStore } from "@/app/auth/authStore";
import { appConfig } from "@/app/config";
import { getApiErrorMessage } from "@/api/errors";
import { GoogleSignInButton } from "@/components/shared/GoogleSignInButton";
import { cn } from "@/lib/utils";
import { variantContent, type JoinVariant } from "../joinContent";

export type SignupSuccessInfo = {
  email: string;
  preLaunch: boolean;
  devVerificationUrl?: string;
};

type SignupFormProps = {
  variant: JoinVariant;
  preLaunch: boolean;
  onBack: () => void;
  onSuccess: (info: SignupSuccessInfo) => void;
};

const baseFields = {
  fullName: z.string().min(1, "Enter your full name"),
  email: z.string().email("Enter a valid email address"),
  companyName: z.string().optional(),
  phone: z.string().optional(),
  city: z.string().optional(),
};

const passwordSchema = z
  .string()
  .min(8, "Password must be at least 8 characters")
  .regex(/[A-Z]/, "Include at least one uppercase letter")
  .regex(/[0-9]/, "Include at least one number");

const inputClass =
  "w-full rounded-xl border border-[#E5E5EE] bg-white px-3.5 py-2.5 text-[15px] text-[#1A1A2E] placeholder:text-[#ABABBE] transition-colors focus:border-[#5B3FE0] focus:outline-none focus:ring-2 focus:ring-[#5B3FE0]/20 disabled:opacity-60";

export const SignupForm = ({ variant, preLaunch, onBack, onSuccess }: SignupFormProps) => {
  const content = variantContent[variant];
  const [serverError, setServerError] = useState<string | null>(null);
  const [googleLoading, setGoogleLoading] = useState(false);
  const [segments, setSegments] = useState<Record<number, string>>({});
  const setUser = useAuthStore((s) => s.setUser);
  const navigate = useNavigate();

  const schema = z.object(
    preLaunch ? baseFields : { ...baseFields, password: passwordSchema },
  );
  type FormData = z.infer<typeof schema> & { password?: string };

  const form = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: { fullName: "", email: "", companyName: "", phone: "", city: "" } as FormData,
  });

  const showGoogle = !preLaunch && Boolean(appConfig.googleClientId);

  const onSubmit = form.handleSubmit(async (data) => {
    setServerError(null);
    const [housingType, placementsPerYear] =
      variant === "partner" ? [segments[0], segments[1]] : [undefined, undefined];
    const portfolioSize = variant === "host" ? segments[0] : undefined;

    try {
      const result = await authApi.register({
        email: data.email,
        role: content.role,
        password: preLaunch ? undefined : data.password,
        fullName: data.fullName,
        companyName: data.companyName || undefined,
        phone: data.phone || undefined,
        city: data.city || undefined,
        signupType: content.signupType,
        portfolioSize,
        housingType,
        placementsPerYear,
      });
      onSuccess({
        email: data.email,
        preLaunch: result.preLaunch ?? preLaunch,
        devVerificationUrl: result.dev_verificationUrl,
      });
    } catch (error) {
      setServerError(getApiErrorMessage(error, "Something went wrong. Please try again."));
    }
  });

  const handleGoogle = async (idToken: string) => {
    setServerError(null);
    setGoogleLoading(true);
    try {
      await authApi.externalLogin({ provider: "Google", idToken, preferredRole: content.role });
      const me = await authApi.getCurrentUser();
      setUser(me);
      navigate("/app", { replace: true });
    } catch (error) {
      setServerError(getApiErrorMessage(error, "Google sign-up failed. Please try again."));
    } finally {
      setGoogleLoading(false);
    }
  };

  const isSubmitting = form.formState.isSubmitting || googleLoading;

  return (
    <div className="mx-auto w-full max-w-md px-6 py-10 lg:px-10 lg:py-14">
      <button
        type="button"
        onClick={onBack}
        disabled={isSubmitting}
        className="mb-8 inline-flex items-center gap-2 text-sm font-semibold text-[#3D3D4E] transition-colors hover:text-[#1A1A2E]"
      >
        <ArrowLeft className="h-4 w-4" />
        Back
      </button>

      <h2 className="text-3xl font-extrabold tracking-tight text-[#1A1A2E]">{content.formTitle}</h2>
      <p className="mt-2 text-[15px] text-[#3D3D4E]">{content.formSubtitle}</p>

      {showGoogle && (
        <div className="mt-6">
          <GoogleSignInButton
            onSuccess={handleGoogle}
            onError={() => setServerError("Google sign-up failed.")}
            text="signup_with"
          />
          <div className="relative my-6">
            <div className="h-px w-full bg-[#E5E5EE]" />
            <span className="absolute left-1/2 top-1/2 -translate-x-1/2 -translate-y-1/2 bg-white px-3 text-xs text-[#ABABBE]">
              or sign up with email
            </span>
          </div>
        </div>
      )}

      <form onSubmit={onSubmit} className={cn("space-y-4", !showGoogle && "mt-8")}>
        <Field label="Full name" error={form.formState.errors.fullName?.message}>
          <input
            className={inputClass}
            placeholder="Jordan Rivera"
            disabled={isSubmitting}
            {...form.register("fullName")}
          />
        </Field>

        <Field label="Company name">
          <input
            className={inputClass}
            placeholder={content.companyPlaceholder}
            disabled={isSubmitting}
            {...form.register("companyName")}
          />
        </Field>

        <Field label="Email" error={form.formState.errors.email?.message}>
          <input
            type="email"
            className={inputClass}
            placeholder="you@company.com"
            disabled={isSubmitting}
            {...form.register("email")}
          />
        </Field>

        {!preLaunch && (
          <Field
            label="Password"
            error={(form.formState.errors as Record<string, { message?: string }>).password?.message}
            hint="At least 8 characters with one uppercase letter and one number."
          >
            <input
              type="password"
              className={inputClass}
              placeholder="Create a strong password"
              disabled={isSubmitting}
              {...form.register("password" as keyof FormData)}
            />
          </Field>
        )}

        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <Field label="Phone (optional)">
            <input
              className={inputClass}
              placeholder="(555) 000-0000"
              disabled={isSubmitting}
              {...form.register("phone")}
            />
          </Field>
          <Field label="City">
            <input
              className={inputClass}
              placeholder="Austin, TX"
              disabled={isSubmitting}
              {...form.register("city")}
            />
          </Field>
        </div>

        {content.segments.map((segment, index) => (
          <SegmentedFieldControlled
            key={segment.label}
            label={segment.label}
            options={segment.options}
            value={segments[index] ?? null}
            disabled={isSubmitting}
            onChange={(value) => setSegments((prev) => ({ ...prev, [index]: value }))}
          />
        ))}

        {serverError && (
          <p className="rounded-lg bg-red-50 px-3 py-2 text-sm text-red-600">{serverError}</p>
        )}

        <button
          type="submit"
          disabled={isSubmitting}
          className="inline-flex w-full items-center justify-center gap-2 rounded-xl bg-[#5B3FE0] px-5 py-3 text-[15px] font-semibold text-white transition-colors hover:bg-[#4A2FC7] disabled:opacity-60"
        >
          {form.formState.isSubmitting ? (
            preLaunch ? "Joining..." : "Creating account..."
          ) : (
            <>
              {preLaunch ? "Join the partner program" : "Create account"}
              <ArrowRight className="h-4 w-4" />
            </>
          )}
        </button>

        <p className="text-center text-xs text-[#ABABBE]">{content.consent}</p>
      </form>
    </div>
  );
};

function Field({
  label,
  error,
  hint,
  children,
}: {
  label: string;
  error?: string;
  hint?: string;
  children: React.ReactNode;
}) {
  return (
    <div className="space-y-1.5">
      <label className="block text-sm font-semibold text-[#1A1A2E]">{label}</label>
      {children}
      {hint && !error && <p className="text-xs text-[#ABABBE]">{hint}</p>}
      {error && <p className="text-xs text-red-600">{error}</p>}
    </div>
  );
}

function SegmentedFieldControlled({
  label,
  options,
  value,
  onChange,
  disabled,
}: {
  label: string;
  options: readonly string[];
  value: string | null;
  onChange: (value: string) => void;
  disabled?: boolean;
}) {
  return (
    <div className="space-y-2">
      <span className="block text-sm font-semibold text-[#1A1A2E]">{label}</span>
      <div className="flex flex-wrap gap-2">
        {options.map((option) => {
          const active = value === option;
          return (
            <button
              key={option}
              type="button"
              disabled={disabled}
              onClick={() => onChange(option)}
              className={cn(
                "rounded-xl border-2 px-4 py-2.5 text-sm font-medium transition-all disabled:cursor-not-allowed disabled:opacity-60",
                active
                  ? "border-[#5B3FE0] bg-[#F3F0FE] text-[#4A2FC7]"
                  : "border-[#E5E5EE] bg-white text-[#3D3D4E] hover:border-[#5B3FE0]/40",
              )}
            >
              {option}
            </button>
          );
        })}
      </div>
    </div>
  );
}
