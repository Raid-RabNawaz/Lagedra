import type { ListingAmenityDto, AmenityCategory } from "@/api/types";
import { DynamicIcon } from "./DynamicIcon";

const categoryLabels: Record<AmenityCategory, string> = {
  Kitchen: "Kitchen",
  Bathroom: "Bathroom",
  Bedroom: "Bedroom",
  LivingArea: "Living area",
  Outdoor: "Outdoor",
  Parking: "Parking",
  Entertainment: "Entertainment",
  WorkSpace: "Work space",
  Accessibility: "Accessibility",
  Laundry: "Laundry",
  ClimateControl: "Climate",
  Internet: "Internet",
};

type AmenityGridProps = {
  amenities: ListingAmenityDto[];
};

export function AmenityGrid({ amenities }: AmenityGridProps) {
  if (amenities.length === 0) return null;

  const grouped = new Map<string, ListingAmenityDto[]>();
  for (const a of amenities) {
    const list = grouped.get(a.category) ?? [];
    list.push(a);
    grouped.set(a.category, list);
  }

  return (
    <div className="space-y-5">
      {Array.from(grouped.entries()).map(([category, items]) => (
        <div key={category}>
          <h4 className="text-sm font-medium text-muted-foreground mb-2">
            {categoryLabels[category as AmenityCategory] ?? category}
          </h4>
          <div className="grid grid-cols-2 gap-2 sm:grid-cols-3">
            {items.map((amenity) => (
              <div
                key={amenity.id}
                className="flex items-center gap-2 rounded-lg border px-3 py-2 text-sm"
              >
                <DynamicIcon iconKey={amenity.iconKey} className="h-4 w-4 text-muted-foreground shrink-0" />
                <span>{amenity.name}</span>
              </div>
            ))}
          </div>
        </div>
      ))}
    </div>
  );
}
