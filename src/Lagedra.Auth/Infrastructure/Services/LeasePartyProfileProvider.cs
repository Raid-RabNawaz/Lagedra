using Lagedra.Auth.Domain;
using Lagedra.Auth.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Auth.Infrastructure.Services;

public sealed class LeasePartyProfileProvider(AuthDbContext dbContext) : ILeasePartyProfileProvider
{
    public async Task<LeasePartyProfileDto?> GetAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await dbContext.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, ct)
            .ConfigureAwait(false);

        if (user is null)
        {
            return null;
        }

        return new LeasePartyProfileDto(
            user.Id,
            ResolveFullName(user),
            user.Email,
            user.PhoneNumber,
            FormatAddress(
                user.MailingStreet, user.MailingCity, user.MailingState, user.MailingZip, user.MailingCountry)
            ?? FormatAddress(null, user.City, user.State, null, user.Country),
            user.NoticeAddressSameAsMailing
                ? FormatAddress(
                    user.MailingStreet, user.MailingCity, user.MailingState, user.MailingZip, user.MailingCountry)
                : FormatAddress(
                    user.NoticeStreet, user.NoticeCity, user.NoticeState, user.NoticeZip, user.NoticeCountry),
            user.BrokerName,
            user.BrokerDreLicense,
            user.BrokerScopeNotes);
    }

    private static string ResolveFullName(ApplicationUser user)
    {
        var combined = $"{user.FirstName} {user.LastName}".Trim();
        if (!string.IsNullOrWhiteSpace(combined))
        {
            return combined;
        }

        return string.IsNullOrWhiteSpace(user.DisplayName) ? user.Email ?? user.Id.ToString() : user.DisplayName!;
    }

    private static string? FormatAddress(
        string? street, string? city, string? state, string? zip, string? country)
    {
        var parts = new[] { street, city, string.Join(" ", new[] { state, zip }.Where(s => !string.IsNullOrWhiteSpace(s))), country }
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p!.Trim())
            .ToList();

        return parts.Count == 0 ? null : string.Join(", ", parts);
    }
}
