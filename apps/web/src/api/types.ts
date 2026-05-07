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

export type ResponseType = "YesNo" | "MultipleChoice" | "Numeric";

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
};

export type InquiryDto = {
  sessionId: string;
  dealId: string;
  status: InquirySessionStatus;
  unlockedByLandlordAt: string | null;
  closedAt: string | null;
  createdAt: string;
  questions: InquiryQuestionDto[];
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
};

export type SubmitApplicationRequest = {
  listingId: string;
  requestedCheckIn: string;
  requestedCheckOut: string;
};

export type ApproveApplicationRequest = {
  depositAmountCents: number;
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
  | "Inquiry"
  | "TruthSurface"
  | "AwaitingPayment"
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

export type DecisionDto = {
  summary: string;
  awardAmount: number | null;
  decidedAt: string;
};

export type EvidenceSlotDto = {
  slotId: string;
  slotType: string;
  submittedBy: string;
  evidenceManifestId: string;
  submittedAt: string;
};

export type CaseDto = {
  caseId: string;
  dealId: string;
  filedByUserId: string;
  tier: ArbitrationTier;
  category: ArbitrationCategory;
  status: ArbitrationStatus;
  filingFeeCents: number;
  filedAt: string;
  evidenceCompleteAt: string | null;
  decisionDueAt: string | null;
  evidenceSlotCount: number;
  decision: DecisionDto | null;
  evidenceSlots: EvidenceSlotDto[] | null;
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

export type PackVersionSummaryDto = {
  packId: string;
  versionId: string;
  jurisdictionCode: string;
  versionLabel: string;
  status: PackVersionStatus;
  createdAt: string;
  effectiveDate: string | null;
  createdByUserId: string;
  approvedByUserId: string | null;
};

export type PackVersionDetailDto = PackVersionSummaryDto & {
  depositCapRules: DepositCapRuleDto[];
  fieldGatingRules: FieldGatingRuleDto[];
};

export type DepositCapRuleDto = {
  id: string;
  condition: string;
  maxMonths: number;
};

export type FieldGatingRuleDto = {
  id: string;
  fieldPath: string;
  gatingType: string;
  ruleExpression: string;
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
