import { useState } from "react";
import { useForm } from "react-hook-form";
import { Link, useSearchParams } from "react-router-dom";
import { z } from "zod";
import { zodResolver } from "@hookform/resolvers/zod";
import { Lock, CheckCircle2, ArrowRight } from "lucide-react";
import { authApi } from "@/features/auth/services/authApi";
import { Button, buttonVariants } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Separator } from "@/components/ui/separator";
import { FormError } from "@/components/shared/FormError";

const schema = z
  .object({
    password: z
      .string()
      .min(8, "Password must be at least 8 characters")
      .regex(/[A-Z]/, "Include at least one uppercase letter")
      .regex(/[0-9]/, "Include at least one number"),
    confirmPassword: z.string().min(1, "Confirm your password"),
  })
  .refine((data) => data.password === data.confirmPassword, {
    path: ["confirmPassword"],
    message: "Passwords must match",
  });

type FormData = z.infer<typeof schema>;

export const ResetPasswordPage = () => {
  const [params] = useSearchParams();
  const [message, setMessage] = useState<string | null>(null);
  const [serverError, setServerError] = useState<string | null>(null);
  const userId = params.get("userId");
  const token = params.get("token");

  const form = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: { password: "", confirmPassword: "" },
  });

  const onSubmit = form.handleSubmit(async (data) => {
    if (!userId || !token) {
      setServerError("Invalid reset link. Please request a new password reset email.");
      return;
    }

    setMessage(null);
    setServerError(null);

    try {
      const response = await authApi.resetPassword({
        userId,
        token,
        newPassword: data.password,
      });
      setMessage(response.message);
      form.reset();
    } catch {
      setServerError("Could not reset password. Your link may be expired.");
    }
  });

  if (message) {
    return (
      <div className="text-center">
        <div className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-success/10">
          <CheckCircle2 className="h-8 w-8 text-success" />
        </div>
        <h1 className="text-2xl font-bold tracking-tight">Password updated</h1>
        <p className="mt-2 text-muted-foreground">{message}</p>
        <Link
          to="/auth/login"
          className={cn(buttonVariants({ variant: "accent", size: "lg" }), "mt-6 w-full")}
        >
          Sign in with new password
          <ArrowRight className="h-4 w-4" />
        </Link>
      </div>
    );
  }

  return (
    <div>
      <div className="mb-8">
        <h1 className="text-3xl font-bold tracking-tight">Set new password</h1>
        <p className="mt-2 text-muted-foreground">
          Choose a strong password for your account.
        </p>
      </div>

      <form onSubmit={onSubmit} className="space-y-4">
        <div className="space-y-2">
          <Label htmlFor="password">New password</Label>
          <div className="relative">
            <Lock className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
            <Input
              id="password"
              type="password"
              placeholder="Enter new password"
              className="pl-10"
              {...form.register("password")}
            />
          </div>
          <FormError message={form.formState.errors.password?.message} />
        </div>

        <div className="space-y-2">
          <Label htmlFor="confirmPassword">Confirm password</Label>
          <div className="relative">
            <Lock className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
            <Input
              id="confirmPassword"
              type="password"
              placeholder="Confirm new password"
              className="pl-10"
              {...form.register("confirmPassword")}
            />
          </div>
          <FormError message={form.formState.errors.confirmPassword?.message} />
        </div>

        <FormError message={serverError} />

        <Button
          type="submit"
          variant="accent"
          size="lg"
          className="w-full"
          disabled={form.formState.isSubmitting}
        >
          {form.formState.isSubmitting ? "Updating..." : "Reset password"}
        </Button>
      </form>

      <Separator className="my-8" />

      <p className="text-center text-sm text-muted-foreground">
        Remember your password?{" "}
        <Link to="/auth/login" className="font-medium text-foreground hover:underline">
          Sign in
        </Link>
      </p>
    </div>
  );
};
