import type { ReactNode } from "react";

export const AdminDetailList = ({
  rows,
}: {
  rows: { label: string; value: ReactNode }[];
}) => (
  <dl className="grid grid-cols-1 gap-3 text-sm sm:grid-cols-2">
    {rows.map((row) => (
      <div key={row.label} className="min-w-0">
        <dt className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
          {row.label}
        </dt>
        <dd className="mt-1 break-words">{row.value ?? "—"}</dd>
      </div>
    ))}
  </dl>
);
