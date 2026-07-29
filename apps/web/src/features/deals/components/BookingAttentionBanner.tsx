import { Link } from "react-router-dom";
import { AlertTriangle, Clock, ArrowRight, MessageCircle } from "lucide-react";
import { buttonVariants } from "@/components/ui/button-variants";
import { cn } from "@/lib/utils";
import type { BookingIssue, EndingSoonInfo, ListingIssue } from "@/features/deals/utils/bookingAttention";
import { isCriticalDealIssue } from "@/features/deals/utils/bookingAttention";

type Tone = "critical" | "ending";

const toneStyles: Record<
  Tone,
  { wrap: string; iconWrap: string; title: string; body: string; primaryBtn: string; outlineBtn: string }
> = {
  critical: {
    wrap: "border-destructive/40 bg-destructive/5",
    iconWrap: "bg-destructive text-destructive-foreground",
    title: "text-destructive",
    body: "text-destructive/90",
    primaryBtn: "",
    outlineBtn:
      "border-destructive/40 text-destructive hover:bg-destructive/10 hover:text-destructive",
  },
  ending: {
    wrap: "border-amber-300 bg-amber-50",
    iconWrap: "bg-amber-500 text-white",
    title: "text-amber-950",
    body: "text-amber-900/90",
    primaryBtn: "bg-amber-600 text-white hover:bg-amber-700",
    outlineBtn:
      "border-amber-400/60 text-amber-950 hover:bg-amber-100 hover:text-amber-950",
  },
};

function AttentionShell({
  tone,
  title,
  children,
  actions,
  className,
}: {
  tone: Tone;
  title: string;
  children: React.ReactNode;
  actions?: React.ReactNode;
  className?: string;
}) {
  const s = toneStyles[tone];
  const Icon = tone === "critical" ? AlertTriangle : Clock;

  return (
    <div
      role="alert"
      className={cn(
        "flex flex-col gap-3 rounded-xl border p-4 sm:flex-row sm:items-center sm:justify-between",
        s.wrap,
        className,
      )}
    >
      <div className="flex min-w-0 items-start gap-3">
        <span
          className={cn(
            "flex h-10 w-10 shrink-0 items-center justify-center rounded-full",
            s.iconWrap,
          )}
        >
          <Icon className="h-5 w-5" />
        </span>
        <div className="min-w-0 space-y-1">
          <p className={cn("font-semibold leading-snug", s.title)}>{title}</p>
          <div className={cn("text-sm leading-relaxed", s.body)}>{children}</div>
        </div>
      </div>
      {actions && (
        <div className="flex shrink-0 flex-wrap items-center gap-2 sm:justify-end">
          {actions}
        </div>
      )}
    </div>
  );
}

export function DealIssueBanner({
  issue,
  showContactGuest,
  className,
}: {
  issue: BookingIssue;
  showContactGuest?: boolean;
  className?: string;
}) {
  const critical = isCriticalDealIssue(issue.kind);
  const tone: Tone = critical ? "critical" : "ending";
  const s = toneStyles[tone];

  return (
    <AttentionShell
      tone={tone}
      title={issue.title}
      className={className}
      actions={
        <>
          <Link
            to={issue.href}
            className={cn(
              buttonVariants({
                size: "sm",
                variant: critical ? "destructive" : "default",
              }),
              "gap-1.5",
              !critical && s.primaryBtn,
            )}
          >
            {issue.ctaLabel}
            <ArrowRight className="h-3.5 w-3.5" />
          </Link>
          {showContactGuest && (
            <Link
              to={issue.href}
              className={cn(buttonVariants({ size: "sm", variant: "outline" }), s.outlineBtn)}
            >
              <MessageCircle className="mr-1.5 h-3.5 w-3.5" />
              Contact guest
            </Link>
          )}
        </>
      }
    >
      <p>{issue.problem}</p>
      <p className="mt-1 opacity-90">
        <span className="font-medium">How to resolve: </span>
        {issue.resolution}
      </p>
    </AttentionShell>
  );
}

export function EndingSoonBanner({
  listingTitle,
  ending,
  href,
  className,
}: {
  listingTitle: string;
  ending: EndingSoonInfo;
  href: string;
  className?: string;
}) {
  const s = toneStyles.ending;
  return (
    <AttentionShell
      tone="ending"
      title={`${ending.label} — ${listingTitle}`}
      className={className}
      actions={
        <Link
          to={href}
          className={cn(buttonVariants({ size: "sm" }), "gap-1.5", s.primaryBtn)}
        >
          View booking
          <ArrowRight className="h-3.5 w-3.5" />
        </Link>
      }
    >
      <p>
        This active stay is wrapping up soon. Review move-out details and prepare
        the deposit return when the guest leaves.
      </p>
    </AttentionShell>
  );
}

export function ListingIssueBanner({
  issue,
  className,
}: {
  issue: ListingIssue;
  className?: string;
}) {
  return (
    <AttentionShell
      tone="critical"
      title={issue.title}
      className={className}
      actions={
        <Link
          to={issue.href}
          className={cn(buttonVariants({ size: "sm", variant: "destructive" }), "gap-1.5")}
        >
          Fix listing
          <ArrowRight className="h-3.5 w-3.5" />
        </Link>
      }
    >
      <p>{issue.problem}</p>
      <p className="mt-1 opacity-90">
        <span className="font-medium">How to resolve: </span>
        {issue.resolution}
      </p>
    </AttentionShell>
  );
}

export function EndingSoonBadge({
  ending,
  className,
}: {
  ending: EndingSoonInfo;
  className?: string;
}) {
  return (
    <span
      className={cn(
        "inline-flex items-center gap-1 rounded-full border border-amber-300 bg-amber-50 px-2 py-0.5 text-[11px] font-medium text-amber-950",
        className,
      )}
    >
      <Clock className="h-3 w-3" />
      {ending.label}
    </span>
  );
}
