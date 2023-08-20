namespace DocumentAISample.Repositories;
public interface IDocumentRepository
{
    ValueTask InsertDocumentAsync(InsertTargetDocument insertTargetDocument, CancellationToken cancellationToken = default);
    ValueTask<SearchDocumentsResult> SearchDocumentsAsync(IReadOnlyCollection<float> keyword, int resultNumber, CancellationToken cancellationToken = default);
}

public record InsertTargetDocument(
    string ContaienrName,
    string DocumentName,
    IEnumerable<DocumentChunk> DocumentChunks);

public record DocumentChunk(
    string Text,
    IEnumerable<int> PageNumbers,
    IReadOnlyCollection<float> Embeddings);

public record SearchDocumentsResult(IReadOnlyCollection<FoundDocument> Documents);
public record FoundDocument(string ContainerName, string DocumentName, string Text, IReadOnlyList<int> PageNumbers, double Score);
