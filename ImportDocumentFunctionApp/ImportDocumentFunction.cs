using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using DocumentAISample.Utils;
using ImportDocumentFunctionApp.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ImportDocumentFunctionApp;

public class ImportDocumentFunction
{
    private readonly ILogger _logger;
    private readonly IImportDocumentService _importDocumentService;
    private readonly ISystemClock _systemClock;

    public ImportDocumentFunction(ILoggerFactory loggerFactory,
        IImportDocumentService importDocumentService,
        ISystemClock systemClock)
    {
        _logger = loggerFactory.CreateLogger<ImportDocumentFunction>();
        _importDocumentService = importDocumentService;
        _systemClock = systemClock;
    }

    [Function(nameof(ImportDocumentFunction))]
    public async Task RunAsync([BlobTrigger("upload/{name}", Connection = "InputStorage")] BlobClient document,
        CancellationToken cancellationToken)
    {
        var sasUri = document.GenerateSasUri(BlobSasPermissions.Read, _systemClock.UtcNow().AddHours(1));
        await _importDocumentService.ImportAsync(new(document.Name, sasUri), cancellationToken);
    }
}
