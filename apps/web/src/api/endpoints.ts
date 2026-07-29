export const endpoints = {
  platform: {
    publicConfig: "/v1/platform/public-config",
  },
  auth: {
    register: "/v1/auth/register",
    verifyEmail: "/v1/auth/verify-email",
    resendVerification: "/v1/auth/resend-verification",
    login: "/v1/auth/login",
    externalLogin: "/v1/auth/external-login",
    refresh: "/v1/auth/refresh",
    logout: "/v1/auth/logout",
    forgotPassword: "/v1/auth/forgot-password",
    resetPassword: "/v1/auth/reset-password",
    changePassword: "/v1/auth/change-password",
    me: "/v1/auth/me",
    profilePhoto: "/v1/auth/me/profile-photo",
    sendPhoneCode: "/v1/auth/phone/send-code",
    confirmPhoneCode: "/v1/auth/phone/confirm",
    users: "/v1/auth/users",
    userRole: (userId: string) => `/v1/auth/users/${userId}/role`,
    sendSetPasswordEmail: (userId: string) =>
      `/v1/auth/users/${userId}/send-set-password-email`,
    publicProfile: (userId: string) => `/v1/auth/users/${userId}/public-profile`,
  },
  listings: {
    search: "/v1/listings",
    importFromUrl: "/v1/listings/import-from-url",
    mine: "/v1/listings/mine",
    detail: (id: string) => `/v1/listings/${id}`,
    similar: (id: string) => `/v1/listings/${id}/similar`,
    shareUrl: (id: string) => `/v1/listings/${id}/share-url`,
    priceHistory: (id: string) => `/v1/listings/${id}/price-history`,
    availability: (id: string) => `/v1/listings/${id}/availability`,
    quote: (id: string) => `/v1/listings/${id}/quote`,
    publish: (id: string) => `/v1/listings/${id}/publish`,
    submitForReview: (id: string) => `/v1/listings/${id}/submit-for-review`,
    close: (id: string) => `/v1/listings/${id}/close`,
    delete: (id: string) => `/v1/listings/${id}`,
    approxLocation: (id: string) => `/v1/listings/${id}/approx-location`,
    lockAddress: (id: string) => `/v1/listings/${id}/lock-address`,
    addPhoto: (id: string) => `/v1/listings/${id}/photos`,
    uploadMedia: (id: string) => `/v1/listings/${id}/media/upload`,
    photo: (listingId: string, photoId: string) => `/v1/listings/${listingId}/photos/${photoId}`,
    coverPhoto: (listingId: string, photoId: string) =>
      `/v1/listings/${listingId}/photos/${photoId}/cover`,
    reorderPhotos: (id: string) => `/v1/listings/${id}/photos/reorder`,
    blockDates: (id: string) => `/v1/listings/${id}/block-dates`,
    unblockDates: (id: string, blockId: string) =>
      `/v1/listings/${id}/block-dates/${blockId}`,
  },
  savedListings: {
    save: (listingId: string) => `/v1/saved-listings/${listingId}`,
    list: "/v1/saved-listings",
    collections: "/v1/saved-listings/collections",
    collection: (collectionId: string) => `/v1/saved-listings/collections/${collectionId}`,
    addToCollection: (listingId: string, collectionId: string) =>
      `/v1/saved-listings/${listingId}/collections/${collectionId}`,
    removeFromCollection: (listingId: string) => `/v1/saved-listings/${listingId}/collections`,
  },
  definitions: {
    amenities: "/v1/listing-definitions/amenities",
    safetyDevices: "/v1/listing-definitions/safety-devices",
    considerations: "/v1/listing-definitions/considerations",
  },
  applications: {
    submit: "/v1/applications",
    setupIntent: "/v1/applications/setup-intent",
    preview: "/v1/applications/preview",
    mine: "/v1/applications/mine",
    detail: (id: string) => `/v1/applications/${id}`,
    approve: (id: string) => `/v1/applications/${id}/approve`,
    reject: (id: string) => `/v1/applications/${id}/reject`,
    attachPayment: (id: string) => `/v1/applications/${id}/attach-payment`,
    forListing: (listingId: string) => `/v1/applications/listing/${listingId}`,
  },
  me: {
    verificationTier: "/v1/me/verification-tier",
  },
  adminDefinitions: {
    amenities: "/v1/admin/listing-definitions/amenities",
    amenity: (id: string) => `/v1/admin/listing-definitions/amenities/${id}`,
    safetyDevices: "/v1/admin/listing-definitions/safety-devices",
    safetyDevice: (id: string) => `/v1/admin/listing-definitions/safety-devices/${id}`,
    considerations: "/v1/admin/listing-definitions/considerations",
    consideration: (id: string) => `/v1/admin/listing-definitions/considerations/${id}`,
  },
  hostStripe: {
    onboard: "/v1/hosts/stripe/onboard",
    refreshLink: "/v1/hosts/stripe/refresh-link",
    status: "/v1/hosts/stripe/status",
  },
  hostPayouts: {
    start: "/v1/hosts/payouts/start",
    refreshLink: "/v1/hosts/payouts/refresh-link",
    status: "/v1/hosts/payouts/status",
  },
  hostPayment: {
    details: "/v1/hosts/payment-details",
  },
  channels: {
    providers: "/v1/channels/providers",
    list: "/v1/channels/",
    connect: "/v1/channels/",
    enable: (id: string) => `/v1/channels/${id}/enable`,
    disable: (id: string) => `/v1/channels/${id}/disable`,
    sync: (id: string) => `/v1/channels/${id}/sync`,
    listings: (id: string) => `/v1/channels/${id}/listings`,
  },
  deals: {
    mine: "/v1/deals/mine",
    stayAccess: (dealId: string) => `/v1/deals/${dealId}/stay-access`,
  },
  checkout: {
    create: (dealId: string) => `/v1/deals/${dealId}/checkout`,
    confirm: (dealId: string) => `/v1/deals/${dealId}/checkout/confirm`,
    status: (dealId: string) => `/v1/deals/${dealId}/checkout/status`,
  },
  billing: {
    status: (dealId: string) => `/v1/deals/${dealId}/billing`,
    prorationQuote: (dealId: string) => `/v1/deals/${dealId}/proration-quote`,
    stopBilling: (dealId: string) => `/v1/deals/${dealId}/stop-billing`,
    activate: (dealId: string) => `/v1/deals/${dealId}/activate`,
    hostStatement: "/v1/me/billing/statement",
  },
  payment: {
    details: (dealId: string) => `/v1/deals/${dealId}/payment/details`,
    status: (dealId: string) => `/v1/deals/${dealId}/payment/status`,
    confirm: (dealId: string) => `/v1/deals/${dealId}/payment/confirm`,
    confirmPlatform: (dealId: string) => `/v1/deals/${dealId}/payment/confirm-platform-payment`,
    dispute: (dealId: string) => `/v1/deals/${dealId}/payment/dispute`,
    cancel: (dealId: string) => `/v1/deals/${dealId}/payment/cancel`,
    damageClaim: (dealId: string) => `/v1/deals/${dealId}/payment/damage-claim`,
    resolveDispute: (dealId: string) => `/v1/admin/deals/${dealId}/resolve-payment-dispute`,
    beginMoveOut: (dealId: string) => `/v1/deals/${dealId}/payment/begin-move-out`,
    depositReturnHostConfirm: (dealId: string) =>
      `/v1/deals/${dealId}/payment/deposit-return/host-confirm`,
    depositReturnTenantConfirm: (dealId: string) =>
      `/v1/deals/${dealId}/payment/deposit-return/tenant-confirm`,
    forceDepositReturn: (dealId: string) =>
      `/v1/admin/deals/${dealId}/force-deposit-return`,
  },
  reviews: {
    deal: (dealId: string) => `/v1/deals/${dealId}/reviews`,
    userReviews: (userId: string) => `/v1/users/${userId}/reviews`,
    userReputation: (userId: string) => `/v1/users/${userId}/reputation`,
    listing: (listingId: string) => `/v1/listings/${listingId}/reviews`,
    partner: (orgId: string) => `/v1/partners/organizations/${orgId}/reviews`,
    partnerReputation: (orgId: string) =>
      `/v1/partners/organizations/${orgId}/reputation`,
  },
  truthSurface: {
    create: "/v1/truth-surface",
    fromDeal: (dealId: string) => `/v1/truth-surface/from-deal/${dealId}`,
    snapshot: (snapshotId: string) => `/v1/truth-surface/${snapshotId}`,
    byDeal: (dealId: string) => `/v1/truth-surface/by-deal/${dealId}`,
    confirm: (snapshotId: string) => `/v1/truth-surface/${snapshotId}/confirm`,
    reconfirm: (snapshotId: string) => `/v1/truth-surface/${snapshotId}/reconfirm`,
    verify: (snapshotId: string) => `/v1/truth-surface/${snapshotId}/verify`,
    receipt: (snapshotId: string) => `/v1/truth-surface/${snapshotId}/receipt`,
  },
  inquiry: {
    thread: (dealId: string) => `/v1/inquiries/${dealId}`,
    requestUnlock: (dealId: string) => `/v1/inquiries/${dealId}/unlock-request`,
    approveUnlock: (dealId: string) => `/v1/inquiries/${dealId}/approve-unlock`,
    lock: (dealId: string) => `/v1/inquiries/${dealId}/lock`,
    submitQuestion: (dealId: string) => `/v1/inquiries/${dealId}/questions`,
    submitAnswer: (dealId: string) => `/v1/inquiries/${dealId}/answers`,
    close: (dealId: string) => `/v1/inquiries/${dealId}/close`,
    predefinedQuestions: "/v1/inquiries/predefined-questions",
    // Phase 17 — pre-booking + session-id-based routes.
    startListingInquiry: (listingId: string) => `/v1/listings/${listingId}/inquiry`,
    myListingInquiry: (listingId: string) => `/v1/listings/${listingId}/inquiry/mine`,
    sessionThread: (sessionId: string) => `/v1/inquiry-sessions/${sessionId}`,
    sessionQuestions: (sessionId: string) => `/v1/inquiry-sessions/${sessionId}/questions`,
    sessionAnswers: (sessionId: string) => `/v1/inquiry-sessions/${sessionId}/answers`,
    sessionOffers: (sessionId: string) => `/v1/inquiry-sessions/${sessionId}/offers`,
    acceptOffer: (sessionId: string, offerId: string) =>
      `/v1/inquiry-sessions/${sessionId}/offers/${offerId}/accept`,
    counterOffer: (sessionId: string, offerId: string) =>
      `/v1/inquiry-sessions/${sessionId}/offers/${offerId}/counter`,
    withdrawAcceptedOffer: (sessionId: string) =>
      `/v1/inquiry-sessions/${sessionId}/offers/accepted/withdraw`,
    sessionPartner: (sessionId: string) => `/v1/inquiry-sessions/${sessionId}/partner`,
    partnerInbox: "/v1/inquiry-sessions/partner",
    startPartnerListingInquiry: (listingId: string) =>
      `/v1/listings/${listingId}/inquiry/partner`,
    hostInbox: "/v1/inquiry-sessions/host",
    myInbox: "/v1/inquiry-sessions/mine",
  },
  identity: {
    startKyc: "/v1/identity/kyc/start",
    completeKyc: "/v1/identity/kyc/complete",
    status: (userId: string) => `/v1/identity/status?userId=${userId}`,
    manualKycDocuments: "/v1/identity/kyc/manual/documents",
    manualKycSubmit: "/v1/identity/kyc/manual/submit",
  },
  verification: {
    backgroundCheckConsent: "/v1/verification/background-check/consent",
  },
  risk: {
    view: (userId: string) => `/v1/risk/${userId}`,
  },
  notifications: {
    all: "/v1/notifications/all",
    unread: "/v1/notifications/unread",
    unreadCount: "/v1/notifications/unread/count",
    markRead: (notificationId: string) =>
      `/v1/notifications/${notificationId}/read`,
    markAllRead: "/v1/notifications/read-all",
    preferences: (userId: string) =>
      `/v1/notifications/preferences/${userId}`,
    history: (userId: string) => `/v1/notifications/history/${userId}`,
  },
  privacy: {
    recordConsent: "/v1/privacy/consent",
    userConsents: (userId: string) => `/v1/privacy/consents/${userId}`,
    myConsentStatus: "/v1/privacy/consents/me/status",
  },
  complianceMonitoring: {
    status: (dealId: string) => `/v1/deals/${dealId}/compliance`,
    violations: (dealId: string) => `/v1/deals/${dealId}/compliance/violations`,
    detectViolation: (dealId: string) => `/v1/deals/${dealId}/compliance/violations`,
    cureViolation: (dealId: string, violationId: string) =>
      `/v1/deals/${dealId}/compliance/violations/${violationId}/cure`,
    recordSignal: (dealId: string) => `/v1/deals/${dealId}/compliance/signal`,
  },
  compliance: {
    violations: "/v1/compliance/violations",
    resolveViolation: (id: string) => `/v1/compliance/violations/${id}/resolve`,
    dismissViolation: (id: string) => `/v1/compliance/violations/${id}/dismiss`,
    escalateViolation: (id: string) => `/v1/compliance/violations/${id}/escalate`,
    userLedger: (userId: string) => `/v1/compliance/ledger/user/${userId}`,
    dealLedger: (dealId: string) => `/v1/compliance/ledger/deal/${dealId}`,
  },
  evidence: {
    createManifest: "/v1/evidence/manifests",
    sealManifest: (id: string) => `/v1/evidence/manifests/${id}/seal`,
    getManifest: (id: string) => `/v1/evidence/manifests/${id}`,
    requestUploadUrl: "/v1/evidence/uploads/request-url",
    completeUpload: (id: string) => `/v1/evidence/uploads/${id}/complete`,
    directUpload: "/v1/evidence/uploads/direct",
    scanStatus: (id: string) => `/v1/evidence/uploads/${id}/scan`,
    downloadUrl: (id: string) => `/v1/evidence/uploads/${id}/download-url`,
  },
  arbitration: {
    fileCase: "/v1/arbitration/cases",
    list: "/v1/arbitration/cases",
    getCase: (caseId: string) => `/v1/arbitration/cases/${caseId}`,
    filingFeeCheckout: (caseId: string) =>
      `/v1/arbitration/cases/${caseId}/filing-fee/checkout`,
    attachEvidence: (caseId: string) => `/v1/arbitration/cases/${caseId}/evidence`,
    markEvidenceComplete: (caseId: string) =>
      `/v1/arbitration/cases/${caseId}/evidence-complete`,
    assignArbitrator: (caseId: string) => `/v1/arbitration/cases/${caseId}/assign`,
    beginReview: (caseId: string) => `/v1/arbitration/cases/${caseId}/begin-review`,
    issueDecision: (caseId: string) => `/v1/arbitration/cases/${caseId}/decision`,
    closeCase: (caseId: string) => `/v1/arbitration/cases/${caseId}/close`,
    appeal: (caseId: string) => `/v1/arbitration/cases/${caseId}/appeal`,
  },

  // ── Admin-only endpoints ──────────────────────────────────
  adminInsurance: {
    unknownQueue: "/v1/admin/insurance/unknown-queue",
  },
  adminIntegrity: {
    allFlags: "/v1/admin/integrity/flags",
    resolveFlag: (id: string) => `/v1/admin/integrity/flags/${id}/resolve`,
    allRestrictions: "/v1/admin/integrity/restrictions",
    applyRestriction: "/v1/admin/integrity/restrictions",
    removeRestriction: (id: string) => `/v1/admin/integrity/restrictions/${id}`,
  },
  adminArbitration: {
    backlog: "/v1/admin/arbitration/backlog",
    caseload: "/v1/admin/arbitration/caseload",
    assignAuto: (caseId: string) => `/v1/admin/arbitration/cases/${caseId}/assign-auto`,
  },
  adminLeaseAgreements: {
    list: "/v1/admin/lease-agreements",
    pendingApprovals: "/v1/admin/lease-agreements/pending-approvals",
  },
  adminJurisdictionPacks: {
    list: "/v1/admin/jurisdiction-packs",
    pendingApprovals: "/v1/admin/jurisdiction-packs/pending-approvals",
  },
  adminEvidence: {
    scanQueue: "/v1/admin/evidence/scan-queue",
    quarantine: (id: string) => `/v1/admin/evidence/uploads/${id}/quarantine`,
  },
  adminIdentity: {
    manualQueue: "/v1/admin/identity/manual-queue",
    manualDetail: (id: string) => `/v1/admin/identity/manual-queue/${id}`,
    approveManual: (id: string) => `/v1/admin/identity/manual-queue/${id}/approve`,
    rejectManual: (id: string) => `/v1/admin/identity/manual-queue/${id}/reject`,
  },
  adminCompliance: {
    allViolations: "/v1/admin/compliance/violations",
  },
  adminAudit: {
    search: "/v1/admin/audit",
  },
  adminSettings: {
    list: "/v1/admin/settings",
    update: (key: string) => `/v1/admin/settings/${encodeURIComponent(key)}`,
    protocolFeeReconciliation: "/v1/admin/protocol-fee-reconciliation",
  },
  adminAnalytics: {
    summary: "/v1/admin/analytics/summary",
    listings: "/v1/admin/analytics/listings",
  },
  adminListingReview: {
    pending: "/v1/admin/listings/pending-review",
    approve: (id: string) => `/v1/admin/listings/${id}/approve`,
    deny: (id: string) => `/v1/admin/listings/${id}/deny`,
  },
  adminBlog: {
    list: "/api/v1/admin/blog",
    create: "/api/v1/admin/blog",
    update: (id: string) => `/api/v1/admin/blog/${id}`,
    publish: (id: string) => `/api/v1/admin/blog/${id}/publish`,
    archive: (id: string) => `/api/v1/admin/blog/${id}/archive`,
  },
  adminSeoPages: {
    get: (slug: string) => `/api/v1/pages/${slug}`,
    upsert: (slug: string) => `/api/v1/admin/pages/${slug}`,
    list: "/api/v1/pages",
  },
  partners: {
    register: "/v1/partners/",
    discover: "/v1/partners/discover",
    me: "/v1/partners/me",
    detail: (id: string) => `/v1/partners/${id}`,
    verify: (id: string) => `/v1/partners/${id}/verify`,
    members: (id: string) => `/v1/partners/${id}/members`,
    referralLinks: (id: string) => `/v1/partners/${id}/referral-links`,
    deactivateReferralLink: (id: string, linkId: string) =>
      `/v1/partners/${id}/referral-links/${linkId}/deactivate`,
    reservations: (id: string) => `/v1/partners/${id}/reservations`,
    setupIntent: (id: string) => `/v1/partners/${id}/setup-intent`,
    endorsedMembers: (id: string) => `/v1/partners/${id}/endorsed-members`,
    invites: (id: string) => `/v1/partners/${id}/invites`,
    endorsements: (id: string) => `/v1/partners/${id}/endorsements`,
    approveEndorsement: (id: string, endorsementId: string) =>
      `/v1/partners/${id}/endorsements/${endorsementId}/approve`,
    revokeEndorsement: (id: string, endorsementId: string) =>
      `/v1/partners/${id}/endorsements/${endorsementId}/revoke`,
    redeemReferral: (code: string) => `/v1/referral/${code}/redeem`,
  },
  meEndorsements: {
    list: "/v1/me/partner-endorsements/",
    request: "/v1/me/partner-endorsements/",
  },
  adminPartners: {
    list: "/v1/admin/partners/",
    pending: "/v1/admin/partners/pending",
    suspend: (id: string) => `/v1/admin/partners/${id}/suspend`,
  },
  leaseAgreements: {
    placeholders: "/v1/lease-agreements/placeholders",
    create: "/v1/lease-agreements",
    addVersion: (id: string) => `/v1/lease-agreements/${id}/versions`,
    listVersions: (id: string) => `/v1/lease-agreements/${id}/versions`,
    versionDetails: (id: string, versionId: string) =>
      `/v1/lease-agreements/${id}/versions/${versionId}`,
    updateDraft: (id: string, versionId: string) =>
      `/v1/lease-agreements/${id}/versions/${versionId}`,
    requestApproval: (id: string, versionId: string) =>
      `/v1/lease-agreements/${id}/versions/${versionId}/request-approval`,
    approve: (id: string, versionId: string) =>
      `/v1/lease-agreements/${id}/versions/${versionId}/approve`,
    publish: (id: string, versionId: string) =>
      `/v1/lease-agreements/${id}/versions/${versionId}/publish`,
    deprecate: (id: string, versionId: string) =>
      `/v1/lease-agreements/${id}/versions/${versionId}/deprecate`,
    getByCode: (code: string) => `/v1/lease-agreements/${code}`,
    dealPdf: (dealId: string) => `/v1/lease-agreements/deals/${dealId}/pdf`,
  },
  jurisdictionPacks: {
    create: "/v1/jurisdiction-packs",
    listVersions: (id: string) => `/v1/jurisdiction-packs/${id}/versions`,
    versionDetails: (id: string, versionId: string) =>
      `/v1/jurisdiction-packs/${id}/versions/${versionId}`,
    updateDraft: (id: string, versionId: string) =>
      `/v1/jurisdiction-packs/${id}/versions/${versionId}`,
    requestApproval: (id: string, versionId: string) =>
      `/v1/jurisdiction-packs/${id}/versions/${versionId}/request-approval`,
    approve: (id: string, versionId: string) =>
      `/v1/jurisdiction-packs/${id}/versions/${versionId}/approve`,
    publish: (id: string, versionId: string) =>
      `/v1/jurisdiction-packs/${id}/versions/${versionId}/publish`,
    deprecate: (id: string, versionId: string) =>
      `/v1/jurisdiction-packs/${id}/versions/${versionId}/deprecate`,
    getByCode: (code: string) => `/v1/jurisdiction-packs/${code}`,
  },
} as const;
