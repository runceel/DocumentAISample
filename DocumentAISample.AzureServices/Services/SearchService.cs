using Azure;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Logging;

namespace DocumentAISample.Services;
public class SearchService : ISearchService
{
    private const string _searchIndexName = "document-index";
    private const int _modelDimensions = 1536;

    private readonly SemaphoreSlim _semaphore = new(0, 1);
    private volatile bool _initialized;
    private readonly SearchIndexClient _searchIndexClient;
    private readonly ILogger<SearchService> _logger;

    public SearchService(SearchIndexClient searchIndexClient,
        ILogger<SearchService> logger)
    {
        _searchIndexClient = searchIndexClient;
        _logger = logger;
    }

    public async ValueTask InsertDocumentAsync(InsertTargetDocument insertTargetDocument, CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken).ConfigureAwait(false);
        var indexClient = _searchIndexClient.GetSearchClient(_searchIndexName);
        foreach (var chunk in insertTargetDocument.DocumentChunks.Chunk(10))
        {
            var documents = chunk.Select(x => new SearchDocument
            {
                ["id"] = Guid.NewGuid().ToString(),
                ["documentName"] = insertTargetDocument.DocumentName,
                ["text"] = x.Text,
                ["textVector"] = x.Embeddings,
                ["pageNumbers"] = x.PageNumbers,
            });

            await indexClient.IndexDocumentsAsync(IndexDocumentsBatch.Upload(documents));
        }
    }

    private async ValueTask InitializeAsync(CancellationToken cancellationToken)
    {
        if (_initialized) return;

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_initialized) return;

            var index = new SearchIndex(_searchIndexName)
            {
                Fields =
                {
                    // identity
                    new SimpleField("id", SearchFieldDataType.String) { IsKey = true },
                    // document name
                    new SearchableField("documentName") { IsFilterable = true },
                    // text
                    new SearchableField("text"), 
                    // vector data
                    new SearchField("textVector", SearchFieldDataType.Collection(SearchFieldDataType.Single))
                    {
                        IsSearchable = true,
                        VectorSearchDimensions = _modelDimensions,
                        VectorSearchConfiguration = "vector",
                    },
                    new SimpleField("pageNumbers", SearchFieldDataType.Collection(SearchFieldDataType.Int32))
                }
            };

            try
            {
                await _searchIndexClient.CreateIndexAsync(index, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (RequestFailedException ex)
            {
                // noop
                _logger.LogInformation(ex, "Search index already exist.");
            }

            _initialized = true;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
