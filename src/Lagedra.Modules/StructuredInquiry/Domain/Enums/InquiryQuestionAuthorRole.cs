namespace Lagedra.Modules.StructuredInquiry.Domain.Enums;

/// <summary>
/// Who submitted an inquiry question. Legacy rows with null role are treated as Tenant.
/// </summary>
public enum InquiryQuestionAuthorRole
{
    Tenant = 0,
    Partner = 1,
}
