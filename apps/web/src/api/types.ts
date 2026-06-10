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

export type PublicUserProfileDto = {
  userId: string;
  displayName?: string | null;
  firstName?: string | null;
  lastName?: string | null;
  bio?: string | null;
  profilePhotoUrl?: string | null;
  city?: string | null;
  state?: string | null;
  country?: string | null;
  languages?: string | null;
  occupation?: string | null;
  isGovernmentIdVerified: boolean;
  isPhoneVerified: boolean;
  isEmailVerified: boolean;
  responseRatePercent?: number | null;
  responseTimeMinutes?: number | null;
  memberSince: string;
};

export type UserProfileDto = {
  userId: string;
  email: string;
  role: UserRole | number;
  isActive: boolean;
  emailConfirmed?: boolean;
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

export type ListingStatus =
  | "Draft"
  | "InReview"
  | "Published"
  | "Activated"
  | "Closed"
  | "Denied";

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
  /**
   * Phase 16.7 — host's default security deposit. Surfaced here so
   * inline approve actions on the host inbox can pre-fill the deposit
   * field without an extra round trip to the listing details endpoint.
   */
  defaultDepositCents?: number | null;
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
  defaultDepositCents?: number | null;
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
  rejectionReason?: string | null;
  submittedForReviewAt?: string | null;
  reviewedAt?: string | null;
};

export type ListingReviewItemDto = {
  id: string;
  landlordUserId: string;
  title: string;
  propertyType: PropertyType;
  bedrooms: number;
  bathrooms: number;
  monthlyRentCents: number;
  coverPhotoUrl?: string | null;
  photoCount: number;
  submittedForReviewAt?: string | null;
  createdAt: string;
};

export type DenyListingRequest = {
  reason: string;
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
  defaultDepositCents?: number | null;
};

// ── Import from URL (opt-in create-listing pre-fill) ───────────
// Additive only. These mirror the backend ImportedListingDraftDto and are pure
// suggestions: nothing is persisted until the host saves the listing.

export type ImportListingFromUrlRequest = {
  url: string;
  hostAttestation: boolean;
};

export type ImportedPhotoCandidateDto = {
  url: string;
  altText?: string | null;
  width?: number | null;
  height?: number | null;
};

export type ImportedListingDraftDto = {
  title?: string | null;
  description?: string | null;
  propertyType?: string | null;
  bedrooms?: number | null;
  bathrooms?: number | null;
  squareFootage?: number | null;
  maxGuests?: number | null;
  checkInTime?: string | null;
  checkOutTime?: string | null;
  monthlyRentCents?: number | null;
  nightlyRateCents?: number | null;
  currency?: string | null;
  approxAddress?: string | null;
  amenityHints?: string[] | null;
  photos?: ImportedPhotoCandidateDto[] | null;
  sourceUrl?: string | null;
  sourceHost?: string | null;
  petsAllowed?: boolean | null;
  smokingAllowed?: boolean | null;
  partiesAllowed?: boolean | null;
  quietHoursStart?: string | null;
  quietHoursEnd?: string | null;
  houseRules?: string | null;
  cancellationPolicy?: string | null;
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
  defaultDepositCents?: number | null;
  clearDefaultDeposit?: boolean;
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

// ── Truth Surface ─────────────────────────────────────────────

export type TruthSurfaceStatus =
  | "Draft"
  | "PendingBothConfirmations"
  | "PendingLandlordConfirmation"
  | "PendingTenantConfirmation"
  | "Confirmed"
  | "Superseded";

export type ConfirmingParty = "Landlord" | "Tenant";

export type SnapshotProofDto = {
  proofId: string;
  hash: string;
  signature: string;
  signedAt: string;
  isValid: boolean;
};

export type TruthSurfaceDto = {
  snapshotId: string;
  dealId: string;
  status: TruthSurfaceStatus;
  protocolVersion: string;
  jurisdictionPackVersion: string;
  canonicalContent: string | null;
  inquiryClosed: boolean;
  landlordConfirmed: boolean;
  tenantConfirmed: boolean;
  createdAt: string;
  sealedAt: string | null;
  proof: SnapshotProofDto | null;
};

export type CreateSnapshotRequest = {
  dealId: string;
  protocolVersion: string;
  jurisdictionPackVersion: string;
  canonicalContent: string;
};

export type ConfirmSnapshotRequest = {
  party: ConfirmingParty;
};

export type ReconfirmSnapshotRequest = {
  newJurisdictionPackVersion: string;
  updatedCanonicalContent: string;
  reason: string;
};

// ── Structured Inquiry ────────────────────────────────────────

export type InquirySessionStatus = "Locked" | "Open" | "Closed";

export type InquiryCategory =
  | "UtilitySpecifics"
  | "AccessibilityLayout"
  | "RuleClarification"
  | "Proximity";

/**
 * Phase 17 — `OpenText` lets the host respond to a tenant's free-form
 * "Other" question with prose. Older bundles will still see the existing
 * three structured types unchanged.
 */
export type ResponseType = "YesNo" | "MultipleChoice" | "Numeric" | "OpenText";

export type InquiryAnswerDto = {
  answerId: string;
  responseType: ResponseType;
  answerValue: string;
  answeredAt: string;
};

export type InquiryQuestionDto = {
  questionId: string;
  predefinedQuestionId: string | null;
  category: InquiryCategory;
  submittedAt: string;
  answer: InquiryAnswerDto | null;
  customText?: string | null;
  /**
   * Phase 17 — populated when the tenant chose to ask a free-form
   * question (typically via the "Other" option in a category). Up to
   * 1000 characters; rendered as the question prompt when present.
   */
  openQuestionText?: string | null;
};

/**
 * Phase 17 — sessions can now be either pre-booking (no `dealId`) or
 * deal-linked. `listingId` and `tenantUserId` are always present so the
 * UI can resolve display context without a deal round-trip.
 */
export type InquiryDto = {
  sessionId: string;
  dealId: string | null;
  listingId: string;
  tenantUserId: string;
  status: InquirySessionStatus;
  unlockedByLandlordAt: string | null;
  closedAt: string | null;
  createdAt: string;
  questions: InquiryQuestionDto[];
};

/**
 * Phase 17 — host inbox row for the Inquiries page. Lightweight: fetches
 * just enough listing + tenant context to render a card without pulling
 * the entire thread.
 */
export type HostInquirySummaryDto = {
  sessionId: string;
  listingId: string;
  listingTitle: string | null;
  listingCoverPhotoUri: string | null;
  listingCity: string | null;
  tenantUserId: string;
  tenantDisplayName: string | null;
  status: InquirySessionStatus;
  dealId: string | null;
  createdAt: string;
  lastActivityAt: string;
  questionCount: number;
  unansweredCount: number;
};

/**
 * Phase 17 — tenant counterpart of {@link HostInquirySummaryDto}. The
 * `unansweredByHostCount` field is the symmetric metric: how many of
 * the tenant's questions are still waiting on a host reply.
 */
export type TenantInquirySummaryDto = {
  sessionId: string;
  listingId: string;
  listingTitle: string | null;
  listingCoverPhotoUri: string | null;
  listingCity: string | null;
  landlordUserId: string;
  landlordDisplayName: string | null;
  status: InquirySessionStatus;
  dealId: string | null;
  createdAt: string;
  lastActivityAt: string;
  questionCount: number;
  unansweredByHostCount: number;
};

export type PredefinedQuestionDto = {
  id: string;
  category: InquiryCategory;
  text: string;
  expectedResponseType: ResponseType;
};

export type SubmitInquiryQuestionRequest = {
  category: InquiryCategory;
  predefinedQuestionId?: string | null;
  customQuestionText?: string | null;
  /** Phase 17 — free-form question text (≤1000 chars). */
  openQuestionText?: string | null;
};

export type SubmitLandlordResponseRequest = {
  questionId: string;
  responseType: ResponseType;
  answerValue: string;
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
  /** Headcount the tenant declared at submission. Defaults to 1 on legacy rows. */
  guestCount: number;
  /** Airbnb-style cover note from the tenant. Null when none was provided. */
  message: string | null;
};

/**
 * Phase 16: API now returns the application DTO plus a frontend-relative
 * route (`nextPath`) telling the caller where to send the guest. For
 * instant book: `/app/deals/{dealId}/checkout`. For request-to-book:
 * `/app/applications/{applicationId}`.
 */
export type SubmitApplicationResult = {
  application: DealApplicationDto;
  nextPath: string;
};

export type SubmitApplicationRequest = {
  listingId: string;
  requestedCheckIn: string;
  requestedCheckOut: string;
  /**
   * Headcount the tenant is booking for. Server validates against the
   * listing's `houseRules.maxGuests` and rejects values below 1. Optional
   * over the wire so callers that haven't shipped the UI yet still
   * submit valid applications (server default = 1).
   */
  guestCount?: number;
  /**
   * Optional Airbnb-style cover note. Up to 1000 chars; whitespace-only
   * notes are normalised to null server-side.
   */
  message?: string | null;
  /**
   * Phase 16.9 — optional Stripe `pm_…` id captured during the apply
   * dialog's SetupIntent step. When supplied, the host's approve action
   * (or instant-book) charges this card off-session and the tenant
   * skips the checkout page entirely.
   */
  stripePaymentMethodId?: string | null;
};

export type ApproveApplicationRequest = {
  depositAmountCents: number;
};

/**
 * Phase 16.9 — server response from `POST /v1/applications/setup-intent`
 * used to mount Stripe Elements in the apply dialog before the tenant
 * confirms an off-session usage SetupIntent.
 */
export type BookingSetupIntentResult = {
  setupIntentId: string;
  clientSecret: string;
  customerId: string;
};

// ── Host Stripe Connect ──────────────────────────────────────

export type StripeOnboardingStatus = "Pending" | "Completed" | "Restricted";

export type HostStripeStatusDto = {
  id: string;
  hostUserId: string;
  stripeAccountId: string;
  onboardingStatus: StripeOnboardingStatus;
  chargesEnabled: boolean;
  payoutsEnabled: boolean;
  onboardingUrl: string | null;
};

// ── Host Payment Details ─────────────────────────────────────
export type HostPaymentDetailsDto = {
  paymentInfo: string;
};

export type SavePaymentDetailsRequest = {
  paymentInfo: string;
};

// ── Checkout ─────────────────────────────────────────────────

export type CheckoutDto = {
  clientSecret: string;
  paymentIntentId: string;
  status: string;
  totalAmountCents: number;
  firstMonthRentCents: number;
  depositAmountCents: number;
  insuranceFeeCents: number;
  applicationFeeCents: number;
  serviceFeeCents: number;
  currency: string;
};

// ── Billing & Activation ──────────────────────────────────────

export type BillingAccountStatus = "Inactive" | "Active" | "Suspended" | "Closed";

export type PaymentConfirmationStatus =
  | "Pending"
  | "Confirmed"
  | "Disputed"
  | "Rejected"
  | "Cancelled";

export type DamageClaimStatus =
  | "Filed"
  | "UnderReview"
  | "Approved"
  | "PartiallyApproved"
  | "Rejected"
  | "Settled";

export type BillingStatusDto = {
  billingAccountId: string;
  dealId: string;
  status: BillingAccountStatus;
  startDate: string;
  endDate: string | null;
  stripeCustomerId: string | null;
  stripeSubscriptionId: string | null;
  totalInvoices: number;
  paidInvoices: number;
};

export type ProrationQuoteDto = {
  startDate: string;
  endDate: string;
  totalDays: number;
  proratedAmountCents: number;
  monthlyFeeCents: number;
  currency: string;
};

export type PaymentConfirmationDto = {
  id: string;
  dealId: string;
  status: PaymentConfirmationStatus;
  hostConfirmed: boolean;
  hostConfirmedAt: string | null;
  tenantDisputed: boolean;
  tenantDisputedAt: string | null;
  disputeReason: string | null;
  gracePeriodExpiresAt: string;
  totalTenantPaymentCents: number;
  totalHostPlatformPaymentCents: number;
  firstMonthRentCents: number;
  depositAmountCents: number;
  insuranceFeeCents: number;
  monthlyProtocolFeeCents: number;
  hostPaidPlatform: boolean;
  hostPaidPlatformAt: string | null;
};

export type PaymentDetailsDto = {
  dealId: string;
  paymentInfoPlain: string;
};

export type CancellationResultDto = {
  dealId: string;
  tenantRefundCents: number;
  insuranceRefundCents: number;
  policyApplied: string;
};

export type DamageClaimDto = {
  id: string;
  dealId: string;
  listingId: string;
  filedByUserId: string;
  tenantUserId: string;
  status: DamageClaimStatus;
  description: string;
  claimedAmountCents: number;
  approvedAmountCents: number | null;
  depositDeductionCents: number;
  insuranceClaimCents: number | null;
  evidenceManifestId: string | null;
  filedAt: string;
  resolvedAt: string | null;
  resolutionNotes: string | null;
};

export type DisputePaymentRequest = {
  reason: string;
  evidenceManifestId?: string | null;
};

export type CancelBookingRequest = {
  reason: string;
};

export type FileDamageClaimRequest = {
  description: string;
  claimedAmountCents: number;
  evidenceManifestId?: string | null;
};

// ── Availability calendar ─────────────────────────────────────

export type AvailabilityBlockType = "Booked" | "HostBlocked";

export type AvailabilityBlockDto = {
  id: string;
  checkInDate: string;
  checkOutDate: string;
  blockType: AvailabilityBlockType;
};

/**
 * Range-aware availability response (Phase 16). When the caller supplies
 * ?from=&to=, `available` reflects whether the listing is bookable in the
 * requested window and `blocks` contains only overlapping blocks.
 */
export type ListingAvailabilityDto = {
  available: boolean;
  blocks: AvailabilityBlockDto[];
};

/** Itemised pre-flight quote returned by `POST /v1/listings/{id}/quote`. */
export type QuoteDto = {
  checkIn: string;
  checkOut: string;
  stayDurationDays: number;
  rentCents: number;
  depositCents: number;
  insuranceFeeCents: number;
  /** Disclosed for transparency only — charged to the host, not the tenant. */
  protocolFeeCents: number;
  /** Platform service fee charged to the tenant; included in totalCents. */
  serviceFeeCents: number;
  /** Tenant-payable total = rent + deposit + insurance + service fee. */
  totalCents: number;
  currency: string;
};

/** Pre-flight consent state for the booking funnel. */
export type ConsentStatusDto = {
  hasRequired: boolean;
  missing: string[];
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

// ── Identity & Verification ──────────────────────────────────
export type VerificationStatus =
  | "NotStarted"
  | "Pending"
  | "Verified"
  | "Failed"
  | "ManualReviewRequired";

export type VerificationClassLevel = "Low" | "Medium" | "High";

export type VerificationStatusDto = {
  profileId: string;
  userId: string;
  status: VerificationStatus;
  verificationClass: VerificationClassLevel;
  firstName: string | null;
  lastName: string | null;
  dateOfBirth: string | null;
  createdAt: string;
};

export type StartKycRequest = {
  userId: string;
  firstName?: string | null;
  lastName?: string | null;
  dateOfBirth?: string | null;
};

export type CompleteKycRequest = {
  userId: string;
  externalInquiryId?: string | null;
};

export type BackgroundCheckConsentRequest = {
  userId: string;
};

export type ProtectionTier = "Uninsured" | "ThirdPartyInsured" | "PartnerBacked";

export type EndorsementSummaryDto = {
  organizationId: string;
  organizationName: string;
  approvedAt: string;
  expiresAt: string | null;
};

export type RiskViewDto = {
  tenantUserId: string;
  verificationClass: VerificationClassLevel;
  confidenceLevel: string;
  confidenceReason: string;
  depositBandLowCents: number;
  depositBandHighCents: number;
  computedAt: string;
  protectionTier: ProtectionTier;
  endorsedBy: EndorsementSummaryDto[];
};

// ── Deals Hub ────────────────────────────────────────────────

export type DealPhase =
  | "TruthSurface"
  | "Checkout"
  | "Active"
  | "Closed"
  | "Cancelled";

export type DealPhaseFilter = "active" | "past" | "all";

export type DealSummaryDto = {
  dealId: string;
  applicationId: string;
  listingId: string;
  listingTitle: string;
  listingCoverPhotoUri: string | null;
  listingCity: string | null;
  tenantUserId: string;
  landlordUserId: string;
  applicationStatus: DealApplicationStatus;
  dealPhase: DealPhase;
  requestedCheckIn: string;
  requestedCheckOut: string;
  stayDurationDays: number;
  monthlyRentCents: number | null;
  depositAmountCents: number | null;
  totalAmountCents: number | null;
  billingStatus: BillingAccountStatus | null;
  paymentStatus: PaymentConfirmationStatus | null;
  createdAt: string;
};

// ── Notifications ────────────────────────────────────────────
export type InAppNotificationDto = {
  id: string;
  title: string;
  body: string;
  category: string;
  relatedEntityId: string | null;
  relatedEntityType: string | null;
  isRead: boolean;
  createdAt: string;
};

export type NotificationPreferencesDto = {
  userId: string;
  eventOptIns: Record<string, boolean>;
  transactionalAlwaysSent: boolean;
};

export type UpdatePreferencesRequest = {
  eventOptIns: Record<string, boolean>;
};

// ── Compliance Monitoring ───────────────────────────────────

export type MonitoredViolationCategory = "CategoryA" | "CategoryB" | "CategoryC";
export type MonitoredViolationStatus = "Open" | "Cured" | "Escalated";

export type ComplianceStatusDto = {
  dealId: string;
  openViolations: number;
  curedViolations: number;
  escalatedViolations: number;
  totalSignals: number;
  unprocessedSignals: number;
  isCompliant: boolean;
};

export type MonitoredViolationDto = {
  violationId: string;
  dealId: string;
  category: MonitoredViolationCategory;
  status: MonitoredViolationStatus;
  detectedAt: string;
  cureDeadline: string | null;
};

// ── Compliance (Core) ───────────────────────────────────────

export type ViolationCategory =
  | "NonPayment"
  | "UnauthorizedOccupants"
  | "PropertyDamage"
  | "RuleViolation"
  | "InsuranceLapse"
  | "EarlyTermination"
  | "Other";

export type ViolationStatus =
  | "Open"
  | "UnderReview"
  | "Resolved"
  | "Dismissed"
  | "Escalated";

export type ViolationDto = {
  id: string;
  dealId: string;
  reportedByUserId: string;
  targetUserId: string;
  category: ViolationCategory;
  status: ViolationStatus;
  description: string;
  evidenceReference: string | null;
  detectedAt: string;
  resolvedAt: string | null;
};

export type TrustLedgerEntryType =
  | "DealCompleted"
  | "ViolationRecorded"
  | "ViolationDismissed"
  | "ArbitrationRuling"
  | "InsuranceClaim"
  | "PaymentDefault"
  | "EarlyTermination"
  | "PositiveReview"
  | "IdentityVerified";

export type TrustLedgerEntryDto = {
  id: string;
  userId: string;
  entryType: TrustLedgerEntryType;
  referenceId: string | null;
  description: string | null;
  occurredAt: string;
  isPublic: boolean;
};

// ── Evidence ────────────────────────────────────────────────

export type ManifestType = "MoveIn" | "MoveOut" | "Arbitration" | "Insurance" | "Damage";
export type ManifestStatus = "Open" | "Sealed";
export type ScanStatus = "Pending" | "Clean" | "Infected";

export type ManifestUploadDto = {
  uploadId: string;
  originalFileName: string;
  mimeType: string;
  fileHash: string | null;
  uploadedAt: string;
};

export type ManifestDto = {
  manifestId: string;
  dealId: string;
  manifestType: ManifestType;
  status: ManifestStatus;
  createdAt: string;
  sealedAt: string | null;
  hashOfAllFiles: string | null;
  uploads: ManifestUploadDto[];
};

export type UploadUrlDto = {
  uploadId: string;
  presignedUrl: string;
  storageKey: string;
};

export type ScanResultDto = {
  uploadId: string;
  status: ScanStatus;
  scannedAt: string | null;
};

export type DownloadUrlDto = {
  uploadId: string;
  presignedUrl: string;
  originalFileName: string;
};

// ── Arbitration ─────────────────────────────────────────────

export type ArbitrationStatus =
  | "Filed"
  | "EvidencePending"
  | "EvidenceComplete"
  | "UnderReview"
  | "Decided"
  | "Appealed"
  | "Closed";

export type ArbitrationCategory =
  | "CategoryA"
  | "CategoryB"
  | "CategoryC"
  | "CategoryD"
  | "CategoryE"
  | "CategoryF"
  | "CategoryG"
  | "Other";

export type ArbitrationTier = "ProtocolAdjudication" | "BindingArbitration";

export type EvidenceSlotDto = {
  slotId: string;
  slotType: string;
  submittedBy: string;
  evidenceManifestId: string;
  submittedAt: string;
};

export type DecisionOutcome = "LandlordFavored" | "TenantFavored" | "SharedFault" | "Dismissed";
export type DecisionSeverity = "Low" | "Medium" | "High";
export type PenaltyType =
  | "Monetary"
  | "DepositWithhold"
  | "TrustLedgerMark"
  | "AccountWarning"
  | "ProtocolFee"
  | "Custom"
  | "RentCredit"
  | "LateFee"
  | "DamageRestitution"
  | "InsuranceRecovery"
  | "AccountRestriction"
  | "PlatformBan"
  | "CorrectiveAction"
  | "LeaseTermination"
  | "CleaningFee"
  | "UtilitiesRecovery";

export type DecisionPenaltyDto = {
  penaltyId: string;
  partyUserId: string;
  penaltyType: PenaltyType;
  amountCents: number | null;
  description: string | null;
};

export type DecisionDto = {
  summary: string;
  awardAmount: number | null;
  decidedAt: string;
  isStructured: boolean;
  outcome: DecisionOutcome | null;
  severity: DecisionSeverity | null;
  penalties: DecisionPenaltyDto[];
};

export type CaseDto = {
  caseId: string;
  dealId: string;
  filedByUserId: string;
  landlordUserId: string | null;
  tenantUserId: string | null;
  tier: ArbitrationTier;
  category: ArbitrationCategory;
  status: ArbitrationStatus;
  filingFeeCents: number;
  filedAt: string;
  evidenceCompleteAt: string | null;
  decisionDueAt: string | null;
  evidenceSlotCount: number;
  assignedArbitratorUserId: string | null;
  assignedArbitratorEmail: string | null;
  decision: DecisionDto | null;
  /** Verdict from before an appeal; shown while case is back in review. */
  priorDecision: DecisionDto | null;
  evidenceSlots: EvidenceSlotDto[] | null;
};

export type IssueDecisionRequest = {
  decisionSummary: string;
  awardAmount?: number | null;
  isStructured: boolean;
  outcome?: DecisionOutcome | null;
  severity?: DecisionSeverity | null;
  penalties?: {
    partyUserId: string;
    penaltyType: PenaltyType;
    amountCents?: number | null;
    description?: string | null;
  }[];
};

export type ArbitratorCaseloadDto = {
  arbitratorUserId: string;
  email: string;
  displayName: string | null;
  activeCaseCount: number;
  isOverSoftCap: boolean;
  isAtHardCap: boolean;
};

// ── Admin: Insurance Unknown Queue ─────────────────────────
export type InsuranceQueueItemDto = {
  policyRecordId: string;
  dealId: string;
  tenantUserId: string;
  unknownSince: string;
  hoursRemaining: number;
};

// ── Admin: Fraud Flags ──────────────────────────────────────
export type FraudFlagSeverity = "High" | "Medium" | "Low";

export type FraudFlagDto = {
  id: string;
  userId: string;
  severity: string;
  category: string;
  detectedAt: string;
  isResolved: boolean;
};

// ── Admin: User Restrictions ────────────────────────────────
export type RestrictionType = "Warning" | "Suspension" | "Ban";

export type UserRestrictionDto = {
  id: string;
  userId: string;
  restrictionType: string;
  reason: string;
  appliedAt: string;
};

export type ApplyRestrictionRequest = {
  userId: string;
  restrictionLevel: RestrictionType;
  reason: string;
};

// ── Admin: Arbitration Backlog ──────────────────────────────
export type ArbitrationBacklogItemDto = {
  caseId: string;
  dealId: string;
  arbitratorUserId: string | null;
  arbitratorEmail: string | null;
  status: ArbitrationStatus;
  category: ArbitrationCategory;
  tier: ArbitrationTier;
  filedAt: string;
  decisionDueAt: string | null;
  isOverdue: boolean;
};

// ── Admin: Evidence Review ──────────────────────────────────
export type EvidenceScanQueueItemDto = {
  uploadId: string;
  manifestId: string;
  dealId: string;
  originalFileName: string;
  mimeType: string;
  uploadedAt: string;
  scanStatus: ScanStatus;
  scannedAt: string | null;
};

// ── Admin: Manual Verification ──────────────────────────────
export type ManualVerificationItemDto = {
  profileId: string;
  userId: string;
  email: string;
  firstName: string | null;
  lastName: string | null;
  submittedAt: string;
  hoursRemaining: number;
};

// ── Admin: Audit Log ────────────────────────────────────────
export type AuditEventDto = {
  id: string;
  userId: string | null;
  eventType: string;
  entityType: string;
  entityId: string;
  details: string | null;
  ipAddress: string | null;
  timestamp: string;
};

export type AuditSearchParams = {
  userId?: string;
  eventType?: string;
  entityType?: string;
  startDate?: string;
  endDate?: string;
  page?: number;
  pageSize?: number;
};

export type AuditSearchResultDto = {
  items: AuditEventDto[];
  totalCount: number;
};

// ── Admin: Analytics ────────────────────────────────────────
export type PlatformSummaryDto = {
  totalListings: number;
  activeDeals: number;
  mrrCents: number;
  conversionRatePercent: number;
  periodStart: string;
  periodEnd: string;
};

export type ListingAnalyticsItemDto = {
  listingId: string;
  title: string;
  views: number;
  applicationCount: number;
  conversionPercent: number;
  qualityScore: number;
};

// ── Admin: Blog ─────────────────────────────────────────────
export type BlogStatus = "Draft" | "Published" | "Archived";

export type BlogPostSummaryDto = {
  id: string;
  slug: string;
  title: string;
  excerpt: string;
  status: BlogStatus;
  tags: string[];
  readingTimeMinutes: number;
  publishedAt: string | null;
  createdAt: string;
};

export type BlogPostDetailDto = {
  id: string;
  slug: string;
  title: string;
  excerpt: string;
  content: string;
  tags: string[];
  metaTitle: string;
  metaDescription: string;
  ogImageUrl: string | null;
  readingTimeMinutes: number;
  status: BlogStatus;
  authorUserId: string;
  publishedAt: string | null;
  createdAt: string;
  updatedAt: string;
};

export type CreateBlogPostRequest = {
  slug: string;
  title: string;
  excerpt: string;
  content: string;
  tags: string[];
  metaTitle: string;
  metaDescription: string;
  ogImageUrl?: string | null;
  readingTimeMinutes: number;
};

export type UpdateBlogPostRequest = CreateBlogPostRequest;

// ── Admin: SEO Pages ────────────────────────────────────────
export type SeoPageDto = {
  slug: string;
  metaTitle: string;
  metaDescription: string;
  noIndex: boolean;
  updatedAt: string;
};

export type UpsertSeoPageRequest = {
  metaTitle: string;
  metaDescription: string;
  noIndex: boolean;
};

// ── Admin: Jurisdiction Packs ───────────────────────────────
export type PackVersionStatus = "Draft" | "PendingApproval" | "Active" | "Deprecated";

export type JurisdictionPackDto = {
  packId: string;
  jurisdictionCode: string;
  activeVersionId: string | null;
  versions: PackVersionSummaryDto[];
};

export type JurisdictionPackSummaryDto = {
  packId: string;
  jurisdictionCode: string;
  activeVersionId: string | null;
  versionCount: number;
};

export type PendingPackApprovalDto = {
  packId: string;
  jurisdictionCode: string;
  versionId: string;
  versionNumber: number;
  effectiveDate: string | null;
  approvedBy: string | null;
  secondApproverId: string | null;
};

export type PackVersionSummaryDto = {
  packId: string;
  jurisdictionCode: string;
  versionId: string;
  versionNumber: number;
  status: PackVersionStatus;
  effectiveDate: string | null;
  approvedAt: string | null;
  approvedBy: string | null;
  secondApproverId: string | null;
};

export type PackVersionDetailDto = PackVersionSummaryDto & {
  effectiveDateRules: EffectiveDateRuleDto[];
  fieldGatingRules: FieldGatingRuleDto[];
  evidenceSchedules: EvidenceScheduleDto[];
  depositCapRules: DepositCapRuleDto[];
};

export type EffectiveDateRuleDto = {
  id: string;
  fieldName: string;
  effectiveDate: string;
};

export type DepositCapRuleDto = {
  id: string;
  jurisdictionCode: string;
  maxMultiplier: number;
  exceptionCondition: string | null;
  exceptionMultiplier: number | null;
  legalReference: string;
};

export type FieldGatingRuleDto = {
  id: string;
  fieldName: string;
  gatingType: "Hard" | "Soft";
  value: string;
  condition: string | null;
};

export type EvidenceScheduleDto = {
  id: string;
  category: string;
  minimumRequirements: string;
};

export type UpdatePackDraftBody = {
  effectiveDate?: string;
  effectiveDateRules?: Omit<EffectiveDateRuleDto, "id">[];
  fieldGatingRules?: Omit<FieldGatingRuleDto, "id">[];
  evidenceSchedules?: Omit<EvidenceScheduleDto, "id">[];
  depositCapRules?: Omit<DepositCapRuleDto, "id">[];
};

// ── Partner Network ─────────────────────────────────────────

export type PartnerOrganizationType = "Relocation" | "Tech" | "Other";

export type PartnerOrganizationStatus =
  | "PendingVerification"
  | "Verified"
  | "Suspended";

export type PartnerMemberRole = "Admin" | "Member";

export type PartnerEndorsementStatus =
  | "Requested"
  | "Approved"
  | "Revoked"
  | "Expired";

export type PartnerOrganizationDto = {
  id: string;
  name: string;
  organizationType: PartnerOrganizationType;
  status: PartnerOrganizationStatus;
  contactEmail: string;
  taxId: string | null;
  verifiedAt: string | null;
  createdAt: string;
};

export type DiscoveredPartnerDto = {
  id: string;
  name: string;
  organizationType: PartnerOrganizationType;
};

export type PartnerMemberDto = {
  id: string;
  organizationId: string;
  userId: string;
  memberRole: PartnerMemberRole;
  joinedAt: string;
  invitedBy: string | null;
};

export type MyPartnerMembershipDto = {
  organization: PartnerOrganizationDto;
  memberRole: PartnerMemberRole;
  joinedAt: string;
};

export type ReferralLinkDto = {
  id: string;
  organizationId: string;
  code: string;
  createdByUserId: string;
  expiresAt: string | null;
  maxUses: number | null;
  usageCount: number;
  isActive: boolean;
  createdAt: string;
};

export type DirectReservationDto = {
  id: string;
  organizationId: string;
  guestName: string;
  guestEmail: string;
  listingId: string;
  dealApplicationId: string | null;
  reservedByUserId: string;
  createdAt: string;
};

export type PartnerEndorsementDto = {
  id: string;
  organizationId: string;
  organizationName: string;
  tenantUserId: string;
  status: PartnerEndorsementStatus;
  requestedAt: string;
  requestedByUserId: string;
  approvedAt: string | null;
  approvedByUserId: string | null;
  revokedAt: string | null;
  revokedByUserId: string | null;
  revokeReason: string | null;
  expiresAt: string | null;
  note: string | null;
};

export type PartnerDirectBookingResultDto = {
  applicationId: string;
  listingId: string;
  tenantUserId: string;
  landlordUserId: string;
  status: string;
  requestedCheckIn: string;
  requestedCheckOut: string;
  stayDurationDays: number;
};

export type DirectReservationConversionDto = {
  reservation: DirectReservationDto;
  dealApplication: PartnerDirectBookingResultDto;
  truthSurfacePending: boolean;
};

export type PartnerGuestInviteResultDto = {
  inviteId: string;
  invitedUserId: string;
  email: string;
  wasUserJustCreated: boolean;
  setPasswordUrl: string | null;
  setPasswordTokenExpiresAt: string | null;
  endorsementId: string | null;
  directReservationId: string | null;
};

export type RegisterPartnerRequest = {
  name: string;
  organizationType: PartnerOrganizationType;
  contactEmail: string;
  taxId?: string | null;
  endorsementTermsAccepted: boolean;
};

export type AddPartnerMemberRequest = {
  userId: string;
  role: PartnerMemberRole;
};

export type GenerateReferralLinkRequest = {
  expiresAt?: string | null;
  maxUses?: number | null;
};

export type CreateDirectReservationRequest = {
  guestName: string;
  guestEmail: string;
  listingId: string;
};

export type RequestEndorsementRequest = {
  tenantUserId: string;
  note?: string | null;
};

export type RequestEndorsementByTenantRequest = {
  organizationId: string;
  note?: string | null;
};

export type ApproveEndorsementRequest = {
  note?: string | null;
};

export type RevokeEndorsementRequest = {
  reason: string;
};

export type InvitePartnerGuestRequest = {
  email: string;
  fullName: string;
  listingId?: string | null;
  withEndorsement: boolean;
  endorsementNote?: string | null;
};

export type SuspendPartnerRequest = {
  reason: string;
};

export type ListPartnersParams = {
  status?: PartnerOrganizationStatus;
  search?: string;
  skip?: number;
  take?: number;
};

export type ListReservationsParams = {
  status?: "pending" | "linked";
  skip?: number;
  take?: number;
};

export type ListEndorsementsParams = {
  status?: PartnerEndorsementStatus;
  skip?: number;
  take?: number;
};

// ── Platform settings (admin-configurable fees & toggles) ───
export type PlatformSettingDto = {
  key: string;
  value: string;
  description: string | null;
  updatedAt: string;
  updatedByUserId: string | null;
};

export type UpdatePlatformSettingRequest = {
  value: string;
  description?: string | null;
};
