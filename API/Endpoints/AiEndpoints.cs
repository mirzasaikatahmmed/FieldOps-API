using FieldOps.COMMON.Constants;
using FieldOps.COMMON.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FieldOps.API.Endpoints;

public static class AiEndpoints
{
    public static RouteGroupBuilder MapAiEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ai")
            .WithTags("AI")
            .RequireRoles(Roles.CompanyAdmin, Roles.Dispatcher);

        group.MapPost("/ask", async (
            AiAskRequest request,
            IAiAssistantService service,
            CancellationToken cancellationToken) =>
            (await service.AskAsync(request.Question ?? string.Empty, cancellationToken)).ToHttpResult())
        .WithSummary("Ask the dispatcher assistant about today's jobs and SLA risk");

        group.MapGet("/risk-hints", async (
            [FromQuery] int? limit,
            IAiAssistantService service,
            CancellationToken cancellationToken) =>
            (await service.GetRiskHintsAsync(limit ?? 20, cancellationToken)).ToHttpResult())
        .WithSummary("Rule-scored SLA risk hints with LLM recommendations");

        return group;
    }
}

public record AiAskRequest(string? Question);
