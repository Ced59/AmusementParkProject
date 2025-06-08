namespace Services.Interfaces.Settings;

public interface IImageStorageSettings
{
    string Endpoint { get; set; }
    string AccessKey { get; set; }
    string SecretKey { get; set; }
    string WithSsl { get; set; }
    string Bucket { get; set; }
}