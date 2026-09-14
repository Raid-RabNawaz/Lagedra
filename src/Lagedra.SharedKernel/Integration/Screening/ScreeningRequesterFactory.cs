namespace Lagedra.SharedKernel.Integration.Screening;

/// <summary>
/// Builds the end-user who receives a screening report (the paying host).
/// </summary>
public static class ScreeningRequesterFactory
{
    public static ScreeningRequester? TryCreateFromHost(Guid hostUserId, LeasePartyProfileDto? host)
    {
        if (host is null || string.IsNullOrWhiteSpace(host.Email))
        {
            return null;
        }

        var isBusiness = !string.IsNullOrWhiteSpace(host.CompanyName);
        var name = isBusiness ? host.CompanyName!.Trim() : PersonName(host);
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        return new ScreeningRequester(
            hostUserId.ToString(),
            name,
            host.Email.Trim(),
            ScreeningPhone.UsOrNull(host.Phone),
            BlankToNull(host.MailingStreet),
            BlankToNull(host.MailingCity),
            BlankToNull(host.MailingState),
            BlankToNull(host.MailingZip),
            isBusiness ? ScreeningRequesterType.Business : ScreeningRequesterType.Individual);
    }

    private static string? PersonName(LeasePartyProfileDto host)
    {
        var combined = $"{host.FirstName} {host.LastName}".Trim();
        if (!string.IsNullOrWhiteSpace(combined))
        {
            return combined;
        }

        if (!string.IsNullOrWhiteSpace(host.FullName)
            && !string.Equals(host.FullName, host.Email, StringComparison.OrdinalIgnoreCase))
        {
            return host.FullName.Trim();
        }

        return null;
    }

    private static string? BlankToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
