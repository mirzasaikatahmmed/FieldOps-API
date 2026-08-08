using FieldOps.BLL.DTOs.Jobs;
using FieldOps.COMMON.Constants;
using FieldOps.COMMON.Entities;
using FieldOps.COMMON.Enums;
using FieldOps.COMMON.Interfaces;
using FieldOps.COMMON.Models;
using FieldOps.DAL.Repositories;

namespace FieldOps.BLL.Services;

public interface IJobService
{
    Task<Result<PagedResult<JobDto>>> GetAsync(JobFilter filter, PaginationQuery pagination, CancellationToken cancellationToken = default);
    Task<Result<JobDetailDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<JobDto>> CreateAsync(CreateJobRequest request, CancellationToken cancellationToken = default);
    Task<Result<JobDto>> AssignAsync(Guid id, AssignJobRequest request, CancellationToken cancellationToken = default);
    Task<Result<JobDto>> UpdateStatusAsync(Guid id, UpdateJobStatusRequest request, CancellationToken cancellationToken = default);
    Task<Result> SubmitResponsesAsync(Guid id, SubmitJobResponsesRequest request, CancellationToken cancellationToken = default);
    Task<Result<PresignUploadResponse>> PresignPhotoAsync(Guid id, PresignUploadRequest request, CancellationToken cancellationToken = default);
    Task<Result<JobPhotoDto>> ConfirmPhotoAsync(Guid id, ConfirmPhotoRequest request, CancellationToken cancellationToken = default);
    Task<Result<PresignUploadResponse>> PresignSignatureAsync(Guid id, PresignUploadRequest request, CancellationToken cancellationToken = default);
    Task<Result<SignatureDto>> ConfirmSignatureAsync(Guid id, ConfirmSignatureRequest request, CancellationToken cancellationToken = default);
    Task<Result<JobDetailDto>> CompleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<ReportDto>> GetReportAsync(Guid id, CancellationToken cancellationToken = default);
}

public class JobService : IJobService
{
    private readonly IJobRepository _jobRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IJobTemplateRepository _templateRepository;
    private readonly IUserRepository _userRepository;
    private readonly ITenantProvider _tenantProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStorageService _storageService;
    private readonly IPdfService _pdfService;
    private readonly IJobStatusNotifier _jobStatusNotifier;

    public JobService(
        IJobRepository jobRepository,
        ICustomerRepository customerRepository,
        IJobTemplateRepository templateRepository,
        IUserRepository userRepository,
        ITenantProvider tenantProvider,
        IUnitOfWork unitOfWork,
        IStorageService storageService,
        IPdfService pdfService,
        IJobStatusNotifier jobStatusNotifier)
    {
        _jobRepository = jobRepository;
        _customerRepository = customerRepository;
        _templateRepository = templateRepository;
        _userRepository = userRepository;
        _tenantProvider = tenantProvider;
        _unitOfWork = unitOfWork;
        _storageService = storageService;
        _pdfService = pdfService;
        _jobStatusNotifier = jobStatusNotifier;
    }

    public async Task<Result<PagedResult<JobDto>>> GetAsync(JobFilter filter, PaginationQuery pagination, CancellationToken cancellationToken = default)
    {
        if (IsTechnician())
            filter.TechnicianId = _tenantProvider.UserId;

        var page = await _jobRepository.GetPagedAsync(filter, pagination, cancellationToken);
        return Result<PagedResult<JobDto>>.Success(new PagedResult<JobDto>
        {
            Items = page.Items.Select(MapSummary).ToList(),
            TotalCount = page.TotalCount,
            Page = page.Page,
            PageSize = page.PageSize
        });
    }

    public async Task<Result<JobDetailDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var job = await _jobRepository.GetByIdAsync(id, includeDetails: true, cancellationToken);
        if (job is null)
            return Result<JobDetailDto>.NotFound();

        if (!CanAccessJob(job))
            return Result<JobDetailDto>.NotFound();

        return Result<JobDetailDto>.Success(MapDetail(job));
    }

    public async Task<Result<JobDto>> CreateAsync(CreateJobRequest request, CancellationToken cancellationToken = default)
    {
        if (_tenantProvider.CompanyId is not Guid companyId)
            return Result<JobDto>.Forbidden("Company context required.");

        var customer = await _customerRepository.GetByIdAsync(request.CustomerId, cancellationToken);
        if (customer is null)
            return Result<JobDto>.NotFound("Customer not found.");

        var template = await _templateRepository.GetByIdAsync(request.JobTemplateId, includeFields: false, cancellationToken);
        if (template is null || !template.IsActive)
            return Result<JobDto>.NotFound("Job template not found.");

        if (request.AssignedTechnicianId.HasValue)
        {
            var tech = await _userRepository.GetByIdAsync(request.AssignedTechnicianId.Value, cancellationToken);
            if (tech is null || tech.CompanyId != companyId || tech.Role != Roles.Technician)
                return Result<JobDto>.Failure("Assigned technician is invalid.");
        }

        var job = new Job
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            CustomerId = request.CustomerId,
            JobTemplateId = request.JobTemplateId,
            AssignedTechnicianId = request.AssignedTechnicianId,
            Title = request.Title.Trim(),
            ScheduledAt = request.ScheduledAt.ToUniversalTime(),
            Status = JobStatus.Scheduled,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow
        };

        await _jobRepository.AddAsync(job, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        job = await _jobRepository.GetByIdAsync(job.Id, includeDetails: true, cancellationToken);
        return Result<JobDto>.Success(MapSummary(job!), 201);
    }

    public async Task<Result<JobDto>> AssignAsync(Guid id, AssignJobRequest request, CancellationToken cancellationToken = default)
    {
        var job = await _jobRepository.GetByIdAsync(id, includeDetails: true, cancellationToken);
        if (job is null)
            return Result<JobDto>.NotFound();

        if (job.Status is JobStatus.Completed or JobStatus.Cancelled)
            return Result<JobDto>.Failure("Cannot reassign a completed or cancelled job.");

        var tech = await _userRepository.GetByIdAsync(request.TechnicianId, cancellationToken);
        if (tech is null || tech.CompanyId != job.CompanyId || tech.Role != Roles.Technician)
            return Result<JobDto>.Failure("Assigned technician is invalid.");

        job.AssignedTechnicianId = request.TechnicianId;
        _jobRepository.Update(job);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        job = await _jobRepository.GetByIdAsync(id, includeDetails: true, cancellationToken);
        return Result<JobDto>.Success(MapSummary(job!));
    }

    public async Task<Result<JobDto>> UpdateStatusAsync(Guid id, UpdateJobStatusRequest request, CancellationToken cancellationToken = default)
    {
        var job = await _jobRepository.GetByIdAsync(id, includeDetails: true, cancellationToken);
        if (job is null)
            return Result<JobDto>.NotFound();

        if (!CanAccessJob(job))
            return Result<JobDto>.NotFound();

        if (IsTechnician() && request.Status == JobStatus.Completed)
            return Result<JobDto>.Failure("Use the complete endpoint to finish a job.");

        if (!JobStatusTransitions.CanTransition(job.Status, request.Status))
            return Result<JobDto>.Failure($"Cannot transition from {job.Status} to {request.Status}.");

        ApplyStatusSideEffects(job, request.Status);
        _jobRepository.Update(job);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _jobStatusNotifier.NotifyJobStatusChangedAsync(
            job.CompanyId,
            job.Id,
            job.Status.ToString(),
            job.AssignedTechnician?.FullName,
            DateTime.UtcNow,
            cancellationToken);

        return Result<JobDto>.Success(MapSummary(job));
    }

    public async Task<Result> SubmitResponsesAsync(Guid id, SubmitJobResponsesRequest request, CancellationToken cancellationToken = default)
    {
        var job = await _jobRepository.GetByIdAsync(id, includeDetails: true, cancellationToken);
        if (job is null)
            return Result.NotFound();

        if (!CanAccessJob(job))
            return Result.NotFound();

        if (job.Status is JobStatus.Completed or JobStatus.Cancelled)
            return Result.Failure("Cannot submit responses for a completed or cancelled job.");

        var fieldIds = job.JobTemplate.TemplateFields.Select(f => f.Id).ToHashSet();
        var existing = (await _jobRepository.GetResponsesAsync(id, cancellationToken))
            .ToDictionary(r => r.TemplateFieldId);

        var toAdd = new List<JobResponse>();
        foreach (var item in request.Responses)
        {
            if (!fieldIds.Contains(item.TemplateFieldId))
                return Result.Failure($"Template field {item.TemplateFieldId} does not belong to this job.");

            if (existing.TryGetValue(item.TemplateFieldId, out var current))
            {
                current.ValueText = item.ValueText;
                current.ValueNumber = item.ValueNumber;
                current.ValueBool = item.ValueBool;
                current.PhotoUrl = item.PhotoUrl;
            }
            else
            {
                toAdd.Add(new JobResponse
                {
                    Id = Guid.NewGuid(),
                    JobId = id,
                    TemplateFieldId = item.TemplateFieldId,
                    ValueText = item.ValueText,
                    ValueNumber = item.ValueNumber,
                    ValueBool = item.ValueBool,
                    PhotoUrl = item.PhotoUrl
                });
            }
        }

        if (toAdd.Count > 0)
            await _jobRepository.AddResponsesAsync(toAdd, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<PresignUploadResponse>> PresignPhotoAsync(Guid id, PresignUploadRequest request, CancellationToken cancellationToken = default)
    {
        var access = await EnsureJobAccessAsync(id, cancellationToken);
        if (!access.IsSuccess)
            return Result<PresignUploadResponse>.Failure(access.Error!, access.StatusCode);

        var key = $"companies/{access.Data!.CompanyId}/jobs/{id}/photos/{Guid.NewGuid()}-{SanitizeFileName(request.FileName)}";
        var uploadUrl = await _storageService.GeneratePresignedUploadUrlAsync(key, request.ContentType, cancellationToken);
        var publicUrl = _storageService.GetPublicUrl(key);
        return Result<PresignUploadResponse>.Success(new PresignUploadResponse(uploadUrl, key, publicUrl));
    }

    public async Task<Result<JobPhotoDto>> ConfirmPhotoAsync(Guid id, ConfirmPhotoRequest request, CancellationToken cancellationToken = default)
    {
        var access = await EnsureJobAccessAsync(id, cancellationToken);
        if (!access.IsSuccess)
            return Result<JobPhotoDto>.Failure(access.Error!, access.StatusCode);

        if (!request.StorageKey.Contains(id.ToString(), StringComparison.OrdinalIgnoreCase))
            return Result<JobPhotoDto>.Failure("Storage key does not match this job.");

        var photo = new JobPhoto
        {
            Id = Guid.NewGuid(),
            JobId = id,
            StorageKey = request.StorageKey,
            Url = _storageService.GetPublicUrl(request.StorageKey),
            Caption = request.Caption,
            UploadedAt = DateTime.UtcNow
        };

        await _jobRepository.AddPhotoAsync(photo, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<JobPhotoDto>.Success(new JobPhotoDto(photo.Id, photo.Url, photo.Caption, photo.UploadedAt), 201);
    }

    public async Task<Result<PresignUploadResponse>> PresignSignatureAsync(Guid id, PresignUploadRequest request, CancellationToken cancellationToken = default)
    {
        var access = await EnsureJobAccessAsync(id, cancellationToken);
        if (!access.IsSuccess)
            return Result<PresignUploadResponse>.Failure(access.Error!, access.StatusCode);

        var key = $"companies/{access.Data!.CompanyId}/jobs/{id}/signatures/{Guid.NewGuid()}-{SanitizeFileName(request.FileName)}";
        var uploadUrl = await _storageService.GeneratePresignedUploadUrlAsync(key, request.ContentType, cancellationToken);
        var publicUrl = _storageService.GetPublicUrl(key);
        return Result<PresignUploadResponse>.Success(new PresignUploadResponse(uploadUrl, key, publicUrl));
    }

    public async Task<Result<SignatureDto>> ConfirmSignatureAsync(Guid id, ConfirmSignatureRequest request, CancellationToken cancellationToken = default)
    {
        var access = await EnsureJobAccessAsync(id, cancellationToken);
        if (!access.IsSuccess)
            return Result<SignatureDto>.Failure(access.Error!, access.StatusCode);

        if (!request.StorageKey.Contains(id.ToString(), StringComparison.OrdinalIgnoreCase))
            return Result<SignatureDto>.Failure("Storage key does not match this job.");

        var signature = new Signature
        {
            Id = Guid.NewGuid(),
            JobId = id,
            StorageKey = request.StorageKey,
            Url = _storageService.GetPublicUrl(request.StorageKey),
            SignedByName = request.SignedByName.Trim(),
            SignedAt = DateTime.UtcNow
        };

        await _jobRepository.UpsertSignatureAsync(signature, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var saved = (await _jobRepository.GetByIdAsync(id, includeDetails: true, cancellationToken))!.Signature!;
        return Result<SignatureDto>.Success(new SignatureDto(saved.Id, saved.Url, saved.SignedByName, saved.SignedAt), 201);
    }

    public async Task<Result<JobDetailDto>> CompleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var job = await _jobRepository.GetByIdForReportAsync(id, cancellationToken);
        if (job is null)
            return Result<JobDetailDto>.NotFound();

        if (!CanAccessJob(job))
            return Result<JobDetailDto>.NotFound();

        if (!JobStatusTransitions.CanTransition(job.Status, JobStatus.Completed) && job.Status != JobStatus.Completed)
            return Result<JobDetailDto>.Failure($"Cannot complete job from status {job.Status}.");

        var responses = job.Responses.ToDictionary(r => r.TemplateFieldId);
        var missing = job.JobTemplate.TemplateFields
            .Where(f => f.IsRequired)
            .Where(f => !responses.TryGetValue(f.Id, out var r) || !HasValue(r, f.FieldType))
            .Select(f => f.Label)
            .ToList();

        if (missing.Count > 0)
            return Result<JobDetailDto>.Failure($"Required fields missing: {string.Join(", ", missing)}");

        if (job.Signature is null)
            return Result<JobDetailDto>.Failure("Customer signature is required before completing the job.");

        ApplyStatusSideEffects(job, JobStatus.Completed);
        _jobRepository.Update(job);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (job.Report is null)
        {
            var (storageKey, url) = await _pdfService.GenerateJobReportAsync(job, cancellationToken);
            await _jobRepository.AddReportAsync(new Report
            {
                Id = Guid.NewGuid(),
                JobId = job.Id,
                StorageKey = storageKey,
                Url = url,
                GeneratedAt = DateTime.UtcNow
            }, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        await _jobStatusNotifier.NotifyJobStatusChangedAsync(
            job.CompanyId,
            job.Id,
            JobStatus.Completed.ToString(),
            job.AssignedTechnician?.FullName,
            DateTime.UtcNow,
            cancellationToken);

        job = await _jobRepository.GetByIdForReportAsync(id, cancellationToken);
        return Result<JobDetailDto>.Success(MapDetail(job!));
    }

    public async Task<Result<ReportDto>> GetReportAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var job = await _jobRepository.GetByIdForReportAsync(id, cancellationToken);
        if (job is null)
            return Result<ReportDto>.NotFound();

        if (!CanAccessJob(job))
            return Result<ReportDto>.NotFound();

        if (job.Report is not null)
            return Result<ReportDto>.Success(new ReportDto(job.Report.Id, job.Report.Url, job.Report.GeneratedAt));

        if (job.Status != JobStatus.Completed)
            return Result<ReportDto>.Failure("Report is only available for completed jobs.");

        var (storageKey, url) = await _pdfService.GenerateJobReportAsync(job, cancellationToken);
        var report = new Report
        {
            Id = Guid.NewGuid(),
            JobId = job.Id,
            StorageKey = storageKey,
            Url = url,
            GeneratedAt = DateTime.UtcNow
        };
        await _jobRepository.AddReportAsync(report, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<ReportDto>.Success(new ReportDto(report.Id, report.Url, report.GeneratedAt));
    }

    private async Task<Result<Job>> EnsureJobAccessAsync(Guid id, CancellationToken cancellationToken)
    {
        var job = await _jobRepository.GetByIdAsync(id, includeDetails: false, cancellationToken);
        if (job is null || !CanAccessJob(job))
            return Result<Job>.NotFound();
        return Result<Job>.Success(job);
    }

    private bool CanAccessJob(Job job)
    {
        if (!IsTechnician())
            return true;

        return job.AssignedTechnicianId == _tenantProvider.UserId;
    }

    private bool IsTechnician() =>
        string.Equals(_tenantProvider.Role, Roles.Technician, StringComparison.OrdinalIgnoreCase);

    private static void ApplyStatusSideEffects(Job job, JobStatus status)
    {
        job.Status = status;
        if (status == JobStatus.InProgress && job.StartedAt is null)
            job.StartedAt = DateTime.UtcNow;
        if (status == JobStatus.Completed)
            job.CompletedAt ??= DateTime.UtcNow;
    }

    private static bool HasValue(JobResponse response, FieldType fieldType) => fieldType switch
    {
        FieldType.Text or FieldType.Select or FieldType.Signature => !string.IsNullOrWhiteSpace(response.ValueText),
        FieldType.Number => response.ValueNumber.HasValue,
        FieldType.Boolean => response.ValueBool.HasValue,
        FieldType.Photo => !string.IsNullOrWhiteSpace(response.PhotoUrl),
        _ => false
    };

    private static string SanitizeFileName(string fileName)
    {
        var name = Path.GetFileName(fileName);
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return string.IsNullOrWhiteSpace(name) ? "file.bin" : name;
    }

    private static JobDto MapSummary(Job job) => new(
        job.Id,
        job.CustomerId,
        job.Customer?.Name ?? string.Empty,
        job.AssignedTechnicianId,
        job.AssignedTechnician?.FullName,
        job.JobTemplateId,
        job.Title,
        job.ScheduledAt,
        job.StartedAt,
        job.CompletedAt,
        job.Status,
        job.Notes,
        job.CreatedAt);

    private static JobDetailDto MapDetail(Job job) => new(
        job.Id,
        job.CustomerId,
        job.Customer?.Name ?? string.Empty,
        job.AssignedTechnicianId,
        job.AssignedTechnician?.FullName,
        job.JobTemplateId,
        job.JobTemplate?.Name ?? string.Empty,
        job.Title,
        job.ScheduledAt,
        job.StartedAt,
        job.CompletedAt,
        job.Status,
        job.Notes,
        job.AiSummary,
        job.AiSummaryGeneratedAt,
        job.CreatedAt,
        job.Responses.Select(r => new JobResponseDto(
            r.Id,
            r.TemplateFieldId,
            r.TemplateField?.Label ?? string.Empty,
            r.ValueText,
            r.ValueNumber,
            r.ValueBool,
            r.PhotoUrl)).ToList(),
        job.Photos.Select(p => new JobPhotoDto(p.Id, p.Url, p.Caption, p.UploadedAt)).ToList(),
        job.Signature is null ? null : new SignatureDto(job.Signature.Id, job.Signature.Url, job.Signature.SignedByName, job.Signature.SignedAt),
        job.Report is null ? null : new ReportDto(job.Report.Id, job.Report.Url, job.Report.GeneratedAt));
}
