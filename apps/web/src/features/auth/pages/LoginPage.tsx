import { useNavigate, useSearchParams, useLocation } from "react-router-dom";
import { usePublicConfigStore } from "@/app/config/publicConfigStore";
import { PRE_LAUNCH_HOST_HOME } from "@/app/auth/preLaunchAccess";
import { SignInForm } from "@/features/auth/components/SignInForm";

export const LoginPage = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const [searchParams] = useSearchParams();
  const preLaunchEnabled = usePublicConfigStore((s) => s.preLaunchEnabled);
  const defaultNext = preLaunchEnabled ? PRE_LAUNCH_HOST_HOME : "/app";
  const redirectParam = searchParams.get("redirect");
  const safeRedirect =
    redirectParam && redirectParam.startsWith("/") && !redirectParam.startsWith("//")
      ? redirectParam
      : null;
  const nextPath =
    (location.state as { from?: string } | null)?.from ?? safeRedirect ?? defaultNext;

  return (
    <SignInForm
      variant="page"
      onSuccess={() => {
        navigate(nextPath, { replace: true });
      }}
    />
  );
};
