import { describe, expect, it } from "vitest";
import {
  isNightUnavailable,
  isSelectionDisabled,
  isValidCheckout,
  nextRangeSelection,
  stayOverlapsUnavailable,
  type UnavailableStay,
} from "./unavailableStays";

const booked: UnavailableStay = { checkIn: "2026-09-10", checkOut: "2026-09-15" };

function day(iso: string) {
  const [year, month, date] = iso.split("-").map(Number);
  return new Date(year, month - 1, date);
}

const today = day("2026-09-01");

describe("unavailable stays", () => {
  it("marks occupied nights and leaves the checkout morning free", () => {
    expect(isNightUnavailable(day("2026-09-10"), [booked])).toBe(true);
    expect(isNightUnavailable(day("2026-09-14"), [booked])).toBe(true);
    expect(isNightUnavailable(day("2026-09-15"), [booked])).toBe(false);
    expect(isNightUnavailable(day("2026-09-09"), [booked])).toBe(false);
  });

  it("allows a stay that ends when the next booking begins", () => {
    expect(stayOverlapsUnavailable(day("2026-09-05"), day("2026-09-10"), [booked])).toBe(false);
    expect(isValidCheckout(day("2026-09-05"), day("2026-09-10"), [booked])).toBe(true);
  });

  it("rejects a stay that includes a taken night", () => {
    expect(stayOverlapsUnavailable(day("2026-09-05"), day("2026-09-11"), [booked])).toBe(true);
    expect(stayOverlapsUnavailable(day("2026-09-10"), day("2026-09-12"), [booked])).toBe(true);
    expect(stayOverlapsUnavailable(day("2026-09-14"), day("2026-09-20"), [booked])).toBe(true);
  });

  it("allows a new check-in on the morning the previous stay ends", () => {
    expect(stayOverlapsUnavailable(day("2026-09-15"), day("2026-09-20"), [booked])).toBe(false);
    expect(isValidCheckout(day("2026-09-15"), day("2026-09-20"), [booked])).toBe(true);
  });

  it("disables booked nights until one is a valid checkout", () => {
    const empty = { start: null, end: null };
    expect(isSelectionDisabled(day("2026-09-12"), empty, [booked], today)).toBe(true);
    expect(isSelectionDisabled(day("2026-09-08"), empty, [booked], today)).toBe(false);

    const pickingCheckout = { start: day("2026-09-05"), end: null };
    expect(isSelectionDisabled(day("2026-09-10"), pickingCheckout, [booked], today)).toBe(false);
    expect(isSelectionDisabled(day("2026-09-11"), pickingCheckout, [booked], today)).toBe(true);
  });

  it("restarts check-in when a later free day would cross a booking", () => {
    const result = nextRangeSelection(
      { start: day("2026-09-05"), end: null },
      day("2026-09-16"),
      [booked],
      today,
    );
    expect(result).toEqual({ kind: "start", date: day("2026-09-16") });
  });

  it("ignores a click in the middle of a booking", () => {
    const result = nextRangeSelection(
      { start: day("2026-09-05"), end: null },
      day("2026-09-12"),
      [booked],
      today,
    );
    expect(result).toEqual({ kind: "ignore" });
  });

  it("sets checkout on the morning the booked stay begins", () => {
    const result = nextRangeSelection(
      { start: day("2026-09-05"), end: null },
      day("2026-09-10"),
      [booked],
      today,
    );
    expect(result).toEqual({
      kind: "range",
      start: day("2026-09-05"),
      end: day("2026-09-10"),
    });
  });
});
