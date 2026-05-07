namespace Lagedra.SharedKernel.Domain;

/// <summary>
/// Marker interface for aggregates that must never be soft-deleted or mutated
/// after creation/sealing. Entities implementing <see cref="IAppendOnly"/> are
/// excluded from the soft-delete query filter, and the <c>SoftDeleteInterceptor</c>
/// will throw if a delete is attempted at the application layer.
///
/// Used by Truth Surface aggregates so the "append-only legal anchor" claim is
/// honest at the application boundary, not just documentation.
/// </summary>
public interface IAppendOnly
{
}
