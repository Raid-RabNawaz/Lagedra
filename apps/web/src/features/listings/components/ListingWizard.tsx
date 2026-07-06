import { useMemo, useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import {
  Building2,
  DollarSign,
  Sparkles,
  ScrollText,
  CalendarX,
  CheckCircle2,
  ChevronLeft,
  ChevronRight,
  Home,
  Check,
} from "lucide-react";
import type {
  AmenityCategory,
  AmenityDefinitionDto,
  ConsiderationDefinitionDto,
  SafetyDeviceDefinitionDto,
} from "@/api/types";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
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

type Step = {
  id: string;
  label: string;
  shortLabel: string;
  icon: typeof Home;
  description: string;
  fields: (keyof ListingFormValues)[];
};

const STEPS: Step[] = [
  {
    id: "basics",
    label: "Basics",
    shortLabel: "Basics",
    icon: Home,
    description: "Property type, title and a clear description.",
    fields: ["propertyType", "title", "description"],
  },
  {
    id: "property",
    label: "Property details",
    shortLabel: "Property",
    icon: Building2,
    description: "Capacity, size and how long renters can stay.",
    fields: ["bedrooms", "bathrooms", "squareFootage", "minStayDays", "maxStayDays"],
  },
  {
    id: "pricing",
    label: "Pricing & deposit",
    shortLabel: "Pricing",
    icon: DollarSign,
    description: "Monthly rent and predetermined deposits.",
    fields: [
      "monthlyRentDollars",
      "maxDepositDollars",
      "depositUnverifiedDollars",
      "depositBackgroundVerifiedDollars",
      "depositPartnerGuaranteedDollars",
    ],
  },
  {
    id: "amenities",
    label: "Amenities & safety",
    shortLabel: "Amenities",
    icon: Sparkles,
    description: "Pick what's included, safety devices and considerations.",
    fields: ["amenityIds", "safetyDeviceIds", "considerationIds"],
  },
  {
    id: "rules",
    label: "House rules",
    shortLabel: "Rules",
    icon: ScrollText,
    description: "Check-in window, pets, smoking and any extra notes.",
    fields: [
      "checkInTime",
      "checkOutTime",
      "maxGuests",
      "petsAllowed",
      "petsNotes",
      "smokingAllowed",
      "partiesAllowed",
      "quietHoursStart",
      "quietHoursEnd",
      "additionalRules",
    ],
  },
  {
    id: "cancellation",
    label: "Cancellation policy",
    shortLabel: "Cancellation",
    icon: CalendarX,
    description: "Choose how flexible cancellations should be.",
    fields: [
      "cancellationType",
      "freeCancellationDays",
      "partialRefundPercent",
      "partialRefundDays",
      "customTerms",
    ],
  },
  {
    id: "review",
    label: "Booking & review",
    shortLabel: "Review",
    icon: CheckCircle2,
    description: "Review everything and create your listing.",
    fields: ["instantBookingEnabled", "virtualTourUrl"],
  },
];

type ListingWizardProps = {
  defaultValues?: Partial<ListingFormValues>;
  onSubmit: (data: ListingFormValues) => Promise<void>;
  submitLabel?: string;
  definitions: {
    amenities: AmenityDefinitionDto[];
    safetyDevices: SafetyDeviceDefinitionDto[];
    considerations: ConsiderationDefinitionDto[];
  };
};

export function ListingWizard({
  defaultValues,
  onSubmit,
  submitLabel = "Create listing",
  definitions,
}: ListingWizardProps) {
  const form = useForm<ListingFormValues>({
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    resolver: zodResolver(listingFormSchema) as any,
    defaultValues: { ...defaultListingFormValues, ...defaultValues },
    mode: "onBlur",
  });

  const [stepIndex, setStepIndex] = useState(0);
  const [furthestVisited, setFurthestVisited] = useState(0);

  const step = STEPS[stepIndex];

  const handleNext = async () => {
    const fields = step.fields;
    const valid = await form.trigger(fields);
    if (!valid) return;
    if (stepIndex < STEPS.length - 1) {
      const next = stepIndex + 1;
      setStepIndex(next);
      setFurthestVisited((f) => Math.max(f, next));
      window.scrollTo({ top: 0, behavior: "smooth" });
    }
  };

  const goTo = (idx: number) => {
    if (idx <= furthestVisited) {
      setStepIndex(idx);
      window.scrollTo({ top: 0, behavior: "smooth" });
    }
  };

  const handleSubmit = form.handleSubmit(async (data: ListingFormValues) => {
    await onSubmit(data);
  });

  const toggleId = (
    field: "amenityIds" | "safetyDeviceIds" | "considerationIds",
    id: string,
  ) => {
    const current = form.getValues(field);
    const next = current.includes(id) ? current.filter((x) => x !== id) : [...current, id];
    form.setValue(field, next, { shouldDirty: true });
  };

  return (
    <div className="space-y-6">
      <StepHeader
        steps={STEPS}
        current={stepIndex}
        furthestVisited={furthestVisited}
        onJump={goTo}
      />

      <form
        onSubmit={(e) => {
          if (stepIndex < STEPS.length - 1) {
            e.preventDefault();
            void handleNext();
            return;
          }
          void handleSubmit(e);
        }}
        className="space-y-6"
      >
        <Card>
          <CardHeader>
            <CardTitle className="text-xl flex items-center gap-2">
              <step.icon className="h-5 w-5" />
              {step.label}
            </CardTitle>
            <CardDescription>{step.description}</CardDescription>
          </CardHeader>
          <CardContent className="space-y-6">
            {step.id === "basics" && <BasicsStep form={form} />}
            {step.id === "property" && <PropertyStep form={form} />}
            {step.id === "pricing" && <PricingStep form={form} />}
            {step.id === "amenities" && (
              <AmenitiesStep form={form} definitions={definitions} toggleId={toggleId} />
            )}
            {step.id === "rules" && <RulesStep form={form} />}
            {step.id === "cancellation" && <CancellationStep form={form} />}
            {step.id === "review" && (
              <ReviewStep form={form} definitions={definitions} onJump={(i) => setStepIndex(i)} />
            )}
          </CardContent>
        </Card>

        <div className="flex flex-col-reverse gap-3 sm:flex-row sm:items-center sm:justify-between">
          <Button
            type="button"
            variant="outline"
            disabled={stepIndex === 0}
            onClick={() => {
              if (stepIndex > 0) setStepIndex(stepIndex - 1);
            }}
          >
            <ChevronLeft className="h-4 w-4" />
            Back
          </Button>

          <div className="flex items-center gap-3">
            <p className="text-xs text-muted-foreground">
              Step {stepIndex + 1} of {STEPS.length}
            </p>
            {stepIndex < STEPS.length - 1 ? (
              <Button type="submit" variant="default">
                Next
                <ChevronRight className="h-4 w-4" />
              </Button>
            ) : (
              <Button type="submit" variant="accent" disabled={form.formState.isSubmitting}>
                <Check className="h-4 w-4" />
                {form.formState.isSubmitting ? "Creating..." : submitLabel}
              </Button>
            )}
          </div>
        </div>
      </form>
    </div>
  );
}

// ?? Step header ???????????????????????????????????????????????

function StepHeader({
  steps,
  current,
  furthestVisited,
  onJump,
}: {
  steps: Step[];
  current: number;
  furthestVisited: number;
  onJump: (idx: number) => void;
}) {
  const progress = ((current + 1) / steps.length) * 100;

  return (
    <div className="space-y-3">
      <div className="h-1.5 w-full overflow-hidden rounded-full bg-muted">
        <div
          className="h-full bg-accent transition-[width] duration-500"
          style={{ width: `${progress}%` }}
        />
      </div>

      <ol className="grid grid-cols-3 gap-1.5 sm:grid-cols-7">
        {steps.map((s, i) => {
          const isDone = i < current;
          const isActive = i === current;
          const isReachable = i <= furthestVisited;
          return (
            <li key={s.id}>
              <button
                type="button"
                onClick={() => onJump(i)}
                disabled={!isReachable}
                className={cn(
                  "flex w-full flex-col items-start gap-1 rounded-lg border px-3 py-2 text-left transition-colors",
                  isActive && "border-foreground bg-secondary",
                  isDone && !isActive && "border-success/40 bg-success/5",
                  !isReachable && "opacity-50 cursor-not-allowed",
                  isReachable && !isActive && "hover:bg-muted/50 cursor-pointer",
                )}
              >
                <div className="flex items-center gap-1.5">
                  <span
                    className={cn(
                      "flex h-5 w-5 items-center justify-center rounded-full text-[10px] font-semibold",
                      isActive
                        ? "bg-foreground text-background"
                        : isDone
                          ? "bg-success text-background"
                          : "bg-muted text-muted-foreground",
                    )}
                  >
                    {isDone ? <Check className="h-3 w-3" /> : i + 1}
                  </span>
                  <s.icon className="h-3.5 w-3.5 text-muted-foreground" />
                </div>
                <span
                  className={cn(
                    "text-xs font-medium leading-tight",
                    isActive ? "text-foreground" : "text-muted-foreground",
                  )}
                >
                  {s.shortLabel}
                </span>
              </button>
            </li>
          );
        })}
      </ol>
    </div>
  );
}

// ?? Step bodies ??????????????????????????????????????????????

type StepFormProps = { form: ReturnType<typeof useForm<ListingFormValues>> };

function BasicsStep({ form }: StepFormProps) {
  return (
    <div className="grid gap-4 sm:grid-cols-2">
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
        <p className="text-[11px] text-muted-foreground">
          Make it descriptive � renters search by these words.
        </p>
      </Field>
      <div className="sm:col-span-2">
        <Field label="Description" error={form.formState.errors.description?.message}>
          <Textarea
            rows={6}
            placeholder="Describe the space, neighborhood, what makes it a great mid-term stay..."
            {...form.register("description")}
          />
          <p className="text-[11px] text-muted-foreground">
            At least 50 characters. Include layout, vibe, transit and any standout amenities.
          </p>
        </Field>
      </div>
    </div>
  );
}

function PropertyStep({ form }: StepFormProps) {
  return (
    <div className="grid gap-4 sm:grid-cols-3">
      <Field label="Bedrooms" error={form.formState.errors.bedrooms?.message}>
        <Input type="number" min={0} {...form.register("bedrooms", { valueAsNumber: true })} />
      </Field>
      <Field label="Bathrooms" error={form.formState.errors.bathrooms?.message}>
        <Input
          type="number"
          step="0.5"
          min={0.5}
          {...form.register("bathrooms", { valueAsNumber: true })}
        />
      </Field>
      <Field label="Sq ft (optional)" error={form.formState.errors.squareFootage?.message}>
        <Input
          type="number"
          min={0}
          placeholder="�"
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
      <div className="rounded-lg border bg-muted/30 p-3 text-xs text-muted-foreground self-end">
        Stay length must be between 30 and 180 days for mid-term rentals.
      </div>
    </div>
  );
}

function PricingStep({ form }: StepFormProps) {
  return (
    <div className="grid gap-4 sm:grid-cols-2">
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
        <p className="text-[11px] text-muted-foreground">
          The upper limit. Tenants never pay more than this.
        </p>
      </Field>
      <div className="sm:col-span-2 rounded-lg border bg-muted/30 p-3 text-xs text-muted-foreground">
        Set the deposit each tenant pays based on their verification level. The
        system charges the matching amount automatically when a tenant requests
        to book — you no longer enter a deposit at approval. Leave a field blank
        to fall back to the maximum deposit.
      </div>
      <Field
        label="Deposit — unverified tenant (USD)"
        error={form.formState.errors.depositUnverifiedDollars?.message}
      >
        <Input
          type="number"
          step="0.01"
          min={0}
          {...form.register("depositUnverifiedDollars", { valueAsNumber: true })}
        />
        <p className="text-[11px] text-muted-foreground">
          Typically the full maximum deposit.
        </p>
      </Field>
      <Field
        label="Deposit — background-verified tenant (USD)"
        error={form.formState.errors.depositBackgroundVerifiedDollars?.message}
      >
        <Input
          type="number"
          step="0.01"
          min={0}
          {...form.register("depositBackgroundVerifiedDollars", { valueAsNumber: true })}
        />
        <p className="text-[11px] text-muted-foreground">
          A reduced deposit (e.g. 10–20% less) rewards verified tenants.
        </p>
      </Field>
      <Field
        label="Deposit — partner-guaranteed tenant (USD)"
        error={form.formState.errors.depositPartnerGuaranteedDollars?.message}
      >
        <Input
          type="number"
          step="0.01"
          min={0}
          {...form.register("depositPartnerGuaranteedDollars", { valueAsNumber: true })}
        />
        <p className="text-[11px] text-muted-foreground">
          Much lower (e.g. under 50% of max) since a partner backs the tenant.
        </p>
      </Field>
    </div>
  );
}

function AmenitiesStep({
  form,
  definitions,
  toggleId,
}: StepFormProps & {
  definitions: ListingWizardProps["definitions"];
  toggleId: (
    field: "amenityIds" | "safetyDeviceIds" | "considerationIds",
    id: string,
  ) => void;
}) {
  const amenitiesByCategory = useMemo(() => {
    const map = new Map<string, AmenityDefinitionDto[]>();
    for (const a of definitions.amenities) {
      const list = map.get(a.category) ?? [];
      list.push(a);
      map.set(a.category, list);
    }
    return map;
  }, [definitions.amenities]);

  const selectedAmenityIds = form.watch("amenityIds");
  const selectedSafetyIds = form.watch("safetyDeviceIds");
  const selectedConsiderationIds = form.watch("considerationIds");

  return (
    <div className="space-y-8">
      <section>
        <SectionHeader title="Amenities" count={selectedAmenityIds.length} />
        <div className="space-y-5">
          {Array.from(amenitiesByCategory.entries()).map(([category, items]) => (
            <div key={category}>
              <h4 className="mb-2 text-xs font-medium uppercase tracking-wide text-muted-foreground">
                {categoryLabels[category as AmenityCategory] ?? category}
              </h4>
              <div className="grid grid-cols-2 gap-2 sm:grid-cols-3 lg:grid-cols-4">
                {items.map((a) => {
                  const checked = selectedAmenityIds.includes(a.id);
                  return (
                    <ToggleChip
                      key={a.id}
                      checked={checked}
                      iconKey={a.iconKey}
                      label={a.name}
                      onClick={() => toggleId("amenityIds", a.id)}
                    />
                  );
                })}
              </div>
            </div>
          ))}
        </div>
      </section>

      <Separator />

      <section>
        <SectionHeader title="Safety devices" count={selectedSafetyIds.length} />
        <div className="grid grid-cols-2 gap-2 sm:grid-cols-3">
          {definitions.safetyDevices.map((s) => {
            const checked = selectedSafetyIds.includes(s.id);
            return (
              <ToggleChip
                key={s.id}
                checked={checked}
                iconKey={s.iconKey}
                label={s.name}
                onClick={() => toggleId("safetyDeviceIds", s.id)}
              />
            );
          })}
        </div>
      </section>

      <Separator />

      <section>
        <SectionHeader title="Considerations" count={selectedConsiderationIds.length} />
        <div className="grid grid-cols-2 gap-2 sm:grid-cols-3">
          {definitions.considerations.map((c) => {
            const checked = selectedConsiderationIds.includes(c.id);
            return (
              <ToggleChip
                key={c.id}
                checked={checked}
                iconKey={c.iconKey}
                label={c.name}
                onClick={() => toggleId("considerationIds", c.id)}
              />
            );
          })}
        </div>
      </section>
    </div>
  );
}

function RulesStep({ form }: StepFormProps) {
  return (
    <div className="grid gap-4 sm:grid-cols-2">
      <Field label="Check-in" error={form.formState.errors.checkInTime?.message}>
        <Input type="time" {...form.register("checkInTime")} />
      </Field>
      <Field label="Check-out" error={form.formState.errors.checkOutTime?.message}>
        <Input type="time" {...form.register("checkOutTime")} />
      </Field>
      <Field label="Max guests" error={form.formState.errors.maxGuests?.message}>
        <Input type="number" min={1} {...form.register("maxGuests", { valueAsNumber: true })} />
      </Field>
      <Field label="Quiet hours start (optional)">
        <Input type="time" {...form.register("quietHoursStart")} />
      </Field>
      <Field label="Quiet hours end (optional)">
        <Input type="time" {...form.register("quietHoursEnd")} />
      </Field>

      <div className="sm:col-span-2 grid gap-2 sm:grid-cols-3">
        <label className="flex items-start gap-2 rounded-lg border p-3 cursor-pointer hover:bg-muted/30 transition-colors">
          <input type="checkbox" {...form.register("petsAllowed")} className="mt-0.5 rounded border-input" />
          <div>
            <p className="text-sm font-medium">Pets allowed</p>
            <p className="text-xs text-muted-foreground">Renters can bring pets.</p>
          </div>
        </label>
        <label className="flex items-start gap-2 rounded-lg border p-3 cursor-pointer hover:bg-muted/30 transition-colors">
          <input
            type="checkbox"
            {...form.register("smokingAllowed")}
            className="mt-0.5 rounded border-input"
          />
          <div>
            <p className="text-sm font-medium">Smoking allowed</p>
            <p className="text-xs text-muted-foreground">Inside the property.</p>
          </div>
        </label>
        <label className="flex items-start gap-2 rounded-lg border p-3 cursor-pointer hover:bg-muted/30 transition-colors">
          <input
            type="checkbox"
            {...form.register("partiesAllowed")}
            className="mt-0.5 rounded border-input"
          />
          <div>
            <p className="text-sm font-medium">Parties allowed</p>
            <p className="text-xs text-muted-foreground">Events with invited guests.</p>
          </div>
        </label>
      </div>

      <div className="sm:col-span-2">
        <Field label="Pet notes (optional)">
          <Input
            placeholder="Breed or size restrictions"
            {...form.register("petsNotes")}
          />
        </Field>
      </div>

      <div className="sm:col-span-2">
        <Field label="Additional rules (optional)">
          <Textarea rows={3} {...form.register("additionalRules")} />
        </Field>
      </div>
    </div>
  );
}

function CancellationStep({ form }: StepFormProps) {
  const cancellationType = form.watch("cancellationType");
  return (
    <div className="grid gap-4 sm:grid-cols-2">
      <Field label="Policy type" error={form.formState.errors.cancellationType?.message}>
        <Select {...form.register("cancellationType")}>
          <option value="Flexible">Flexible</option>
          <option value="Moderate">Moderate</option>
          <option value="Strict">Strict</option>
          <option value="NonRefundable">Non-refundable</option>
          <option value="Custom">Custom</option>
        </Select>
      </Field>
      <Field
        label="Free cancellation (days before)"
        error={form.formState.errors.freeCancellationDays?.message}
      >
        <Input
          type="number"
          min={0}
          {...form.register("freeCancellationDays", { valueAsNumber: true })}
        />
      </Field>
      <Field
        label="Partial refund % (optional)"
        error={form.formState.errors.partialRefundPercent?.message}
      >
        <Input
          type="number"
          min={0}
          max={100}
          {...form.register("partialRefundPercent", {
            setValueAs: (v) => (v === "" || v === undefined ? undefined : Number(v)),
          })}
        />
      </Field>
      <Field
        label="Partial refund window (days)"
        error={form.formState.errors.partialRefundDays?.message}
      >
        <Input
          type="number"
          min={0}
          {...form.register("partialRefundDays", {
            setValueAs: (v) => (v === "" || v === undefined ? undefined : Number(v)),
          })}
        />
      </Field>
      {cancellationType === "Custom" && (
        <div className="sm:col-span-2">
          <Field label="Custom terms">
            <Textarea
              rows={3}
              placeholder="Spell out any custom cancellation rules..."
              {...form.register("customTerms")}
            />
          </Field>
        </div>
      )}
    </div>
  );
}

function ReviewStep({
  form,
  definitions,
  onJump,
}: StepFormProps & {
  definitions: ListingWizardProps["definitions"];
  onJump: (idx: number) => void;
}) {
  const v = form.watch();
  const amenityNames = v.amenityIds
    .map((id) => definitions.amenities.find((a) => a.id === id)?.name)
    .filter(Boolean) as string[];
  const safetyNames = v.safetyDeviceIds
    .map((id) => definitions.safetyDevices.find((a) => a.id === id)?.name)
    .filter(Boolean) as string[];
  const considerationNames = v.considerationIds
    .map((id) => definitions.considerations.find((a) => a.id === id)?.name)
    .filter(Boolean) as string[];

  return (
    <div className="space-y-6">
      <div className="grid gap-4 sm:grid-cols-2">
        <label className="flex items-start gap-3 rounded-lg border p-3 cursor-pointer hover:bg-muted/30 transition-colors">
          <input
            type="checkbox"
            {...form.register("instantBookingEnabled")}
            className="mt-1 rounded border-input"
          />
          <div>
            <p className="text-sm font-medium">Instant booking</p>
            <p className="text-xs text-muted-foreground">
              Tenants can book without your approval.
            </p>
          </div>
        </label>
        <Field label="Virtual tour URL (optional)">
          <Input
            type="url"
            placeholder="https://..."
            {...form.register("virtualTourUrl")}
          />
        </Field>
      </div>

      <Separator />

      <div className="space-y-4">
        <h3 className="text-base font-semibold">Listing summary</h3>

        <ReviewSection title="Basics" onEdit={() => onJump(0)}>
          <ReviewRow label="Property type" value={v.propertyType} />
          <ReviewRow label="Title" value={v.title || "�"} />
          <ReviewRow
            label="Description"
            value={v.description ? `${v.description.slice(0, 120)}${v.description.length > 120 ? "�" : ""}` : "�"}
          />
        </ReviewSection>

        <ReviewSection title="Property" onEdit={() => onJump(1)}>
          <ReviewRow
            label="Bedrooms"
            value={v.bedrooms === 0 ? "Studio" : v.bedrooms.toString()}
          />
          <ReviewRow label="Bathrooms" value={v.bathrooms.toString()} />
          {v.squareFootage != null && (
            <ReviewRow label="Square footage" value={`${v.squareFootage.toLocaleString()} sq ft`} />
          )}
          <ReviewRow label="Stay length" value={`${v.minStayDays}�${v.maxStayDays} days`} />
        </ReviewSection>

        <ReviewSection title="Pricing" onEdit={() => onJump(2)}>
          <ReviewRow label="Monthly rent" value={`$${v.monthlyRentDollars.toLocaleString()}`} />
          <ReviewRow label="Max deposit" value={`$${v.maxDepositDollars.toLocaleString()}`} />
          <ReviewRow
            label="Deposit — unverified"
            value={
              v.depositUnverifiedDollars != null
                ? `$${v.depositUnverifiedDollars.toLocaleString()}`
                : "Max deposit"
            }
          />
          <ReviewRow
            label="Deposit — verified"
            value={
              v.depositBackgroundVerifiedDollars != null
                ? `$${v.depositBackgroundVerifiedDollars.toLocaleString()}`
                : "Max deposit"
            }
          />
          <ReviewRow
            label="Deposit — partner-guaranteed"
            value={
              v.depositPartnerGuaranteedDollars != null
                ? `$${v.depositPartnerGuaranteedDollars.toLocaleString()}`
                : "Max deposit"
            }
          />
        </ReviewSection>

        <ReviewSection title="Amenities & safety" onEdit={() => onJump(3)}>
          <ReviewRow
            label="Amenities"
            value={
              amenityNames.length === 0
                ? "None"
                : `${amenityNames.length} (${amenityNames.slice(0, 4).join(", ")}${amenityNames.length > 4 ? ", �" : ""})`
            }
          />
          <ReviewRow
            label="Safety devices"
            value={
              safetyNames.length === 0
                ? "None"
                : `${safetyNames.length} (${safetyNames.slice(0, 4).join(", ")}${safetyNames.length > 4 ? ", �" : ""})`
            }
          />
          <ReviewRow
            label="Considerations"
            value={
              considerationNames.length === 0
                ? "None"
                : `${considerationNames.length} (${considerationNames.slice(0, 4).join(", ")}${considerationNames.length > 4 ? ", �" : ""})`
            }
          />
        </ReviewSection>

        <ReviewSection title="Rules" onEdit={() => onJump(4)}>
          <ReviewRow label="Check-in" value={v.checkInTime} />
          <ReviewRow label="Check-out" value={v.checkOutTime} />
          <ReviewRow label="Max guests" value={v.maxGuests.toString()} />
          <ReviewRow label="Pets" value={v.petsAllowed ? "Allowed" : "Not allowed"} />
          <ReviewRow label="Smoking" value={v.smokingAllowed ? "Allowed" : "Not allowed"} />
          <ReviewRow label="Parties" value={v.partiesAllowed ? "Allowed" : "Not allowed"} />
        </ReviewSection>

        <ReviewSection title="Cancellation" onEdit={() => onJump(5)}>
          <ReviewRow label="Policy" value={v.cancellationType} />
          <ReviewRow
            label="Free cancellation"
            value={`${v.freeCancellationDays} days before stay`}
          />
        </ReviewSection>
      </div>

      <div className="rounded-lg border-2 border-dashed border-accent/40 bg-accent/5 p-4">
        <p className="text-sm">
          <strong>Almost done.</strong> After creating your listing, you'll set the map location
          and upload photos before publishing it to the marketplace.
        </p>
      </div>
    </div>
  );
}

// ?? Reusable bits ?????????????????????????????????????????????

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

function SectionHeader({ title, count }: { title: string; count: number }) {
  return (
    <div className="mb-3 flex items-center justify-between">
      <h3 className="text-base font-semibold">{title}</h3>
      <span className="rounded-full bg-muted px-2 py-0.5 text-xs text-muted-foreground tabular-nums">
        {count} selected
      </span>
    </div>
  );
}

function ToggleChip({
  checked,
  iconKey,
  label,
  onClick,
}: {
  checked: boolean;
  iconKey: string;
  label: string;
  onClick: () => void;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={cn(
        "flex items-center gap-2 rounded-lg border px-3 py-2 text-left text-sm transition-colors cursor-pointer",
        checked ? "border-foreground bg-secondary" : "border-border hover:bg-muted/50",
      )}
    >
      <DynamicIcon iconKey={iconKey} className="shrink-0" />
      <span className="line-clamp-2">{label}</span>
    </button>
  );
}

function ReviewSection({
  title,
  children,
  onEdit,
}: {
  title: string;
  children: React.ReactNode;
  onEdit: () => void;
}) {
  return (
    <div className="rounded-lg border bg-muted/20 p-3">
      <div className="mb-2 flex items-center justify-between">
        <h4 className="text-sm font-medium">{title}</h4>
        <button
          type="button"
          onClick={onEdit}
          className="text-xs text-muted-foreground hover:text-foreground underline cursor-pointer"
        >
          Edit
        </button>
      </div>
      <dl className="space-y-1 text-sm">{children}</dl>
    </div>
  );
}

function ReviewRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex items-baseline justify-between gap-4 text-sm">
      <dt className="text-xs text-muted-foreground">{label}</dt>
      <dd className="text-right font-medium truncate max-w-[60%]">{value}</dd>
    </div>
  );
}
