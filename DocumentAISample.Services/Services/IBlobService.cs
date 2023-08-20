namespace DocumentAISample.Services;
public interface IBlobService
{
    ValueTask CopyFromUriAsync(Uri sourceUri,
        string destinationContainerName,
        string destinationBlobName,
        CancellationToken cancellationToken = default);
}
