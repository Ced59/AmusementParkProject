namespace AmusementPark.WebAPI.RateLimiting;

/// <summary>
/// Policies de rate limiting applicatif appliquées explicitement aux endpoints sensibles.
/// </summary>
public static class RateLimitPolicyNames
{
    public const string AuthLogin = "auth-login";
    public const string AuthExternalLogin = "auth-external-login";
    public const string AuthRefresh = "auth-refresh";
    public const string AuthRegistration = "auth-registration";
    public const string AuthEmailChallenge = "auth-email-challenge";
    public const string AuthPasswordReset = "auth-password-reset";
}
