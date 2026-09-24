using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using UniPilot.Application.Documents;

namespace UniPilot.Infrastructure.Documents;

public sealed class SupabaseFileStorage : IFileStorage,
    IDisposable
{
    private readonly AmazonS3Client _client;
    private readonly string _bucketName;

    public SupabaseFileStorage(
        IConfiguration configuration)
    {
        var endpoint = RequireSetting(
            configuration,
            "SupabaseStorage:Endpoint");

        var region = RequireSetting(
            configuration,
            "SupabaseStorage:Region");

        var accessKeyId = RequireSetting(
            configuration,
            "SupabaseStorage:AccessKeyId");

        var secretAccessKey = RequireSetting(
            configuration,
            "SupabaseStorage:SecretAccessKey");

        _bucketName = RequireSetting(
            configuration,
            "SupabaseStorage:BucketName");

        var clientConfiguration = new AmazonS3Config
        {
            ServiceURL = endpoint.TrimEnd('/'),
            AuthenticationRegion = region,
            ForcePathStyle = true
        };

        _client = new AmazonS3Client(
            accessKeyId,
            secretAccessKey,
            clientConfiguration);
    }

    public async Task SaveAsync(
        string storageKey,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        var objectKey = NormalizeStorageKey(storageKey);

        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = objectKey,
            InputStream = content,
            AutoCloseStream = false,
            ContentType = "application/pdf"
        };

        await _client.PutObjectAsync(
            request,
            cancellationToken);
    }

    public async Task<Stream> OpenReadAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        var objectKey = NormalizeStorageKey(storageKey);

        using var response = await _client.GetObjectAsync(
            new GetObjectRequest
            {
                BucketName = _bucketName,
                Key = objectKey
            },
            cancellationToken);

        var content = new MemoryStream();

        await response.ResponseStream.CopyToAsync(
            content,
            cancellationToken);

        content.Position = 0;
        return content;
    }

    public async Task DeleteAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        var objectKey = NormalizeStorageKey(storageKey);

        await _client.DeleteObjectAsync(
            new DeleteObjectRequest
            {
                BucketName = _bucketName,
                Key = objectKey
            },
            cancellationToken);
    }

    public void Dispose()
    {
        _client.Dispose();
    }

    private static string NormalizeStorageKey(
        string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
        {
            throw new InvalidOperationException(
                "The storage key is required.");
        }

        var normalized = storageKey
            .Replace('\\', '/')
            .TrimStart('/');

        var segments = normalized.Split(
            '/',
            StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length == 0 ||
            segments.Any(segment =>
                segment is "." or ".."))
        {
            throw new InvalidOperationException(
                "The storage key contains an invalid path.");
        }

        return string.Join('/', segments);
    }

    private static string RequireSetting(
        IConfiguration configuration,
        string key)
    {
        return configuration[key]
            ?? throw new InvalidOperationException(
                $"{key} was not configured.");
    }
}
