import { useEffect, useState } from "react";
import { useAuthStore } from "@/app/auth/authStore";
import { usePublicConfigStore } from "@/app/config/publicConfigStore";
import { RoleChooser } from "../components/RoleChooser";
import { JoinBrandPanel } from "../components/JoinBrandPanel";
import { SignupForm, type SignupSuccessInfo } from "../components/SignupForm";
import { SignupSuccess } from "../components/SignupSuccess";
import { variantContent, type JoinVariant } from "../joinContent";

type Screen =
  | { name: "chooser" }
  | { name: "form"; variant: JoinVariant }
  | { name: "success"; info: SignupSuccessInfo };

export const JoinPage = () => {
  const preLaunchEnabled = usePublicConfigStore((s) => s.preLaunchEnabled);
  const isLoggedIn = Boolean(useAuthStore((s) => s.accessToken));
  const [screen, setScreen] = useState<Screen>({ name: "chooser" });

  // Keep the flow at the top of the viewport as steps change.
  useEffect(() => {
    window.scrollTo(0, 0);
  }, [screen.name]);

  if (screen.name === "chooser") {
    return (
      <RoleChooser
        onChoose={(variant) => setScreen({ name: "form", variant })}
        showBrowse={!preLaunchEnabled}
      />
    );
  }

  if (screen.name === "success") {
    return (
      <SignupSuccess info={screen.info} onRestart={() => setScreen({ name: "chooser" })} />
    );
  }

  const content = variantContent[screen.variant];

  return (
    <div className="grid min-h-screen grid-cols-1 lg:grid-cols-2">
      <JoinBrandPanel content={content} showFounding={preLaunchEnabled} />
      <div className="flex items-start justify-center bg-white">
        <SignupForm
          variant={screen.variant}
          preLaunch={preLaunchEnabled && !isLoggedIn}
          onBack={() => setScreen({ name: "chooser" })}
          onSuccess={(info) => setScreen({ name: "success", info })}
        />
      </div>
    </div>
  );
};
