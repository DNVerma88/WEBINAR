namespace KnowHub.Infrastructure.AI;

public class AiConfiguration
{
    public string Provider { get; set; } = "OpenAI";
    public OpenAiConfiguration OpenAI { get; set; } = new();
    public AzureOpenAiConfiguration AzureOpenAI { get; set; } = new();
    public GeminiConfiguration Gemini { get; set; } = new();
}

public class OpenAiConfiguration
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-4o-mini";
    public string EmbeddingModel { get; set; } = "text-embedding-3-small";
}

public class AzureOpenAiConfiguration
{
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string DeploymentName { get; set; } = "gpt-4o";
}

public class GeminiConfiguration
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gemini-2.0-flash";
}
