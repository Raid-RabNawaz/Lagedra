using Lagedra.Infrastructure.External.Channels;
using Lagedra.Modules.ChannelIntegration.Domain.Aggregates;
using Lagedra.SharedKernel.Security;

namespace Lagedra.Modules.ChannelIntegration.Infrastructure.Services;

internal static class ChannelConnectionExtensions
{
    /// <summary>
    /// Builds the provider call credentials from a connection, decrypting the
    /// stored secret just-in-time. The provider supplies its own base URL from
    /// static settings, so it is left null here.
    /// </summary>
    public static ChannelCredentials ToCredentials(
        this ChannelConnection connection,
        IEncryptionService encryption)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(encryption);

        var secret = string.IsNullOrEmpty(connection.EncryptedSecret)
            ? null
            : encryption.Decrypt(connection.EncryptedSecret);

        return new ChannelCredentials(
            connection.ProviderKey,
            connection.ExternalAccountId,
            connection.Username,
            secret);
    }
}
