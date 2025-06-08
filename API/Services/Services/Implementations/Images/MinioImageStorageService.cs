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

    public async Task<IEnumerable<string>> StoreAsync(Dictionary<string, byte[]> images, string bucketName, string category)
    {
        BucketExistsArgs? bucketExistsArgs = new BucketExistsArgs().WithBucket(bucketName);
        bool exists = await minioClient.BucketExistsAsync(bucketExistsArgs);

        if (!exists)
        {
            await minioClient.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucketName));
        }

        List<string> savedListFiles = new List<string>();

        foreach ((string fileName, byte[] content) in images)
        {
            using MemoryStream stream = new(content);

            string savedFileName = category + '-' + fileName;

            await minioClient.PutObjectAsync(new PutObjectArgs()
                .WithBucket(bucketName)
                .WithObject(savedFileName)
                .WithStreamData(stream)
                .WithObjectSize(stream.Length));

            savedListFiles.Add(savedFileName);
        }

        return savedListFiles;
    }
}