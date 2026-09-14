import { describe, expect, it } from "vitest";
import {
  isScreeningInFlight,
  screeningStatusDetail,
  screeningStatusLabel,
} from "./screeningStatus";

describe("screeningStatusLabel", () => {
  it("uses host-facing copy instead of raw enum names", () => {
    expect(screeningStatusLabel("WaitingForApplicant")).toBe("Waiting for guest");
    expect(screeningStatusLabel("Pending")).toBe("Check in progress");
    expect(screeningStatusLabel("Completed", "Pass")).toBe("Pass");
    expect(screeningStatusLabel("Completed", "Review")).toBe("Review");
    expect(screeningStatusLabel("Failed")).toBe("Could not start");
  });

  it("shows the provider failure reason to the host", () => {
    expect(
      screeningStatusDetail("Failed", null, true, "State must be a valid US state code."),
    ).toBe("State must be a valid US state code.");
  });

  it("treats unpaid through pending as in flight", () => {
    expect(isScreeningInFlight("WaitingForApplicant")).toBe(true);
    expect(isScreeningInFlight("Completed")).toBe(false);
  });
});
