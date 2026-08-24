namespace Lagedra.Infrastructure.RealTime;

/// <summary>
/// Selects how a host delivers real-time in-app notification pushes.
/// </summary>
public enum RealTimePushMode
{
    /// <summary>
    /// The host owns the browser-facing SignalR hub: push straight to the hub
    /// and relay pushes published by other hosts over Postgres NOTIFY.
    /// Used by the API gateway.
    /// </summary>
    SignalRHub,

    /// <summary>
    /// The host has no connected browsers (e.g. the worker): publish pushes
    /// over Postgres NOTIFY so the hub-owning host can forward them.
    /// Broadcasting to the local hub context would silently go nowhere —
    /// which is why worker-delivered notifications never reached users in
    /// real time before this existed.
    /// </summary>
    PostgresNotify,
}
