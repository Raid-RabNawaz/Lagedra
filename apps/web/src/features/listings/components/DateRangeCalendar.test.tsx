/**
 * @vitest-environment jsdom
 */
import { act, useState } from "react";
import { createRoot, type Root } from "react-dom/client";
import { afterEach, describe, expect, it } from "vitest";
import { DateRangeCalendar, type DateRange } from "./DateRangeCalendar";

function iso(date: Date) {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}

function addDays(date: Date, days: number) {
  const next = new Date(date);
  next.setDate(next.getDate() + days);
  return next;
}

function buttonFor(container: HTMLElement, date: Date) {
  const label = date.toLocaleDateString();
  return [...container.querySelectorAll("button")].find((button) => {
    const aria = button.getAttribute("aria-label") ?? "";
    return aria === label || aria.startsWith(`${label},`);
  });
}

describe("DateRangeCalendar booked dates", () => {
  let root: Root | null = null;
  let container: HTMLDivElement | null = null;

  afterEach(() => {
    act(() => {
      root?.unmount();
    });
    container?.remove();
    root = null;
    container = null;
  });

  it("disables occupied nights and keeps the turnover morning selectable", async () => {
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    const checkIn = addDays(today, 20);
    const checkOut = addDays(today, 25);
    const inside = addDays(today, 22);

    function Harness() {
      const [range, setRange] = useState<DateRange>({ start: null, end: null });
      return (
        <DateRangeCalendar
          range={range}
          onChange={setRange}
          unavailableStays={[{ checkIn: iso(checkIn), checkOut: iso(checkOut) }]}
        />
      );
    }

    container = document.createElement("div");
    document.body.appendChild(container);
    root = createRoot(container);
    await act(async () => {
      root?.render(<Harness />);
    });

    const bookedNight = buttonFor(container, checkIn);
    const middleNight = buttonFor(container, inside);
    const turnover = buttonFor(container, checkOut);
    const before = buttonFor(container, addDays(checkIn, -1));

    expect(bookedNight?.disabled).toBe(true);
    expect(bookedNight?.getAttribute("aria-label")).toContain("unavailable");
    expect(middleNight?.disabled).toBe(true);
    expect(turnover?.disabled).toBe(false);
    expect(before?.disabled).toBe(false);
    expect(container.textContent).toContain("Struck-through dates are unavailable.");

    await act(async () => {
      before?.click();
    });

    expect(buttonFor(container, checkIn)?.disabled).toBe(false);
    expect(buttonFor(container, inside)?.disabled).toBe(true);

    await act(async () => {
      buttonFor(container!, checkIn)?.click();
    });

    expect(container.textContent).toContain(checkIn.toLocaleDateString());
    expect(container.textContent).toContain("Clear dates");
  });
});
