using System.Security.Claims;
using Lagedra.Modules.IdentityAndVerification.Application.Commands;
using Lagedra.Modules.IdentityAndVerification.Infrastructure.Persistence;
using Lagedra.Modules.IdentityAndVerification.Presentation.Contracts;
using Lagedra.SharedKernel.Security;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.IdentityAndVerification.Presentation.Endpoints;

public static class HostPaymentEndpoints
{
    public static IEndpointRouteBuilder MapHostPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/v1/hosts/payment-details")
            .WithTags("HostPayment")
            .RequireAuthorization();

        group.MapPut("/", SavePaymentDetails);
        group.MapGet("/", GetPaymentDetails);

        return app;
    }

    private static async Task<IResult> SavePaymentDetails(
        [FromBody] SavePaymentDetailsRequest request,
        ClaimsPrincipal user,
        IMediator mediator,
        CancellationToken ct)
    {
        var userId = GetUserId(user);
        var result = await mediator
            .Send(new SaveHostPaymentDetailsCommand(userId, request.PaymentInfo), ct)
            .ConfigureAwait(false);

        return result.IsSuccess
            ? Results.Ok()
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> GetPaymentDetails(
        ClaimsPrincipal user,
        IdentityDbContext dbContext,
        IEncryptionService encryptionService,
        CancellationToken ct)
    {
        var userId = GetUserId(user);

        var details = await dbContext.HostPaymentDetails
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.HostUserId == userId, ct)
            .ConfigureAwait(false);

        if (details is null)
        {
            return Results.NotFound(new { error = "PaymentDetails.NotFound", detail = "No payment details configured." });
        }

        var decrypted = encryptionService.Decrypt(details.EncryptedPaymentInfo);
        return Results.Ok(new { paymentInfo = decrypted });
    }

    private static Guid GetUserId(ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("User ID claim not found."));
}
