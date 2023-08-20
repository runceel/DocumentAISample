using Azure.Core;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using DocumentAISample.Utils;

namespace DocumentAISample.Services;
public class AzureBlobService : IBlobService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ISystemClock _systemClock;

    public AzureBlobService(BlobServiceClient blobServiceClient,
        ISystemClock systemClock)
    {
        _blobServiceClient = blobServiceClient;
        _systemClock = systemClock;
    }

    public async ValueTask CopyFromUriAsync(Uri sourceUri, string destinationContainerName, string destinationBlobName, CancellationToken cancellationToken = default)
    {
        var blobContainerClient = _blobServiceClient.GetBlobContainerClient(destinationContainerName);
        var blobClient = blobContainerClient.GetBlobClient(destinationBlobName);
        await blobClient.StartCopyFromUriAsync(sourceUri, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async ValueTask<Uri> GenerateReadUriAsync(string containerName, string documentName, CancellationToken cancellationToken)
    {
        var now = _systemClock.UtcNow().AddMinutes(-1);
        var userDelegationKey = await _blobServiceClient.GetUserDelegationKeyAsync(
            now,
            now.AddMinutes(5),
            cancellationToken).ConfigureAwait(false);
        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = containerName,
            BlobName = documentName,
            Resource = "b",
            StartsOn = now,
            ExpiresOn = now.AddHours(1),
        };
        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        var blobContainerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blob = blobContainerClient.GetBlobClient(documentName);

        var uriBuilder = new BlobUriBuilder(blob.Uri)
        {
            Sas = sasBuilder.ToSasQueryParameters(
                userDelegationKey,
                _blobServiceClient.AccountName),
        };
        return uriBuilder.ToUri();
    }
}
