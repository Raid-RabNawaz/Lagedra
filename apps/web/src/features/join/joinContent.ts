import type { UserRole } from "@/app/auth/roles";
import { roles } from "@/app/auth/roles";

export type JoinVariant = "host" | "partner";

export type Benefit = { lead: string; rest: string };
export type Segment = { label: string; options: readonly string[] };

export type VariantContent = {
  /** Maps to the backend UserRole created for this door. */
  role: UserRole;
  /** Value stored in `signupType` for lead qualification. */
  signupType: "Host" | "Partner";
  badge: string;
  brandTitle: string;
  brandSubtitle: string;
  benefits: readonly Benefit[];
  formTitle: string;
  formSubtitle: string;
  companyPlaceholder: string;
  segments: readonly Segment[];
  consent: string;
};

export const foundingFootnote = "Launching 2026 · Founding-partner spots are limited.";

export const chooserContent = {
  title: "How do you want to join Lagedra?",
  subtitle: "Choose the option that fits you — we'll tailor the rest.",
  browsePrompt: "Looking for a place to stay instead?",
  browseCta: "Browse rentals",
  options: [
    {
      variant: "host" as const,
      icon: "Home" as const,
      title: "I'm a host",
      description:
        "List your furnished rentals — one property or fifty — and fill them with qualified 30+ day tenants.",
    },
    {
      variant: "partner" as const,
      icon: "Briefcase" as const,
      title: "I'm a partner institution",
      description: "A relocation or insurance company sourcing housing for your clients.",
    },
  ],
} as const;

export const variantContent: Record<JoinVariant, VariantContent> = {
  host: {
    role: roles.member,
    signupType: "Host",
    badge: "Partner Program",
    brandTitle: "List your furnished rentals for stays of 30 days and up",
    brandSubtitle:
      "Join the marketplace built for mid-term, fully-furnished living. We bring the long-stay demand — you keep your calendars full.",
    benefits: [
      { lead: "Qualified long-stay tenants", rest: " — fewer turnovers, steadier income" },
      { lead: "Free to list", rest: " — founding partners onboard free" },
      { lead: "One simple dashboard", rest: " — every property and booking in one place" },
    ],
    formTitle: "Tell us about your properties",
    formSubtitle: "A few quick details and we'll get you set up. Takes under a minute.",
    companyPlaceholder: "Rivera Stays",
    segments: [
      {
        label: "Roughly how many properties do you manage?",
        options: ["1–5", "6–20", "21–50", "50+"],
      },
    ],
    consent: "By joining you agree to be contacted about listing your properties. No spam, ever.",
  },
  partner: {
    role: roles.institutionPartner,
    signupType: "Partner",
    badge: "Partner Program",
    brandTitle: "Find verified housing for the people you place",
    brandSubtitle:
      "Join the marketplace built for relocation and insurance housing — vetted 30+ day homes, ready when your clients need them.",
    benefits: [
      {
        lead: "Verified, ready homes",
        rest: " — screened properties for corporate, insurance, and relocation stays",
      },
      {
        lead: "One place to source",
        rest: " — search and request housing across markets from a single platform",
      },
      { lead: "Move faster", rest: " — get your clients placed without the usual back-and-forth" },
    ],
    formTitle: "Tell us about your organization",
    formSubtitle: "A few quick details and we'll get you set up. Takes under a minute.",
    companyPlaceholder: "Rivera Relocation",
    segments: [
      {
        label: "What kind of housing do you source?",
        options: ["Relocation", "Insurance placements", "Both", "Other"],
      },
      {
        label: "Roughly how many placements per year?",
        options: ["1–25", "26–100", "101–500", "500+"],
      },
    ],
    consent:
      "By joining you agree to be contacted about sourcing housing for your clients. No spam, ever.",
  },
};

export const successContent = {
  badge: "You're in",
  title: "You're in. Welcome to Lagedra.",
  subtitle:
    "Thanks for joining our founding partners. We're putting the final pieces in place ahead of launch — and you're on the early list.",
  stepsHeading: "What happens next",
  steps: [
    {
      num: 1,
      title: "We'll reach out personally.",
      body: "Someone from our team will contact you soon to understand your housing needs and get you set up.",
    },
    {
      num: 2,
      title: "We'll connect you to inventory.",
      body: "We'll show you how to search and request verified homes for your clients across the markets you cover.",
    },
    {
      num: 3,
      title: "You place your clients faster.",
      body: "Vetted 30+ day housing, ready when you need it — without the usual scramble.",
    },
  ],
  footnote:
    "As a founding partner, you're first in line — and there's no cost to join. We'll be in touch shortly. In the meantime, keep an eye on your inbox.",
  cta: "Explore how it works",
} as const;

export const howItWorksContent = {
  badge: "How it works",
  title: "A faster, safer way to house your clients",
  subtitle:
    "Lagedra is built for mid-term housing — verified homes, clear terms, and protection built in — so you can place clients with confidence.",
  flowHeading: "From request to move-in",
  flow: [
    { num: 1, title: "Tell us what you need", body: "Location, dates, budget for your client." },
    { num: 2, title: "We surface verified homes", body: "Screened properties that fit the stay." },
    { num: 3, title: "Book with clear terms", body: "Lease, deposit, and paperwork handled up front." },
    {
      num: 4,
      title: "Protection built in",
      body: "Eviction protection and insurance options on qualified stays.",
    },
    { num: 5, title: "Your client moves in", body: "With everything documented from day one." },
  ],
  differentHeading: "Why Lagedra is different",
  differentBody:
    "Built for 30+ day housing — with verified inventory, protection, and clear terms up front. Not a short-term booking site bolted onto long stays, but a marketplace designed for mid-term living from the ground up.",
  differentPills: ["Built for 30+ day stays", "Verified inventory", "Protection & clear terms"],
  faqHeading: "Frequently asked questions",
  faq: [
    {
      q: "How much does it cost to use Lagedra?",
      a: "There's no cost to join, minimal fees, and we'll walk you through the specifics when we connect — nothing to figure out on your own.",
    },
    {
      q: "What kind of homes are on the platform?",
      a: "Fully-furnished homes built for mid-term stays of 30 days and up — apartments, houses, and units suited to corporate, insurance, and relocation placements.",
    },
    {
      q: "How are the properties verified?",
      a: "Every listing is screened before it reaches you, so the homes you surface for clients meet a consistent standard for quality and readiness.",
    },
    {
      q: "What areas do you cover?",
      a: "We're expanding market by market ahead of launch. Tell us where you place clients and we'll let you know our coverage in those areas.",
    },
    {
      q: "How does the insurance and protection work?",
      a: "Qualified stays can include eviction protection and insurance options, with clear terms handled up front so everyone is covered from day one.",
    },
    {
      q: "Can I source for multiple clients at once?",
      a: "Yes — search and request housing across markets from a single platform, and manage placements for as many clients as you need.",
    },
  ],
  preLaunchCta: "You're on the founding-partner list — we'll be in touch soon.",
} as const;
