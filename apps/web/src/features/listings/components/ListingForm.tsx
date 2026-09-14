import { useForm, useWatch } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import type { AmenityDefinitionDto, AmenityCategory, ListingDetailsDto } from "@/api/types";
import type { SafetyDeviceDefinitionDto, ConsiderationDefinitionDto } from "@/api/types";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { FormNumberInput } from "@/components/ui/number-input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { Select } from "@/components/ui/select";
import { Separator } from "@/components/ui/separator";
import { FormError } from "@/components/shared/FormError";
import { DynamicIcon } from "./DynamicIcon";
import { ListingLeaseAgreementEditor } from "./ListingLeaseAgreementEditor";
import { ListingOwnershipFields } from "./ListingOwnershipFields";
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
  /** The listing being edited; the lease agreement card uploads against it. */
  listing: ListingDetailsDto;
  /** When true, fields and submit are disabled (e.g. InReview / Closed). */
  readOnly?: boolean;
  definitions: {
    amenities: AmenityDefinitionDto[];
    safetyDevices: SafetyDeviceDefinitionDto[];
    considerations: ConsiderationDefinitionDto[];
  };
};

export function ListingForm({
  defaultValues,
  onSubmit,
  submitLabel,
  listing,
  definitions,
  readOnly = false,
}: ListingFormProps) {
  const form = useForm<ListingFormValues>({
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    resolver: zodResolver(listingFormSchema) as any,
    defaultValues: { ...defaultListingFormValues, ...defaultValues },
  });

  const handleSubmit = form.handleSubmit(async (data: ListingFormValues) => {
    await onSubmit(data);
  });

  // Subscribe to id-array changes via the React Compiler-friendly `useWatch`
  // hook so the toggle grids below re-render when individual chips flip.
  const amenityIds = useWatch({ control: form.control, name: "amenityIds" }) ?? [];
  const safetyDeviceIds = useWatch({ control: form.control, name: "safetyDeviceIds" }) ?? [];
  const considerationIds = useWatch({ control: form.control, name: "considerationIds" }) ?? [];
  const usesOwnLease =
    useWatch({ control: form.control, name: "leaseAgreementSource" }) === "HostProvided";

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
      <fieldset disabled={readOnly} className="min-w-0 space-y-8 border-0 p-0 m-0">
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

      <Card id="ownership" className="scroll-mt-24">
        <CardHeader>
          <CardTitle className="text-lg">Ownership & lease parties</CardTitle>
        </CardHeader>
        <CardContent>
          <ListingOwnershipFields form={form} showBrokerClause />
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-lg">Rent & deposit</CardTitle>
        </CardHeader>
        <CardContent className="grid gap-4 sm:grid-cols-2">
          <Field label="Monthly rent (USD)" error={form.formState.errors.monthlyRentDollars?.message}>
            <FormNumberInput
              control={form.control}
              name="monthlyRentDollars"
              min={0}
              decimalPlaces={2}
            />
          </Field>
          <Field label="Maximum deposit (USD)" error={form.formState.errors.maxDepositDollars?.message}>
            <FormNumberInput
              control={form.control}
              name="maxDepositDollars"
              min={0}
              decimalPlaces={2}
            />
          </Field>
          <div className="sm:col-span-2">
            <Field
              label="Default deposit for instant book (USD, optional)"
              error={form.formState.errors.defaultDepositDollars?.message}
            >
              <FormNumberInput
                control={form.control}
                name="defaultDepositDollars"
                min={0}
                decimalPlaces={2}
                placeholder="Leave blank to use the maximum deposit"
              />
              <p className="text-xs text-muted-foreground">
                Used when a guest books instantly. Cannot exceed the maximum deposit.
              </p>
            </Field>
          </div>
          <div className="sm:col-span-2 rounded-lg border bg-muted/30 p-3 text-xs text-muted-foreground">
            Predetermined deposits by verification level. The system charges the
            matching amount automatically when a tenant requests to book — you no
            longer enter a deposit at approval. Leave a field blank to fall back
            to the maximum deposit.
          </div>
          <Field
            label="Deposit — unverified tenant (USD)"
            error={form.formState.errors.depositUnverifiedDollars?.message}
          >
            <FormNumberInput
              control={form.control}
              name="depositUnverifiedDollars"
              min={0}
              decimalPlaces={2}
              placeholder="Falls back to maximum deposit"
            />
          </Field>
          <Field
            label="Deposit — Trusted tenant (USD)"
            error={form.formState.errors.depositBackgroundVerifiedDollars?.message}
          >
            <FormNumberInput
              control={form.control}
              name="depositBackgroundVerifiedDollars"
              min={0}
              decimalPlaces={2}
              placeholder="Reduced deposit for Trusted guests"
            />
          </Field>
          <Field
            label="Deposit — Elite tenant (USD)"
            error={form.formState.errors.depositPartnerGuaranteedDollars?.message}
          >
            <FormNumberInput
              control={form.control}
              name="depositPartnerGuaranteedDollars"
              min={0}
              decimalPlaces={2}
              placeholder="Lowest band — Elite guests with partner backing"
            />
          </Field>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-lg">Property details</CardTitle>
        </CardHeader>
        <CardContent className="grid gap-4 sm:grid-cols-3">
          <Field label="Bedrooms" error={form.formState.errors.bedrooms?.message}>
            <FormNumberInput control={form.control} name="bedrooms" min={0} decimalPlaces={0} />
          </Field>
          <Field label="Bathrooms" error={form.formState.errors.bathrooms?.message}>
            <FormNumberInput control={form.control} name="bathrooms" min={0.5} decimalPlaces={1} />
          </Field>
          <Field label="Sq ft (optional)" error={form.formState.errors.squareFootage?.message}>
            <FormNumberInput
              control={form.control}
              name="squareFootage"
              min={0}
              decimalPlaces={0}
              placeholder="—"
            />
          </Field>
          <Field label="Min stay (days)" error={form.formState.errors.minStayDays?.message}>
            <FormNumberInput control={form.control} name="minStayDays" min={30} max={180} decimalPlaces={0} />
          </Field>
          <Field label="Max stay (days)" error={form.formState.errors.maxStayDays?.message}>
            <FormNumberInput control={form.control} name="maxStayDays" min={30} max={180} decimalPlaces={0} />
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
            <FormNumberInput control={form.control} name="maxGuests" min={1} decimalPlaces={0} />
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
            <Field label="Leaving instructions (optional)">
              <Textarea
                rows={3}
                placeholder="What tenants should do at move-out (keys, cleaning, trash...)"
                {...form.register("leavingInstructions")}
              />
            </Field>
          </div>
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
            <FormNumberInput control={form.control} name="freeCancellationDays" min={0} decimalPlaces={0} />
          </Field>
          <Field label="Partial refund % (optional)" error={form.formState.errors.partialRefundPercent?.message}>
            <FormNumberInput
              control={form.control}
              name="partialRefundPercent"
              min={0}
              max={100}
              decimalPlaces={0}
            />
          </Field>
          <Field label="Partial refund days (optional)" error={form.formState.errors.partialRefundDays?.message}>
            <FormNumberInput
              control={form.control}
              name="partialRefundDays"
              min={0}
              decimalPlaces={0}
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
          <CardTitle className="text-lg">Lease agreement</CardTitle>
        </CardHeader>
        <CardContent>
          <ListingLeaseAgreementEditor form={form} listing={listing} />
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-lg">Lease terms</CardTitle>
        </CardHeader>
        <CardContent className="space-y-6">
          <p className="rounded-lg border bg-muted/30 p-3 text-xs text-muted-foreground">
            {usesOwnLease ? (
              <>
                You&apos;re using your own lease agreement, so these terms are
                not merged into it. They still drive the lease preview
                fallback and stay saved if you switch back to Lagedra&apos;s
                standard lease.
              </>
            ) : (
              <>
                These terms are merged into the lease agreement generated when a
                deal is confirmed. Sensible defaults are pre-filled — review them
                so your lease matches how you actually run the property.
              </>
            )}
          </p>

          <div className="grid gap-4 sm:grid-cols-2">
            <Field
              label="Rent due day of month"
              error={form.formState.errors.rentDueDayOfMonth?.message}
            >
              <FormNumberInput
                control={form.control}
                name="rentDueDayOfMonth"
                min={1}
                max={28}
                decimalPlaces={0}
              />
              <p className="text-xs text-muted-foreground">Between the 1st and the 28th.</p>
            </Field>
            <Field label="Accepted payment methods (optional)">
              <Input
                placeholder="e.g. ACH transfer, check, money order"
                {...form.register("paymentMethods")}
              />
            </Field>
            <Field
              label="Late fee (% of rent)"
              error={form.formState.errors.lateFeePercent?.message}
            >
              <FormNumberInput
                control={form.control}
                name="lateFeePercent"
                min={0}
                max={100}
                decimalPlaces={1}
              />
            </Field>
            <Field
              label="Late fee grace period (days)"
              error={form.formState.errors.lateFeeGraceDays?.message}
            >
              <FormNumberInput
                control={form.control}
                name="lateFeeGraceDays"
                min={0}
                decimalPlaces={0}
              />
            </Field>
            <Field
              label="Returned payment fee — first (USD)"
              error={form.formState.errors.nsfFirstFeeDollars?.message}
            >
              <FormNumberInput
                control={form.control}
                name="nsfFirstFeeDollars"
                min={0}
                decimalPlaces={2}
              />
            </Field>
            <Field
              label="Returned payment fee — subsequent (USD)"
              error={form.formState.errors.nsfSubsequentFeeDollars?.message}
            >
              <FormNumberInput
                control={form.control}
                name="nsfSubsequentFeeDollars"
                min={0}
                decimalPlaces={2}
              />
            </Field>
          </div>

          <Separator />

          <div className="grid gap-4 sm:grid-cols-2">
            <div className="flex flex-wrap gap-4 sm:col-span-2">
              <label className="flex items-center gap-2 text-sm cursor-pointer">
                <input type="checkbox" {...form.register("furnished")} className="rounded border-input" />
                Furnished
              </label>
              <label className="flex items-center gap-2 text-sm cursor-pointer">
                <input
                  type="checkbox"
                  {...form.register("yardMaintenanceByTenant")}
                  className="rounded border-input"
                />
                Tenant handles yard maintenance
              </label>
            </div>
            <div className="sm:col-span-2">
              <Field label="Utilities responsibility (optional)">
                <Input
                  placeholder="e.g. Tenant pays all utilities except water and trash"
                  {...form.register("utilitiesResponsibility")}
                />
              </Field>
            </div>
            <div className="sm:col-span-2">
              <Field label="Included appliances (optional)">
                <Input
                  placeholder="e.g. Refrigerator, washer, dryer, dishwasher"
                  {...form.register("includedAppliancesNotes")}
                />
                <p className="text-xs text-muted-foreground">
                  Leave blank to list your selected amenities in the lease.
                </p>
              </Field>
            </div>
          </div>

          <Separator />

          <div className="grid gap-4 sm:grid-cols-2">
            <Field label="Keys provided" error={form.formState.errors.keyCount?.message}>
              <FormNumberInput control={form.control} name="keyCount" min={0} decimalPlaces={0} />
            </Field>
            <Field
              label="Mailbox keys provided"
              error={form.formState.errors.mailboxKeyCount?.message}
            >
              <FormNumberInput
                control={form.control}
                name="mailboxKeyCount"
                min={0}
                decimalPlaces={0}
              />
            </Field>
            <Field
              label="Key replacement fee (USD)"
              error={form.formState.errors.keyReplacementFeeDollars?.message}
            >
              <FormNumberInput
                control={form.control}
                name="keyReplacementFeeDollars"
                min={0}
                decimalPlaces={2}
              />
            </Field>
            <Field
              label="Lockout fee (USD)"
              error={form.formState.errors.lockoutFeeDollars?.message}
            >
              <FormNumberInput
                control={form.control}
                name="lockoutFeeDollars"
                min={0}
                decimalPlaces={2}
              />
            </Field>
            <Field
              label="Parking spaces"
              error={form.formState.errors.parkingSpaceCount?.message}
            >
              <FormNumberInput
                control={form.control}
                name="parkingSpaceCount"
                min={0}
                decimalPlaces={0}
              />
            </Field>
            <div className="self-end pb-2">
              <label className="flex items-center gap-2 text-sm cursor-pointer">
                <input
                  type="checkbox"
                  {...form.register("parkingIncludedInRent")}
                  className="rounded border-input"
                />
                Parking included in rent
              </label>
            </div>
            <div className="sm:col-span-2">
              <Field label="Parking description (optional)">
                <Input
                  placeholder="e.g. One assigned spot in the garage, #12"
                  {...form.register("parkingDescription")}
                />
              </Field>
            </div>
          </div>

          <Separator />

          <div className="grid gap-4 sm:grid-cols-2">
            <Field
              label="Max consecutive days for guests"
              error={form.formState.errors.maxGuestConsecutiveDays?.message}
            >
              <FormNumberInput
                control={form.control}
                name="maxGuestConsecutiveDays"
                min={0}
                decimalPlaces={0}
              />
            </Field>
            <Field
              label="Renter's insurance minimum liability (USD)"
              error={form.formState.errors.rentersInsuranceMinLiabilityDollars?.message}
            >
              <FormNumberInput
                control={form.control}
                name="rentersInsuranceMinLiabilityDollars"
                min={0}
                decimalPlaces={2}
              />
            </Field>
            <Field
              label="Early termination fee (months of rent)"
              error={form.formState.errors.earlyTerminationFeeMonths?.message}
            >
              <FormNumberInput
                control={form.control}
                name="earlyTerminationFeeMonths"
                min={0}
                decimalPlaces={0}
              />
            </Field>
          </div>

          <Separator />

          <div className="grid gap-4 sm:grid-cols-2">
            <div className="flex flex-wrap gap-4 sm:col-span-2">
              <label className="flex items-center gap-2 text-sm cursor-pointer">
                <input
                  type="checkbox"
                  {...form.register("builtBefore1978")}
                  className="rounded border-input"
                />
                Property was built before 1978
              </label>
              <label className="flex items-center gap-2 text-sm cursor-pointer">
                <input
                  type="checkbox"
                  {...form.register("rentCapJustCauseExempt")}
                  className="rounded border-input"
                />
                Exempt from rent cap / just-cause rules
              </label>
            </div>
            <div className="sm:col-span-2">
              <Field label="Lead paint knowledge (optional)">
                <Textarea
                  rows={2}
                  placeholder="Any known lead-based paint or hazards on the property"
                  {...form.register("leadPaintKnowledge")}
                />
                <p className="text-xs text-muted-foreground">
                  Required disclosure for pre-1978 buildings. Leave blank for the
                  standard "no known lead-based paint" statement.
                </p>
              </Field>
            </div>
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
                  const checked = amenityIds.includes(a.id);
                  return (
                    <button
                      key={a.id}
                      type="button"
                      onClick={() => toggleId("amenityIds", a.id)}
                      className={cn(
                        "flex items-center gap-2 rounded-lg border px-3 py-2 text-left text-sm transition-colors cursor-pointer",
                        checked
                          ? "border-accent bg-accent/10 text-accent font-medium ring-1 ring-accent/30"
                          : "border-border hover:bg-muted/50",
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
              const checked = safetyDeviceIds.includes(s.id);
              return (
                <button
                  key={s.id}
                  type="button"
                  onClick={() => toggleId("safetyDeviceIds", s.id)}
                  className={cn(
                    "flex items-center gap-2 rounded-lg border px-3 py-2 text-left text-sm cursor-pointer",
                    checked
                      ? "border-accent bg-accent/10 text-accent font-medium ring-1 ring-accent/30"
                      : "border-border hover:bg-muted/50",
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
              const checked = considerationIds.includes(c.id);
              return (
                <button
                  key={c.id}
                  type="button"
                  onClick={() => toggleId("considerationIds", c.id)}
                  className={cn(
                    "flex items-center gap-2 rounded-lg border px-3 py-2 text-left text-sm cursor-pointer",
                    checked
                      ? "border-accent bg-accent/10 text-accent font-medium ring-1 ring-accent/30"
                      : "border-border hover:bg-muted/50",
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

      {!readOnly && (
        <div className="flex justify-end gap-3">
          <Button type="submit" variant="accent" size="lg" disabled={form.formState.isSubmitting}>
            {form.formState.isSubmitting ? "Saving..." : submitLabel}
          </Button>
        </div>
      )}
      </fieldset>
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
