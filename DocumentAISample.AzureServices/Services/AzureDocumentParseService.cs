using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using ImportDocumentFunctionApp.Services;

namespace DocumentAISample.Services;
public class AzureDocumentParseService : IDocumentParseService
{
    private const string _modelId = "prebuilt-document";
    private readonly DocumentAnalysisClient _documentAnalysisClient;

    public AzureDocumentParseService(DocumentAnalysisClient documentAnalysisClient)
    {
        _documentAnalysisClient = documentAnalysisClient;
    }

    public async ValueTask<ParseDocumentResult> ParseDocumentAsync(ParseDocumentInput input, CancellationToken cancellationToken = default)
    {
        var analyzeResult = await _documentAnalysisClient.AnalyzeDocumentFromUriAsync(
            WaitUntil.Completed,
            _modelId,
            input.Uri,
            cancellationToken: cancellationToken);

        var pageContents = analyzeResult.Value.Pages
            .Select(page => (page, tables: analyzeResult.Value.Tables.Where(x => x.BoundingRegions[0].PageNumber == page.PageNumber)))
            .Select(page => ExtractText(page.page, page.tables));

        return new(pageContents.ToArray());
    }

    private static PageContent ExtractText(DocumentPage page, IEnumerable<DocumentTable> tables)
    {
        static string normalize(string text) => text
            .Replace("\r\n", " ")
            .Replace("\n", " ");

        var text = normalize(string.Join(" ", page.Lines.Select(x => x.Content)));
        var tableText = normalize(string.Join("|", tables.SelectMany(x => x.Cells).Select(x => x.Content)));
        return new(page.PageNumber, $"{text} {tableText}");
    }
}
