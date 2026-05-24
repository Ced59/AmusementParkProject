namespace AmusementPark.WebAPI.Configuration;

/// <summary>
/// Paramètres d'une fenêtre fixe de rate limiting.
/// </summary>
public sealed class FixedWindowRateLimitSettings
{
    public int PermitLimit { get; set; }

    public int WindowSeconds { get; set; }

    public static FixedWindowRateLimitSettings Create(int permitLimit, int windowSeconds)
    {
        return new FixedWindowRateLimitSettings
        {
            PermitLimit = permitLimit,
            WindowSeconds = windowSeconds,
        };
    }
}
