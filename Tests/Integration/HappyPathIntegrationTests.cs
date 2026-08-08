using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FieldOps.BLL.DTOs.Auth;
using FieldOps.BLL.DTOs.Customers;
using FieldOps.BLL.DTOs.Jobs;
using FieldOps.BLL.DTOs.JobTemplates;
using FieldOps.BLL.DTOs.Users;
using FieldOps.COMMON.Constants;
using FieldOps.COMMON.Entities;
using FieldOps.COMMON.Enums;
using FieldOps.COMMON.Interfaces;
using FieldOps.DAL;
using FieldOps.API.BackgroundServices;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;
using Xunit;

namespace FieldOps.Tests.Integration;

public class FieldOpsApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16")
        .WithDatabase("fieldops_test")
        .WithUsername("fieldops")
        .WithPassword("fieldops")
        .Build();

    public async Task InitializeAsync() => await _postgres.StartAsync();

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            var dbDescriptors = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>)
                            || d.ServiceType == typeof(AppDbContext)
                            || d.ServiceType == typeof(DbContextOptions))
                .ToList();
            foreach (var descriptor in dbDescriptors)
                services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(_postgres.GetConnectionString()));

            services.RemoveAll<IStorageService>();
            services.AddSingleton<IStorageService, FakeStorageService>();

            services.RemoveAll<IPdfService>();
            services.AddSingleton<IPdfService, FakePdfService>();

            services.RemoveAll<IJobStatusNotifier>();
            services.AddSingleton<IJobStatusNotifier, NoOpJobStatusNotifier>();

            var hosted = services.Where(d =>
                d.ServiceType == typeof(IHostedService) &&
                d.ImplementationType == typeof(SlaBreachChecker)).ToList();
            foreach (var descriptor in hosted)
                services.Remove(descriptor);
        });
    }

    public async Task EnsureMigratedAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
        }
    }
}

internal sealed class FakeStorageService : IStorageService
{
    public Task<string> GeneratePresignedUploadUrlAsync(string key, string contentType, CancellationToken cancellationToken = default)
        => Task.FromResult($"https://storage.test/upload/{key}");

    public string GetPublicUrl(string key) => $"https://storage.test/{key}";

    public Task DeleteObjectAsync(string key, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public async Task UploadAsync(string key, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        await content.CopyToAsync(Stream.Null, cancellationToken);
    }

    public Task<Stream> DownloadAsync(string key, CancellationToken cancellationToken = default)
        => Task.FromResult<Stream>(new MemoryStream([0x89, 0x50, 0x4E, 0x47]));
}

internal sealed class FakePdfService : IPdfService
{
    private readonly IStorageService _storage;

    public FakePdfService(IStorageService storage) => _storage = storage;

    public async Task<(string StorageKey, string Url)> GenerateJobReportAsync(Job job, CancellationToken cancellationToken = default)
    {
        var key = $"reports/{job.Id}.pdf";
        await using var stream = new MemoryStream("%PDF-1.4 fake"u8.ToArray());
        await _storage.UploadAsync(key, stream, "application/pdf", cancellationToken);
        return (key, _storage.GetPublicUrl(key));
    }
}

internal sealed class NoOpJobStatusNotifier : IJobStatusNotifier
{
    public Task NotifyJobStatusChangedAsync(
        Guid companyId,
        Guid jobId,
        string newStatus,
        string? technicianName,
        DateTime updatedAt,
        CancellationToken cancellationToken = default) => Task.CompletedTask;
}

public class HappyPathIntegrationTests : IClassFixture<FieldOpsApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly FieldOpsApiFactory _factory;

    public HappyPathIntegrationTests(FieldOpsApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Register_Login_CreateJob_Submit_Complete_GeneratesReport()
    {
        await _factory.EnsureMigratedAsync();
        var client = _factory.CreateClient();

        var register = await client.PostAsJsonAsync("/api/auth/register-company", new RegisterCompanyRequest(
            "Acme Field Services",
            "Admin User",
            $"admin-{Guid.NewGuid():N}@acme.test",
            "Password123!"));
        register.EnsureSuccessStatusCode();
        var adminAuth = await register.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        adminAuth.Should().NotBeNull();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminAuth!.AccessToken);

        var techEmail = $"tech-{Guid.NewGuid():N}@acme.test";
        var createTech = await client.PostAsJsonAsync("/api/users", new CreateUserRequest(
            "Tech User",
            techEmail,
            "Password123!",
            "Technician"));
        createTech.EnsureSuccessStatusCode();
        var tech = await createTech.Content.ReadFromJsonAsync<UserDto>(JsonOptions);

        var customerResponse = await client.PostAsJsonAsync("/api/customers", new CreateCustomerRequest(
            "Jane Customer",
            "555-0100",
            "jane@customer.test",
            "123 Main St"));
        customerResponse.EnsureSuccessStatusCode();
        var customer = await customerResponse.Content.ReadFromJsonAsync<CustomerDto>(JsonOptions);

        var templateResponse = await client.PostAsJsonAsync("/api/job-templates", new CreateJobTemplateRequest(
            "AC Checklist",
            [
                new TemplateFieldRequest("Unit OK?", FieldType.Boolean, null, 0, true),
                new TemplateFieldRequest("Notes", FieldType.Text, null, 1, false)
            ]));
        templateResponse.EnsureSuccessStatusCode();
        var template = await templateResponse.Content.ReadFromJsonAsync<JobTemplateDto>(JsonOptions);
        template.Should().NotBeNull();
        var requiredField = template!.Fields.Single(f => f.IsRequired);

        var jobResponse = await client.PostAsJsonAsync("/api/jobs", new CreateJobRequest(
            customer!.Id,
            template.Id,
            tech!.Id,
            "AC Tune-up",
            DateTime.UtcNow.AddHours(2),
            "Bring filters"));
        jobResponse.EnsureSuccessStatusCode();
        var job = await jobResponse.Content.ReadFromJsonAsync<JobDto>(JsonOptions);
        job.Should().NotBeNull();

        var techLogin = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(techEmail, "Password123!"));
        techLogin.EnsureSuccessStatusCode();
        var techAuth = await techLogin.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", techAuth!.AccessToken);

        var statusResponse = await client.PatchAsJsonAsync($"/api/jobs/{job!.Id}/status", new UpdateJobStatusRequest(JobStatus.InProgress));
        statusResponse.EnsureSuccessStatusCode();

        var responses = await client.PostAsJsonAsync($"/api/jobs/{job.Id}/responses", new SubmitJobResponsesRequest(
        [
            new JobResponseItemRequest(requiredField.Id, null, null, true, null),
            new JobResponseItemRequest(template.Fields.First(f => !f.IsRequired).Id, "All good", null, null, null)
        ]));
        responses.StatusCode.Should().Be(System.Net.HttpStatusCode.NoContent);

        var signatureConfirm = await client.PostAsJsonAsync($"/api/jobs/{job.Id}/signature", new ConfirmSignatureRequest(
            $"companies/{adminAuth.CompanyId}/jobs/{job.Id}/signatures/sig.png",
            "Jane Customer"));
        signatureConfirm.EnsureSuccessStatusCode();

        var complete = await client.PostAsync($"/api/jobs/{job.Id}/complete", null);
        complete.EnsureSuccessStatusCode();
        var completed = await complete.Content.ReadFromJsonAsync<JobDetailDto>(JsonOptions);
        completed!.Status.Should().Be(JobStatus.Completed);
        completed.Report.Should().NotBeNull();

        var report = await client.GetAsync($"/api/jobs/{job.Id}/report");
        report.EnsureSuccessStatusCode();
        var reportDto = await report.Content.ReadFromJsonAsync<ReportDto>(JsonOptions);
        reportDto!.Url.Should().NotBeNullOrWhiteSpace();
    }
}
