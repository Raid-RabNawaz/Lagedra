import { describe, expect, it } from "vitest";
import { violationActions } from "./adminQueueActions";

describe("violationActions", () => {
  it("allows resolve, dismiss, and escalate on open items", () => {
    expect(violationActions("Open")).toEqual({
      canResolve: true,
      canDismiss: true,
      canEscalate: true,
    });
  });

  it("blocks escalate after the item is already closed", () => {
    expect(violationActions("Resolved")).toEqual({
      canResolve: false,
      canDismiss: false,
      canEscalate: false,
    });
    expect(violationActions("Dismissed").canEscalate).toBe(false);
  });

  it("still allows resolve or dismiss after escalation", () => {
    expect(violationActions("Escalated")).toEqual({
      canResolve: true,
      canDismiss: true,
      canEscalate: false,
    });
  });
});
