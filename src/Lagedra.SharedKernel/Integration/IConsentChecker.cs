namespace Lagedra.SharedKernel.Integration;

public interface IConsentChecker
{
    Task<bool> HasRequiredConsentsAsync(Guid userId, CancellationToken ct = default);
}
