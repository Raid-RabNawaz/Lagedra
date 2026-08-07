import { useId, useState } from "react";
import { Link } from "react-router-dom";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { zodResolver } from "@hookform/resolvers/zod";
import { Mail, Lock, ArrowRight, Eye, EyeOff } from "lucide-react";
import { authApi } from "@/features/auth/services/authApi";
import { useAuthStore } from "@/app/auth/authStore";
import { usePublicConfigStore } from "@/app/config/publicConfigStore";
import { getApiErrorMessage } from "@/api/errors";
import { appConfig } from "@/app/config";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Separator } from "@/components/ui/separator";
import { FormError } from "@/components/shared/FormError";
import { GoogleSignInButton } from "@/components/shared/GoogleSignInButton";

const schema = z.object({
  email: z.string().email("Enter a valid email address"),
  password: z.string().min(8, "Password must be at least 8 characters"),
});

type FormData = z.infer<typeof schema>;

type SignInFormProps = {
  /** Called after tokens + current user are loaded into the auth store. */
  onSuccess: () => void | Promise<void>;
  /** Compact copy for dialogs; full-page login keeps the longer welcome text. */
  variant?: "page" | "dialog";
  /** Extra classes on the outer wrapper. */
  className?: string;
};

/**
 * Shared email/password (+ optional Google) sign-in form used by the login
 * page and the in-place SignInDialog so guests can authenticate without
 * leaving the listing they were viewing.
 */
export function SignInForm({
  onSuccess,
  variant = "page",
  className,
}: SignInFormProps) {
  const id = useId();
  const emailId = `${id}-email`;
  const passwordId = `${id}-password`;

  const [serverError, setServerError] = useState<string | null>(null);
  const [googleLoading, setGoogleLoading] = useState(false);
  const [showPassword, setShowPassword] = useState(false);
  const preLaunchEnabled = usePublicConfigStore((s) => s.preLaunchEnabled);
  const setUser = useAuthStore((state) => state.setUser);

  const form = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: { email: "", password: "" },
  });

  const completeLogin = async () => {
    const me = await authApi.getCurrentUser();
    setUser(me);
    await onSuccess();
  };

  const onSubmit = form.handleSubmit(async (data) => {
    setServerError(null);
    try {
      await authApi.login(data);
      await completeLogin();
    } catch (error) {
      setServerError(
        getApiErrorMessage(
          error,
          "Login failed. Check your credentials and try again.",
        ),
      );
    }
  });

  const handleGoogleLogin = async (idToken: string) => {
    setServerError(null);
    setGoogleLoading(true);
    try {
      await authApi.externalLogin({ provider: "Google", idToken });
      await completeLogin();
    } catch (error) {
      setServerError(
        getApiErrorMessage(error, "Google sign-in failed. Please try again."),
      );
    } finally {
      setGoogleLoading(false);
    }
  };

  const isSubmitting = form.formState.isSubmitting || googleLoading;
  const showGoogle = Boolean(appConfig.googleClientId) && !preLaunchEnabled;

  return (
    <div className={className}>
      {variant === "page" && (
        <div className="mb-8">
          <h1 className="text-3xl font-bold tracking-tight">Welcome back</h1>
          <p className="mt-2 text-muted-foreground">
            {preLaunchEnabled
              ? "Sign in to add listings and import from Hostaway."
              : "Sign in to your Lagedra account to continue."}
          </p>
        </div>
      )}

      {showGoogle && (
        <>
          <GoogleSignInButton
            onSuccess={handleGoogleLogin}
            onError={() => setServerError("Google sign-in failed.")}
            text="signin_with"
          />

          <div className="relative my-6">
            <Separator />
            <span className="absolute left-1/2 top-1/2 -translate-x-1/2 -translate-y-1/2 bg-background px-3 text-xs text-muted-foreground">
              or
            </span>
          </div>
        </>
      )}

      <form onSubmit={onSubmit} className="space-y-4">
        <div className="space-y-2">
          <Label htmlFor={emailId}>Email</Label>
          <div className="relative">
            <Mail className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
            <Input
              id={emailId}
              type="email"
              autoComplete="email"
              placeholder="you@example.com"
              className="pl-10"
              disabled={isSubmitting}
              {...form.register("email")}
            />
          </div>
          <FormError message={form.formState.errors.email?.message} />
        </div>

        <div className="space-y-2">
          <div className="flex items-center justify-between">
            <Label htmlFor={passwordId}>Password</Label>
            <Link
              to="/auth/forgot-password"
              className="text-xs text-muted-foreground hover:text-foreground transition-colors"
              onClick={(e) => e.stopPropagation()}
            >
              Forgot password?
            </Link>
          </div>
          <div className="relative">
            <Lock className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
            <Input
              id={passwordId}
              type={showPassword ? "text" : "password"}
              autoComplete="current-password"
              placeholder="Enter your password"
              className="pl-10 pr-10"
              disabled={isSubmitting}
              {...form.register("password")}
            />
            <button
              type="button"
              onClick={() => setShowPassword((v) => !v)}
              className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground cursor-pointer"
              aria-label={showPassword ? "Hide password" : "Show password"}
            >
              {showPassword ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
            </button>
          </div>
          <FormError message={form.formState.errors.password?.message} />
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
            "Signing in..."
          ) : (
            <>
              Sign in
              <ArrowRight className="h-4 w-4" />
            </>
          )}
        </Button>
      </form>

      <Separator className="my-6" />

      <p className="text-center text-sm text-muted-foreground">
        Don&apos;t have an account?{" "}
        <Link
          to="/join"
          className="font-medium text-foreground hover:underline"
          onClick={(e) => e.stopPropagation()}
        >
          Sign up
        </Link>
      </p>
    </div>
  );
}
