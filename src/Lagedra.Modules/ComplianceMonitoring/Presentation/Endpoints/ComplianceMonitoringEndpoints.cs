using Lagedra.Modules.ComplianceMonitoring.Application.Commands;
using Lagedra.Modules.ComplianceMonitoring.Application.Queries;
using Lagedra.Modules.ComplianceMonitoring.Presentation.Contracts;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Lagedra.Modules.ComplianceMonitoring.Presentation.Endpoints;

public static class ComplianceMonitoringEndpoints
{
    public static IEndpointRouteBuilder MapComplianceMonitoringEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/v1/deals/{dealId:guid}/compliance")
            .WithTags("ComplianceMonitoring")
            .RequireAuthorization();

        group.MapGet("/", GetDealComplianceStatus);
        group.MapGet("/violations", ListViolations);
        group.MapPost("/signal", RecordSignal);

        return app;
    }

    private static async Task<IResult> GetDealComplianceStatus(
        [FromRoute] Guid dealId,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(
            new GetDealComplianceStatusQuery(dealId), ct)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(new ComplianceStatusResponse(
                result.Value.DealId,
                result.Value.OpenViolations,
                result.Value.CuredViolations,
                result.Value.EscalatedViolations,
                result.Value.TotalSignals,
                result.Value.UnprocessedSignals,
                result.Value.IsCompliant))
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> ListViolations(
        [FromRoute] Guid dealId,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(
            new ListViolationsQuery(dealId), ct)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> RecordSignal(
        [FromRoute] Guid dealId,
        [FromBody] RecordSignalRequest request,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(
            new RecordComplianceSignalCommand(dealId, request.SignalType, request.Source), ct)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Created($"/v1/deals/{dealId}/compliance/signal/{result.Value}", new { signalId = result.Value })
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }
}
