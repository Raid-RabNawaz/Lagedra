import { FileSearch } from "lucide-react";
import { cn } from "@/lib/utils";
import type { ScreeningOrderStatus } from "@/api/types";
import {
  screeningStatusClassName,
  screeningStatusLabel,
} from "@/features/applications/lib/screeningStatus";

type Props = {
  status: ScreeningOrderStatus | string | null | undefined;
  result?: string | null;
  className?: string;
};

export function ScreeningStatusBadge({ status, result, className }: Props) {
  if (!status) return null;

  return (
    <span
      className={cn(
        "inline-flex items-center gap-1 rounded-full border px-2 py-0.5 text-[11px] font-semibold leading-none",
        screeningStatusClassName(status, result),
        className,
      )}
    >
      <FileSearch className="h-3 w-3 shrink-0" />
      Check · {screeningStatusLabel(status, result)}
    </span>
  );
}
