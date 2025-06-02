using Minio;
using Minio.DataModel.Args;
using Services.Interfaces.Images;

namespace Services.Implementations.Images;

public class MinioImageStorageService : IImageStorageService
{
    private readonly IMinioClient _minioClient;

    public MinioImageStorageService(IMinioClient minioClient)
    {
        _minioClient = minioClient;
    }

    public async Task StoreAsync(Dictionary<string, byte[]> images, string bucketName)
    {
        BucketExistsArgs? bucketExistsArgs = new BucketExistsArgs().WithBucket(bucketName);
        var exists = await _minioClient.BucketExistsAsync(bucketExistsArgs);

        if (!exists)
        {
            await _minioClient.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucketName));
        }

        foreach (var (fileName, content) in images)
        {
            using MemoryStream stream = new(content);

            await _minioClient.PutObjectAsync(new PutObjectArgs()
                .WithBucket(bucketName)
                .WithObject(fileName)
                .WithStreamData(stream)
                .WithObjectSize(stream.Length));
        }
    }
}