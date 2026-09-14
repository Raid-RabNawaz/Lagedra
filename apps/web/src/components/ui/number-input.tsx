import { forwardRef, useEffect, useState } from "react";
import type { Control, FieldPath, FieldValues } from "react-hook-form";
import { Controller } from "react-hook-form";
import { Input } from "@/components/ui/input";
import {
  formatNumericValue,
  parseNumericInput,
  sanitizeNumericInput,
} from "@/components/ui/numericInput";

type NumberInputProps = Omit<
  React.InputHTMLAttributes<HTMLInputElement>,
  "type" | "value" | "onChange" | "min" | "max"
> & {
  value: number | string | null | undefined;
  onValueChange?: (value: number | null) => void;
  min?: number;
  max?: number;
  /** `0` = integers. Omit for unlimited fraction digits. */
  decimalPlaces?: number;
};

export const NumberInput = forwardRef<HTMLInputElement, NumberInputProps>(
  (
    {
      value,
      onValueChange,
      min,
      max,
      decimalPlaces,
      onFocus,
      onBlur,
      inputMode,
      ...props
    },
    ref,
  ) => {
    const allowNegative = min !== undefined && min < 0;
    const committed = formatNumericValue(value, decimalPlaces);
    const [focused, setFocused] = useState(false);
    const [draft, setDraft] = useState(committed);

    useEffect(() => {
      if (!focused) {
        setDraft(formatNumericValue(value, decimalPlaces));
      }
    }, [value, decimalPlaces, focused]);

    const emit = (text: string) => {
      onValueChange?.(parseNumericInput(text));
    };

    return (
      <Input
        {...props}
        ref={ref}
        type="text"
        inputMode={
          inputMode ?? (decimalPlaces === 0 && !allowNegative ? "numeric" : "decimal")
        }
        value={focused ? draft : committed}
        onFocus={(event) => {
          setFocused(true);
          setDraft(formatNumericValue(value, decimalPlaces));
          onFocus?.(event);
        }}
        onChange={(event) => {
          const next = sanitizeNumericInput(event.target.value, {
            min,
            decimalPlaces,
            allowNegative,
          });
          setDraft(next);
          emit(next);
        }}
        onBlur={(event) => {
          const parsed = parseNumericInput(draft);
          let next = parsed;
          if (next !== null && min !== undefined && next < min) {
            next = min;
          }
          if (next !== null && max !== undefined && next > max) {
            next = max;
          }
          const text = formatNumericValue(next, decimalPlaces);
          setDraft(text);
          onValueChange?.(next);
          setFocused(false);
          onBlur?.(event);
        }}
      />
    );
  },
);
NumberInput.displayName = "NumberInput";

type FormNumberInputProps<T extends FieldValues> = Omit<
  NumberInputProps,
  "value" | "onValueChange"
> & {
  control: Control<T>;
  name: FieldPath<T>;
};

export function FormNumberInput<T extends FieldValues>({
  control,
  name,
  ...props
}: FormNumberInputProps<T>) {
  return (
    <Controller
      control={control}
      name={name}
      render={({ field }) => (
        <NumberInput
          {...props}
          ref={field.ref}
          name={field.name}
          value={
            typeof field.value === "number" && Number.isFinite(field.value)
              ? field.value
              : null
          }
          onValueChange={(n) => field.onChange(n ?? undefined)}
          onBlur={field.onBlur}
        />
      )}
    />
  );
}
