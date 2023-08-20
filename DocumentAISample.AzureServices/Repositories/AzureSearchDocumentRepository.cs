using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Logging;

namespace DocumentAISample.Repositories;
public class AzureSearchDocumentRepository : IDocumentRepository
{
    private const string _searchIndexName = "document-index";
    private const int _modelDimensions = 1536;

    private readonly SearchIndexClient _searchIndexClient;
    private readonly ILogger<AzureSearchDocumentRepository> _logger;

    public AzureSearchDocumentRepository(SearchIndexClient searchIndexClient,
        ILogger<AzureSearchDocumentRepository> logger)
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
                    ["containerName"] = insertTargetDocument.ContaienrName,
                    ["documentName"] = insertTargetDocument.DocumentName,
                    ["text"] = x.Text,
                    ["textVector"] = x.Embeddings,
                    ["pageNumbers"] = x.PageNumbers,
                });

                await indexClient.IndexDocumentsAsync(IndexDocumentsBatch.Upload(documents), cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        try
        {
            await insertImpl().ConfigureAwait(false);
        }
        catch (RequestFailedException ex) when (ex.Status is 403 or 404)
        {
            await InitializeAsync(cancellationToken).ConfigureAwait(false);
            await insertImpl().ConfigureAwait(false);
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
                new SearchableField("containerName") { IsFilterable = true },
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

    public async ValueTask<SearchDocumentsResult> SearchDocumentsAsync(IReadOnlyCollection<float> keyword, int resultNumber, CancellationToken cancellationToken = default)
    {
        var client = _searchIndexClient.GetSearchClient(_searchIndexName);
        var result = await client.SearchAsync<SearchDocument>(null, new SearchOptions
        {
            Vectors =
            {
                new SearchQueryVector
                {
                    KNearestNeighborsCount = resultNumber,
                    Fields = { "textVector" },
                    Value = keyword.ToArray(),
                },
            },
            Size = resultNumber,
            Select = { "containerName", "documentName", "text", "pageNumbers" }
        }, cancellationToken).ConfigureAwait(false);

        var list = new List<FoundDocument>();
        await foreach (var doc in result.Value.GetResultsAsync().ConfigureAwait(false))
        {
            list.Add(new(
                doc.Document.GetString("containerName"),
                doc.Document.GetString("documentName"),
                doc.Document.GetString("text"),
                doc.Document.GetInt32Collection("pageNumbers"),
                doc.Score ?? 0.0));
        }

        return new(list);
    }
}
