using Azure;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Logging;

namespace DocumentAISample.Services;
public class AzureSearchService : ISearchService
{
    private const string _searchIndexName = "document-index";
    private const int _modelDimensions = 1536;

    private readonly SemaphoreSlim _semaphore = new(0, 1);
    private volatile bool _initialized;
    private readonly SearchIndexClient _searchIndexClient;
    private readonly ILogger<AzureSearchService> _logger;

    public AzureSearchService(SearchIndexClient searchIndexClient,
        ILogger<AzureSearchService> logger)
    {
        _searchIndexClient = searchIndexClient;
        _logger = logger;
    }

    public async ValueTask InsertDocumentAsync(InsertTargetDocument insertTargetDocument, CancellationToken cancellationToken = default)
    {
        async ValueTask insertImpl()
        {
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

        try
        {
            await insertImpl();
        }
        catch (RequestFailedException ex) when (ex.Status is 403 or 404)
        {
            await InitializeAsync(cancellationToken);
            await insertImpl();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error");
        }
    }

    private async ValueTask InitializeAsync(CancellationToken cancellationToken)
    {
        const string VectorSearchConfigName = "vector-config";
        var index = new SearchIndex(_searchIndexName)
        {
            VectorSearch = new()
            {
                AlgorithmConfigurations =
                {
                    new HnswVectorSearchAlgorithmConfiguration(VectorSearchConfigName)
                }
            },
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
                    VectorSearchConfiguration = VectorSearchConfigName,
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
    }
}
