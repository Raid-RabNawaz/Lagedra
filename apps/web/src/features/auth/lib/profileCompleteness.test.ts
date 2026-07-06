import { describe, it, expect } from "vitest";
import type { UserProfileDto } from "@/api/types";
import {
  computeProfileCompleteness,
  emailVerificationStatus,
  governmentIdVerificationStatus,
  phoneVerificationStatus,
  presenceStatus,
  MIN_HOST_PROFILE_COMPLETENESS,
} from "./profileCompleteness";

function profile(overrides: Partial<UserProfileDto> = {}): UserProfileDto {
  return {
    userId: "u-1",
    email: "host@example.com",
    role: 0,
    isActive: true,
    firstName: null,
    lastName: null,
    displayName: null,
    phoneNumber: null,
    bio: null,
    profilePhotoUrl: null,
    city: null,
    state: null,
    country: null,
    languages: null,
    occupation: null,
    dateOfBirth: null,
    isGovernmentIdVerified: false,
    isPhoneVerified: false,
    memberSince: "2026-01-01T00:00:00Z",
    ...overrides,
  } as UserProfileDto;
}

describe("computeProfileCompleteness", () => {
  it("returns 0% and below-threshold for a null profile", () => {
    const result = computeProfileCompleteness(null);
    expect(result.percent).toBe(0);
    expect(result.meetsListingThreshold).toBe(false);
  });

  it("returns 0% for an empty profile and lists every missing field", () => {
    const result = computeProfileCompleteness(profile());
    expect(result.percent).toBe(0);
    expect(result.missing).toContain("Name");
    expect(result.missing).toContain("Bio");
    expect(result.missing).toContain("Date of birth");
  });

  it("counts a display name OR first+last as the name signal", () => {
    const viaDisplay = computeProfileCompleteness(profile({ displayName: "Jordan Bennett" }));
    const viaFirstLast = computeProfileCompleteness(
      profile({ firstName: "Jordan", lastName: "Bennett" }),
    );
    expect(viaDisplay.missing).not.toContain("Name");
    expect(viaFirstLast.missing).not.toContain("Name");
  });

  it("treats whitespace-only values as empty", () => {
    const result = computeProfileCompleteness(profile({ bio: "   ", city: "  " }));
    expect(result.missing).toContain("Bio");
    expect(result.missing).toContain("City");
  });

  it("does not meet the threshold at 5 of 8 signals (~63%)", () => {
    const result = computeProfileCompleteness(
      profile({
        displayName: "Jordan Bennett",
        bio: "Hi there",
        profilePhotoUrl: "https://example.com/a.png",
        city: "Austin",
        country: "United States",
      }),
    );
    expect(result.percent).toBe(63);
    expect(result.meetsListingThreshold).toBe(false);
  });

  it("meets the threshold at 6 of 8 signals (75%)", () => {
    const result = computeProfileCompleteness(
      profile({
        displayName: "Jordan Bennett",
        bio: "Hi there",
        profilePhotoUrl: "https://example.com/a.png",
        city: "Austin",
        country: "United States",
        languages: "English",
      }),
    );
    expect(result.percent).toBe(75);
    expect(result.percent).toBeGreaterThanOrEqual(MIN_HOST_PROFILE_COMPLETENESS);
    expect(result.meetsListingThreshold).toBe(true);
  });

  it("reaches 100% with every signal filled", () => {
    const result = computeProfileCompleteness(
      profile({
        displayName: "Jordan Bennett",
        bio: "Hi there",
        profilePhotoUrl: "https://example.com/a.png",
        city: "Austin",
        country: "United States",
        languages: "English, Spanish",
        occupation: "Property manager",
        dateOfBirth: "1990-05-14",
      }),
    );
    expect(result.percent).toBe(100);
    expect(result.missing).toHaveLength(0);
  });
});

describe("phoneVerificationStatus", () => {
  it("is empty when no number has been added", () => {
    expect(phoneVerificationStatus(profile())).toBe("empty");
    expect(phoneVerificationStatus(profile({ phoneNumber: "   " }))).toBe("empty");
  });

  it("is pending once a number is added but not yet verified", () => {
    // This is the exact bug: the checklist must not read "empty" here while
    // the verification panel says "Pending".
    expect(phoneVerificationStatus(profile({ phoneNumber: "+1 555 123 4567" }))).toBe(
      "pending",
    );
  });

  it("is complete once the number is verified", () => {
    expect(
      phoneVerificationStatus(
        profile({ phoneNumber: "+1 555 123 4567", isPhoneVerified: true }),
      ),
    ).toBe("complete");
  });
});

describe("emailVerificationStatus", () => {
  it("is pending until confirmed (email always exists, so never empty)", () => {
    expect(emailVerificationStatus(profile({ isActive: false }))).toBe("pending");
    expect(
      emailVerificationStatus(profile({ isActive: true, emailConfirmed: false })),
    ).toBe("pending");
  });

  it("is complete when confirmed, falling back to isActive for legacy records", () => {
    expect(emailVerificationStatus(profile({ emailConfirmed: true }))).toBe("complete");
    expect(
      emailVerificationStatus(profile({ isActive: true, emailConfirmed: undefined })),
    ).toBe("complete");
  });
});

describe("governmentIdVerificationStatus", () => {
  it("is empty when not verified and complete when verified", () => {
    expect(governmentIdVerificationStatus(profile())).toBe("empty");
    expect(
      governmentIdVerificationStatus(profile({ isGovernmentIdVerified: true })),
    ).toBe("complete");
  });
});

describe("presenceStatus", () => {
  it("maps a filled value to complete and blank/whitespace to empty", () => {
    expect(presenceStatus("Austin")).toBe("complete");
    expect(presenceStatus("")).toBe("empty");
    expect(presenceStatus("   ")).toBe("empty");
    expect(presenceStatus(null)).toBe("empty");
    expect(presenceStatus(undefined)).toBe("empty");
  });
});
