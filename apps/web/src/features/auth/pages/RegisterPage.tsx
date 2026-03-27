import { useState } from "react";
import { useForm } from "react-hook-form";
import { Link, useNavigate } from "react-router-dom";
import { z } from "zod";
import { zodResolver } from "@hookform/resolvers/zod";
import { Mail, Lock, ArrowRight, CheckCircle2, Home, Building2, Briefcase } from "lucide-react";
import { authApi } from "@/features/auth/services/authApi";
import { useAuthStore } from "@/app/auth/authStore";
import { appConfig } from "@/app/config";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Separator } from "@/components/ui/separator";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { FormError } from "@/components/shared/FormError";
import { GoogleSignInButton } from "@/components/shared/GoogleSignInButton";
import { cn } from "@/lib/utils";
import type { UserRole } from "@/app/auth/roles";

const schema = z.object({
  email: z.string().email("Enter a valid email address"),
  password: z
    .string()
    .min(8, "Password must be at least 8 characters")
    .regex(/[A-Z]/, "Include at least one uppercase letter")
    .regex(/[0-9]/, "Include at least one number"),
  role: z.enum(["Tenant", "Landlord", "InstitutionPartner"]),
});

type FormData = z.infer<typeof schema>;

const roleOptions = [
  {
    value: "Tenant" as const,
    label: "I'm a tenant",
    description: "Looking for a mid-term rental",
    icon: Home,
  },
  {
    value: "Landlord" as const,
    label: "I'm a landlord",
    description: "Listing a property for rent",
    icon: Building2,
  },
  {
    value: "InstitutionPartner" as const,
    label: "Institution partner",
    description: "Company managing relocations",
    icon: Briefcase,
  },
];

export const RegisterPage = () => {
  const [resultMessage, setResultMessage] = useState<string | null>(null);
  const [verificationHint, setVerificationHint] = useState<string | null>(null);
  const [serverError, setServerError] = useState<string | null>(null);
  const [googleLoading, setGoogleLoading] = useState(false);
  const setUser = useAuthStore((state) => state.setUser);
  const navigate = useNavigate();

  const form = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: { email: "", password: "", role: "Tenant" },
  });

  const selectedRole = form.watch("role");

  const onSubmit = form.handleSubmit(async (data) => {
    setServerError(null);
    setResultMessage(null);
    setVerificationHint(null);

    try {
      const result = await authApi.register(data);
      setResultMessage(result.message ?? "Registration submitted. Check your email to verify your account.");
      if (result.dev_verificationUrl) {
        setVerificationHint(result.dev_verificationUrl);
      }
      form.reset({ email: "", password: "", role: "Tenant" });
    } catch {
      setServerError("Registration failed. Try a different email address.");
    }
  });

  const handleGoogleSignUp = async (idToken: string) => {
    setServerError(null);
    setGoogleLoading(true);
    try {
      await authApi.externalLogin({
        provider: "Google",
        idToken,
        preferredRole: selectedRole as UserRole,
      });
      const me = await authApi.getCurrentUser();
      setUser(me);
      navigate("/app", { replace: true });
    } catch {
      setServerError("Google sign-up failed. Please try again.");
    } finally {
      setGoogleLoading(false);
    }
  };

  const isSubmitting = form.formState.isSubmitting || googleLoading;

  if (resultMessage) {
    return (
      <div className="text-center">
        <div className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-success/10">
          <CheckCircle2 className="h-8 w-8 text-success" />
        </div>
        <h1 className="text-2xl font-bold tracking-tight">Check your email</h1>
        <p className="mt-2 text-muted-foreground">{resultMessage}</p>

        {verificationHint && (
          <Alert className="mt-6 text-left" variant="default">
            <AlertDescription>
              <span className="font-medium">Dev verification URL:</span>{" "}
              <code className="text-xs break-all">{verificationHint}</code>
            </AlertDescription>
          </Alert>
        )}

        <Separator className="my-6" />
        <p className="text-sm text-muted-foreground">
          Already verified?{" "}
          <Link to="/auth/login" className="font-medium text-foreground hover:underline">
            Sign in
          </Link>
        </p>
      </div>
    );
  }

  return (
    <div>
      <div className="mb-8">
        <h1 className="text-3xl font-bold tracking-tight">Create your account</h1>
        <p className="mt-2 text-muted-foreground">
          Join Lagedra to access verified mid-term rentals.
        </p>
      </div>

      <form onSubmit={onSubmit} className="space-y-5">
        <div className="space-y-3">
          <Label>I want to join as</Label>
          <div className="grid grid-cols-3 gap-2">
            {roleOptions.map((option) => (
              <button
                key={option.value}
                type="button"
                onClick={() => form.setValue("role", option.value)}
                disabled={isSubmitting}
                className={cn(
                  "flex flex-col items-center gap-1.5 rounded-xl border-2 p-3 text-center transition-all cursor-pointer",
                  selectedRole === option.value
                    ? "border-foreground bg-secondary"
                    : "border-border hover:border-foreground/30",
                )}
              >
                <option.icon
                  className={cn(
                    "h-5 w-5",
                    selectedRole === option.value ? "text-foreground" : "text-muted-foreground",
                  )}
                />
                <div>
                  <p className="text-xs font-medium leading-tight">{option.label}</p>
                  <p className="text-[10px] text-muted-foreground leading-tight mt-0.5">{option.description}</p>
                </div>
              </button>
            ))}
          </div>
        </div>

        {appConfig.googleClientId && (
          <>
            <GoogleSignInButton
              onSuccess={handleGoogleSignUp}
              onError={() => setServerError("Google sign-up failed.")}
              text="signup_with"
            />

            <div className="relative">
              <Separator />
              <span className="absolute left-1/2 top-1/2 -translate-x-1/2 -translate-y-1/2 bg-background px-3 text-xs text-muted-foreground">
                or sign up with email
              </span>
            </div>
          </>
        )}

        <div className="space-y-2">
          <Label htmlFor="email">Email</Label>
          <div className="relative">
            <Mail className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
            <Input
              id="email"
              type="email"
              placeholder="you@example.com"
              className="pl-10"
              disabled={isSubmitting}
              {...form.register("email")}
            />
          </div>
          <FormError message={form.formState.errors.email?.message} />
        </div>

        <div className="space-y-2">
          <Label htmlFor="password">Password</Label>
          <div className="relative">
            <Lock className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
            <Input
              id="password"
              type="password"
              placeholder="Create a strong password"
              className="pl-10"
              disabled={isSubmitting}
              {...form.register("password")}
            />
          </div>
          <FormError message={form.formState.errors.password?.message} />
          <p className="text-xs text-muted-foreground">
            At least 8 characters with one uppercase letter and one number.
          </p>
        </div>

        <FormError message={serverError} />

        <Button
          type="submit"
          variant="accent"
          size="lg"
          className="w-full"
          disabled={isSubmitting}
        >
          {form.formState.isSubmitting ? (
            "Creating account..."
          ) : (
            <>
              Create account
              <ArrowRight className="h-4 w-4" />
            </>
          )}
        </Button>

        <p className="text-center text-xs text-muted-foreground">
          By signing up, you agree to our Terms of Service and Privacy Policy.
        </p>
      </form>

      <Separator className="my-8" />

      <p className="text-center text-sm text-muted-foreground">
        Already have an account?{" "}
        <Link to="/auth/login" className="font-medium text-foreground hover:underline">
          Sign in
        </Link>
      </p>
    </div>
  );
};
