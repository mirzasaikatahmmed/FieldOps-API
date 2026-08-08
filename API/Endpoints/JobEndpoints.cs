using FieldOps.BLL.DTOs.Jobs;
using FieldOps.BLL.Services;
using FieldOps.COMMON.Constants;
using FieldOps.COMMON.Enums;
using FieldOps.COMMON.Models;
using FieldOps.DAL.Repositories;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace FieldOps.API.Endpoints;

public static class JobEndpoints
{
    public static RouteGroupBuilder MapJobEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/jobs").WithTags("Jobs");

        group.MapGet("/", async (
            [FromQuery] JobStatus? status,
            [FromQuery] Guid? technicianId,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            IJobService service,
            CancellationToken cancellationToken) =>
        {
            var filter = new JobFilter
            {
                Status = status,
                TechnicianId = technicianId,
                From = from,
                To = to
            };
            var pagination = new PaginationQuery { Page = page ?? 1, PageSize = pageSize ?? 20 };
            return (await service.GetAsync(filter, pagination, cancellationToken)).ToHttpResult();
        })
        .RequireRoles(Roles.CompanyAdmin, Roles.Dispatcher, Roles.Technician)
        .WithSummary("List jobs with filters and pagination");

        group.MapPost("/", async (
            CreateJobRequest request,
            IValidator<CreateJobRequest> validator,
            IJobService service,
            CancellationToken cancellationToken) =>
        {
            await validator.ValidateAndThrowAsync(request, cancellationToken);
            return (await service.CreateAsync(request, cancellationToken)).ToHttpResult();
        })
        .RequireRoles(Roles.CompanyAdmin, Roles.Dispatcher)
        .WithSummary("Create a job");

        group.MapGet("/{id:guid}", async (Guid id, IJobService service, CancellationToken cancellationToken) =>
            (await service.GetByIdAsync(id, cancellationToken)).ToHttpResult())
        .RequireRoles(Roles.CompanyAdmin, Roles.Dispatcher, Roles.Technician)
        .WithSummary("Get job details");

        group.MapPatch("/{id:guid}/assign", async (
            Guid id,
            AssignJobRequest request,
            IValidator<AssignJobRequest> validator,
            IJobService service,
            CancellationToken cancellationToken) =>
        {
            await validator.ValidateAndThrowAsync(request, cancellationToken);
            return (await service.AssignAsync(id, request, cancellationToken)).ToHttpResult();
        })
        .RequireRoles(Roles.CompanyAdmin, Roles.Dispatcher)
        .WithSummary("Assign or reassign technician");

        group.MapPatch("/{id:guid}/status", async (
            Guid id,
            UpdateJobStatusRequest request,
            IValidator<UpdateJobStatusRequest> validator,
            IJobService service,
            CancellationToken cancellationToken) =>
        {
            await validator.ValidateAndThrowAsync(request, cancellationToken);
            return (await service.UpdateStatusAsync(id, request, cancellationToken)).ToHttpResult();
        })
        .RequireRoles(Roles.CompanyAdmin, Roles.Dispatcher, Roles.Technician)
        .WithSummary("Update job status (broadcasts via SignalR)");

        group.MapPost("/{id:guid}/responses", async (
            Guid id,
            SubmitJobResponsesRequest request,
            IValidator<SubmitJobResponsesRequest> validator,
            IJobService service,
            CancellationToken cancellationToken) =>
        {
            await validator.ValidateAndThrowAsync(request, cancellationToken);
            var result = await service.SubmitResponsesAsync(id, request, cancellationToken);
            return result.IsSuccess ? Results.NoContent() : result.ToHttpResult();
        })
        .RequireRoles(Roles.Technician, Roles.CompanyAdmin, Roles.Dispatcher)
        .WithSummary("Submit checklist responses");

        group.MapPost("/{id:guid}/photos/presign", async (
            Guid id,
            PresignUploadRequest request,
            IValidator<PresignUploadRequest> validator,
            IJobService service,
            CancellationToken cancellationToken) =>
        {
            await validator.ValidateAndThrowAsync(request, cancellationToken);
            return (await service.PresignPhotoAsync(id, request, cancellationToken)).ToHttpResult();
        })
        .RequireRoles(Roles.Technician, Roles.CompanyAdmin, Roles.Dispatcher)
        .WithSummary("Get presigned URL for photo upload");

        group.MapPost("/{id:guid}/photos", async (
            Guid id,
            ConfirmPhotoRequest request,
            IValidator<ConfirmPhotoRequest> validator,
            IJobService service,
            CancellationToken cancellationToken) =>
        {
            await validator.ValidateAndThrowAsync(request, cancellationToken);
            return (await service.ConfirmPhotoAsync(id, request, cancellationToken)).ToHttpResult();
        })
        .RequireRoles(Roles.Technician, Roles.CompanyAdmin, Roles.Dispatcher)
        .WithSummary("Confirm photo upload");

        group.MapPost("/{id:guid}/signature/presign", async (
            Guid id,
            PresignUploadRequest request,
            IValidator<PresignUploadRequest> validator,
            IJobService service,
            CancellationToken cancellationToken) =>
        {
            await validator.ValidateAndThrowAsync(request, cancellationToken);
            return (await service.PresignSignatureAsync(id, request, cancellationToken)).ToHttpResult();
        })
        .RequireRoles(Roles.Technician, Roles.CompanyAdmin, Roles.Dispatcher)
        .WithSummary("Get presigned URL for signature upload");

        group.MapPost("/{id:guid}/signature", async (
            Guid id,
            ConfirmSignatureRequest request,
            IValidator<ConfirmSignatureRequest> validator,
            IJobService service,
            CancellationToken cancellationToken) =>
        {
            await validator.ValidateAndThrowAsync(request, cancellationToken);
            return (await service.ConfirmSignatureAsync(id, request, cancellationToken)).ToHttpResult();
        })
        .RequireRoles(Roles.Technician, Roles.CompanyAdmin, Roles.Dispatcher)
        .WithSummary("Confirm signature upload");

        group.MapPost("/{id:guid}/complete", async (Guid id, IJobService service, CancellationToken cancellationToken) =>
            (await service.CompleteAsync(id, cancellationToken)).ToHttpResult())
        .RequireRoles(Roles.Technician, Roles.CompanyAdmin, Roles.Dispatcher)
        .WithSummary("Complete job and generate PDF report");

        group.MapGet("/{id:guid}/report", async (Guid id, IJobService service, CancellationToken cancellationToken) =>
            (await service.GetReportAsync(id, cancellationToken)).ToHttpResult())
        .RequireRoles(Roles.CompanyAdmin, Roles.Dispatcher, Roles.Technician)
        .WithSummary("Get job PDF report URL");

        return group;
    }
}
