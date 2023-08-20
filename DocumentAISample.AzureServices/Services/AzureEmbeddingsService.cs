using Azure.AI.OpenAI;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;

namespace DocumentAISample.Services;
public class AzureEmbeddingsService : IEmbeddingsService
{
    private readonly AzureEmbeddingsServiceOptions _options;
    private readonly OpenAIClient _openAIClient;

    public AzureEmbeddingsService(
        OpenAIClient openAIClient,
        IOptions<AzureEmbeddingsServiceOptions> options)
    {
        _openAIClient = openAIClient;
        _options = options.Value;
    }

    public async ValueTask<GenerateEmbeddingsResult> GenerateEmbeddingsAsync(GenerateEmbeddingsInput input, CancellationToken cancellationToken = default)
    {
        var result = await _openAIClient.GetEmbeddingsAsync(
            _options.ModelId,
            new(input.Text));
        return new(result.Value.Data[0].Embedding);
    }
}

public class AzureEmbeddingsServiceOptions
{
    [Required]
    public string ModelId { get; set; } = "";
}