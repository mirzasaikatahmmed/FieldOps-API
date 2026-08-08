namespace FieldOps.COMMON.Interfaces;

public interface ILlmClient
{
    bool IsStub { get; }
    string ModelName { get; }

    Task<string> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default);
}
