using Lagedra.Modules.ChannelIntegration.Domain.Aggregates;

namespace Lagedra.Modules.ChannelIntegration.Application.DTOs;

public sealed record ChannelConnectionDto(
    Guid Id,
    string ProviderKey,
    string ExternalAccountId,
    string DisplayName,
    string Status,
    DateTime? LastContentSyncAt,
    DateTime? LastBookingSyncAt,
    string? LastError,
    DateTime CreatedAt);

public static class ChannelConnectionMapper
{
    public static ChannelConnectionDto ToDto(ChannelConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);

        return new ChannelConnectionDto(
            connection.Id,
            connection.ProviderKey,
            connection.ExternalAccountId,
            connection.DisplayName,
            connection.Status.ToString(),
            connection.LastContentSyncAt,
            connection.LastBookingSyncAt,
            connection.LastError,
            connection.CreatedAt);
    }
}
