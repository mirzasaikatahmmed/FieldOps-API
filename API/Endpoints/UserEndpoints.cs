using FieldOps.BLL.DTOs.Users;
using FieldOps.BLL.Services;
using FieldOps.COMMON.Constants;
using FieldOps.COMMON.Models;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace FieldOps.API.Endpoints;

public static class UserEndpoints
{
    public static RouteGroupBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users")
            .WithTags("Users")
            .RequireRoles(Roles.CompanyAdmin);

        group.MapGet("/", async (
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            IUserService userService,
            CancellationToken cancellationToken) =>
        {
            var pagination = new PaginationQuery { Page = page ?? 1, PageSize = pageSize ?? 20 };
            var result = await userService.GetUsersAsync(pagination, cancellationToken);
            return result.ToHttpResult();
        })
        .WithSummary("List company users");

        group.MapPost("/", async (
            CreateUserRequest request,
            IValidator<CreateUserRequest> validator,
            IUserService userService,
            CancellationToken cancellationToken) =>
        {
            await validator.ValidateAndThrowAsync(request, cancellationToken);
            var result = await userService.CreateUserAsync(request, cancellationToken);
            return result.ToHttpResult();
        })
        .WithSummary("Create Dispatcher or Technician user");

        group.MapDelete("/{id:guid}", async (
            Guid id,
            IUserService userService,
            CancellationToken cancellationToken) =>
        {
            var result = await userService.DeleteUserAsync(id, cancellationToken);
            return result.IsSuccess ? Results.NoContent() : result.ToHttpResult();
        })
        .WithSummary("Delete a company user");

        return group;
    }
}
