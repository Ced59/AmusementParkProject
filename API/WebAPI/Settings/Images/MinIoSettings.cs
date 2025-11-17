using Services.Interfaces.Settings;

namespace WebAPI.Settings.Images
{
    public class MinIoSettings: IImageStorageSettings
    {
        public string Endpoint { get; set; } = string.Empty;
        public string AccessKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public string WithSsl { get; set; } = string.Empty;
        public string Bucket { get; set; } = string.Empty;
    }
}