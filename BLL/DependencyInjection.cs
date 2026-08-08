using FieldOps.BLL.Options;
using FieldOps.BLL.Services;
using FieldOps.BLL.Validators;
using FieldOps.COMMON.Interfaces;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FieldOps.BLL;

public static class DependencyInjection
{
    public static IServiceCollection AddBll(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));

        services.AddHttpContextAccessor();
        services.AddScoped<ITenantProvider, TenantProvider>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<ICompanyService, CompanyService>();
        services.AddScoped<IJobTemplateService, JobTemplateService>();
        services.AddScoped<IJobService, JobService>();
        services.AddScoped<IJobCommentService, JobCommentService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IPdfService, PdfService>();

        var storageOptions = configuration.GetSection(StorageOptions.SectionName).Get<StorageOptions>()
            ?? new StorageOptions();
        services.AddSingleton(StorageService.CreateClient(storageOptions));
        services.AddScoped<IStorageService, StorageService>();

        services.AddValidatorsFromAssemblyContaining<RegisterCompanyRequestValidator>();

        return services;
    }
}
