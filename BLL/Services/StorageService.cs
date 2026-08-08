using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using FieldOps.BLL.Options;
using FieldOps.COMMON.Interfaces;
using Microsoft.Extensions.Options;

namespace FieldOps.BLL.Services;

public class StorageService : IStorageService
{
    private readonly IAmazonS3 _s3;
    private readonly StorageOptions _options;

    public StorageService(IAmazonS3 s3, IOptions<StorageOptions> options)
    {
        _s3 = s3;
        _options = options.Value;
    }

    public async Task<string> GeneratePresignedUploadUrlAsync(string key, string contentType, CancellationToken cancellationToken = default)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _options.Bucket,
            Key = key,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.AddMinutes(15),
            ContentType = contentType
        };

        // AWSSDK.S3 exposes sync GetPreSignedURL; wrap for async API surface.
        return await Task.FromResult(_s3.GetPreSignedURL(request));
    }

    public string GetPublicUrl(string key)
    {
        if (!string.IsNullOrWhiteSpace(_options.PublicBaseUrl))
            return $"{_options.PublicBaseUrl.TrimEnd('/')}/{key}";

        return $"{_options.Endpoint.TrimEnd('/')}/{_options.Bucket}/{key}";
    }

    public async Task DeleteObjectAsync(string key, CancellationToken cancellationToken = default)
    {
        await _s3.DeleteObjectAsync(_options.Bucket, key, cancellationToken);
    }

    public async Task UploadAsync(string key, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        var request = new PutObjectRequest
        {
            BucketName = _options.Bucket,
            Key = key,
            InputStream = content,
            ContentType = contentType,
            AutoCloseStream = false
        };
        await _s3.PutObjectAsync(request, cancellationToken);
    }

    public async Task<Stream> DownloadAsync(string key, CancellationToken cancellationToken = default)
    {
        var response = await _s3.GetObjectAsync(_options.Bucket, key, cancellationToken);
        var memory = new MemoryStream();
        await response.ResponseStream.CopyToAsync(memory, cancellationToken);
        memory.Position = 0;
        return memory;
    }

    public static IAmazonS3 CreateClient(StorageOptions options)
    {
        var config = new AmazonS3Config
        {
            ServiceURL = options.Endpoint,
            ForcePathStyle = true,
            AuthenticationRegion = "us-east-1"
        };

        var credentials = new BasicAWSCredentials(options.AccessKey, options.SecretKey);
        return new AmazonS3Client(credentials, config);
    }
}
