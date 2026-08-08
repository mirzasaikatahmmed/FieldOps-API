using FieldOps.BLL.DTOs.Customers;
using FieldOps.BLL.Services;
using FieldOps.COMMON.Constants;
using FieldOps.COMMON.Models;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace FieldOps.API.Endpoints;

public static class CustomerEndpoints
{
    public static RouteGroupBuilder MapCustomerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/customers")
            .WithTags("Customers")
            .RequireRoles(Roles.CompanyAdmin, Roles.Dispatcher, Roles.Technician);

        group.MapGet("/", async (
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            [FromQuery] string? search,
            ICustomerService service,
            CancellationToken cancellationToken) =>
        {
            var pagination = new PaginationQuery { Page = page ?? 1, PageSize = pageSize ?? 20 };
            return (await service.GetAsync(pagination, search, cancellationToken)).ToHttpResult();
        })
        .WithSummary("List customers (optional search)");

        group.MapGet("/{id:guid}", async (Guid id, ICustomerService service, CancellationToken cancellationToken) =>
            (await service.GetByIdAsync(id, cancellationToken)).ToHttpResult())
        .WithSummary("Get customer by id");

        group.MapPost("/", async (
            CreateCustomerRequest request,
            IValidator<CreateCustomerRequest> validator,
            ICustomerService service,
            CancellationToken cancellationToken) =>
        {
            await validator.ValidateAndThrowAsync(request, cancellationToken);
            return (await service.CreateAsync(request, cancellationToken)).ToHttpResult();
        })
        .RequireRoles(Roles.CompanyAdmin, Roles.Dispatcher)
        .WithSummary("Create customer");

        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateCustomerRequest request,
            IValidator<UpdateCustomerRequest> validator,
            ICustomerService service,
            CancellationToken cancellationToken) =>
        {
            await validator.ValidateAndThrowAsync(request, cancellationToken);
            return (await service.UpdateAsync(id, request, cancellationToken)).ToHttpResult();
        })
        .RequireRoles(Roles.CompanyAdmin, Roles.Dispatcher)
        .WithSummary("Update customer");

        return group;
    }
}
