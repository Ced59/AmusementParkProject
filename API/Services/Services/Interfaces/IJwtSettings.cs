namespace Services.Interfaces;

public interface IJwtSettings
{
    string Key { get; set; }
    string Issuer { get; set; }
    string Audience { get; set; }
    int TokenRefreshLimitMinutes { get; set; }
}