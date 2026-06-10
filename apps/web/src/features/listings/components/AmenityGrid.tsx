import { useState } from "react";
import { ChevronDown, X } from "lucide-react";
import type { ListingAmenityDto, AmenityCategory } from "@/api/types";
import { DynamicIcon } from "./DynamicIcon";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";

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
  /**
   * Soft cap on how many amenities to render inline before collapsing the
   * rest behind a "View all N" button that opens a modal with the
   * complete, category-grouped list. Defaults to 12 — short enough to
   * keep the listing detail page scannable, long enough that small-set
   * listings never need the modal.
   */
  previewLimit?: number;
};

function groupByCategory(amenities: ListingAmenityDto[]) {
  const grouped = new Map<string, ListingAmenityDto[]>();
  for (const a of amenities) {
    const list = grouped.get(a.category) ?? [];
    list.push(a);
    grouped.set(a.category, list);
  }
  return grouped;
}

function AmenityChip({ amenity }: { amenity: ListingAmenityDto }) {
  return (
    <div className="flex items-center gap-2 rounded-lg border px-3 py-2 text-sm">
      <DynamicIcon
        iconKey={amenity.iconKey}
        className="h-4 w-4 text-muted-foreground shrink-0"
      />
      <span>{amenity.name}</span>
    </div>
  );
}

export function AmenityGrid({ amenities, previewLimit = 12 }: AmenityGridProps) {
  const [showAllDialog, setShowAllDialog] = useState(false);

  if (amenities.length === 0) return null;

  const totalCount = amenities.length;
  const needsCollapse = totalCount > previewLimit;
  const preview = needsCollapse ? amenities.slice(0, previewLimit) : amenities;

  // Inline preview is a flat grid (chip-style), the same layout the
  // "View all" modal uses inside each category. Grouping by category is
  // reserved for the modal — at 12+ chips the headers add visual noise
  // without enough items beneath them to justify the indentation.
  const groupedAll = groupByCategory(amenities);

  return (
    <>
      <div className="grid grid-cols-2 gap-2 sm:grid-cols-3">
        {preview.map((amenity) => (
          <AmenityChip key={amenity.id} amenity={amenity} />
        ))}
      </div>

      {needsCollapse && (
        <div className="mt-4">
          <Button
            type="button"
            variant="outline"
            onClick={() => setShowAllDialog(true)}
            className="gap-2"
          >
            <ChevronDown className="h-4 w-4" />
            View all {totalCount} amenities
          </Button>
        </div>
      )}

      <Dialog open={showAllDialog} onOpenChange={setShowAllDialog}>
        <DialogContent className="max-w-2xl">
          <DialogHeader>
            <DialogTitle className="pr-8">
              All {totalCount} amenities
            </DialogTitle>
          </DialogHeader>
          <div className="max-h-[70vh] overflow-y-auto space-y-5 pr-1">
            {Array.from(groupedAll.entries()).map(([category, items]) => (
              <div key={category}>
                <h4 className="text-sm font-medium text-muted-foreground mb-2">
                  {categoryLabels[category as AmenityCategory] ?? category}
                </h4>
                <div className="grid grid-cols-1 gap-2 sm:grid-cols-2">
                  {items.map((amenity) => (
                    <AmenityChip key={amenity.id} amenity={amenity} />
                  ))}
                </div>
              </div>
            ))}
          </div>
          <div className="mt-4 flex justify-end border-t pt-3">
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={() => setShowAllDialog(false)}
              className="gap-1.5"
            >
              <X className="h-3.5 w-3.5" />
              Close
            </Button>
          </div>
        </DialogContent>
      </Dialog>
    </>
  );
}
