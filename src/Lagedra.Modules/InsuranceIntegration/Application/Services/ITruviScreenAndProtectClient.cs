using Lagedra.SharedKernel.Insurance;

namespace Lagedra.Modules.InsuranceIntegration.Application.Services;

public interface ITruviScreenAndProtectClient
{
    Task<TruviVerificationResult> CreateAsync(
        TruviCreateVerificationRequest request,
        CancellationToken cancellationToken = default);

    Task ModifyAsync(
        TruviModifyVerificationRequest request,
        CancellationToken cancellationToken = default);

    Task CancelAsync(
        TruviCancelVerificationRequest request,
        CancellationToken cancellationToken = default);
}
