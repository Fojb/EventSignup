namespace EventSignup.Api.Search;

/// <summary>Any OpenAI-compatible chat completions API (Groq by default). Bound from the "Llm" config section.</summary>
public class LlmOptions
{
    public const string SectionName = "Llm";

    public string BaseUrl { get; set; } = "";

    public string Model { get; set; } = "";

    /// <summary>Never in appsettings.json: use user-secrets locally or the Llm__ApiKey environment variable.</summary>
    public string? ApiKey { get; set; }

    public int TimeoutSeconds { get; set; } = 10;

    /// <summary>For reasoning models this also covers the hidden reasoning tokens.</summary>
    public int MaxTokens { get; set; } = 1000;

    /// <summary>"low", "medium" or "high" for reasoning models (e.g. gpt-oss); leave empty for other models.</summary>
    public string? ReasoningEffort { get; set; }

    public int RequestsPerMinutePerClient { get; set; } = 10;
}
