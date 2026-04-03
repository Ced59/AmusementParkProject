using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Services.Interfaces.Images;
using Services.Interfaces.Settings;

namespace Services.Implementations.Images
{
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

        public async Task<(Stream Stream, string ContentType)?> GetBestImageAsync(
            string imagePathWithoutExtension,
            string? acceptHeader,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(imagePathWithoutExtension))
            {
                return null;
            }

            bool clientSupportsWebp = !string.IsNullOrWhiteSpace(acceptHeader)
                                      && acceptHeader.Contains("image/webp", StringComparison.OrdinalIgnoreCase);

            if (clientSupportsWebp)
            {
                var webp = await TryGetObjectAsync(
                    $"{imagePathWithoutExtension}.webp",
                    "image/webp",
                    cancellationToken);

                if (webp != null)
                {
                    return webp;
                }
            }

            var jpg = await TryGetObjectAsync(
                $"{imagePathWithoutExtension}.jpg",
                "image/jpeg",
                cancellationToken);

            if (jpg != null)
            {
                return jpg;
            }

            var png = await TryGetObjectAsync(
                $"{imagePathWithoutExtension}.png",
                "image/png",
                cancellationToken);

            if (png != null)
            {
                return png;
            }

            return null;
        }

        private async Task<(Stream Stream, string ContentType)?> TryGetObjectAsync(
            string objectName,
            string contentType,
            CancellationToken cancellationToken)
        {
            try
            {
                await minioClient.StatObjectAsync(
                new StatObjectArgs()
                        .WithBucket(settings.Bucket)
                        .WithObject(objectName),
                    cancellationToken);

                MemoryStream ms = new MemoryStream();

                await minioClient.GetObjectAsync(
                new GetObjectArgs()
                        .WithBucket(settings.Bucket)
                        .WithObject(objectName)
                        .WithCallbackStream(s => s.CopyTo(ms)),
                    cancellationToken);

                ms.Position = 0;
                return (ms, contentType);
            }
            catch (Minio.Exceptions.ObjectNotFoundException)
            {
                return null;
            }
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
}
