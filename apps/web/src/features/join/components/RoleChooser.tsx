import { Link } from "react-router-dom";
import { ArrowRight, Briefcase, Home } from "lucide-react";
import { JoinLogo } from "./JoinLogo";
import { chooserContent, type JoinVariant } from "../joinContent";

const icons = { Home, Briefcase } as const;

type RoleChooserProps = {
  onChoose: (variant: JoinVariant) => void;
  /** Show the "browse rentals" escape hatch. Hidden while pre-launch is on
   *  (public browsing is closed, so the link would just bounce back here). */
  showBrowse?: boolean;
};

export const RoleChooser = ({ onChoose, showBrowse = true }: RoleChooserProps) => (
  <div className="mx-auto flex min-h-screen w-full max-w-3xl flex-col items-center px-6 py-12 sm:py-16">
    <JoinLogo />

    <h1 className="mt-12 text-center text-4xl font-extrabold leading-tight tracking-tight text-[#1A1A2E] sm:text-5xl">
      {chooserContent.title}
    </h1>
    <p className="mt-4 text-center text-lg text-[#3D3D4E]">{chooserContent.subtitle}</p>

    <div className="mt-10 grid w-full grid-cols-1 gap-4 sm:grid-cols-2">
      {chooserContent.options.map((option) => {
        const Icon = icons[option.icon];
        return (
          <button
            key={option.variant}
            type="button"
            onClick={() => onChoose(option.variant)}
            className="group flex h-full flex-col rounded-2xl border border-[#E5E5EE] bg-white p-6 text-left transition-all hover:-translate-y-0.5 hover:border-[#5B3FE0] hover:shadow-lg hover:shadow-[#5B3FE0]/10"
          >
            <span className="flex h-12 w-12 items-center justify-center rounded-xl bg-[#F3F0FE] text-[#5B3FE0]">
              <Icon className="h-6 w-6" />
            </span>
            <h2 className="mt-5 text-xl font-bold text-[#1A1A2E]">{option.title}</h2>
            <p className="mt-2 flex-1 text-sm leading-relaxed text-[#3D3D4E]">{option.description}</p>
            <span className="mt-5 inline-flex items-center gap-1.5 text-sm font-semibold text-[#5B3FE0]">
              Get started
              <ArrowRight className="h-4 w-4 transition-transform group-hover:translate-x-0.5" />
            </span>
          </button>
        );
      })}
    </div>

    {showBrowse && (
      <p className="mt-10 text-center text-sm text-[#3D3D4E]">
        {chooserContent.browsePrompt}{" "}
        <Link to="/listings" className="font-semibold text-[#5B3FE0] hover:underline">
          {chooserContent.browseCta} &rarr;
        </Link>
      </p>
    )}
  </div>
);
