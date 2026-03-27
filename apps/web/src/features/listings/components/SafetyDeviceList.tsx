import type { ListingSafetyDeviceDto } from "@/api/types";
import { DynamicIcon } from "./DynamicIcon";
import { ShieldCheck } from "lucide-react";

type SafetyDeviceListProps = {
  devices: ListingSafetyDeviceDto[];
};

export function SafetyDeviceList({ devices }: SafetyDeviceListProps) {
  if (devices.length === 0) return null;

  return (
    <div className="space-y-2">
      {devices.map((device) => (
        <div key={device.id} className="flex items-center gap-2.5 text-sm">
          <DynamicIcon iconKey={device.iconKey} className="h-4 w-4 text-success shrink-0" fallback={ShieldCheck} />
          <span>{device.name}</span>
        </div>
      ))}
    </div>
  );
}
