import { cn } from "@/lib/utils";
import { EVICTION_PROTECTION_TERMS } from "@/features/listings/lib/evictionProtection";

type Props = {
  className?: string;
};

export function EvictionProtectionTermsList({ className }: Props) {
  return (
    <ul className={cn("list-disc space-y-1 pl-4", className)}>
      {EVICTION_PROTECTION_TERMS.map((term) => (
        <li key={term}>{term}</li>
      ))}
    </ul>
  );
}
