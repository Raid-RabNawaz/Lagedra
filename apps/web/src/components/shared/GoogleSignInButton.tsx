import { useEffect, useRef, useState } from "react";
import { GoogleLogin, type CredentialResponse } from "@react-oauth/google";
import { cn } from "@/lib/utils";

type GoogleButtonText = "signin_with" | "signup_with" | "continue_with";

type GoogleSignInButtonProps = {
  onSuccess: (idToken: string) => void;
  onError?: () => void;
  text?: GoogleButtonText;
  className?: string;
};

/** Google's rendered button is capped at 400px and refuses anything under 200. */
function buttonWidth(containerWidth: number): number {
  return Math.max(200, Math.min(400, Math.floor(containerWidth)));
}

export function GoogleSignInButton({
  onSuccess,
  onError,
  text = "continue_with",
  className,
}: GoogleSignInButtonProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const [width, setWidth] = useState(0);

  useEffect(() => {
    const el = containerRef.current;
    if (!el) return;

    const update = () => {
      const next = buttonWidth(el.getBoundingClientRect().width);
      setWidth((current) => (current === next ? current : next));
    };

    update();
    const observer = new ResizeObserver(update);
    observer.observe(el);
    return () => observer.disconnect();
  }, []);

  const handleSuccess = (response: CredentialResponse) => {
    if (response.credential) {
      onSuccess(response.credential);
    }
  };

  return (
    <div ref={containerRef} className={cn("flex w-full justify-center", className)}>
      {/* The iframe has to stay visible. Chrome does not deliver the click when
          an ancestor is fully transparent, so a styled label over an invisible
          Google button never opens the account chooser. */}
      {width > 0 && (
        <GoogleLogin
          key={width}
          onSuccess={handleSuccess}
          onError={onError}
          text={text}
          shape="pill"
          size="large"
          width={String(width)}
          theme="outline"
          logo_alignment="center"
        />
      )}
    </div>
  );
}
