using FieldOps.BLL.Options;
using FieldOps.BLL.Services;
using FieldOps.BLL.Validators;
using FieldOps.COMMON.Interfaces;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FieldOps.BLL;

public static class DependencyInjection
{
    public static IServiceCollection AddBll(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));
        services.Configure<AiOptions>(configuration.GetSection(AiOptions.SectionName));

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
        services.AddScoped<IAiAssistantService, AiAssistantService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IPdfService, PdfService>();

        var storageOptions = configuration.GetSection(StorageOptions.SectionName).Get<StorageOptions>()
            ?? new StorageOptions();
        services.AddSingleton(StorageService.CreateClient(storageOptions));
        services.AddScoped<IStorageService, StorageService>();

        var aiOptions = configuration.GetSection(AiOptions.SectionName).Get<AiOptions>() ?? new AiOptions();
        if (aiOptions.HasApiKey)
        {
            services.AddHttpClient<ILlmClient, OpenAiCompatibleLlmClient>((sp, client) =>
            {
                var opts = sp.GetRequiredService<IOptions<AiOptions>>().Value;
                client.BaseAddress = new Uri(opts.BaseUrl.TrimEnd('/') + "/");
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", opts.ApiKey);
                client.Timeout = TimeSpan.FromSeconds(Math.Clamp(opts.TimeoutSeconds, 5, 180));
            });
        }
        else
        {
            services.AddSingleton<ILlmClient, StubLlmClient>();
        }

        services.AddValidatorsFromAssemblyContaining<RegisterCompanyRequestValidator>();

        return services;
    }
}
