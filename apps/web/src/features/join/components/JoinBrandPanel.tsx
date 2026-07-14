import { Check } from "lucide-react";
import { JoinLogo } from "./JoinLogo";
import { foundingFootnote, type VariantContent } from "../joinContent";

type JoinBrandPanelProps = {
  content: VariantContent;
  showFounding: boolean;
};

export const JoinBrandPanel = ({ content, showFounding }: JoinBrandPanelProps) => (
  <div className="relative flex flex-col justify-between overflow-hidden bg-[#5B3FE0] px-8 py-10 text-white lg:px-12 lg:py-14">
    {/* soft radial highlight, matching the source design */}
    <div
      aria-hidden
      className="pointer-events-none absolute -right-24 -top-24 h-80 w-80 rounded-full bg-white/10 blur-2xl"
    />

    <div className="relative">
      <JoinLogo tone="onDark" />

      <span className="mt-8 inline-flex items-center gap-2 rounded-full bg-white/15 px-4 py-2 text-sm font-semibold">
        <span className="h-1.5 w-1.5 rounded-full bg-white/80" />
        {content.badge}
        {showFounding ? " · Pre-launch" : ""}
      </span>

      <h1 className="mt-6 text-4xl font-extrabold leading-[1.1] tracking-tight lg:text-5xl">
        {content.brandTitle}
      </h1>

      <p className="mt-5 max-w-md text-base leading-relaxed text-white/85">
        {content.brandSubtitle}
      </p>

      <ul className="mt-8 space-y-3">
        {content.benefits.map((benefit) => (
          <li key={benefit.lead} className="flex items-start gap-3">
            <span className="mt-0.5 flex h-5 w-5 shrink-0 items-center justify-center rounded-full bg-white/20">
              <Check className="h-3 w-3" strokeWidth={3} />
            </span>
            <span className="text-sm text-white/90">
              <span className="font-semibold text-white">{benefit.lead}</span>
              {benefit.rest}
            </span>
          </li>
        ))}
      </ul>
    </div>

    {showFounding ? (
      <p className="relative mt-10 text-sm font-medium text-white/70">{foundingFootnote}</p>
    ) : (
      <div className="relative mt-10" />
    )}
  </div>
);
