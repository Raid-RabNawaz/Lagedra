using System.Security.Claims;
using Lagedra.Modules.IdentityAndVerification.Application.Commands;
using Lagedra.Modules.IdentityAndVerification.Application.DTOs;
using Lagedra.Modules.IdentityAndVerification.Application.Queries;
using Lagedra.Modules.IdentityAndVerification.Domain.Enums;
using Lagedra.Modules.IdentityAndVerification.Domain.ValueObjects;
using Lagedra.Modules.IdentityAndVerification.Presentation.Contracts;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Lagedra.Modules.IdentityAndVerification.Presentation.Endpoints;

public static class IdentityEndpoints
{
    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/v1/identity")
            .WithTags("Identity")
            .RequireAuthorization();

        group.MapPost("/kyc/start", StartKyc);
        group.MapPost("/kyc/complete", CompleteKyc);
        group.MapGet("/status", GetStatus);

        // Manual KYC — user uploads ID photos + a live selfie, an admin
        // reviews them in the manual verification queue.
        group.MapPost("/kyc/manual/documents", UploadManualKycDocument)
            .DisableAntiforgery()
            .WithMetadata(new RequestSizeLimitAttribute(12L * 1024 * 1024));
        group.MapGet("/kyc/manual/documents", GetMyManualKycDocuments);
        group.MapPost("/kyc/manual/submit", SubmitManualKyc);

        return app;
    }

    private static async Task<IResult> UploadManualKycDocument(
        [FromForm] string documentType,
        IFormFile file,
        ClaimsPrincipal principal,
        IMediator mediator,
        CancellationToken ct)
    {
        var userId = GetUserId(principal);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        if (file is null || file.Length == 0)
        {
            return Results.BadRequest(new
            {
                error = "Identity.Kyc.EmptyFile",
                detail = "No file was uploaded.",
            });
        }

        if (!Enum.TryParse<KycDocumentType>(documentType, ignoreCase: true, out var parsedType))
        {
            return Results.BadRequest(new
            {
                error = "Identity.Kyc.InvalidDocumentType",
                detail = "documentType must be IdFront, IdBack, or Selfie.",
            });
        }

        var result = await mediator.Send(
            new UploadKycDocumentCommand(
                userId.Value,
                parsedType,
                file.FileName,
                file.ContentType,
                file.Length,
                _ => Task.FromResult(file.OpenReadStream())),
            ct).ConfigureAwait(false);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> GetMyManualKycDocuments(
        ClaimsPrincipal principal,
        IMediator mediator,
        CancellationToken ct)
    {
        var userId = GetUserId(principal);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var result = await mediator.Send(new GetMyKycDocumentsQuery(userId.Value), ct)
            .ConfigureAwait(false);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> SubmitManualKyc(
        [FromBody] SubmitManualKycRequest request,
        ClaimsPrincipal principal,
        IMediator mediator,
        CancellationToken ct)
    {
        var userId = GetUserId(principal);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var result = await mediator.Send(
            new SubmitManualKycCommand(
                userId.Value, request.FirstName, request.LastName, request.DateOfBirth),
            ct).ConfigureAwait(false);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static Guid? GetUserId(ClaimsPrincipal principal)
    {
        var raw = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    private static async Task<IResult> StartKyc(
        [FromBody] StartKycRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new StartKycCommand(request.UserId, request.FirstName, request.LastName, request.DateOfBirth),
            cancellationToken)
            .ConfigureAwait(false);

        return result.IsSuccess
            ? Results.Created($"/v1/identity/status?userId={result.Value.UserId}", result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> CompleteKyc(
        [FromBody] CompleteKycRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new CompleteKycCommand(request.UserId, request.ExternalInquiryId),
            cancellationToken)
            .ConfigureAwait(false);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> GetStatus(
        [FromQuery] Guid userId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetVerificationStatusQuery(userId),
            cancellationToken)
            .ConfigureAwait(false);

        if (result.IsSuccess)
        {
            return Results.Ok(result.Value);
        }

        if (result.Error.Code == "Identity.NotFound")
        {
            var notStarted = new VerificationStatusDto(
                Guid.Empty,
                userId,
                VerificationStatus.NotStarted,
                VerificationClass.Low,
                null,
                null,
                null,
                DateTime.UtcNow);

            return Results.Ok(notStarted);
        }

        return Results.NotFound(new { error = result.Error.Code, detail = result.Error.Description });
    }
}

public sealed record CompleteKycRequest(Guid UserId, string? ExternalInquiryId);

public sealed record SubmitManualKycRequest(
    string? FirstName,
    string? LastName,
    DateTime? DateOfBirth);
