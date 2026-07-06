import type { PublicUserProfileDto } from "@/api/types";

export function profileDisplayName(
  profile: PublicUserProfileDto | null | undefined,
  fallback = "Member",
): string {
  const heading =
    profile?.displayName
    ?? [profile?.firstName, profile?.lastName].filter(Boolean).join(" ").trim();
  return heading && heading.length > 0 ? heading : fallback;
}

export function profileInitials(name?: string | null, fallback = "U"): string {
  if (!name) return fallback;
  const parts = name.split(/\s+/).filter(Boolean).slice(0, 2);
  if (parts.length === 0) return fallback;
  return parts.map((p) => p[0]?.toUpperCase() ?? "").join("");
}

export function profileLocation(profile: {
  city?: string | null;
  state?: string | null;
  country?: string | null;
}): string | null {
  const parts = [profile.city, profile.state, profile.country]
    .map((p) => p?.trim())
    .filter((p): p is string => Boolean(p && p.length > 0));
  return parts.length > 0 ? parts.join(", ") : null;
}

export function profileVerificationCount(
  profile: PublicUserProfileDto | null | undefined,
): number {
  if (!profile) return 0;
  return [
    profile.isGovernmentIdVerified,
    profile.isPhoneVerified,
    profile.isEmailVerified,
  ].filter(Boolean).length;
}
