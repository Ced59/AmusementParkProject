namespace Services.Interfaces.Images;

public interface IImageStorageService
{
    Task<IEnumerable<string>> StoreAsync(Dictionary<string, byte[]> images, string bucketName, string category);
}