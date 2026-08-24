using Lagedra.SharedKernel.RealTime;

namespace Lagedra.Infrastructure.RealTime;

/// <summary>
/// Postgres NOTIFY channel shared by <see cref="PgNotifyNotificationPusher"/>
/// (worker side) and <see cref="PgNotificationRelayService"/> (API side).
/// </summary>
public static class InAppPushChannel
{
    public const string Name = "lagedra_inapp_push";
}

/// <summary>
/// Wire format for a push traveling over the NOTIFY channel.
/// </summary>
public sealed record InAppPushEnvelope(IReadOnlyList<Guid> UserIds, InAppNotificationDto Notification);
