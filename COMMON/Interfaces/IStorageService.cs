namespace FieldOps.COMMON.Interfaces;

public interface IStorageService
{
    Task<string> GeneratePresignedUploadUrlAsync(string key, string contentType, CancellationToken cancellationToken = default);
    string GetPublicUrl(string key);
    Task DeleteObjectAsync(string key, CancellationToken cancellationToken = default);
    Task UploadAsync(string key, Stream content, string contentType, CancellationToken cancellationToken = default);
    Task<Stream> DownloadAsync(string key, CancellationToken cancellationToken = default);
}
