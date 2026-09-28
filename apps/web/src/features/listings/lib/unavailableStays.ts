/**
 * Half-open stay windows already taken on a listing. `checkIn` is the first
 * occupied night; `checkOut` is the morning the listing turns over, so that
 * date can be the next guest's check-in.
 */
export type UnavailableStay = {
  checkIn: string;
  checkOut: string;
};

export function toUnavailableStays(
  blocks: readonly { checkInDate: string; checkOutDate: string }[] | undefined,
): UnavailableStay[] {
  return (blocks ?? []).map((block) => ({
    checkIn: block.checkInDate.slice(0, 10),
    checkOut: block.checkOutDate.slice(0, 10),
  }));
}

export type DateSelection = {
  start: Date | null;
  end: Date | null;
};

export type SelectionResult =
  | { kind: "ignore" }
  | { kind: "clear" }
  | { kind: "start"; date: Date }
  | { kind: "range"; start: Date; end: Date };

function startOfDay(d: Date) {
  const x = new Date(d);
  x.setHours(0, 0, 0, 0);
  return x;
}

function parseIsoDate(iso: string): Date | null {
  const match = /^(\d{4})-(\d{2})-(\d{2})/.exec(iso);
  if (!match) return null;
  const year = Number(match[1]);
  const month = Number(match[2]) - 1;
  const day = Number(match[3]);
  const date = new Date(year, month, day);
  if (date.getFullYear() !== year || date.getMonth() !== month || date.getDate() !== day) {
    return null;
  }
  return date;
}

type ParsedStay = { checkIn: number; checkOut: number };

function parsedStays(stays: readonly UnavailableStay[]): ParsedStay[] {
  const parsed: ParsedStay[] = [];
  for (const stay of stays) {
    const checkIn = parseIsoDate(stay.checkIn);
    const checkOut = parseIsoDate(stay.checkOut);
    if (!checkIn || !checkOut) continue;
    const start = checkIn.getTime();
    const end = checkOut.getTime();
    if (end <= start) continue;
    parsed.push({ checkIn: start, checkOut: end });
  }
  return parsed;
}

/** True when the night that begins on `date` is already taken. */
export function isNightUnavailable(date: Date, stays: readonly UnavailableStay[]): boolean {
  const night = startOfDay(date).getTime();
  return parsedStays(stays).some((stay) => night >= stay.checkIn && night < stay.checkOut);
}

/**
 * True when the stay [start, end) overlaps a taken window. Matches the
 * server overlap test: start < block.CheckOut && end > block.CheckIn.
 */
export function stayOverlapsUnavailable(
  start: Date,
  end: Date,
  stays: readonly UnavailableStay[],
): boolean {
  const stayStart = startOfDay(start).getTime();
  const stayEnd = startOfDay(end).getTime();
  if (stayEnd <= stayStart) return true;
  return parsedStays(stays).some(
    (stay) => stayStart < stay.checkOut && stayEnd > stay.checkIn,
  );
}

/** Checkout is allowed when it lands after check-in and skips every taken night. */
export function isValidCheckout(
  start: Date,
  end: Date,
  stays: readonly UnavailableStay[],
): boolean {
  return startOfDay(end).getTime() > startOfDay(start).getTime()
    && !stayOverlapsUnavailable(start, end, stays);
}

/**
 * Next calendar selection after a day click. Occupied nights cannot be a
 * check-in. The morning an existing stay begins can still be a checkout.
 * A click that would cross a taken night starts a new check-in when that
 * night is free, and is ignored when it is not.
 */
export function nextRangeSelection(
  current: DateSelection,
  clicked: Date,
  stays: readonly UnavailableStay[],
  minDate: Date,
): SelectionResult {
  const date = startOfDay(clicked);
  if (date.getTime() < startOfDay(minDate).getTime()) return { kind: "ignore" };

  const nightTaken = isNightUnavailable(date, stays);
  const { start, end } = current;

  if (!start || end) {
    if (nightTaken) return { kind: "ignore" };
    return { kind: "start", date };
  }

  const startDay = startOfDay(start);
  if (date.getTime() === startDay.getTime()) return { kind: "clear" };

  if (date.getTime() < startDay.getTime()) {
    if (nightTaken) return { kind: "ignore" };
    return { kind: "start", date };
  }

  if (!isValidCheckout(startDay, date, stays)) {
    if (nightTaken) return { kind: "ignore" };
    return { kind: "start", date };
  }

  return { kind: "range", start: startDay, end: date };
}

export function isSelectionDisabled(
  date: Date,
  current: DateSelection,
  stays: readonly UnavailableStay[],
  minDate: Date,
): boolean {
  return nextRangeSelection(current, date, stays, minDate).kind === "ignore";
}
