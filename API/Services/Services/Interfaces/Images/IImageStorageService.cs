namespace Services.Interfaces.Images;

public interface IImageStorageService
{
    Task StoreAsync(Dictionary<string, byte[]> images, string bucketName);
}