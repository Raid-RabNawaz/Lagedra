import logoSvg from "@/assets/logo.svg";
import { cn } from "@/lib/utils";

type JoinLogoProps = {
  /** "brand" = full-color logo on a light bg (matches the rest of the app),
   *  "onDark" = same logo tinted white for the purple brand panel. */
  tone?: "brand" | "onDark";
  className?: string;
};

export const JoinLogo = ({ tone = "brand", className }: JoinLogoProps) => (
  <img
    src={logoSvg}
    alt="Lagedra"
    className={cn(
      "h-7 w-auto",
      // Same asset, just tinted white so it stays legible on the purple panel.
      tone === "onDark" && "brightness-0 invert",
      className,
    )}
  />
);
