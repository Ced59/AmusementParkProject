using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Infrastructure.Configuration.Images;
using Minio;
using Minio.DataModel.Args;

namespace AmusementPark.Infrastructure.Services.Parks;

/// <summary>
/// Stockage MinIO des documents de carte officielle sans passer par le pipeline de
/// redimensionnement, recompression et watermark réservé aux images éditoriales.
/// </summary>
public sealed class MinioParkOfficialMapBinaryStorage : IParkOfficialMapBinaryStorage
{
    private readonly IMinioClient minioClient;
    private readonly MinioImageStorageSettings settings;

    public MinioParkOfficialMapBinaryStorage(
        IMinioClient minioClient,
        MinioImageStorageSettings settings)
    {
        this.minioClient = minioClient;
        this.settings = settings;
    }

    public async Task SaveAsync(
        string storageKey,
        FilePayload file,
        string canonicalContentType,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);
        ArgumentNullException.ThrowIfNull(file);
        ArgumentException.ThrowIfNullOrWhiteSpace(canonicalContentType);

        await this.EnsureBucketExistsAsync(cancellationToken);
        if (file.Content.CanSeek)
        {
            file.Content.Position = 0;
        }

        await this.minioClient.PutObjectAsync(
            new PutObjectArgs()
                .WithBucket(this.settings.Bucket)
                .WithObject(storageKey)
                .WithStreamData(file.Content)
                .WithObjectSize(file.Length)
                .WithContentType(canonicalContentType),
            cancellationToken);
    }

    public async Task<bool> ExistsAsync(string storageKey, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);
        try
        {
            await this.minioClient.StatObjectAsync(
                new StatObjectArgs()
                    .WithBucket(this.settings.Bucket)
                    .WithObject(storageKey),
                cancellationToken);

            return true;
        }
        catch (Minio.Exceptions.ObjectNotFoundException)
        {
            return false;
        }
    }

    public async Task CopyToAsync(
        string storageKey,
        Stream destination,
        long offset,
        long? length,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);
        ArgumentNullException.ThrowIfNull(destination);

        GetObjectArgs arguments = new GetObjectArgs()
            .WithBucket(this.settings.Bucket)
            .WithObject(storageKey)
            .WithCallbackStream((source, callbackCancellationToken) =>
                source.CopyToAsync(destination, callbackCancellationToken));
        if (length.HasValue)
        {
            arguments = arguments.WithOffsetAndLength(offset, length.Value);
        }

        await this.minioClient.GetObjectAsync(arguments, cancellationToken);
    }

    public async Task<bool> CopyAsync(
        string sourceStorageKey,
        string targetStorageKey,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceStorageKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetStorageKey);
        try
        {
            await this.minioClient.CopyObjectAsync(
                new CopyObjectArgs()
                    .WithBucket(this.settings.Bucket)
                    .WithObject(targetStorageKey)
                    .WithCopyObjectSource(new CopySourceObjectArgs()
                        .WithBucket(this.settings.Bucket)
                        .WithObject(sourceStorageKey)),
                cancellationToken);
            return true;
        }
        catch (Minio.Exceptions.ObjectNotFoundException)
        {
            return false;
        }
    }

    private async Task EnsureBucketExistsAsync(CancellationToken cancellationToken)
    {
        bool exists = await this.minioClient.BucketExistsAsync(
            new BucketExistsArgs().WithBucket(this.settings.Bucket));
        if (!exists)
        {
            await this.minioClient.MakeBucketAsync(
                new MakeBucketArgs().WithBucket(this.settings.Bucket),
                cancellationToken);
        }
    }
}
