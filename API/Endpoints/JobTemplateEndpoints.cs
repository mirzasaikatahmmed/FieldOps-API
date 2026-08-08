using FieldOps.BLL.DTOs.JobTemplates;
using FieldOps.BLL.Services;
using FieldOps.COMMON.Constants;
using FieldOps.COMMON.Models;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace FieldOps.API.Endpoints;

public static class JobTemplateEndpoints
{
    public static RouteGroupBuilder MapJobTemplateEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/job-templates")
            .WithTags("JobTemplates");

        group.MapGet("/", async (
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            IJobTemplateService service,
            CancellationToken cancellationToken) =>
        {
            var pagination = new PaginationQuery { Page = page ?? 1, PageSize = pageSize ?? 20 };
            return (await service.GetAsync(pagination, cancellationToken)).ToHttpResult();
        })
        .RequireRoles(Roles.CompanyAdmin, Roles.Dispatcher)
        .WithSummary("List job templates");

        group.MapPost("/", async (
            CreateJobTemplateRequest request,
            IValidator<CreateJobTemplateRequest> validator,
            IJobTemplateService service,
            CancellationToken cancellationToken) =>
        {
            await validator.ValidateAndThrowAsync(request, cancellationToken);
            return (await service.CreateAsync(request, cancellationToken)).ToHttpResult();
        })
        .RequireRoles(Roles.CompanyAdmin)
        .WithSummary("Create job template with fields");

        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateJobTemplateRequest request,
            IValidator<UpdateJobTemplateRequest> validator,
            IJobTemplateService service,
            CancellationToken cancellationToken) =>
        {
            await validator.ValidateAndThrowAsync(request, cancellationToken);
            return (await service.UpdateAsync(id, request, cancellationToken)).ToHttpResult();
        })
        .RequireRoles(Roles.CompanyAdmin)
        .WithSummary("Update job template");

        group.MapDelete("/{id:guid}", async (
            Guid id,
            IJobTemplateService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.DeleteAsync(id, cancellationToken);
            return result.IsSuccess ? Results.NoContent() : result.ToHttpResult();
        })
        .RequireRoles(Roles.CompanyAdmin)
        .WithSummary("Delete job template");

        return group;
    }
}
