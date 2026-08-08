using FieldOps.BLL.DTOs.Companies;
using FieldOps.BLL.Services;
using FieldOps.COMMON.Constants;
using FieldOps.COMMON.Models;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace FieldOps.API.Endpoints;

public static class CompanyEndpoints
{
    public static RouteGroupBuilder MapCompanyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/companies")
            .WithTags("Companies")
            .RequireRoles(Roles.SuperAdmin);

        group.MapGet("/", async (
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            [FromQuery] string? search,
            ICompanyService service,
            CancellationToken cancellationToken) =>
        {
            var pagination = new PaginationQuery { Page = page ?? 1, PageSize = pageSize ?? 20 };
            return (await service.GetAsync(pagination, search, cancellationToken)).ToHttpResult();
        })
        .WithSummary("List companies (SuperAdmin)");

        group.MapGet("/{id:guid}", async (Guid id, ICompanyService service, CancellationToken cancellationToken) =>
            (await service.GetByIdAsync(id, cancellationToken)).ToHttpResult())
        .WithSummary("Get company by id");

        group.MapPost("/", async (
            CreateCompanyRequest request,
            IValidator<CreateCompanyRequest> validator,
            ICompanyService service,
            CancellationToken cancellationToken) =>
        {
            await validator.ValidateAndThrowAsync(request, cancellationToken);
            return (await service.CreateAsync(request, cancellationToken)).ToHttpResult();
        })
        .WithSummary("Create company shell");

        group.MapPatch("/{id:guid}/deactivate", async (Guid id, ICompanyService service, CancellationToken cancellationToken) =>
            (await service.SetActiveAsync(id, false, cancellationToken)).ToHttpResult())
        .WithSummary("Deactivate company");

        group.MapPatch("/{id:guid}/activate", async (Guid id, ICompanyService service, CancellationToken cancellationToken) =>
            (await service.SetActiveAsync(id, true, cancellationToken)).ToHttpResult())
        .WithSummary("Activate company");

        return group;
    }
}
