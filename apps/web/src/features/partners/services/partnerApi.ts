import { http } from "@/api/http";
import { endpoints } from "@/api/endpoints";
import type {
  AddPartnerMemberRequest,
  ApproveEndorsementRequest,
  BookingSetupIntentResult,
  CreateDirectReservationRequest,
  DirectReservationConversionDto,
  DirectReservationDto,
  DiscoveredPartnerDto,
  EndorsedMemberDto,
  GenerateReferralLinkRequest,
  InvitePartnerGuestRequest,
  ListEndorsementsParams,
  ListPartnersParams,
  ListReservationsParams,
  MyPartnerMembershipDto,
  PartnerEndorsementDto,
  PartnerGuestInviteResultDto,
  PartnerMemberDto,
  PartnerOrganizationDto,
  ReferralLinkDto,
  RegisterPartnerRequest,
  RequestEndorsementByTenantRequest,
  RequestEndorsementRequest,
  RevokeEndorsementRequest,
  SuspendPartnerRequest,
} from "@/api/types";

export const partnerApi = {
  // ── Partner self-service ───────────────────────────────
  async register(req: RegisterPartnerRequest): Promise<PartnerOrganizationDto> {
    const r = await http.post<PartnerOrganizationDto>(endpoints.partners.register, req);
    return r.data;
  },

  async discoverVerifiedPartners(
    search?: string,
    take = 25,
  ): Promise<DiscoveredPartnerDto[]> {
    const r = await http.get<DiscoveredPartnerDto[]>(endpoints.partners.discover, {
      params: { search, take },
    });
    return r.data;
  },

  async getMyMembership(): Promise<MyPartnerMembershipDto | null> {
    try {
      const r = await http.get<MyPartnerMembershipDto>(endpoints.partners.me);
      return r.data ?? null;
    } catch (err) {
      const status = (err as { response?: { status?: number } })?.response?.status;
      // Hosts/tenants are not partner members — treat as "no membership"
      // instead of leaving the query in an error state that can disrupt
      // inquiry pages that only need an optional partner check.
      if (status === 401 || status === 403 || status === 404) return null;
      throw err;
    }
  },

  async getOrganization(id: string): Promise<PartnerOrganizationDto> {
    const r = await http.get<PartnerOrganizationDto>(endpoints.partners.detail(id));
    return r.data;
  },

  async verifyOrganization(id: string): Promise<PartnerOrganizationDto> {
    const r = await http.post<PartnerOrganizationDto>(endpoints.partners.verify(id));
    return r.data;
  },

  // ── Members ────────────────────────────────────────────
  async listMembers(orgId: string): Promise<PartnerMemberDto[]> {
    const r = await http.get<PartnerMemberDto[]>(endpoints.partners.members(orgId));
    return r.data;
  },

  async addMember(orgId: string, req: AddPartnerMemberRequest): Promise<PartnerMemberDto> {
    const r = await http.post<PartnerMemberDto>(endpoints.partners.members(orgId), req);
    return r.data;
  },

  async removeMember(orgId: string, memberId: string): Promise<void> {
    await http.delete(endpoints.partners.member(orgId, memberId));
  },

  // ── Referral links ─────────────────────────────────────
  async listReferralLinks(orgId: string): Promise<ReferralLinkDto[]> {
    const r = await http.get<ReferralLinkDto[]>(endpoints.partners.referralLinks(orgId));
    return r.data;
  },

  async createReferralLink(
    orgId: string,
    req: GenerateReferralLinkRequest,
  ): Promise<ReferralLinkDto> {
    const r = await http.post<ReferralLinkDto>(endpoints.partners.referralLinks(orgId), req);
    return r.data;
  },

  async deactivateReferralLink(orgId: string, linkId: string): Promise<ReferralLinkDto> {
    const r = await http.post<ReferralLinkDto>(
      endpoints.partners.deactivateReferralLink(orgId, linkId),
    );
    return r.data;
  },

  async redeemReferralLink(code: string): Promise<void> {
    await http.post(endpoints.partners.redeemReferral(code));
  },

  // ── Direct reservations ────────────────────────────────
  async listReservations(
    orgId: string,
    params: ListReservationsParams = {},
  ): Promise<DirectReservationDto[]> {
    const r = await http.get<DirectReservationDto[]>(endpoints.partners.reservations(orgId), {
      params,
    });
    return r.data;
  },

  async createReservation(
    orgId: string,
    req: CreateDirectReservationRequest,
  ): Promise<DirectReservationConversionDto> {
    const r = await http.post<DirectReservationConversionDto>(
      endpoints.partners.reservations(orgId),
      req,
    );
    return r.data;
  },

  async createSetupIntent(
    orgId: string,
    listingId: string,
  ): Promise<BookingSetupIntentResult> {
    const r = await http.post<BookingSetupIntentResult>(
      endpoints.partners.setupIntent(orgId),
      { listingId },
    );
    return r.data;
  },

  async listEndorsedMembers(orgId: string): Promise<EndorsedMemberDto[]> {
    const r = await http.get<EndorsedMemberDto[]>(endpoints.partners.endorsedMembers(orgId));
    return r.data;
  },

  // ── Guest invites ──────────────────────────────────────
  async inviteGuest(
    orgId: string,
    req: InvitePartnerGuestRequest,
  ): Promise<PartnerGuestInviteResultDto> {
    const r = await http.post<PartnerGuestInviteResultDto>(endpoints.partners.invites(orgId), req);
    return r.data;
  },

  // ── Endorsements (partner side) ────────────────────────
  async listEndorsements(
    orgId: string,
    params: ListEndorsementsParams = {},
  ): Promise<PartnerEndorsementDto[]> {
    const r = await http.get<PartnerEndorsementDto[]>(endpoints.partners.endorsements(orgId), {
      params,
    });
    return r.data;
  },

  async requestEndorsement(
    orgId: string,
    req: RequestEndorsementRequest,
  ): Promise<PartnerEndorsementDto> {
    const r = await http.post<PartnerEndorsementDto>(endpoints.partners.endorsements(orgId), req);
    return r.data;
  },

  async approveEndorsement(
    orgId: string,
    endorsementId: string,
    req: ApproveEndorsementRequest,
  ): Promise<PartnerEndorsementDto> {
    const r = await http.post<PartnerEndorsementDto>(
      endpoints.partners.approveEndorsement(orgId, endorsementId),
      req,
    );
    return r.data;
  },

  async revokeEndorsement(
    orgId: string,
    endorsementId: string,
    req: RevokeEndorsementRequest,
  ): Promise<PartnerEndorsementDto> {
    const r = await http.post<PartnerEndorsementDto>(
      endpoints.partners.revokeEndorsement(orgId, endorsementId),
      req,
    );
    return r.data;
  },

  // ── Endorsements (tenant side) ─────────────────────────
  async listMyEndorsements(): Promise<PartnerEndorsementDto[]> {
    const r = await http.get<PartnerEndorsementDto[]>(endpoints.meEndorsements.list);
    return r.data;
  },

  async requestEndorsementAsTenant(
    req: RequestEndorsementByTenantRequest,
  ): Promise<PartnerEndorsementDto> {
    const r = await http.post<PartnerEndorsementDto>(endpoints.meEndorsements.request, req);
    return r.data;
  },

  // ── Admin ──────────────────────────────────────────────
  async listAllOrganizations(params: ListPartnersParams = {}): Promise<PartnerOrganizationDto[]> {
    const r = await http.get<PartnerOrganizationDto[]>(endpoints.adminPartners.list, { params });
    return r.data;
  },

  async listPendingOrganizations(): Promise<PartnerOrganizationDto[]> {
    const r = await http.get<PartnerOrganizationDto[]>(endpoints.adminPartners.pending);
    return r.data;
  },

  async suspendOrganization(
    id: string,
    req: SuspendPartnerRequest,
  ): Promise<PartnerOrganizationDto> {
    const r = await http.post<PartnerOrganizationDto>(endpoints.adminPartners.suspend(id), req);
    return r.data;
  },
};
