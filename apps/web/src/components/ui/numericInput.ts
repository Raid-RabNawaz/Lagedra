export type NumericInputOptions = {
  min?: number;
  decimalPlaces?: number;
  allowNegative?: boolean;
};

/** Keep only a typable numeric draft (digits, one dot, optional leading minus). */
export function sanitizeNumericInput(
  raw: string,
  options: NumericInputOptions = {},
): string {
  const allowNegative = options.allowNegative ?? (options.min !== undefined && options.min < 0);
  let sign = "";
  let body = raw;
  if (allowNegative && body.startsWith("-")) {
    sign = "-";
    body = body.slice(1);
  }

  body = body.replace(/[^\d.]/g, "");
  const dot = body.indexOf(".");
  if (options.decimalPlaces === 0) {
    body = body.replace(/\./g, "");
  } else if (dot !== -1) {
    const whole = body.slice(0, dot).replace(/\./g, "");
    let fraction = body.slice(dot + 1).replace(/\./g, "");
    if (options.decimalPlaces !== undefined) {
      fraction = fraction.slice(0, options.decimalPlaces);
    }
    body = `${whole}.${fraction}`;
  }

  return sign + body;
}

export function parseNumericInput(text: string): number | null {
  if (text === "" || text === "." || text === "-" || text === "-.") {
    return null;
  }
  const n = Number(text);
  return Number.isFinite(n) ? n : null;
}

export function formatNumericValue(
  value: number | string | null | undefined,
  decimalPlaces?: number,
): string {
  if (value === "" || value === null || value === undefined) {
    return "";
  }
  const n = typeof value === "number" ? value : Number(value);
  if (!Number.isFinite(n)) {
    return "";
  }
  if (decimalPlaces === undefined) {
    return String(n);
  }
  if (decimalPlaces === 0) {
    return String(Math.round(n));
  }
  return n.toFixed(decimalPlaces);
}
