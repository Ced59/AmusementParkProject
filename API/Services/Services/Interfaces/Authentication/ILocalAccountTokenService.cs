namespace Services.Interfaces.Authentication
{
    public interface ILocalAccountTokenService
    {
        string GenerateToken();

        string ComputeHash(string token);
    }
}
