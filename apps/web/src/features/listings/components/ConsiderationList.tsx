import type { ListingConsiderationDto } from "@/api/types";
import { DynamicIcon } from "./DynamicIcon";
import { AlertTriangle } from "lucide-react";

type ConsiderationListProps = {
  considerations: ListingConsiderationDto[];
};

export function ConsiderationList({ considerations }: ConsiderationListProps) {
  if (considerations.length === 0) return null;

  return (
    <div className="space-y-2">
      {considerations.map((item) => (
        <div key={item.id} className="flex items-center gap-2.5 text-sm">
          <DynamicIcon iconKey={item.iconKey} className="h-4 w-4 text-amber-500 shrink-0" fallback={AlertTriangle} />
          <span>{item.name}</span>
        </div>
      ))}
    </div>
  );
}
