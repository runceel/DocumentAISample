namespace DocumentAISample.Services;
public interface ISearchService
{
    ValueTask InsertDocumentAsync(InsertTargetDocument insertTargetDocument, CancellationToken cancellationToken = default);
}

public record InsertTargetDocument(string DocumentName,
    IEnumerable<DocumentChunk> DocumentChunks);

public record DocumentChunk(
    string Text,
    IEnumerable<int> PageNumbers,
    IReadOnlyCollection<float> Embeddings);
