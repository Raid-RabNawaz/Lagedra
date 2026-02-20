using System.Security.Claims;
using Lagedra.Auth.Application.Commands;
using Lagedra.Auth.Application.Queries;
using Lagedra.Auth.Presentation.Contracts;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;

namespace Lagedra.Auth.Presentation.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/auth").WithTags("Auth");

        group.MapPost("/register", Register).AllowAnonymous();
        group.MapGet("/verify-email", VerifyEmail).AllowAnonymous();
        group.MapPost("/resend-verification", ResendVerification).AllowAnonymous();
        group.MapPost("/login", Login).AllowAnonymous();
        group.MapPost("/refresh", Refresh).AllowAnonymous();
        group.MapPost("/logout", Logout).RequireAuthorization();
        group.MapPost("/forgot-password", ForgotPassword).AllowAnonymous();
        group.MapPost("/reset-password", ResetPassword).AllowAnonymous();
        group.MapGet("/me", GetMe).RequireAuthorization();

        return app;
    }

    private static async Task<IResult> Register(
        [FromBody] RegisterRequest request,
        IMediator mediator,
        IWebHostEnvironment env,
        CancellationToken ct)
    {
        var result = await mediator.Send(
            new RegisterUserCommand(request.Email, request.Password, request.Role), ct).ConfigureAwait(true);

        if (!result.IsSuccess)
        {
            return Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
        }

        var dto = result.Value;

        if (env.IsDevelopment())
        {
            return Results.Ok(new
            {
                userId = dto.UserId,
                message = "Registration successful. Check your email to verify your account.",
                dev_verificationToken = dto.VerificationToken,
                dev_verificationUrl = dto.VerificationUrl.ToString()
            });
        }

        return Results.Ok(new
        {
            userId = dto.UserId,
            message = "Registration successful. Check your email to verify your account."
        });
    }

    private static async Task<IResult> VerifyEmail(
        [FromQuery] Guid userId,
        [FromQuery] string token,
        IMediator mediator,
        CancellationToken ct)
    {
        // URL-decode the token to handle both direct email link clicks and
        // Swagger/Postman testing where the token may arrive double-encoded.
        var decodedToken = Uri.UnescapeDataString(token);

        var result = await mediator.Send(new VerifyEmailCommand(userId, decodedToken), ct).ConfigureAwait(true);
        return result.IsSuccess
            ? Results.Ok(new { message = "Email verified successfully. You may now log in." })
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> ResendVerification(
        [FromBody] ResendVerificationRequest request,
        IMediator mediator,
        IWebHostEnvironment env,
        CancellationToken ct)
    {
        var result = await mediator
            .Send(new ResendVerificationCommand(request.Email), ct).ConfigureAwait(true);

        if (!result.IsSuccess)
        {
            return Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
        }

        var dto = result.Value;

        if (env.IsDevelopment() && dto.Sent)
        {
            return Results.Ok(new
            {
                message = "If your email is registered and unverified, a new verification link has been sent.",
                dev_verificationToken = dto.VerificationToken,
                dev_verificationUrl = dto.VerificationUrl?.ToString()
            });
        }

        return Results.Ok(new
        {
            message = "If your email is registered and unverified, a new verification link has been sent."
        });
    }

    private static async Task<IResult> Login(
        [FromBody] LoginRequest request,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken ct)
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var result = await mediator.Send(new LoginCommand(request.Email, request.Password, ip), ct).ConfigureAwait(true);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Unauthorized();
    }

    private static async Task<IResult> Refresh(
        [FromBody] RefreshTokenRequest request,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken ct)
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var result = await mediator.Send(new RefreshTokenCommand(request.RefreshToken, ip), ct).ConfigureAwait(true);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Unauthorized();
    }

    private static async Task<IResult> Logout(
        [FromBody] RefreshTokenRequest request,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken ct)
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var result = await mediator.Send(new RevokeTokenCommand(request.RefreshToken, ip), ct).ConfigureAwait(true);
        return result.IsSuccess
            ? Results.NoContent()
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> ForgotPassword(
        [FromBody] ForgotPasswordCommand command,
        IMediator mediator,
        CancellationToken ct)
    {
        await mediator.Send(command, ct).ConfigureAwait(true);
        return Results.Ok(new { message = "If an account with that email exists, a reset link has been sent." });
    }

    private static async Task<IResult> ResetPassword(
        [FromBody] ResetPasswordRequest request,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(
            new ResetPasswordCommand(request.UserId, request.Token, request.NewPassword), ct).ConfigureAwait(true);
        return result.IsSuccess
            ? Results.Ok(new { message = "Password reset successfully." })
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> GetMe(
        ClaimsPrincipal principal,
        IMediator mediator,
        CancellationToken ct)
    {
        var userIdClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Results.Unauthorized();
        }

        var result = await mediator.Send(new GetCurrentUserQuery(userId), ct).ConfigureAwait(true);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.NotFound(new { error = result.Error.Code });
    }
}
