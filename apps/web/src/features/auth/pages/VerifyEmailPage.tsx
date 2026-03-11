import { useEffect, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { CheckCircle2, XCircle, Loader2 } from "lucide-react";
import { authApi } from "@/features/auth/services/authApi";
import { buttonVariants } from "@/components/ui/button";
import { cn } from "@/lib/utils";

export const VerifyEmailPage = () => {
  const [params] = useSearchParams();
  const [status, setStatus] = useState<"loading" | "ok" | "error">("loading");

  useEffect(() => {
    const userId = params.get("userId");
    const token = params.get("token");

    if (!userId || !token) {
      setStatus("error");
      return;
    }

    authApi
      .verifyEmail(userId, token)
      .then(() => setStatus("ok"))
      .catch(() => setStatus("error"));
  }, [params]);

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
