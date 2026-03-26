import type { UserRole } from "@/app/auth/roles";

export type ErrorResponse = {
  error?: string;
  detail?: string;
  message?: string;
};

export type AuthResultDto = {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
  role: UserRole | number;
};

export type UserProfileDto = {
  userId: string;
  email: string;
  role: UserRole | number;
  isActive: boolean;
  firstName?: string | null;
  lastName?: string | null;
  displayName?: string | null;
  phoneNumber?: string | null;
  bio?: string | null;
  profilePhotoUrl?: string | null;
  city?: string | null;
  state?: string | null;
  country?: string | null;
  languages?: string | null;
  occupation?: string | null;
  dateOfBirth?: string | null;
  emergencyContactName?: string | null;
  emergencyContactPhone?: string | null;
  isGovernmentIdVerified?: boolean;
  isPhoneVerified?: boolean;
  responseRatePercent?: number | null;
  responseTimeMinutes?: number | null;
  memberSince: string;
  lastLoginAt?: string | null;
};

export type LoginRequest = {
  email: string;
  password: string;
};

export type RegisterRequest = {
  email: string;
  password: string;
  role: UserRole;
};

export type RegisterResponse = {
  userId: string;
  message: string;
  dev_verificationToken?: string;
  dev_verificationUrl?: string;
};

export type ExternalLoginRequest = {
  provider: "Google" | "Apple" | "Microsoft";
  idToken: string;
  preferredRole?: UserRole | null;
};

export type RefreshTokenRequest = {
  refreshToken: string;
};

export type ForgotPasswordRequest = {
  email: string;
};

export type ResetPasswordRequest = {
  userId: string;
  token: string;
  newPassword: string;
};

export type ChangePasswordRequest = {
  currentPassword: string;
  newPassword: string;
};

export type UpdateProfileRequest = {
  firstName: string | null;
  lastName: string | null;
  displayName: string | null;
  phoneNumber: string | null;
  bio: string | null;
  profilePhotoUrl: string | null;
  city: string | null;
  state: string | null;
  country: string | null;
  languages: string | null;
  occupation: string | null;
  dateOfBirth: string | null;
  emergencyContactName: string | null;
  emergencyContactPhone: string | null;
};

export type UpdateRoleRequest = {
  newRole: UserRole;
};

// ── Privacy / consent ───────────────────────────────────────────

/** Matches backend ConsentType (JsonStringEnumConverter). */
export type ConsentTypeDto = "KYCConsent" | "FCRAConsent" | "MarketingEmail" | "DataProcessing";

export type ConsentRecordDto = {
  consentType: ConsentTypeDto;
  grantedAt: string;
  withdrawnAt: string | null;
  ipAddress: string;
  userAgent: string;
};

export type RecordConsentRequest = {
  userId: string;
  consentType: ConsentTypeDto;
  ipAddress: string;
  userAgent: string;
};

// ── Saved listing collections ──────────────────────────────────

export type SavedListingCollectionDto = {
  id: string;
  name: string;
  createdAt: string;
  listingCount: number;
};

// ── Listing types ──────────────────────────────────────────────

export type PropertyType =
  | "Apartment" | "House" | "Condo" | "Townhouse" | "Studio"
  | "Loft" | "Villa" | "Cottage" | "Cabin" | "Other";

export type ListingStatus = "Draft" | "Published" | "Activated" | "Closed";

export type AmenityCategory =
  | "Kitchen" | "Bathroom" | "Bedroom" | "LivingArea" | "Outdoor"
  | "Parking" | "Entertainment" | "WorkSpace" | "Accessibility"
  | "Laundry" | "ClimateControl" | "Internet";

export type SearchListingsSortBy = "Newest" | "PriceAsc" | "PriceDesc" | "Distance";

export type CancellationPolicyType = "Flexible" | "Moderate" | "Strict" | "NonRefundable" | "Custom";

export type ListingSummaryDto = {
  id: string;
  title: string;
  status: ListingStatus;
  propertyType: PropertyType;
  monthlyRentCents: number;
  insuranceRequired: boolean;
  bedrooms: number;
  bathrooms: number;
  minStayDays?: number | null;
  maxStayDays?: number | null;
  latitude?: number | null;
  longitude?: number | null;
  coverPhotoUrl?: string | null;
  qualityScore?: number | null;
  createdAt: string;
};

export type ListingPhotoDto = {
  id: string;
  url?: string | null;
  caption?: string | null;
  isCover: boolean;
  sortOrder: number;
};

export type ListingAmenityDto = {
  id: string;
  name: string;
  category: AmenityCategory;
  iconKey: string;
};

export type ListingSafetyDeviceDto = {
  id: string;
  name: string;
  iconKey: string;
};

export type ListingConsiderationDto = {
  id: string;
  name: string;
  iconKey: string;
};

export type HouseRulesDto = {
  checkInTime: string;
  checkOutTime: string;
  maxGuests: number;
  petsAllowed: boolean;
  petsNotes?: string | null;
  smokingAllowed: boolean;
  partiesAllowed: boolean;
  quietHoursStart?: string | null;
  quietHoursEnd?: string | null;
  leavingInstructions?: string | null;
  additionalRules?: string | null;
};

export type CancellationPolicyDto = {
  type: CancellationPolicyType;
  freeCancellationDays: number;
  partialRefundPercent?: number | null;
  partialRefundDays?: number | null;
  customTerms?: string | null;
};

export type AddressDto = {
  street: string;
  city: string;
  state: string;
  zipCode: string;
  country: string;
};

export type HostProfileDto = {
  displayName?: string | null;
  profilePhotoUrl?: string | null;
  isGovernmentIdVerified: boolean;
  isPhoneVerified: boolean;
  responseRatePercent?: number | null;
  responseTimeMinutes?: number | null;
  memberSince: string;
};

export type ListingVerificationBadgesDto = {
  isHostVerified: boolean;
  isHostKycComplete: boolean;
  isInsuranceActive?: boolean | null;
};

export type ListingDetailsDto = {
  id: string;
  landlordUserId: string;
  status: ListingStatus;
  propertyType: PropertyType;
  title: string;
  description: string;
  monthlyRentCents: number;
  insuranceRequired: boolean;
  bedrooms: number;
  bathrooms: number;
  squareFootage?: number | null;
  minStayDays?: number | null;
  maxStayDays?: number | null;
  latitude?: number | null;
  longitude?: number | null;
  preciseAddress?: AddressDto | null;
  jurisdictionCode?: string | null;
  maxDepositCents: number;
  suggestedDepositLowCents?: number | null;
  suggestedDepositHighCents?: number | null;
  houseRules?: HouseRulesDto | null;
  cancellationPolicy?: CancellationPolicyDto | null;
  amenities: ListingAmenityDto[];
  safetyDevices: ListingSafetyDeviceDto[];
  considerations: ListingConsiderationDto[];
  photos: ListingPhotoDto[];
  instantBookingEnabled: boolean;
  virtualTourUrl?: string | null;
  hostVerificationBadges?: ListingVerificationBadgesDto | null;
  hostProfile?: HostProfileDto | null;
  qualityScore: number;
  createdAt: string;
  updatedAt: string;
};

export type SearchListingsResultDto = {
  items: ListingSummaryDto[];
  totalCount: number;
};

export type SearchListingsParams = {
  keyword?: string;
  latitude?: number;
  longitude?: number;
  radiusKm?: number;
  swLat?: number;
  swLng?: number;
  neLat?: number;
  neLng?: number;
  propertyType?: PropertyType;
  minBedrooms?: number;
  minBathrooms?: number;
  minPriceCents?: number;
  maxPriceCents?: number;
  minStayDays?: number;
  maxStayDays?: number;
  availableFrom?: string;
  availableTo?: string;
  amenityIds?: string[];
  safetyDeviceIds?: string[];
  considerationIds?: string[];
  sortBy?: SearchListingsSortBy;
  page?: number;
  pageSize?: number;
};

export type AmenityDefinitionDto = {
  id: string;
  name: string;
  category: AmenityCategory;
  iconKey: string;
};

export type SafetyDeviceDefinitionDto = {
  id: string;
  name: string;
  iconKey: string;
};

export type ConsiderationDefinitionDto = {
  id: string;
  name: string;
  iconKey: string;
};

export type HouseRulesRequest = {
  checkInTime: string;
  checkOutTime: string;
  maxGuests: number;
  petsAllowed: boolean;
  petsNotes?: string | null;
  smokingAllowed: boolean;
  partiesAllowed: boolean;
  quietHoursStart?: string | null;
  quietHoursEnd?: string | null;
  leavingInstructions?: string | null;
  additionalRules?: string | null;
};

export type CancellationPolicyRequest = {
  type: CancellationPolicyType;
  freeCancellationDays: number;
  partialRefundPercent?: number | null;
  partialRefundDays?: number | null;
  customTerms?: string | null;
};

export type CreateListingRequest = {
  landlordUserId: string;
  propertyType: PropertyType;
  title: string;
  description: string;
  monthlyRentCents: number;
  insuranceRequired: boolean;
  bedrooms: number;
  bathrooms: number;
  minStayDays: number;
  maxStayDays: number;
  maxDepositCents: number;
  squareFootage?: number | null;
  houseRules?: HouseRulesRequest | null;
  cancellationPolicy?: CancellationPolicyRequest | null;
  amenityIds?: string[] | null;
  safetyDeviceIds?: string[] | null;
  considerationIds?: string[] | null;
  instantBookingEnabled: boolean;
  virtualTourUrl?: string | null;
};

export type UpdateListingRequest = {
  propertyType: PropertyType;
  title: string;
  description: string;
  monthlyRentCents: number;
  insuranceRequired: boolean;
  bedrooms: number;
  bathrooms: number;
  minStayDays: number;
  maxStayDays: number;
  maxDepositCents: number;
  squareFootage?: number | null;
  houseRules?: HouseRulesRequest | null;
  cancellationPolicy?: CancellationPolicyRequest | null;
  amenityIds?: string[] | null;
  safetyDeviceIds?: string[] | null;
  considerationIds?: string[] | null;
  instantBookingEnabled?: boolean | null;
  virtualTourUrl?: string | null;
};

export type SetApproxLocationRequest = {
  latitude: number;
  longitude: number;
};

export type LockPreciseAddressRequest = {
  street: string;
  city: string;
  state: string;
  zipCode: string;
  country: string;
  jurisdictionCode?: string | null;
};

export type AddListingPhotoRequest = {
  storageKey: string;
  url: string;
  caption?: string | null;
};

// ── Applications & Deals ──────────────────────────────────────

export type DealApplicationStatus = "Pending" | "Approved" | "Rejected" | "Cancelled";

export type DealApplicationDto = {
  applicationId: string;
  listingId: string;
  tenantUserId: string;
  landlordUserId: string;
  status: DealApplicationStatus;
  dealId: string | null;
  submittedAt: string;
  decidedAt: string | null;
  requestedCheckIn: string;
  requestedCheckOut: string;
  stayDurationDays: number;
  depositAmountCents: number | null;
  insuranceFeeCents: number | null;
  firstMonthRentCents: number | null;
  partnerOrganizationId: string | null;
  isPartnerReferred: boolean;
  jurisdictionWarning: string | null;
};

export type SubmitApplicationRequest = {
  listingId: string;
  tenantUserId: string;
  landlordUserId: string;
  requestedCheckIn: string;
  requestedCheckOut: string;
};

export type ApproveApplicationRequest = {
  depositAmountCents: number;
};

// ── Availability calendar ─────────────────────────────────────

export type AvailabilityBlockType = "Booked" | "HostBlocked";

export type AvailabilityBlockDto = {
  id: string;
  checkInDate: string;
  checkOutDate: string;
  blockType: AvailabilityBlockType;
};

export type BlockDatesRequest = {
  checkInDate: string;
  checkOutDate: string;
};

// ── Price history ─────────────────────────────────────────────

export type ListingPriceHistoryDto = {
  id: string;
  monthlyRentCents: number;
  effectiveFrom: string;
  effectiveTo: string | null;
};

// ── Admin listing definitions ─────────────────────────────────

export type CreateAmenityDefinitionRequest = {
  name: string;
  category: AmenityCategory;
  iconKey: string;
  sortOrder: number;
};

export type UpdateAmenityDefinitionRequest = {
  name: string;
  category: AmenityCategory;
  iconKey: string;
  isActive: boolean;
  sortOrder: number;
};

export type CreateSafetyDeviceDefinitionRequest = {
  name: string;
  iconKey: string;
  sortOrder: number;
};

export type UpdateSafetyDeviceDefinitionRequest = {
  name: string;
  iconKey: string;
  isActive: boolean;
  sortOrder: number;
};

export type CreateConsiderationDefinitionRequest = {
  name: string;
  iconKey: string;
  sortOrder: number;
};

export type UpdateConsiderationDefinitionRequest = {
  name: string;
  iconKey: string;
  isActive: boolean;
  sortOrder: number;
};
