import { GoogleLogin, type CredentialResponse } from "@react-oauth/google";

type GoogleSignInButtonProps = {
  onSuccess: (idToken: string) => void;
  onError?: () => void;
  text?: "signin_with" | "signup_with" | "continue_with";
};

export function GoogleSignInButton({
  onSuccess,
  onError,
  text = "continue_with",
}: GoogleSignInButtonProps) {
  const handleSuccess = (response: CredentialResponse) => {
    if (response.credential) {
      onSuccess(response.credential);
    }
  };

  return (
    <div className="flex justify-center [&>div]:w-full">
      <GoogleLogin
        onSuccess={handleSuccess}
        onError={onError}
        text={text}
        shape="rectangular"
        size="large"
        width="400"
        theme="outline"
      />
    </div>
  );
}
