namespace AmusementPark.Infrastructure.Configuration.Images;

/// <summary>
/// Paramètres de stockage MinIO des images.
/// </summary>
public sealed class MinioImageStorageSettings
{
    public const string SectionName = "Images:MinIo";

    public string Endpoint { get; set; } = string.Empty;

    public string AccessKey { get; set; } = string.Empty;

    public string SecretKey { get; set; } = string.Empty;

    public bool WithSsl { get; set; }

    public string Bucket { get; set; } = "amusement-park-images";
}
