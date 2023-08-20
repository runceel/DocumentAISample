using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using ImportDocumentFunctionApp.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ImportDocumentFunctionApp;

public class ImportDocumentFunction
{
    private readonly ILogger _logger;
    private readonly IImportDocumentService _importDocumentService;

    public ImportDocumentFunction(ILoggerFactory loggerFactory,
        IImportDocumentService importDocumentService)
    {
        _logger = loggerFactory.CreateLogger<ImportDocumentFunction>();
        _importDocumentService = importDocumentService;
    }

    [Function(nameof(ImportDocumentFunction))]
    public async Task RunAsync([BlobTrigger("upload/{name}", Connection = "InputStorage")] BlobClient document,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("C# Blob trigger function Processed blob.");
        var sasUri = document.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddHours(1));
        await _importDocumentService.ImportAsync(new(document.Name, sasUri), cancellationToken);
    }
}
