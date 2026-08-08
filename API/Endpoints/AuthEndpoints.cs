using FieldOps.BLL.DTOs.Auth;
using FieldOps.BLL.Services;
using FluentValidation;

namespace FieldOps.API.Endpoints;

public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/register-company", async (
            RegisterCompanyRequest request,
            IValidator<RegisterCompanyRequest> validator,
            IAuthService authService,
            CancellationToken cancellationToken) =>
        {
            await validator.ValidateAndThrowAsync(request, cancellationToken);
            var result = await authService.RegisterCompanyAsync(request, cancellationToken);
            return result.ToHttpResult();
        })
        .WithSummary("Register a new company and first CompanyAdmin user")
        .AllowAnonymous();

        group.MapPost("/login", async (
            LoginRequest request,
            IValidator<LoginRequest> validator,
            IAuthService authService,
            CancellationToken cancellationToken) =>
        {
            await validator.ValidateAndThrowAsync(request, cancellationToken);
            var result = await authService.LoginAsync(request, cancellationToken);
            return result.ToHttpResult();
        })
        .WithSummary("Login and receive JWT access + refresh tokens")
        .AllowAnonymous();

        group.MapPost("/refresh", async (
            RefreshRequest request,
            IValidator<RefreshRequest> validator,
            IAuthService authService,
            CancellationToken cancellationToken) =>
        {
            await validator.ValidateAndThrowAsync(request, cancellationToken);
            var result = await authService.RefreshAsync(request, cancellationToken);
            return result.ToHttpResult();
        })
        .WithSummary("Exchange a refresh token for a new access token")
        .AllowAnonymous();

        return group;
    }
}
