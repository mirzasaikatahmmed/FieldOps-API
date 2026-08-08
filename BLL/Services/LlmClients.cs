using System.Net.Http.Json;
using System.Text.Json;
using FieldOps.BLL.Options;
using FieldOps.COMMON.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FieldOps.BLL.Services;

public class OpenAiCompatibleLlmClient : ILlmClient
{
    private readonly HttpClient _httpClient;
    private readonly AiOptions _options;
    private readonly ILogger<OpenAiCompatibleLlmClient> _logger;

    public OpenAiCompatibleLlmClient(
        HttpClient httpClient,
        IOptions<AiOptions> options,
        ILogger<OpenAiCompatibleLlmClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public bool IsStub => false;
    public string ModelName => _options.Model;

    public async Task<string> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            model = _options.Model,
            temperature = 0.2,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            }
        };

        _logger.LogDebug("Calling OpenAI-compatible chat completions model {Model}", _options.Model);

        using var response = await _httpClient.PostAsJsonAsync("chat/completions", payload, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"LLM request failed ({(int)response.StatusCode}): {body}");

        using var doc = JsonDocument.Parse(body);
        var content = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return string.IsNullOrWhiteSpace(content)
            ? throw new InvalidOperationException("LLM returned empty content.")
            : content.Trim();
    }
}

public class StubLlmClient : ILlmClient
{
    public bool IsStub => true;
    public string ModelName => "stub-local";

    public Task<string> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default)
    {
        var lower = userPrompt.ToLowerInvariant();

        if (lower.Contains("risk") || lower.Contains("recommend"))
        {
            return Task.FromResult("Contact the assigned technician and confirm ETA; escalate to dispatcher if unacknowledged within 15 minutes.");
        }

        if (lower.Contains("question") || systemPrompt.Contains("dispatcher", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(
                "Based on the provided company job context: prioritize Scheduled jobs past their window and any unassigned work. " +
                "I can only answer from the supplied JSON — ask for a status filter or job title if you need a narrower list.");
        }

        // Default: job summary style
        return Task.FromResult(
            "Inspection completed with checklist responses recorded. Key customer and schedule details were reviewed. " +
            "Findings appear consistent with a routine service visit. Follow-up is recommended only if required fields flagged issues.\n\n" +
            "Findings:\n- Checklist responses captured\n- Schedule and customer context reviewed\n- No critical anomalies inferred by stub model");
    }
}
