using Minio;
using Minio.DataModel.Args;
using Services.Interfaces.Images;

namespace Services.Implementations.Images;

public class MinioImageStorageService : IImageStorageService
{
    private readonly IMinioClient minioClient;

    public MinioImageStorageService(IMinioClient minioClient)
    {
        this.minioClient = minioClient;
    }

    public async Task StoreAsync(Dictionary<string, byte[]> images, string bucketName)
    {
        BucketExistsArgs? bucketExistsArgs = new BucketExistsArgs().WithBucket(bucketName);
        var exists = await minioClient.BucketExistsAsync(bucketExistsArgs);

        if (!exists)
        {
            await minioClient.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucketName));
        }

        foreach (var (fileName, content) in images)
        {
            using MemoryStream stream = new(content);

            await minioClient.PutObjectAsync(new PutObjectArgs()
                .WithBucket(bucketName)
                .WithObject(fileName)
                .WithStreamData(stream)
                .WithObjectSize(stream.Length));
        }
    }
}