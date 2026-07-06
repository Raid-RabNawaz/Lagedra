import type { UserProfileDto } from "@/api/types";

/**
 * Minimum profile completeness (percent) a host must reach before a listing
 * can be submitted for review. Kept in lockstep with the server-side gate in
 * SubmitListingForReviewCommandHandler.MinimumProfileCompletenessPercent.
 */
export const MIN_HOST_PROFILE_COMPLETENESS = 75;

export type ProfileCompleteness = {
  /** 0–100, rounded. */
  percent: number;
  /** Human-readable labels for the still-empty fields. */
  missing: string[];
  /** True once the host clears {@link MIN_HOST_PROFILE_COMPLETENESS}. */
  meetsListingThreshold: boolean;
};

type ProfileFields = Pick<
  UserProfileDto,
  | "firstName"
  | "lastName"
  | "displayName"
  | "bio"
  | "profilePhotoUrl"
  | "city"
  | "country"
  | "languages"
  | "occupation"
  | "dateOfBirth"
>;

const filled = (value: string | null | undefined): boolean =>
  typeof value === "string" && value.trim().length > 0;

/**
 * Tri-state used by BOTH the profile checklist and the verification panel so
 * they can never drift apart (the checklist saying "empty" while the badge
 * says "Pending"). The middle state exists because supplying a value and
 * having it verified are two distinct milestones:
 *
 *  - `empty`     nothing supplied yet — an outline circle / "Not started".
 *  - `pending`   supplied but awaiting an external check — amber / "Pending".
 *  - `complete`  present (for plain fields) or verified (for trust checks).
 */
export type ProfileSignalStatus = "empty" | "pending" | "complete";

/** Plain profile fields have no verification step: present ⇒ complete. */
export function presenceStatus(value: string | null | undefined): ProfileSignalStatus {
  return filled(value) ? "complete" : "empty";
}

/**
 * Phone is the field the checklist and verification panel disagreed on: adding
 * a number starts SMS verification, so it must read as `pending` (not `empty`,
 * and not yet `complete`) until {@link UserProfileDto.isPhoneVerified} flips.
 */
export function phoneVerificationStatus(
  user: Pick<UserProfileDto, "phoneNumber" | "isPhoneVerified">,
): ProfileSignalStatus {
  if (user.isPhoneVerified) return "complete";
  return filled(user.phoneNumber) ? "pending" : "empty";
}

/**
 * Every account has an email, so it's never `empty` — it's `pending`
 * confirmation until verified. Falls back to {@link UserProfileDto.isActive}
 * for older records that predate the explicit `emailConfirmed` flag.
 */
export function emailVerificationStatus(
  user: Pick<UserProfileDto, "emailConfirmed" | "isActive">,
): ProfileSignalStatus {
  return (user.emailConfirmed ?? user.isActive) ? "complete" : "pending";
}

/**
 * The profile DTO exposes only a verified flag for government ID — there's no
 * "submitted / in-review" signal here — so from this source it's either
 * `complete` or `empty`. If a KYC-pending signal is ever added to the profile,
 * route it through here and every consumer picks up the `pending` state.
 */
export function governmentIdVerificationStatus(
  user: Pick<UserProfileDto, "isGovernmentIdVerified">,
): ProfileSignalStatus {
  return user.isGovernmentIdVerified ? "complete" : "empty";
}

/**
 * Computes how complete a user's public profile is. The signal set and weights
 * mirror the backend (HostProfileProvider.ComputeCompleteness) so the host sees
 * the same percentage the server enforces. Verification badges are deliberately
 * excluded — they depend on external checks and shouldn't block listing.
 */
export function computeProfileCompleteness(
  profile: ProfileFields | null | undefined,
): ProfileCompleteness {
  if (!profile) {
    return { percent: 0, missing: [], meetsListingThreshold: false };
  }

  const hasName =
    filled(profile.displayName) ||
    (filled(profile.firstName) && filled(profile.lastName));

  const signals: { label: string; filled: boolean }[] = [
    { label: "Name", filled: hasName },
    { label: "Profile photo", filled: filled(profile.profilePhotoUrl) },
    { label: "Bio", filled: filled(profile.bio) },
    { label: "City", filled: filled(profile.city) },
    { label: "Country", filled: filled(profile.country) },
    { label: "Languages", filled: filled(profile.languages) },
    { label: "Occupation", filled: filled(profile.occupation) },
    { label: "Date of birth", filled: filled(profile.dateOfBirth) },
  ];

  const filledCount = signals.filter((s) => s.filled).length;
  const percent = Math.round((filledCount * 100) / signals.length);
  const missing = signals.filter((s) => !s.filled).map((s) => s.label);

  return {
    percent,
    missing,
    meetsListingThreshold: percent >= MIN_HOST_PROFILE_COMPLETENESS,
  };
}
