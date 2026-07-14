using Lagedra.SharedKernel.Domain;
using Microsoft.AspNetCore.Identity;

namespace Lagedra.Auth.Domain;

public sealed class ApplicationUser : IdentityUser<Guid>, ISoftDeletable
{
    public UserRole Role { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? DisplayName { get; set; }
    public string? Bio { get; set; }

    /// <summary>
    /// Company / organisation the member signed up on behalf of. Captured by
    /// the founding-partner sign-up flow (hosts and partner institutions).
    /// </summary>
    public string? CompanyName { get; set; }

    // ── Sign-up flow metadata ───────────────────────────────────────────
    // Lightweight, string-valued lead qualifiers captured by the multi-step
    // join flow. Nullable so legacy accounts and API-created users are
    // unaffected. Kept as free-form strings (rather than enums) because the
    // bracket labels are marketing copy that may evolve without a schema
    // change.

    /// <summary>"Host" or "Partner" — which door the user came through.</summary>
    public string? SignupType { get; set; }

    /// <summary>Host portfolio-size bracket, e.g. "6–20".</summary>
    public string? PortfolioSize { get; set; }

    /// <summary>Partner housing focus, e.g. "Relocation", "Insurance placements".</summary>
    public string? HousingType { get; set; }

    /// <summary>Partner annual placement-volume bracket, e.g. "26–100".</summary>
    public string? PlacementsPerYear { get; set; }

    /// <summary>
    /// True when the account was created through the pre-launch waitlist
    /// (no password set, no dashboard access) rather than a normal sign-up.
    /// </summary>
    public bool IsPreLaunchSignup { get; set; }
    public Uri? ProfilePhotoUrl { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }

    /// <summary>Street line for lease / notice mailing address.</summary>
    public string? MailingStreet { get; set; }
    public string? MailingCity { get; set; }
    public string? MailingState { get; set; }
    public string? MailingZip { get; set; }
    public string? MailingCountry { get; set; }
    public bool NoticeAddressSameAsMailing { get; set; } = true;
    public string? NoticeStreet { get; set; }
    public string? NoticeCity { get; set; }
    public string? NoticeState { get; set; }
    public string? NoticeZip { get; set; }
    public string? NoticeCountry { get; set; }

    /// <summary>Optional California broker disclosure fields for lease generation.</summary>
    public string? BrokerName { get; set; }
    public string? BrokerDreLicense { get; set; }
    public string? BrokerScopeNotes { get; set; }
    public string? Languages { get; set; }
    public string? Occupation { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public bool IsGovernmentIdVerified { get; set; }
    public bool IsPhoneVerified { get; set; }

    /// <summary>SHA-256 hex hash of the pending phone OTP (never store the raw code).</summary>
    public string? PhoneVerificationCodeHash { get; set; }

    public DateTime? PhoneVerificationExpiresAt { get; set; }

    /// <summary>When the last OTP SMS was sent (used for cooldown).</summary>
    public DateTime? PhoneVerificationSentAt { get; set; }

    /// <summary>Start of the current hourly send window for rate limiting.</summary>
    public DateTime? PhoneVerificationWindowStartedAt { get; set; }

    /// <summary>OTP sends within the current hourly window.</summary>
    public int PhoneVerificationSendCount { get; set; }

    public int? ResponseRatePercent { get; set; }
    public int? ResponseTimeMinutes { get; set; }

    /// <summary>
    /// Cached Stripe Customer id for this user. Populated lazily the first
    /// time we call <see cref="Lagedra.Infrastructure.External.Payments.IStripeService.GetOrCreateCustomerAsync"/>
    /// from the Phase 16.9 booking pre-flight (SetupIntent), then reused for
    /// subsequent off-session charges so the tenant only sees a card-input
    /// surface once per account.
    /// </summary>
    public string? StripeCustomerId { get; set; }
}
