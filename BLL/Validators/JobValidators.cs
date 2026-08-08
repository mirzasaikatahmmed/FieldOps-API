using FieldOps.BLL.DTOs.JobTemplates;
using FieldOps.BLL.DTOs.Jobs;
using FieldOps.COMMON.Enums;
using FluentValidation;

namespace FieldOps.BLL.Validators;

public class CreateJobTemplateRequestValidator : AbstractValidator<CreateJobTemplateRequest>
{
    public CreateJobTemplateRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Fields).NotEmpty();
        RuleForEach(x => x.Fields).SetValidator(new TemplateFieldRequestValidator());
    }
}

public class UpdateJobTemplateRequestValidator : AbstractValidator<UpdateJobTemplateRequest>
{
    public UpdateJobTemplateRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Fields).NotEmpty();
        RuleForEach(x => x.Fields).SetValidator(new TemplateFieldRequestValidator());
    }
}

public class TemplateFieldRequestValidator : AbstractValidator<TemplateFieldRequest>
{
    public TemplateFieldRequestValidator()
    {
        RuleFor(x => x.Label).NotEmpty().MaximumLength(300);
        RuleFor(x => x.FieldType).IsInEnum();
        RuleFor(x => x.Options)
            .NotEmpty()
            .When(x => x.FieldType == FieldType.Select)
            .WithMessage("Options are required for Select fields.");
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}

public class CreateJobRequestValidator : AbstractValidator<CreateJobRequest>
{
    public CreateJobRequestValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.JobTemplateId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.ScheduledAt).Must(d => d > DateTime.UtcNow.AddMinutes(-1))
            .WithMessage("ScheduledAt must be in the future.");
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

public class AssignJobRequestValidator : AbstractValidator<AssignJobRequest>
{
    public AssignJobRequestValidator()
    {
        RuleFor(x => x.TechnicianId).NotEmpty();
    }
}

public class UpdateJobStatusRequestValidator : AbstractValidator<UpdateJobStatusRequest>
{
    public UpdateJobStatusRequestValidator()
    {
        RuleFor(x => x.Status).IsInEnum();
    }
}

public class SubmitJobResponsesRequestValidator : AbstractValidator<SubmitJobResponsesRequest>
{
    public SubmitJobResponsesRequestValidator()
    {
        RuleFor(x => x.Responses).NotEmpty();
        RuleForEach(x => x.Responses).ChildRules(r =>
        {
            r.RuleFor(x => x.TemplateFieldId).NotEmpty();
        });
    }
}

public class PresignUploadRequestValidator : AbstractValidator<PresignUploadRequest>
{
    public PresignUploadRequestValidator()
    {
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.ContentType).NotEmpty().MaximumLength(100);
    }
}

public class ConfirmPhotoRequestValidator : AbstractValidator<ConfirmPhotoRequest>
{
    public ConfirmPhotoRequestValidator()
    {
        RuleFor(x => x.StorageKey).NotEmpty();
        RuleFor(x => x.Caption).MaximumLength(500);
    }
}

public class ConfirmSignatureRequestValidator : AbstractValidator<ConfirmSignatureRequest>
{
    public ConfirmSignatureRequestValidator()
    {
        RuleFor(x => x.StorageKey).NotEmpty();
        RuleFor(x => x.SignedByName).NotEmpty().MaximumLength(200);
    }
}
