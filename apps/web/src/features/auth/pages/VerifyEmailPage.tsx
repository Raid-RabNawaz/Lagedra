import { Link, useSearchParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { CheckCircle2, XCircle, Loader2 } from "lucide-react";
import { authApi } from "@/features/auth/services/authApi";
import { buttonVariants } from "@/components/ui/button-variants";
import { cn } from "@/lib/utils";

export const VerifyEmailPage = () => {
  const [params] = useSearchParams();
  const userId = params.get("userId");
  const token = params.get("token");

  // The verification call is keyed by the URL parameters, so react-query
  // dedupes accidental re-runs and gives us loading/error state for free.
  const verification = useQuery({
    queryKey: ["verify-email", userId, token],
    queryFn: () => authApi.verifyEmail(userId!, token!),
    enabled: Boolean(userId && token),
    retry: false,
    staleTime: Infinity,
  });

  const status: "loading" | "ok" | "error" =
    !userId || !token
      ? "error"
      : verification.isLoading
        ? "loading"
        : verification.isSuccess
          ? "ok"
          : "error";

  return (
    <div className="text-center">
      {status === "loading" && (
        <>
          <div className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-muted">
            <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
          </div>
          <h1 className="text-2xl font-bold tracking-tight">Verifying your email</h1>
          <p className="mt-2 text-muted-foreground">Please wait while we confirm your email address...</p>
        </>
      )}

      {status === "ok" && (
        <>
          <div className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-success/10">
            <CheckCircle2 className="h-8 w-8 text-success" />
          </div>
          <h1 className="text-2xl font-bold tracking-tight">Email verified</h1>
          <p className="mt-2 text-muted-foreground">
            Your email has been confirmed. You can now sign in to your account.
          </p>
          <Link
            to="/auth/login"
            className={cn(buttonVariants({ variant: "accent", size: "lg" }), "mt-6 w-full")}
          >
            Continue to sign in
          </Link>
        </>
      )}

      {status === "error" && (
        <>
          <div className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-destructive/10">
            <XCircle className="h-8 w-8 text-destructive" />
          </div>
          <h1 className="text-2xl font-bold tracking-tight">Verification failed</h1>
          <p className="mt-2 text-muted-foreground">
            This link may have expired or already been used. Please request a new verification email.
          </p>
          <Link
            to="/auth/login"
            className={cn(buttonVariants({ variant: "outline", size: "lg" }), "mt-6 w-full")}
          >
            Back to sign in
          </Link>
        </>
      )}
    </div>
  );
};
