namespace Lagedra.Modules.StructuredInquiry.Presentation.Contracts;

public sealed record AddInquiryPartnerRequest(Guid OrganizationId);

public sealed record StartPartnerListingInquiryRequest(Guid TenantUserId);
