using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FieldOps.BLL.DTOs.Auth;
using FieldOps.BLL.DTOs.Companies;
using FieldOps.BLL.DTOs.Customers;
using FieldOps.BLL.DTOs.Dashboard;
using FieldOps.BLL.DTOs.Jobs;
using FieldOps.BLL.DTOs.JobTemplates;
using FieldOps.COMMON.Constants;
using FieldOps.COMMON.Entities;
using FieldOps.COMMON.Enums;
using FieldOps.COMMON.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FieldOps.Tests.Integration;

[Collection("Integration")]
public class FeatureExpansionIntegrationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly FieldOpsApiFactory _factory;

    public FeatureExpansionIntegrationTests(FieldOpsApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Dashboard_Comments_Search_And_ChangePassword_Work()
    {
        await _factory.EnsureMigratedAsync();
        var client = _factory.CreateClient();

        var email = $"admin-{Guid.NewGuid():N}@acme.test";
        var register = await client.PostAsJsonAsync("/api/auth/register-company", new RegisterCompanyRequest(
            "Search Co", "Admin", email, "Password123!"));
        register.EnsureSuccessStatusCode();
        var auth = await register.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        var customer = await (await client.PostAsJsonAsync("/api/customers", new CreateCustomerRequest(
            "UniqueSearchCustomer", null, null, null))).Content.ReadFromJsonAsync<CustomerDto>(JsonOptions);

        var template = await (await client.PostAsJsonAsync("/api/job-templates", new CreateJobTemplateRequest(
            "T", [new TemplateFieldRequest("Q", FieldType.Boolean, null, 0, false)])))
            .Content.ReadFromJsonAsync<JobTemplateDto>(JsonOptions);

        var job = await (await client.PostAsJsonAsync("/api/jobs", new CreateJobRequest(
            customer!.Id, template!.Id, null, "Searchable Job Title", DateTime.UtcNow.AddHours(3), null)))
            .Content.ReadFromJsonAsync<JobDto>(JsonOptions);

        var search = await client.GetAsync("/api/jobs?search=Searchable");
        search.EnsureSuccessStatusCode();
        var jobs = await search.Content.ReadFromJsonAsync<PagedResult<JobDto>>(JsonOptions);
        jobs!.Items.Should().Contain(j => j.Id == job!.Id);

        var customerSearch = await client.GetAsync("/api/customers?search=UniqueSearch");
        customerSearch.EnsureSuccessStatusCode();
        var customers = await customerSearch.Content.ReadFromJsonAsync<PagedResult<CustomerDto>>(JsonOptions);
        customers!.Items.Should().Contain(c => c.Id == customer.Id);

        var comment = await client.PostAsJsonAsync($"/api/jobs/{job!.Id}/comments", new CreateJobCommentRequest("First note"));
        comment.EnsureSuccessStatusCode();

        var comments = await client.GetAsync($"/api/jobs/{job.Id}/comments");
        comments.EnsureSuccessStatusCode();
        var commentPage = await comments.Content.ReadFromJsonAsync<PagedResult<JobCommentDto>>(JsonOptions);
        commentPage!.Items.Should().ContainSingle(c => c.Body == "First note");

        var dashboard = await client.GetAsync("/api/dashboard");
        dashboard.EnsureSuccessStatusCode();
        var dash = await dashboard.Content.ReadFromJsonAsync<DashboardDto>(JsonOptions);
        dash!.CountsByStatus.Should().ContainKey(JobStatus.Scheduled.ToString());

        var changePw = await client.PostAsJsonAsync("/api/auth/change-password",
            new ChangePasswordRequest("Password123!", "Password456!"));
        changePw.StatusCode.Should().Be(System.Net.HttpStatusCode.NoContent);

        var reLogin = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "Password456!"));
        reLogin.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task SuperAdmin_Can_List_And_Deactivate_Company()
    {
        await _factory.EnsureMigratedAsync();
        var client = _factory.CreateClient();

        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
            if (!await roleManager.RoleExistsAsync(Roles.SuperAdmin))
                await roleManager.CreateAsync(new IdentityRole<Guid>(Roles.SuperAdmin));

            var existing = await userManager.FindByEmailAsync("super@test.local");
            if (existing is null)
            {
                var user = new ApplicationUser
                {
                    Id = Guid.NewGuid(),
                    Email = "super@test.local",
                    UserName = "super@test.local",
                    EmailConfirmed = true,
                    FullName = "Super",
                    Role = Roles.SuperAdmin,
                    CompanyId = null,
                    CreatedAt = DateTime.UtcNow
                };
                await userManager.CreateAsync(user, "SuperAdmin123!");
                await userManager.AddToRoleAsync(user, Roles.SuperAdmin);
            }
        }

        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("super@test.local", "SuperAdmin123!"));
        login.EnsureSuccessStatusCode();
        var auth = await login.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        var create = await client.PostAsJsonAsync("/api/companies", new CreateCompanyRequest("Platform Co"));
        create.EnsureSuccessStatusCode();
        var company = await create.Content.ReadFromJsonAsync<CompanyDto>(JsonOptions);

        var deactivate = await client.PatchAsync($"/api/companies/{company!.Id}/deactivate", null);
        deactivate.EnsureSuccessStatusCode();
        var updated = await deactivate.Content.ReadFromJsonAsync<CompanyDto>(JsonOptions);
        updated!.IsActive.Should().BeFalse();

        var list = await client.GetAsync("/api/companies?search=Platform");
        list.EnsureSuccessStatusCode();
    }
}
