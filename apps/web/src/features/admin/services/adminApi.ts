import { http } from "@/api/http";
import { endpoints } from "@/api/endpoints";
import type {
  InsuranceQueueItemDto,
  FraudFlagDto,
  UserRestrictionDto,
  ApplyRestrictionRequest,
  ArbitrationBacklogItemDto,
  EvidenceScanQueueItemDto,
  ManualVerificationItemDto,
  ViolationDto,
  AuditSearchParams,
  AuditSearchResultDto,
  PlatformSummaryDto,
  ListingAnalyticsItemDto,
  BlogPostSummaryDto,
  BlogPostDetailDto,
  CreateBlogPostRequest,
  UpdateBlogPostRequest,
  SeoPageDto,
  UpsertSeoPageRequest,
  PackVersionSummaryDto,
  PackVersionDetailDto,
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
  async getListingAnalytics(): Promise<ListingAnalyticsItemDto[]> {
    const r = await http.get<ListingAnalyticsItemDto[]>(endpoints.adminAnalytics.listings);
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

  // Jurisdiction Packs
  async listPackVersions(packId: string): Promise<PackVersionSummaryDto[]> {
    const r = await http.get<PackVersionSummaryDto[]>(endpoints.jurisdictionPacks.listVersions(packId));
    return r.data;
  },
  async getPackVersionDetails(packId: string, versionId: string): Promise<PackVersionDetailDto> {
    const r = await http.get<PackVersionDetailDto>(
      endpoints.jurisdictionPacks.versionDetails(packId, versionId),
    );
    return r.data;
  },
  async requestApproval(packId: string, versionId: string): Promise<void> {
    await http.post(endpoints.jurisdictionPacks.requestApproval(packId, versionId));
  },
  async approveVersion(packId: string, versionId: string): Promise<void> {
    await http.post(endpoints.jurisdictionPacks.approve(packId, versionId));
  },
  async publishVersion(packId: string, versionId: string): Promise<void> {
    await http.post(endpoints.jurisdictionPacks.publish(packId, versionId));
  },
  async deprecateVersion(packId: string, versionId: string): Promise<void> {
    await http.post(endpoints.jurisdictionPacks.deprecate(packId, versionId));
  },
};
