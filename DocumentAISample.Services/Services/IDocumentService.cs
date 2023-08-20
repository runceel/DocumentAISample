namespace ImportDocumentFunctionApp.Services;
public interface IDocumentService
{
    ValueTask<ParseDocumentResult> ParseDocumentAsync(ParseDocumentInput input, CancellationToken cancellationToken = default);
}

public record ParseDocumentInput(Uri Uri);
public record ParseDocumentResult(IReadOnlyCollection<PageContent> PageContents);
public record PageContent(int PageNumber, string Text);