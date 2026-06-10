namespace Lagedra.Modules.StructuredInquiry.Domain.Enums;

public enum ResponseType
{
    YesNo,
    MultipleChoice,
    Numeric,
    /// <summary>
    /// Phase 17 — free-form text answer. Used both for the "Other" predefined-
    /// question fallback (tenant types their own question, host replies in
    /// prose) and for free-form host responses to numeric/categorical asks.
    /// </summary>
    OpenText,
}

