using Azure.Storage.Blobs;
using Microsoft.Extensions.Azure;

namespace DocumentAISample.Services;
public class BlobService : IBlobService
{
    private readonly BlobServiceClient _blobServiceClient;

    public BlobService(IAzureClientFactory<BlobServiceClient> azureClientFactory)
    {
        _blobServiceClient = azureClientFactory.CreateClient("OutputBlobService");
    }

    public async ValueTask CopyFromUriAsync(Uri sourceUri, string destinationContainerName, string destinationBlobName, CancellationToken cancellationToken = default)
    {
        var blobContainerClient = _blobServiceClient.GetBlobContainerClient(destinationContainerName);
        var blobClient = blobContainerClient.GetBlobClient(destinationBlobName);
        await blobClient.StartCopyFromUriAsync(sourceUri, cancellationToken: cancellationToken);
    }
}
