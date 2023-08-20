using ImportDocumentFunctionApp.Services;
using Microsoft.Extensions.Options;

namespace DocumentAISample.Services.Implementations;
internal class ImportDocumentService : IImportDocumentService
{
    private readonly IDocumentService _documentService;
    private readonly IEmbeddingsService _embeddingsService;
    private readonly IBlobService _blobService;
    private readonly ISearchService _searchService;
    private readonly ImportDocumentServiceOptions _options;

    public ImportDocumentService(
        IDocumentService documentService,
        IEmbeddingsService embeddingsService,
        IBlobService blobService,
        ISearchService searchService,
        IOptions<ImportDocumentServiceOptions> options)
    {
        _documentService = documentService;
        _embeddingsService = embeddingsService;
        _blobService = blobService;
        _searchService = searchService;
        _options = options.Value;
    }

    public async ValueTask ImportAsync(ImportTarget importTarget,
        CancellationToken cancellationToken = default)
    {
        var parseDocumentResult = await _documentService.ParseDocumentAsync(new(importTarget.Uri), cancellationToken);

        List<DocumentChunk> documentChunks = new();
        foreach (var chunk in parseDocumentResult.PageContents.Chunk(_options.PageSize))
        {
            var text = string.Join(" ", chunk.Select(x => x.Text));
            var embeddingsResult = await _embeddingsService.GenerateEmbeddingsAsync(new(text), cancellationToken);
            documentChunks.Add(new(text, chunk.Select(x => x.PageNumber).ToArray(), embeddingsResult.Embeddings));
        }

        var copyTask = _blobService.CopyFromUriAsync(importTarget.Uri,
            _options.ExportContainerName,
            importTarget.Name);

        var insertIndexTask = _searchService.InsertDocumentAsync(
            new(importTarget.Name, documentChunks),
            cancellationToken);
        await Task.WhenAll(copyTask.AsTask(), insertIndexTask.AsTask())
            .ConfigureAwait(false);
    }

}

