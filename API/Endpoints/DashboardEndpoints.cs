using FieldOps.BLL.Services;
using FieldOps.COMMON.Constants;

namespace FieldOps.API.Endpoints;

public static class DashboardEndpoints
{
    public static RouteGroupBuilder MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/dashboard")
            .WithTags("Dashboard")
            .RequireRoles(Roles.CompanyAdmin, Roles.Dispatcher);

        group.MapGet("/", async (IDashboardService service, CancellationToken cancellationToken) =>
            (await service.GetAsync(cancellationToken)).ToHttpResult())
        .WithSummary("Dispatcher dashboard aggregates");

        return group;
    }
}
