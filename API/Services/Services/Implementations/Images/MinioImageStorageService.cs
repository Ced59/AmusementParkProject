using Minio;
using Minio.DataModel.Args;
using Services.Interfaces.Images;
using Services.Interfaces.Settings;

namespace Services.Implementations.Images;

public class MinioImageStorageService : IImageStorageService
{
    private readonly IMinioClient minioClient;
    private readonly IImageStorageSettings settings;


    public MinioImageStorageService(IMinioClient minioClient, IImageStorageSettings settings)
    {
        this.minioClient = minioClient;
        this.settings = settings;
    }

    public async Task<IEnumerable<string>> StoreAsync(Dictionary<string, byte[]> images, string category)
    {
        BucketExistsArgs? bucketExistsArgs = new BucketExistsArgs().WithBucket(settings.Bucket);
        bool exists = await minioClient.BucketExistsAsync(bucketExistsArgs);

        if (!exists)
        {
            await minioClient.MakeBucketAsync(new MakeBucketArgs().WithBucket(settings.Bucket));
        }

        List<string> savedListFiles = new List<string>();

        foreach ((string fileName, byte[] content) in images)
        {
            using MemoryStream stream = new(content);

            string savedFileName = category + '-' + fileName;

            await minioClient.PutObjectAsync(new PutObjectArgs()
                .WithBucket(settings.Bucket)
                .WithObject(savedFileName)
                .WithStreamData(stream)
                .WithObjectSize(stream.Length));

            savedListFiles.Add(savedFileName);
        }

        return savedListFiles;
    }
}