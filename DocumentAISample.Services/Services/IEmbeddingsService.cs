namespace DocumentAISample.Services;
public interface IEmbeddingsService
{
    ValueTask<GenerateEmbeddingsResult> GenerateEmbeddingsAsync(GenerateEmbeddingsInput input, CancellationToken cancellationToken = default);
}

public record GenerateEmbeddingsInput(string Text);
public record GenerateEmbeddingsResult(IReadOnlyCollection<float> Embeddings);
