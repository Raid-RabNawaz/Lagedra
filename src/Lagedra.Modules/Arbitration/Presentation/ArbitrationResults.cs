using Lagedra.SharedKernel.Results;
using Microsoft.AspNetCore.Http;

namespace Lagedra.Modules.Arbitration.Presentation;

internal static class ArbitrationResults
{
    internal static IResult ToErrorResult(Error error)
    {
        var payload = new { error = error.Code, detail = error.Description };

        if (error.Code.EndsWith(".Forbidden", StringComparison.Ordinal))
        {
            return Results.Json(payload, statusCode: StatusCodes.Status403Forbidden);
        }

        return error.Code switch
        {
            "Arbitration.CaseNotFound" => Results.NotFound(payload),
            _ => Results.BadRequest(payload)
        };
    }
}
