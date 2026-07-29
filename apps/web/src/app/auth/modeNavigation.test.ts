import { describe, expect, it } from "vitest";
import { resolveModeSwitchRedirect } from "./modeNavigation";

describe("resolveModeSwitchRedirect", () => {
  it("keeps create-listing URL when already in host mode", () => {
    expect(resolveModeSwitchRedirect("/app/listings/new", "host")).toBeNull();
  });

  it("keeps listing detail and edit URLs in host mode", () => {
    expect(resolveModeSwitchRedirect("/app/listings/abc", "host")).toBeNull();
    expect(resolveModeSwitchRedirect("/app/listings/abc/edit", "host")).toBeNull();
    expect(resolveModeSwitchRedirect("/app/listings", "host")).toBeNull();
  });

  it("sends listing routes to dashboard when in guest mode", () => {
    expect(resolveModeSwitchRedirect("/app/listings/new", "guest")).toBe("/app");
    expect(resolveModeSwitchRedirect("/app/listings", "guest")).toBe("/app");
  });

  it("still swaps applications between modes", () => {
    expect(resolveModeSwitchRedirect("/app/applications", "guest")).toBe(
      "/app/my-applications",
    );
    expect(resolveModeSwitchRedirect("/app/my-applications", "host")).toBe(
      "/app/applications",
    );
  });
});
