import { http } from "@/api/http";
import { endpoints } from "@/api/endpoints";
import type {
  InsuranceQueueItemDto,
  FraudFlagDto,
  UserRestrictionDto,
  ApplyRestrictionRequest,
  ArbitrationBacklogItemDto,
  ArbitratorCaseloadDto,
  LeaseTemplateSummaryDto,
  PendingLeaseApprovalDto,
  LeaseAgreementTemplateDto,
  EvidenceScanQueueItemDto,
  ManualVerificationItemDto,
  ManualVerificationDetailDto,
  ViolationDto,
  AuditSearchParams,
  AuditSearchResultDto,
  PlatformSummaryDto,
  ListingAnalyticsItemDto,
  ListingAnalyticsFilters,
  ListingDetailsDto,
  ListingReviewItemDto,
  BlogPostSummaryDto,
  BlogPostDetailDto,
  CreateBlogPostRequest,
  UpdateBlogPostRequest,
  SeoPageDto,
  UpsertSeoPageRequest,
  LeaseTemplateVersionSummaryDto,
  LeaseTemplateVersionDetailsDto,
  UpdateLeaseTemplateDraftBody,
  LeasePlaceholderCatalogDto,
  PlatformSettingDto,
  UpdatePlatformSettingRequest,
  ProtocolFeeReconciliationDto,
  JurisdictionPackDto,
  JurisdictionPackSummaryDto,
  PackVersionSummaryDto,
  PackVersionDetailDto,
  UpdatePackDraftBody,
  PendingPackApprovalDto,
} from "@/api/types";

export const adminApi = {
  // Insurance Unknown Queue
  async getUnknownQueue(): Promise<InsuranceQueueItemDto[]> {
    const r = await http.get<InsuranceQueueItemDto[]>(endpoints.adminInsurance.unknownQueue);
    return r.data;
  },

  // Fraud Flags
  async getAllFraudFlags(): Promise<FraudFlagDto[]> {
    const r = await http.get<FraudFlagDto[]>(endpoints.adminIntegrity.allFlags);
    return r.data;
  },
  async resolveFlag(id: string): Promise<void> {
    await http.post(endpoints.adminIntegrity.resolveFlag(id));
  },

  // User Restrictions
  async getAllRestrictions(): Promise<UserRestrictionDto[]> {
    const r = await http.get<UserRestrictionDto[]>(endpoints.adminIntegrity.allRestrictions);
    return r.data;
  },
  async applyRestriction(req: ApplyRestrictionRequest): Promise<void> {
    await http.post(endpoints.adminIntegrity.applyRestriction, req);
  },
  async removeRestriction(id: string): Promise<void> {
    await http.delete(endpoints.adminIntegrity.removeRestriction(id));
  },

  // Arbitration Backlog
  async getArbitrationBacklog(): Promise<ArbitrationBacklogItemDto[]> {
    const r = await http.get<ArbitrationBacklogItemDto[]>(endpoints.adminArbitration.backlog);
    return r.data;
  },
  async getArbitratorCaseload(): Promise<ArbitratorCaseloadDto[]> {
    const r = await http.get<ArbitratorCaseloadDto[]>(endpoints.adminArbitration.caseload);
    return r.data;
  },
  async autoAssignArbitrator(caseId: string): Promise<{ arbitratorUserId: string }> {
    const r = await http.post<{ arbitratorUserId: string }>(
      endpoints.adminArbitration.assignAuto(caseId),
    );
    return r.data;
  },

  // Evidence Scan Queue
  async getEvidenceScanQueue(): Promise<EvidenceScanQueueItemDto[]> {
    const r = await http.get<EvidenceScanQueueItemDto[]>(endpoints.adminEvidence.scanQueue);
    return r.data;
  },
  async quarantineUpload(id: string): Promise<void> {
    await http.post(endpoints.adminEvidence.quarantine(id));
  },

  // Manual Verification
  async getManualVerificationQueue(): Promise<ManualVerificationItemDto[]> {
    const r = await http.get<ManualVerificationItemDto[]>(endpoints.adminIdentity.manualQueue);
    return r.data;
  },
  async getManualVerificationDetail(id: string): Promise<ManualVerificationDetailDto> {
    const r = await http.get<ManualVerificationDetailDto>(endpoints.adminIdentity.manualDetail(id));
    return r.data;
  },
  async approveManualVerification(id: string): Promise<void> {
    await http.post(endpoints.adminIdentity.approveManual(id));
  },
  async rejectManualVerification(id: string): Promise<void> {
    await http.post(endpoints.adminIdentity.rejectManual(id));
  },

  // Compliance Violations (cross-deal)
  async getAllViolations(): Promise<ViolationDto[]> {
    const r = await http.get<ViolationDto[]>(endpoints.adminCompliance.allViolations);
    return r.data;
  },

  // Audit Log
  async searchAuditEvents(params: AuditSearchParams): Promise<AuditSearchResultDto> {
    const r = await http.get<AuditSearchResultDto>(endpoints.adminAudit.search, { params });
    return r.data;
  },

  // Analytics
  async getPlatformSummary(startDate?: string, endDate?: string): Promise<PlatformSummaryDto> {
    const r = await http.get<PlatformSummaryDto>(endpoints.adminAnalytics.summary, {
      params: { startDate, endDate },
    });
    return r.data;
  },
  async getListingAnalytics(filters?: ListingAnalyticsFilters): Promise<ListingAnalyticsItemDto[]> {
    const r = await http.get<ListingAnalyticsItemDto[]>(endpoints.adminAnalytics.listings, {
      params: {
        landlordUserId: filters?.landlordUserId || undefined,
        search: filters?.search || undefined,
        status: filters?.status || undefined,
        addedFrom: filters?.addedFrom || undefined,
        addedTo: filters?.addedTo || undefined,
      },
    });
    return r.data;
  },

  // Blog
  async listBlogPosts(): Promise<BlogPostSummaryDto[]> {
    const r = await http.get<BlogPostSummaryDto[]>(endpoints.adminBlog.list);
    return r.data;
  },
  async getBlogPost(id: string): Promise<BlogPostDetailDto> {
    const r = await http.get<BlogPostDetailDto>(`/api/v1/admin/blog/${id}`);
    return r.data;
  },
  async createBlogPost(req: CreateBlogPostRequest): Promise<BlogPostDetailDto> {
    const r = await http.post<BlogPostDetailDto>(endpoints.adminBlog.create, req);
    return r.data;
  },
  async updateBlogPost(id: string, req: UpdateBlogPostRequest): Promise<BlogPostDetailDto> {
    const r = await http.put<BlogPostDetailDto>(endpoints.adminBlog.update(id), req);
    return r.data;
  },
  async publishBlogPost(id: string): Promise<void> {
    await http.post(endpoints.adminBlog.publish(id));
  },
  async archiveBlogPost(id: string): Promise<void> {
    await http.post(endpoints.adminBlog.archive(id));
  },

  // SEO Pages
  async listSeoPages(): Promise<SeoPageDto[]> {
    const r = await http.get<SeoPageDto[]>(endpoints.adminSeoPages.list);
    return r.data;
  },
  async getSeoPage(slug: string): Promise<SeoPageDto> {
    const r = await http.get<SeoPageDto>(endpoints.adminSeoPages.get(slug));
    return r.data;
  },
  async upsertSeoPage(slug: string, req: UpsertSeoPageRequest): Promise<SeoPageDto> {
    const r = await http.put<SeoPageDto>(endpoints.adminSeoPages.upsert(slug), req);
    return r.data;
  },

  // Listing Review Queue
  async getPendingListingReviews(): Promise<ListingReviewItemDto[]> {
    const r = await http.get<ListingReviewItemDto[]>(endpoints.adminListingReview.pending);
    return r.data;
  },
  async approveListing(id: string): Promise<ListingDetailsDto> {
    const r = await http.post<ListingDetailsDto>(endpoints.adminListingReview.approve(id));
    return r.data;
  },
  async denyListing(id: string, reason: string): Promise<ListingDetailsDto> {
    const r = await http.post<ListingDetailsDto>(endpoints.adminListingReview.deny(id), { reason });
    return r.data;
  },

  // Lease Agreement Templates
  async listLeaseTemplates(): Promise<LeaseTemplateSummaryDto[]> {
    const r = await http.get<LeaseTemplateSummaryDto[]>(endpoints.adminLeaseAgreements.list);
    return r.data;
  },
  async listPendingLeaseApprovals(): Promise<PendingLeaseApprovalDto[]> {
    const r = await http.get<PendingLeaseApprovalDto[]>(
      endpoints.adminLeaseAgreements.pendingApprovals,
    );
    return r.data;
  },
  async getLeasePlaceholderCatalog(): Promise<LeasePlaceholderCatalogDto> {
    const r = await http.get<LeasePlaceholderCatalogDto>(endpoints.leaseAgreements.placeholders);
    return r.data;
  },
  async createLeaseTemplate(
    jurisdictionCode: string,
    title: string,
  ): Promise<LeaseAgreementTemplateDto> {
    const r = await http.post<LeaseAgreementTemplateDto>(endpoints.leaseAgreements.create, {
      jurisdictionCode,
      title,
    });
    return r.data;
  },
  async addLeaseTemplateVersion(templateId: string): Promise<{ versionId: string }> {
    const r = await http.post<{ versionId: string }>(
      endpoints.leaseAgreements.addVersion(templateId),
    );
    return r.data;
  },
  async listLeaseTemplateVersions(templateId: string): Promise<LeaseTemplateVersionSummaryDto[]> {
    const r = await http.get<LeaseTemplateVersionSummaryDto[]>(
      endpoints.leaseAgreements.listVersions(templateId),
    );
    return r.data;
  },
  async getLeaseTemplateVersionDetails(
    templateId: string,
    versionId: string,
  ): Promise<LeaseTemplateVersionDetailsDto> {
    const r = await http.get<LeaseTemplateVersionDetailsDto>(
      endpoints.leaseAgreements.versionDetails(templateId, versionId),
    );
    return r.data;
  },
  async requestLeaseApproval(templateId: string, versionId: string): Promise<void> {
    await http.post(endpoints.leaseAgreements.requestApproval(templateId, versionId));
  },
  async approveLeaseVersion(templateId: string, versionId: string): Promise<void> {
    // The backend derives the approver from the authenticated user.
    await http.post(endpoints.leaseAgreements.approve(templateId, versionId), {});
  },
  async updateLeaseTemplateDraft(
    templateId: string,
    versionId: string,
    body: UpdateLeaseTemplateDraftBody,
  ): Promise<LeaseTemplateVersionDetailsDto> {
    const r = await http.put<LeaseTemplateVersionDetailsDto>(
      endpoints.leaseAgreements.updateDraft(templateId, versionId),
      body,
    );
    return r.data;
  },
  async publishLeaseVersion(templateId: string, versionId: string): Promise<void> {
    await http.post(endpoints.leaseAgreements.publish(templateId, versionId));
  },
  async deprecateLeaseVersion(templateId: string, versionId: string): Promise<void> {
    await http.post(endpoints.leaseAgreements.deprecate(templateId, versionId));
  },

  // Jurisdiction Packs
  async listJurisdictionPacks(): Promise<JurisdictionPackSummaryDto[]> {
    const r = await http.get<JurisdictionPackSummaryDto[]>(endpoints.adminJurisdictionPacks.list);
    return r.data;
  },
  async listPendingPackApprovals(): Promise<PendingPackApprovalDto[]> {
    const r = await http.get<PendingPackApprovalDto[]>(
      endpoints.adminJurisdictionPacks.pendingApprovals,
    );
    return r.data;
  },
  async createJurisdictionPack(jurisdictionCode: string): Promise<JurisdictionPackDto> {
    const r = await http.post<JurisdictionPackDto>(endpoints.jurisdictionPacks.create, {
      jurisdictionCode,
    });
    return r.data;
  },
  async listPackVersions(packId: string): Promise<PackVersionSummaryDto[]> {
    const r = await http.get<PackVersionSummaryDto[]>(
      endpoints.jurisdictionPacks.listVersions(packId),
    );
    return r.data;
  },
  async getPackVersionDetails(
    packId: string,
    versionId: string,
  ): Promise<PackVersionDetailDto> {
    const r = await http.get<PackVersionDetailDto>(
      endpoints.jurisdictionPacks.versionDetails(packId, versionId),
    );
    return r.data;
  },
  async requestApproval(packId: string, versionId: string): Promise<void> {
    await http.post(endpoints.jurisdictionPacks.requestApproval(packId, versionId));
  },
  async approveVersion(
    packId: string,
    versionId: string,
    approverId?: string,
  ): Promise<void> {
    await http.post(
      endpoints.jurisdictionPacks.approve(packId, versionId),
      approverId ? { approverId } : {},
    );
  },
  async updatePackDraft(
    packId: string,
    versionId: string,
    body: UpdatePackDraftBody,
  ): Promise<PackVersionDetailDto> {
    const r = await http.put<PackVersionDetailDto>(
      endpoints.jurisdictionPacks.updateDraft(packId, versionId),
      body,
    );
    return r.data;
  },
  async publishVersion(packId: string, versionId: string): Promise<void> {
    await http.post(endpoints.jurisdictionPacks.publish(packId, versionId));
  },
  async deprecateVersion(packId: string, versionId: string): Promise<void> {
    await http.post(endpoints.jurisdictionPacks.deprecate(packId, versionId));
  },

  // Platform Settings (fees & toggles)
  async listPlatformSettings(): Promise<PlatformSettingDto[]> {
    const r = await http.get<PlatformSettingDto[]>(endpoints.adminSettings.list);
    return r.data;
  },
  async updatePlatformSetting(
    key: string,
    req: UpdatePlatformSettingRequest,
  ): Promise<void> {
    await http.put(endpoints.adminSettings.update(key), req);
  },
  async getProtocolFeeReconciliation(): Promise<ProtocolFeeReconciliationDto> {
    const r = await http.get<ProtocolFeeReconciliationDto>(
      endpoints.adminSettings.protocolFeeReconciliation,
    );
    return r.data;
  },
};
