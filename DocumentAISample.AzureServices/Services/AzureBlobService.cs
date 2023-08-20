using Azure.Storage.Blobs;
using Microsoft.Extensions.Azure;

namespace DocumentAISample.Services;
public class AzureBlobService : IBlobService
{
    private readonly BlobServiceClient _blobServiceClient;

    public AzureBlobService(BlobServiceClient blobServiceClient)
    {
        _blobServiceClient = blobServiceClient;
    }

    public async ValueTask CopyFromUriAsync(Uri sourceUri, string destinationContainerName, string destinationBlobName, CancellationToken cancellationToken = default)
    {
        var blobContainerClient = _blobServiceClient.GetBlobContainerClient(destinationContainerName);
        var blobClient = blobContainerClient.GetBlobClient(destinationBlobName);
        await blobClient.StartCopyFromUriAsync(sourceUri, cancellationToken: cancellationToken);
    }
}
