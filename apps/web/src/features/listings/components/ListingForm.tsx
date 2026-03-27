import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import type { AmenityDefinitionDto, AmenityCategory } from "@/api/types";
import type { SafetyDeviceDefinitionDto, ConsiderationDefinitionDto } from "@/api/types";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { Select } from "@/components/ui/select";
import { Separator } from "@/components/ui/separator";
import { FormError } from "@/components/shared/FormError";
import { DynamicIcon } from "./DynamicIcon";
import { cn } from "@/lib/utils";
import {
  listingFormSchema,
  type ListingFormValues,
  defaultListingFormValues,
} from "@/features/listings/lib/listingFormSchema";

const propertyTypeOptions = [
  "Apartment",
  "House",
  "Condo",
  "Townhouse",
  "Studio",
  "Loft",
  "Villa",
  "Cottage",
  "Cabin",
  "Other",
] as const;

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

type ListingFormProps = {
  defaultValues?: Partial<ListingFormValues>;
  onSubmit: (data: ListingFormValues) => Promise<void>;
  submitLabel: string;
  definitions: {
    amenities: AmenityDefinitionDto[];
    safetyDevices: SafetyDeviceDefinitionDto[];
    considerations: ConsiderationDefinitionDto[];
  };
};

export function ListingForm({ defaultValues, onSubmit, submitLabel, definitions }: ListingFormProps) {
  const form = useForm<ListingFormValues>({
    resolver: zodResolver(listingFormSchema),
    defaultValues: { ...defaultListingFormValues, ...defaultValues },
  });

  const handleSubmit = form.handleSubmit(async (data) => {
    await onSubmit(data);
  });

  const toggleId = (field: "amenityIds" | "safetyDeviceIds" | "considerationIds", id: string) => {
    const current = form.getValues(field);
    const next = current.includes(id) ? current.filter((x) => x !== id) : [...current, id];
    form.setValue(field, next, { shouldDirty: true });
  };

  const amenitiesByCategory = new Map<string, AmenityDefinitionDto[]>();
  for (const a of definitions.amenities) {
    const list = amenitiesByCategory.get(a.category) ?? [];
    list.push(a);
    amenitiesByCategory.set(a.category, list);
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-8">
      <Card>
        <CardHeader>
          <CardTitle className="text-lg">Basics</CardTitle>
        </CardHeader>
        <CardContent className="grid gap-4 sm:grid-cols-2">
          <Field label="Property type" error={form.formState.errors.propertyType?.message}>
            <Select {...form.register("propertyType")}>
              {propertyTypeOptions.map((pt) => (
                <option key={pt} value={pt}>
                  {pt}
                </option>
              ))}
            </Select>
          </Field>
          <Field label="Title" error={form.formState.errors.title?.message}>
            <Input placeholder="Bright 2BR near downtown" {...form.register("title")} />
          </Field>
          <div className="sm:col-span-2">
            <Field label="Description" error={form.formState.errors.description?.message}>
              <Textarea rows={6} placeholder="Describe the space, neighborhood, and what makes it a great mid-term stay..." {...form.register("description")} />
            </Field>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-lg">Rent & deposit</CardTitle>
        </CardHeader>
        <CardContent className="grid gap-4 sm:grid-cols-2">
          <Field label="Monthly rent (USD)" error={form.formState.errors.monthlyRentDollars?.message}>
            <Input
              type="number"
              step="0.01"
              min={0}
              {...form.register("monthlyRentDollars", { valueAsNumber: true })}
            />
          </Field>
          <Field label="Maximum deposit (USD)" error={form.formState.errors.maxDepositDollars?.message}>
            <Input
              type="number"
              step="0.01"
              min={0}
              {...form.register("maxDepositDollars", { valueAsNumber: true })}
            />
          </Field>
          <label className="flex items-center gap-2 text-sm sm:col-span-2 cursor-pointer">
            <input type="checkbox" {...form.register("insuranceRequired")} className="rounded border-input" />
            Tenant must carry rental insurance
          </label>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-lg">Property details</CardTitle>
        </CardHeader>
        <CardContent className="grid gap-4 sm:grid-cols-3">
          <Field label="Bedrooms" error={form.formState.errors.bedrooms?.message}>
            <Input type="number" min={0} {...form.register("bedrooms", { valueAsNumber: true })} />
          </Field>
          <Field label="Bathrooms" error={form.formState.errors.bathrooms?.message}>
            <Input type="number" step="0.5" min={0.5} {...form.register("bathrooms", { valueAsNumber: true })} />
          </Field>
          <Field label="Sq ft (optional)" error={form.formState.errors.squareFootage?.message}>
            <Input
              type="number"
              min={0}
              placeholder="—"
              {...form.register("squareFootage", {
                setValueAs: (v) => (v === "" || v === undefined ? undefined : Number(v)),
              })}
            />
          </Field>
          <Field label="Min stay (days)" error={form.formState.errors.minStayDays?.message}>
            <Input type="number" min={30} max={180} {...form.register("minStayDays", { valueAsNumber: true })} />
          </Field>
          <Field label="Max stay (days)" error={form.formState.errors.maxStayDays?.message}>
            <Input type="number" min={30} max={180} {...form.register("maxStayDays", { valueAsNumber: true })} />
          </Field>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-lg">House rules</CardTitle>
        </CardHeader>
        <CardContent className="grid gap-4 sm:grid-cols-2">
          <Field label="Check-in" error={form.formState.errors.checkInTime?.message}>
            <Input type="time" {...form.register("checkInTime")} />
          </Field>
          <Field label="Check-out" error={form.formState.errors.checkOutTime?.message}>
            <Input type="time" {...form.register("checkOutTime")} />
          </Field>
          <Field label="Max guests" error={form.formState.errors.maxGuests?.message}>
            <Input type="number" min={1} {...form.register("maxGuests", { valueAsNumber: true })} />
          </Field>
          <div className="flex flex-wrap gap-4 sm:col-span-2">
            <label className="flex items-center gap-2 text-sm cursor-pointer">
              <input type="checkbox" {...form.register("petsAllowed")} className="rounded border-input" />
              Pets allowed
            </label>
            <label className="flex items-center gap-2 text-sm cursor-pointer">
              <input type="checkbox" {...form.register("smokingAllowed")} className="rounded border-input" />
              Smoking allowed
            </label>
            <label className="flex items-center gap-2 text-sm cursor-pointer">
              <input type="checkbox" {...form.register("partiesAllowed")} className="rounded border-input" />
              Parties / events allowed
            </label>
          </div>
          <div className="sm:col-span-2">
            <Field label="Pet notes (optional)">
              <Input placeholder="Breed or size restrictions" {...form.register("petsNotes")} />
            </Field>
          </div>
          <Field label="Quiet hours start (optional)">
            <Input type="time" {...form.register("quietHoursStart")} />
          </Field>
          <Field label="Quiet hours end (optional)">
            <Input type="time" {...form.register("quietHoursEnd")} />
          </Field>
          <div className="sm:col-span-2">
            <Field label="Additional rules (optional)">
              <Textarea rows={3} {...form.register("additionalRules")} />
            </Field>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-lg">Cancellation policy</CardTitle>
        </CardHeader>
        <CardContent className="grid gap-4 sm:grid-cols-2">
          <Field label="Policy type" error={form.formState.errors.cancellationType?.message}>
            <Select {...form.register("cancellationType")}>
              <option value="Flexible">Flexible</option>
              <option value="Moderate">Moderate</option>
              <option value="Strict">Strict</option>
              <option value="NonRefundable">Non-refundable</option>
              <option value="Custom">Custom</option>
            </Select>
          </Field>
          <Field label="Free cancellation (days before)" error={form.formState.errors.freeCancellationDays?.message}>
            <Input type="number" min={0} {...form.register("freeCancellationDays", { valueAsNumber: true })} />
          </Field>
          <Field label="Partial refund % (optional)" error={form.formState.errors.partialRefundPercent?.message}>
            <Input
              type="number"
              min={0}
              max={100}
              {...form.register("partialRefundPercent", {
                setValueAs: (v) => (v === "" || v === undefined ? undefined : Number(v)),
              })}
            />
          </Field>
          <Field label="Partial refund days (optional)" error={form.formState.errors.partialRefundDays?.message}>
            <Input
              type="number"
              min={0}
              {...form.register("partialRefundDays", {
                setValueAs: (v) => (v === "" || v === undefined ? undefined : Number(v)),
              })}
            />
          </Field>
          <div className="sm:col-span-2">
            <Field label="Custom terms (optional)">
              <Textarea rows={2} {...form.register("customTerms")} />
            </Field>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-lg">Amenities</CardTitle>
        </CardHeader>
        <CardContent className="space-y-6">
          {Array.from(amenitiesByCategory.entries()).map(([category, items]) => (
            <div key={category}>
              <h4 className="text-sm font-medium text-muted-foreground mb-2">
                {categoryLabels[category as AmenityCategory] ?? category}
              </h4>
              <div className="grid grid-cols-2 gap-2 sm:grid-cols-3 lg:grid-cols-4">
                {items.map((a) => {
                  const checked = form.watch("amenityIds").includes(a.id);
                  return (
                    <button
                      key={a.id}
                      type="button"
                      onClick={() => toggleId("amenityIds", a.id)}
                      className={cn(
                        "flex items-center gap-2 rounded-lg border px-3 py-2 text-left text-sm transition-colors cursor-pointer",
                        checked ? "border-foreground bg-secondary" : "border-border hover:bg-muted/50",
                      )}
                    >
                      <DynamicIcon iconKey={a.iconKey} className="shrink-0" />
                      <span className="line-clamp-2">{a.name}</span>
                    </button>
                  );
                })}
              </div>
            </div>
          ))}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-lg">Safety devices</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-2 gap-2 sm:grid-cols-3">
            {definitions.safetyDevices.map((s) => {
              const checked = form.watch("safetyDeviceIds").includes(s.id);
              return (
                <button
                  key={s.id}
                  type="button"
                  onClick={() => toggleId("safetyDeviceIds", s.id)}
                  className={cn(
                    "flex items-center gap-2 rounded-lg border px-3 py-2 text-left text-sm cursor-pointer",
                    checked ? "border-foreground bg-secondary" : "border-border hover:bg-muted/50",
                  )}
                >
                  <DynamicIcon iconKey={s.iconKey} className="shrink-0" />
                  {s.name}
                </button>
              );
            })}
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-lg">Considerations</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-2 gap-2 sm:grid-cols-3">
            {definitions.considerations.map((c) => {
              const checked = form.watch("considerationIds").includes(c.id);
              return (
                <button
                  key={c.id}
                  type="button"
                  onClick={() => toggleId("considerationIds", c.id)}
                  className={cn(
                    "flex items-center gap-2 rounded-lg border px-3 py-2 text-left text-sm cursor-pointer",
                    checked ? "border-foreground bg-secondary" : "border-border hover:bg-muted/50",
                  )}
                >
                  <DynamicIcon iconKey={c.iconKey} className="shrink-0" />
                  {c.name}
                </button>
              );
            })}
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-lg">Booking</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <label className="flex items-center gap-2 text-sm cursor-pointer">
            <input type="checkbox" {...form.register("instantBookingEnabled")} className="rounded border-input" />
            Instant booking (tenants can book without approval)
          </label>
          <Field label="Virtual tour URL (optional)">
            <Input type="url" placeholder="https://..." {...form.register("virtualTourUrl")} />
          </Field>
        </CardContent>
      </Card>

      <Separator />

      <div className="flex justify-end gap-3">
        <Button type="submit" variant="accent" size="lg" disabled={form.formState.isSubmitting}>
          {form.formState.isSubmitting ? "Saving..." : submitLabel}
        </Button>
      </div>
    </form>
  );
}

function Field({
  label,
  error,
  children,
}: {
  label: string;
  error?: string;
  children: React.ReactNode;
}) {
  return (
    <div className="space-y-2">
      <Label>{label}</Label>
      {children}
      <FormError message={error} />
    </div>
  );
}
