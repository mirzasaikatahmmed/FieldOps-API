using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FieldOps.BLL.DTOs.Auth;
using FieldOps.BLL.DTOs.Customers;
using FieldOps.BLL.DTOs.Jobs;
using FieldOps.BLL.DTOs.JobTemplates;
using FieldOps.COMMON.Enums;
using FieldOps.COMMON.Interfaces;
using FieldOps.DAL;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FieldOps.Tests.Integration;

[Collection("Integration")]
public class AiFeaturesIntegrationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly FieldOpsApiFactory _factory;

    public AiFeaturesIntegrationTests(FieldOpsApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Ask_Summary_And_RiskHints_Work_With_Stub()
    {
        await _factory.EnsureMigratedAsync();
        var client = _factory.CreateClient();

        var email = $"ai-admin-{Guid.NewGuid():N}@acme.test";
        var register = await client.PostAsJsonAsync("/api/auth/register-company", new RegisterCompanyRequest(
            "AI Co", "Admin", email, "Password123!"));
        register.EnsureSuccessStatusCode();
        var auth = await register.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        var customer = await (await client.PostAsJsonAsync("/api/customers", new CreateCustomerRequest(
            "AI Customer", null, null, null))).Content.ReadFromJsonAsync<CustomerDto>(JsonOptions);

        var template = await (await client.PostAsJsonAsync("/api/job-templates", new CreateJobTemplateRequest(
            "AI Template", [new TemplateFieldRequest("OK?", FieldType.Boolean, null, 0, false)])))
            .Content.ReadFromJsonAsync<JobTemplateDto>(JsonOptions);

        var job = await (await client.PostAsJsonAsync("/api/jobs", new CreateJobRequest(
            customer!.Id, template!.Id, null, "Overdue Unassigned", DateTime.UtcNow.AddHours(3), null)))
            .Content.ReadFromJsonAsync<JobDto>(JsonOptions);
        job.Should().NotBeNull();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var entity = await db.Jobs.IgnoreQueryFilters().SingleAsync(j => j.Id == job!.Id);
            entity.ScheduledAt = DateTime.UtcNow.AddHours(-2);
            await db.SaveChangesAsync();
        }

        var ask = await client.PostAsJsonAsync("/api/ai/ask", new { question = "What should I prioritize today?" });
        ask.EnsureSuccessStatusCode();
        var askBody = await ask.Content.ReadFromJsonAsync<AiAskResponseDto>(JsonOptions);
        askBody!.UsedStub.Should().BeTrue();
        askBody.Answer.Should().NotBeNullOrWhiteSpace();
        askBody.Model.Should().Be("stub-local");

        var summary = await client.PostAsJsonAsync($"/api/jobs/{job!.Id}/ai-summary", new { });
        summary.EnsureSuccessStatusCode();
        var summaryBody = await summary.Content.ReadFromJsonAsync<JobAiSummaryDto>(JsonOptions);
        summaryBody!.UsedStub.Should().BeTrue();
        summaryBody.Summary.Should().Contain("Findings");
        summaryBody.JobId.Should().Be(job.Id);

        var detail = await client.GetAsync($"/api/jobs/{job.Id}");
        detail.EnsureSuccessStatusCode();
        var jobDetail = await detail.Content.ReadFromJsonAsync<JobDetailDto>(JsonOptions);
        jobDetail!.AiSummary.Should().Be(summaryBody.Summary);
        jobDetail.AiSummaryGeneratedAt.Should().NotBeNull();

        var risks = await client.GetAsync("/api/ai/risk-hints?limit=10");
        risks.EnsureSuccessStatusCode();
        var riskBody = await risks.Content.ReadFromJsonAsync<AiRiskHintsResponseDto>(JsonOptions);
        riskBody!.UsedStub.Should().BeTrue();
        riskBody.Items.Should().Contain(i => i.JobId == job.Id);
        riskBody.Items.First(i => i.JobId == job.Id).Recommendation.Should().NotBeNullOrWhiteSpace();
    }
}
