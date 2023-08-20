using DocumentAISample.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentAISample.Services.Implementations;
public class DocumentSearchService : IDocumentSearchService
{
    private const int _maxSearchResults = 10;
    private readonly IEmbeddingsService _embeddingsService;
    private readonly IDocumentRepository _documentRepository;
    private readonly IBlobService _blobService;

    public DocumentSearchService(IEmbeddingsService embeddingsService,
        IDocumentRepository documentRepository,
        IBlobService blobService)
    {
        _embeddingsService = embeddingsService;
        _documentRepository = documentRepository;
        _blobService = blobService;
    }

    public async ValueTask<DocumentSearchServiceSearchResult> SearchAsync(string keyword, CancellationToken cancellationToken = default)
    {
        var embeddings = await _embeddingsService.GenerateEmbeddingsAsync(new(keyword), cancellationToken).ConfigureAwait(false);
        var docs = await _documentRepository.SearchDocumentsAsync(
            embeddings.Embeddings, 
            _maxSearchResults, 
            cancellationToken).ConfigureAwait(false);

        var sasDocs = await Task.WhenAll(docs.Documents.Select(async doc =>
        {
            return new DocumentSearchServiceDocument(
                doc.DocumentName,
                await _blobService.GenerateReadUriAsync(
                    doc.ContainerName,
                    doc.DocumentName,
                    cancellationToken).ConfigureAwait(false),
                doc.Text,
                doc.PageNumbers,
                doc.Score);
        }));

        return new(sasDocs);
    }
}
