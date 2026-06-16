const dateFormatter = new Intl.DateTimeFormat("en-US", {
  year: "numeric",
  month: "long",
  day: "numeric",
});

const shortDateFormatter = new Intl.DateTimeFormat("en-US", {
  year: "numeric",
  month: "short",
});

// Currency display policy (Lagedra): never show fractional cents. We
// ceiling-round to the nearest whole dollar so the tenant is never
// surprised by a higher charge than they were shown. Internally we still
// store amounts in cents — this purely controls presentation.
const moneyFormatter = new Intl.NumberFormat("en-US", {
  style: "currency",
  currency: "USD",
  minimumFractionDigits: 0,
  maximumFractionDigits: 0,
});

export function formatDate(dateStr: string | Date): string {
  const date = typeof dateStr === "string" ? new Date(dateStr) : dateStr;
  return dateFormatter.format(date);
}

export function formatShortDate(dateStr: string | Date): string {
  const date = typeof dateStr === "string" ? new Date(dateStr) : dateStr;
  return shortDateFormatter.format(date);
}

/**
 * Format a cents value as a whole-dollar amount (e.g. `$1,235`). Always
 * rounds *up* to the nearest dollar, so:
 *   - `formatMoney(123450)` → `"$1,235"`  (covers $1,234.50)
 *   - `formatMoney(123401)` → `"$1,235"`  (covers $1,234.01)
 *   - `formatMoney(123400)` → `"$1,234"`  (already a whole dollar)
 *   - `formatMoney(0)`      → `"$0"`
 *   - `formatMoney(-50)`    → `"-$0"`     (Math.ceil of -0.5 is -0)
 *
 * Negative amounts stay negative — used in payouts/refund displays.
 */
export function formatMoney(cents: number): string {
  if (!Number.isFinite(cents)) return moneyFormatter.format(0);
  return moneyFormatter.format(Math.ceil(cents / 100));
}

export function formatPercent(value: number): string {
  return `${Math.round(value)}%`;
}
