import { describe, expect, it } from "vitest";
import {
  isPreLaunchHostPath,
  isPreLaunchLimitedHost,
  isPreLaunchStaff,
  PRE_LAUNCH_HOST_HOME,
} from "./preLaunchAccess";
import { roles } from "./roles";

describe("preLaunchAccess", () => {
  it("allows listings and channels paths only", () => {
    expect(isPreLaunchHostPath("/app/listings")).toBe(true);
    expect(isPreLaunchHostPath("/app/listings/new")).toBe(true);
    expect(isPreLaunchHostPath("/app/listings/abc")).toBe(true);
    expect(isPreLaunchHostPath("/app/listings/abc/edit")).toBe(true);
    expect(isPreLaunchHostPath("/app/channels")).toBe(true);
    expect(isPreLaunchHostPath("/app")).toBe(false);
    expect(isPreLaunchHostPath("/app/profile")).toBe(false);
    expect(isPreLaunchHostPath("/app/deals")).toBe(false);
    expect(isPreLaunchHostPath("/listings")).toBe(false);
  });

  it("treats platform admin and arbitrator as staff", () => {
    expect(isPreLaunchStaff(roles.platformAdmin)).toBe(true);
    expect(isPreLaunchStaff(roles.arbitrator)).toBe(true);
    expect(isPreLaunchStaff(roles.member)).toBe(false);
  });

  it("limits non-staff members when pre-launch is on", () => {
    expect(isPreLaunchLimitedHost(true, roles.member)).toBe(true);
    expect(isPreLaunchLimitedHost(true, roles.platformAdmin)).toBe(false);
    expect(isPreLaunchLimitedHost(false, roles.member)).toBe(false);
  });

  it("exports the host home path", () => {
    expect(PRE_LAUNCH_HOST_HOME).toBe("/app/listings");
  });
});
