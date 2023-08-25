namespace ImportDocumentFunctionApp.Services;
public interface IImportDocumentService
{
    ValueTask ImportAsync(ImportTarget importTarget, CancellationToken cancellationToken = default);
}
public record ImportTarget(string Name, Uri Uri);
