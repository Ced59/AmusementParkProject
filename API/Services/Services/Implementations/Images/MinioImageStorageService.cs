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
        BucketExistsArgs bucketExistsArgs = new BucketExistsArgs().WithBucket(settings.Bucket);
        bool exists = await minioClient.BucketExistsAsync(bucketExistsArgs);

        if (!exists)
        {
            await minioClient.MakeBucketAsync(new MakeBucketArgs().WithBucket(settings.Bucket));
        }

        List<string> savedListFiles = new();

        foreach ((string fileName, byte[] content) in images)
        {
            await using MemoryStream stream = new(content);

            string savedFileName = $"{category}/{fileName}";

            string contentType = GetContentTypeFromExtension(fileName);

            await minioClient.PutObjectAsync(new PutObjectArgs()
                .WithBucket(settings.Bucket)
                .WithObject(savedFileName)
                .WithStreamData(stream)
                .WithObjectSize(stream.Length)
                .WithContentType(contentType));

            savedListFiles.Add(savedFileName);
        }

        return savedListFiles;
    }

    private static string GetContentTypeFromExtension(string fileName)
    {
        string ext = Path.GetExtension(fileName).ToLowerInvariant();

        return ext switch
        {
            ".webp" => "image/webp",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            _ => "application/octet-stream"
        };
    }
}
