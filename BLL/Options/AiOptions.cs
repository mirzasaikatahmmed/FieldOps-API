namespace FieldOps.BLL.Options;

public class AiOptions
{
    public const string SectionName = "Ai";

    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-4o-mini";
    public int TimeoutSeconds { get; set; } = 60;

    public bool HasApiKey => !string.IsNullOrWhiteSpace(ApiKey);
}
