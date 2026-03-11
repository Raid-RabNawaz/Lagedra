import { useState } from "react";
import { useForm } from "react-hook-form";
import { Link } from "react-router-dom";
import { z } from "zod";
import { zodResolver } from "@hookform/resolvers/zod";
import { Mail, ArrowLeft, CheckCircle2 } from "lucide-react";
import { authApi } from "@/features/auth/services/authApi";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Separator } from "@/components/ui/separator";
import { FormError } from "@/components/shared/FormError";

const schema = z.object({
  email: z.string().email("Enter a valid email address"),
});

type FormData = z.infer<typeof schema>;

export const ForgotPasswordPage = () => {
  const [message, setMessage] = useState<string | null>(null);
  const [serverError, setServerError] = useState<string | null>(null);

  const form = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: { email: "" },
  });

  const onSubmit = form.handleSubmit(async (data) => {
    setMessage(null);
    setServerError(null);
    try {
      const response = await authApi.forgotPassword(data);
      setMessage(response.message);
    } catch {
      setServerError("Unable to send reset instructions right now.");
    }
  });

  if (message) {
    return (
      <div className="text-center">
        <div className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-success/10">
          <CheckCircle2 className="h-8 w-8 text-success" />
        </div>
        <h1 className="text-2xl font-bold tracking-tight">Check your email</h1>
        <p className="mt-2 text-muted-foreground">{message}</p>
        <Separator className="my-6" />
        <Link
          to="/auth/login"
          className="inline-flex items-center gap-2 text-sm font-medium text-foreground hover:underline"
        >
          <ArrowLeft className="h-4 w-4" />
          Back to sign in
        </Link>
      </div>
    );
  }

  return (
    <div>
      <Link
        to="/auth/login"
        className="mb-6 inline-flex items-center gap-2 text-sm text-muted-foreground hover:text-foreground transition-colors"
      >
        <ArrowLeft className="h-4 w-4" />
        Back to sign in
      </Link>

      <div className="mb-8">
        <h1 className="text-3xl font-bold tracking-tight">Reset your password</h1>
        <p className="mt-2 text-muted-foreground">
          Enter the email address associated with your account and we&apos;ll send you a link to reset your password.
        </p>
      </div>

      <form onSubmit={onSubmit} className="space-y-4">
        <div className="space-y-2">
          <Label htmlFor="email">Email</Label>
          <div className="relative">
            <Mail className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
            <Input
              id="email"
              type="email"
              placeholder="you@example.com"
              className="pl-10"
              {...form.register("email")}
            />
          </div>
          <FormError message={form.formState.errors.email?.message} />
        </div>

        <FormError message={serverError} />

        <Button
          type="submit"
          variant="accent"
          size="lg"
          className="w-full"
          disabled={form.formState.isSubmitting}
        >
          {form.formState.isSubmitting ? "Sending..." : "Send reset link"}
        </Button>
      </form>
    </div>
  );
};
