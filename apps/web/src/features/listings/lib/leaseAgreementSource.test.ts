import { describe, expect, it } from "vitest";
import type { ListingDetailsDto } from "@/api/types";
import { leaseAgreementChoiceLabel, listingHasLeaseAgreement } from "./leaseAgreement";
import {
  defaultListingFormValues,
  listingFormSchema,
  type ListingFormValues,
} from "./listingFormSchema";
import { listingDetailsToFormValues } from "./mapListingToForm";
import { toCreateListingRequest, toUpdateListingRequest } from "./toListingRequests";

// Defaults leave title/description blank for the host to fill in, so fill them
// here to isolate what these tests are actually asserting.
function values(overrides: Partial<ListingFormValues> = {}): ListingFormValues {
  return {
    ...defaultListingFormValues,
    title: "Sunny two-bedroom in Panorama City",
    description: "A bright, quiet two-bedroom apartment close to transit and shops.",
    ...overrides,
  } as ListingFormValues;
}

describe("lease agreement source", () => {
  it("defaults new listings to Lagedra's standard lease", () => {
    expect(defaultListingFormValues.leaseAgreementSource).toBe("LagedraTemplate");
    expect(listingFormSchema.safeParse(values()).success).toBe(true);
  });

  it("rejects a host-provided lease with no document uploaded", () => {
    const result = listingFormSchema.safeParse(
      values({ leaseAgreementSource: "HostProvided", hasCustomLeaseDocument: false }),
    );

    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.issues.some((i) => i.path.includes("hasCustomLeaseDocument"))).toBe(true);
    }
  });

  it("accepts a host-provided lease once a document is attached", () => {
    const result = listingFormSchema.safeParse(
      values({ leaseAgreementSource: "HostProvided", hasCustomLeaseDocument: true }),
    );

    expect(result.success).toBe(true);
  });

  it("sends the choice on both create and update", () => {
    const form = values({ leaseAgreementSource: "HostProvided", hasCustomLeaseDocument: true });

    expect(toCreateListingRequest(form).leaseAgreementSource).toBe("HostProvided");
    expect(toUpdateListingRequest(form).leaseAgreementSource).toBe("HostProvided");
  });

  it("round-trips an existing listing's lease choice into the form", () => {
    const listing = {
      ...baseListing,
      leaseAgreementSource: "HostProvided",
      customLeaseDocument: {
        fileName: "my-lease.pdf",
        contentType: "application/pdf",
        sizeBytes: 2048,
        uploadedAtUtc: "2026-08-01T00:00:00Z",
      },
    } as ListingDetailsDto;

    const form = listingDetailsToFormValues(listing);

    expect(form.leaseAgreementSource).toBe("HostProvided");
    expect(form.hasCustomLeaseDocument).toBe(true);
  });

  it("treats a listing with no stored choice as using the standard lease", () => {
    const form = listingDetailsToFormValues(baseListing);

    expect(form.leaseAgreementSource).toBe("LagedraTemplate");
    expect(form.hasCustomLeaseDocument).toBe(false);
  });

  it("accepts no lease agreement without an uploaded document", () => {
    const result = listingFormSchema.safeParse(
      values({ leaseAgreementSource: "None", hasCustomLeaseDocument: false }),
    );

    expect(result.success).toBe(true);
    expect(toCreateListingRequest(values({ leaseAgreementSource: "None" })).leaseAgreementSource).toBe(
      "None",
    );
  });

  it("round-trips a listing that opted out of a lease", () => {
    const form = listingDetailsToFormValues({
      ...baseListing,
      leaseAgreementSource: "None",
    });

    expect(form.leaseAgreementSource).toBe("None");
    expect(form.hasCustomLeaseDocument).toBe(false);
  });

  it("hides tenant-facing lease UI only when the host chose none", () => {
    expect(listingHasLeaseAgreement("None")).toBe(false);
    expect(listingHasLeaseAgreement("LagedraTemplate")).toBe(true);
    expect(listingHasLeaseAgreement("HostProvided")).toBe(true);
    expect(listingHasLeaseAgreement(undefined)).toBe(true);
    expect(leaseAgreementChoiceLabel("None")).toBe("No lease agreement");
  });
});

describe("require lease signatures", () => {
  it("defaults new listings to download only", () => {
    expect(defaultListingFormValues.requireLeaseSignatures).toBe(false);
    expect(toCreateListingRequest(values()).requireLeaseSignatures).toBe(false);
    expect(toUpdateListingRequest(values()).requireLeaseSignatures).toBe(false);
  });

  it("sends true only when the host chose Lagedra's lease", () => {
    const form = values({ requireLeaseSignatures: true });

    expect(toCreateListingRequest(form).requireLeaseSignatures).toBe(true);
    expect(toUpdateListingRequest(form).requireLeaseSignatures).toBe(true);
    expect(
      toCreateListingRequest(
        values({ leaseAgreementSource: "HostProvided", hasCustomLeaseDocument: true, requireLeaseSignatures: true }),
      ).requireLeaseSignatures,
    ).toBe(false);
    expect(
      toUpdateListingRequest(values({ leaseAgreementSource: "None", requireLeaseSignatures: true }))
        .requireLeaseSignatures,
    ).toBe(false);
  });

  it("round-trips an existing listing's signature choice", () => {
    expect(listingDetailsToFormValues(baseListing).requireLeaseSignatures).toBe(false);
    expect(
      listingDetailsToFormValues({ ...baseListing, requireLeaseSignatures: true }).requireLeaseSignatures,
    ).toBe(true);
  });
});

describe("stay protection opt-in", () => {
  it("defaults new listings to include Stay Protection", () => {
    expect(defaultListingFormValues.stayProtectionEnabled).toBe(true);
    expect(toCreateListingRequest(values()).stayProtectionEnabled).toBe(true);
    expect(toUpdateListingRequest(values()).stayProtectionEnabled).toBe(true);
  });

  it("sends the host choice on create and update", () => {
    const form = values({ stayProtectionEnabled: false });

    expect(toCreateListingRequest(form).stayProtectionEnabled).toBe(false);
    expect(toUpdateListingRequest(form).stayProtectionEnabled).toBe(false);
  });

  it("round-trips an existing listing's Stay Protection choice", () => {
    expect(listingDetailsToFormValues(baseListing).stayProtectionEnabled).toBe(true);
    expect(
      listingDetailsToFormValues({ ...baseListing, stayProtectionEnabled: false }).stayProtectionEnabled,
    ).toBe(false);
  });
});

const baseListing = {
  id: "11111111-1111-1111-1111-111111111111",
  landlordUserId: "22222222-2222-2222-2222-222222222222",
  status: "Draft",
  propertyType: "Apartment",
  title: "Sunny two-bedroom",
  description: "A nice place to stay for a while.",
  monthlyRentCents: 360_000,
  bedrooms: 2,
  bathrooms: 1.5,
  maxDepositCents: 400_000,
  amenities: [],
  safetyDevices: [],
  considerations: [],
  photos: [],
  instantBookingEnabled: false,
  qualityScore: 0,
  createdAt: "2026-08-01T00:00:00Z",
  updatedAt: "2026-08-01T00:00:00Z",
} as unknown as ListingDetailsDto;
