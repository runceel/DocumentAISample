using System;
using System.IO;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ImportDocumentFunctionApp;

public class ImportDocumentFunction
{
    private readonly ILogger _logger;

    public ImportDocumentFunction(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<ImportDocumentFunction>();
    }

    [Function(nameof(ImportDocumentFunction))]
    public async Task RunAsync([BlobTrigger("upload/{name}", Connection = "Storage")] BlobClient document)
    {
        _logger.LogInformation("C# Blob trigger function Processed blob.");
    }
}
