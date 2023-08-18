using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using Azure.AI.OpenAI;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;
using System.Drawing.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ImportDocumentFunctionApp.Services;
internal class ImportDocumentService : IImportDocumentService
{
    // モデルのベクトルの次元数
    private const int _modelDimensions = 1536;

    private bool _isIndexCreated = false;

    private readonly DocumentAnalysisClient _documentAnalysisClient;
    private readonly OpenAIClient _openAIClient;
    private readonly BlobServiceClient _blobServiceClient;
    private readonly SearchIndexClient _searchIndexClient;
    private readonly ImportDocumentServiceOptions _importDocumentServiceOptions;

    public ImportDocumentService(
        DocumentAnalysisClient documentAnalysisClient, 
        OpenAIClient openAIClient,
        BlobServiceClient blobServiceClient,
        SearchIndexClient searchIndexClient,
        IOptions<ImportDocumentServiceOptions> importDocumentServiceOptions)
    {
        _documentAnalysisClient = documentAnalysisClient;
        _openAIClient = openAIClient;
        _blobServiceClient = blobServiceClient;
        _searchIndexClient = searchIndexClient;
        _importDocumentServiceOptions = importDocumentServiceOptions.Value;
    }

    public async ValueTask ImportAsync(ImportTarget importTarget,
        CancellationToken cancellationToken = default)
    {
        var pageContents = await GetTextFromDocumentAsync(importTarget, cancellationToken);
        List <(int[] PageNumbers, float[] Embeddings, string Text)> embeddings = new();
        foreach (var chunk in pageContents)
        {
            var text = string.Join(" ", chunk.Select(x => x.Text));
            var result = await _openAIClient.GetEmbeddingsAsync(
                _importDocumentServiceOptions.EmbeddingModelName,
                new(text));

            embeddings.Add((chunk.Select(x => x.PageNumber).ToArray(), result.Value.Data[0].Embedding.ToArray(), text));
        }

        var blobContainerClient = _blobServiceClient.GetBlobContainerClient(_importDocumentServiceOptions.ExportContainerName);
        var blobClient = blobContainerClient.GetBlobClient($"{importTarget.Name}");
        var copyBlobTask = blobClient.StartCopyFromUriAsync(new Uri(importTarget.Uri));
        var insertIndexTask = InsertIndexAsync(embeddings, importTarget.Uri);
        await Task.WhenAll(copyBlobTask, insertIndexTask);
    }

    private async Task InsertIndexAsync(List<(int[] PageNumbers, float[] Embeddings, string Text)> embeddings, string uri)
    {
        await CreateIndexIfNotExistAsync();
        var indexClient = _searchIndexClient.GetSearchClient(_importDocumentServiceOptions.SearchIndexName);
        foreach (var chunk in embeddings.Chunk(10))
        {
            var documents = chunk.Select(x => new SearchDocument
            {
                ["id"] = Guid.NewGuid().ToString(),
                ["uri"] = uri,
                ["text"] = x.Text,
                ["textVector"] = x.Embeddings,
                ["pageNumbers"] = x.PageNumbers,
            });

            await indexClient.IndexDocumentsAsync(IndexDocumentsBatch.Upload(documents));
        }
    }

    private async ValueTask CreateIndexIfNotExistAsync()
    {
        if (_isIndexCreated) return;

        var searchIndexResponse = await _searchIndexClient.GetIndexAsync(_importDocumentServiceOptions.SearchIndexName);
        if (searchIndexResponse.GetRawResponse().Status != 404)
        {
            return;
        }

        var searchIndex = new SearchIndex(_importDocumentServiceOptions.SearchIndexName)
        {
            VectorSearch = new()
            {
                AlgorithmConfigurations =
                {
                    new HnswVectorSearchAlgorithmConfiguration("vector")
                }
            },
            Fields =
            {
                // identity
                new SimpleField("id", SearchFieldDataType.String) { IsKey = true },
                // document uri
                new SimpleField("uri", SearchFieldDataType.String),
                // text
                new SearchableField("text"), 
                // 概要のベクトルデータ
                new SearchField("textVector", SearchFieldDataType.Collection(SearchFieldDataType.Single))
                {
                    IsSearchable = true,
                    VectorSearchDimensions = _modelDimensions,
                    VectorSearchConfiguration = "vector",
                },
                new SimpleField("pageNumbers", SearchFieldDataType.Collection(SearchFieldDataType.Int32))
            }
        };

        await _searchIndexClient.CreateIndexAsync(searchIndex);
        _isIndexCreated = true;
    }

    private async ValueTask<IEnumerable<(int PageNumber, string Text)[]>> GetTextFromDocumentAsync(ImportTarget importTarget, CancellationToken cancellationToken)
    {
        var analyzeResult = await _documentAnalysisClient.AnalyzeDocumentFromUriAsync(
            WaitUntil.Completed,
            _importDocumentServiceOptions.DocumentAnalysisModelId,
            new Uri(importTarget.Uri),
            cancellationToken: cancellationToken);

        var pageContents = analyzeResult.Value.Pages
            .Select(page => (page, tables: analyzeResult.Value.Tables.Where(x => x.BoundingRegions[0].PageNumber == page.PageNumber)))
            .Select(page => ExtractText(page.page, page.tables))
            .Chunk(_importDocumentServiceOptions.PageSize);

        return pageContents;
    }

    private (int PageNumber, string Text) ExtractText(DocumentPage page, IEnumerable<DocumentTable> tables)
    {
        static string normalize(string text) => text
            .Replace("\r\n", " ")
            .Replace("\n", " ");

        var text = normalize(string.Join(" ", page.Lines.Select(x => x.Content)));
        var tableText = normalize(string.Join("|", tables.SelectMany(x => x.Cells).Select(x => x.Content)));
        return (page.PageNumber, $"{text} {tableText}");
    }
}

internal record ImportTarget(string Name, string Uri);

