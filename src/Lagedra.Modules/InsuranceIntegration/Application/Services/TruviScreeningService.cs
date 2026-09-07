using Lagedra.Modules.InsuranceIntegration.Domain.Aggregates;
using Lagedra.Modules.InsuranceIntegration.Domain.Entities;
using Lagedra.Modules.InsuranceIntegration.Domain.Enums;
using Lagedra.Modules.InsuranceIntegration.Infrastructure.Truvi;
using Lagedra.SharedKernel.Insurance;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Time;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lagedra.Modules.InsuranceIntegration.Application.Services;

public sealed partial class TruviScreeningService(
    ITruviScreenAndProtectClient client,
    IInsurancePolicyRecordStore records,
    IDealApplicationStatusProvider deals,
    IListingProvider listings,
    ILeasePartyProfileProvider parties,
    IClock clock,
    IOptions<TruviScreenAndProtectSettings> settings,
    ILogger<TruviScreeningService> logger)
{
    private static readonly Error Disabled = new(
        "Insurance.TruviDisabled",
        "Screen & Protect is disabled or SubscriptionKey is empty.");

    private readonly TruviScreenAndProtectSettings _settings = settings.Value;

    public async Task RequestForDealAsync(Guid dealId, CancellationToken cancellationToken)
    {
        if (!_settings.CanCallApi)
        {
            LogScreeningSkipped(logger, dealId, "Screening is disabled or SubscriptionKey is empty.");
            return;
        }

        var deal = await deals.GetDealDetailsAsync(dealId, cancellationToken).ConfigureAwait(false);
        if (deal is null)
        {
            LogScreeningSkipped(logger, dealId, "Deal application was not found.");
            return;
        }

        var today = DateOnly.FromDateTime(clock.UtcNow);
        if (deal.RequestedCheckIn < today)
        {
            await PersistFailedAsync(
                deal.TenantUserId,
                dealId,
                "Check-in is in the past; Truvi create was not sent.",
                cancellationToken).ConfigureAwait(false);
            return;
        }

        var record = await records.GetByDealIdAsync(dealId, cancellationToken).ConfigureAwait(false);
        if (record?.HasExternalVerification == true)
        {
            LogAlreadyScreened(logger, dealId, record.ExternalVerificationId!);
            return;
        }

        var created = await CreateVerificationAsync(
            deal,
            record,
            reservationId: TruviVerificationRequestFactory.ReservationIdForDeal(dealId),
            cancellationToken).ConfigureAwait(false);

        if (created.IsFailure && created.Error.Code == "Insurance.InvalidPayload")
        {
            await PersistFailedAsync(
                deal.TenantUserId,
                dealId,
                created.Error.Description,
                cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<Result> ModifyForDealAsync(Guid dealId, CancellationToken cancellationToken)
    {
        if (!_settings.CanCallApi)
        {
            return Result.Failure(Disabled);
        }

        var record = await records.GetByDealIdAsync(dealId, cancellationToken).ConfigureAwait(false);
        if (record is null || !record.HasExternalVerification)
        {
            return Result.Failure(new Error(
                "Insurance.NotScreened",
                "No Truvi verification exists to modify."));
        }

        if (string.Equals(record.ScreeningStatus, TruviScreeningStatus.Cancelled, StringComparison.OrdinalIgnoreCase)
            || string.Equals(record.ScreeningStatus, TruviScreeningStatus.Rejected, StringComparison.OrdinalIgnoreCase)
            || string.Equals(record.ScreeningStatus, TruviScreeningStatus.Failed, StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure(new Error(
                "Insurance.NotModifiable",
                $"A {record.ScreeningStatus} verification cannot be modified."));
        }

        var deal = await deals.GetDealDetailsAsync(dealId, cancellationToken).ConfigureAwait(false);
        if (deal is null)
        {
            return Result.Failure(new Error("Insurance.DealNotFound", "Deal application was not found."));
        }

        var listing = await listings.GetListingDetailsAsync(deal.ListingId, cancellationToken)
            .ConfigureAwait(false);
        var request = TruviVerificationRequestFactory.Modify(
            dealId,
            clock.UtcNow,
            record.ExternalVerificationId!,
            record.TruviReservationId,
            deal.RequestedCheckIn,
            deal.RequestedCheckOut,
            listing?.HouseRules?.PetsAllowed ?? false);

        try
        {
            await client.ModifyAsync(request, cancellationToken).ConfigureAwait(false);
            records.AddAttempt(record, new InsuranceVerificationAttempt(
                record.Id,
                Truncate($"Truvi modified {record.ExternalVerificationId}"),
                VerificationSource.API));
            await records.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            LogModified(logger, dealId, record.ExternalVerificationId!);
            return Result.Success();
        }
        catch (TruviScreenAndProtectException ex)
        {
            LogModifyFailed(logger, dealId, ex);
            return Result.Failure(new Error("Insurance.TruviFailed", ex.Message));
        }
    }

    public async Task<Result> RescreenForDealAsync(Guid dealId, CancellationToken cancellationToken)
    {
        if (!_settings.CanCallApi)
        {
            return Result.Failure(Disabled);
        }

        var deal = await deals.GetDealDetailsAsync(dealId, cancellationToken).ConfigureAwait(false);
        if (deal is null)
        {
            return Result.Failure(new Error("Insurance.DealNotFound", "Deal application was not found."));
        }

        var today = DateOnly.FromDateTime(clock.UtcNow);
        if (deal.RequestedCheckIn < today)
        {
            return Result.Failure(new Error(
                "Insurance.CheckInPast",
                "Check-in is in the past; Truvi create was not sent."));
        }

        var record = await records.GetByDealIdAsync(dealId, cancellationToken).ConfigureAwait(false);
        if (record is null
            || !string.Equals(record.ScreeningStatus, TruviScreeningStatus.Flagged, StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure(new Error(
                "Insurance.NotFlagged",
                "Only a Flagged screening can be sent again after the guest updates contact details."));
        }

        return await CreateVerificationAsync(
            deal,
            record,
            TruviVerificationRequestFactory.RescreenReservationId(dealId, clock.UtcNow),
            cancellationToken).ConfigureAwait(false);
    }

    public async Task CancelForDealAsync(Guid dealId, string reason, CancellationToken cancellationToken)
    {
        var record = await records.GetByDealIdAsync(dealId, cancellationToken).ConfigureAwait(false);
        if (record is null)
        {
            LogNoPolicy(logger, dealId);
            return;
        }

        if (record.HasExternalVerification && _settings.CanCallApi)
        {
            var deal = await deals.GetDealDetailsAsync(dealId, cancellationToken).ConfigureAwait(false);
            var today = DateOnly.FromDateTime(clock.UtcNow);
            if (deal is not null && deal.RequestedCheckIn <= today)
            {
                LogCancelSkippedAfterCheckIn(logger, dealId);
            }
            else
            {
                try
                {
                    var request = TruviVerificationRequestFactory.Cancel(
                        dealId,
                        clock.UtcNow,
                        record.ExternalVerificationId!,
                        record.TruviReservationId);
                    await client.CancelAsync(request, cancellationToken).ConfigureAwait(false);
                }
                catch (TruviScreenAndProtectException ex)
                {
                    LogCancelFailed(logger, dealId, ex);
                }
            }
        }

        record.MarkScreeningCancelled(reason);
        await records.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<Result> CreateVerificationAsync(
        DealApplicationDetailsDto deal,
        InsurancePolicyRecord? record,
        string reservationId,
        CancellationToken cancellationToken)
    {
        var listing = await listings.GetListingDetailsAsync(deal.ListingId, cancellationToken)
            .ConfigureAwait(false);
        if (TruviVerificationRequestFactory.IsExcludedListingType(listing?.PropertyType))
        {
            return Result.Failure(new Error(
                "Insurance.InvalidPayload",
                "Truvi does not cover event or communal spaces."));
        }

        var host = await parties.GetAsync(deal.LandlordUserId, cancellationToken).ConfigureAwait(false);
        if (!TruviVerificationRequestFactory.TryResolveCompany(
            host?.CompanyName,
            host?.FullName,
            host?.Email,
            out var companyName,
            out var companyEmail,
            out var companyError))
        {
            return Result.Failure(new Error(
                "Insurance.InvalidPayload",
                companyError ?? "Host or property-manager identity is incomplete."));
        }

        var guest = await parties.GetAsync(deal.TenantUserId, cancellationToken).ConfigureAwait(false);
        var address = listing?.PreciseAddress;

        if (!TruviVerificationRequestFactory.TryCreate(
            deal.DealId,
            clock.UtcNow,
            companyName,
            companyEmail,
            _settings.ExtendedAmount,
            address?.Street,
            address?.City,
            address?.ZipCode,
            address?.Country,
            listing?.HouseRules?.PetsAllowed ?? false,
            deal.GuestCount,
            listing?.Bedrooms ?? 0,
            listing?.Bathrooms ?? 0m,
            deal.RequestedCheckIn,
            deal.RequestedCheckOut,
            guest?.FirstName,
            guest?.LastName,
            guest?.FullName,
            guest?.Email,
            guest?.Phone,
            deal.DepositAmountCents,
            reservationId,
            out var request,
            out var error))
        {
            return Result.Failure(new Error(
                "Insurance.InvalidPayload",
                error ?? "Invalid screening payload."));
        }

        record ??= EnsureRecord(deal.TenantUserId, deal.DealId);

        try
        {
            var result = await client.CreateAsync(request!, cancellationToken).ConfigureAwait(false);
            ArgumentNullException.ThrowIfNull(result);
            var expiresAt = DateTime.SpecifyKind(
                deal.RequestedCheckOut.ToDateTime(TimeOnly.MinValue),
                DateTimeKind.Utc);
            record.RecordScreeningResult(
                result.VerificationId,
                result.Status,
                result.FlaggedReason,
                expiresAt,
                reservationId);
            records.AddAttempt(record, new InsuranceVerificationAttempt(
                record.Id,
                Truncate($"Truvi {result.Status} {result.VerificationId}"),
                VerificationSource.API));
            LogScreened(logger, deal.DealId, result.VerificationId, result.Status);
            await records.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (TruviScreenAndProtectException ex)
        {
            record.RecordScreeningFailed(ex.Message);
            records.AddAttempt(record, new InsuranceVerificationAttempt(
                record.Id,
                Truncate(ex.Message),
                VerificationSource.API));
            LogScreeningFailed(logger, deal.DealId, ex);
            await records.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result.Failure(new Error("Insurance.TruviFailed", ex.Message));
        }
    }

    private InsurancePolicyRecord EnsureRecord(Guid tenantUserId, Guid dealId)
    {
        var record = InsurancePolicyRecord.Create(tenantUserId, dealId);
        records.Add(record);
        return record;
    }

    private async Task PersistFailedAsync(
        Guid tenantUserId,
        Guid dealId,
        string reason,
        CancellationToken cancellationToken)
    {
        var record = await records.GetByDealIdAsync(dealId, cancellationToken).ConfigureAwait(false)
            ?? EnsureRecord(tenantUserId, dealId);
        record.RecordScreeningFailed(reason);
        records.AddAttempt(record, new InsuranceVerificationAttempt(
            record.Id,
            Truncate(reason),
            VerificationSource.API));
        await records.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        LogScreeningSkipped(logger, dealId, reason);
    }

    private static string Truncate(string value)
        => value.Length <= 500 ? value : value[..500];

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Truvi screening skipped for deal {DealId}: {Reason}")]
    private static partial void LogScreeningSkipped(ILogger logger, Guid dealId, string reason);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Truvi screening already exists for deal {DealId}: {VerificationId}")]
    private static partial void LogAlreadyScreened(ILogger logger, Guid dealId, string verificationId);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Truvi screening stored for deal {DealId}: {VerificationId} {Status}")]
    private static partial void LogScreened(ILogger logger, Guid dealId, string verificationId, string status);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Truvi screening call failed for deal {DealId}")]
    private static partial void LogScreeningFailed(ILogger logger, Guid dealId, Exception exception);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Truvi verification modified for deal {DealId}: {VerificationId}")]
    private static partial void LogModified(ILogger logger, Guid dealId, string verificationId);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Truvi modify failed for deal {DealId}")]
    private static partial void LogModifyFailed(ILogger logger, Guid dealId, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Truvi cancel failed for deal {DealId}")]
    private static partial void LogCancelFailed(ILogger logger, Guid dealId, Exception exception);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Truvi cancel skipped for deal {DealId} because check-in has started")]
    private static partial void LogCancelSkippedAfterCheckIn(ILogger logger, Guid dealId);

    [LoggerMessage(Level = LogLevel.Debug,
        Message = "No insurance policy record found for deal {DealId} — nothing to cancel")]
    private static partial void LogNoPolicy(ILogger logger, Guid dealId);
}
