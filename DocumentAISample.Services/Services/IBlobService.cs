namespace DocumentAISample.Services;
public interface IBlobService
{
    ValueTask CopyFromUriAsync(Uri sourceUri,
        string destinationContainerName,
        string destinationBlobName,
        CancellationToken cancellationToken = default);

    ValueTask<Uri> GenerateReadUriAsync(string containerName, string documentName, CancellationToken cancellationToken);
}
