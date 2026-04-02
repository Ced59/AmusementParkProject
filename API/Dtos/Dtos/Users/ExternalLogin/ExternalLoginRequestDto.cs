namespace Dtos.Users.ExternalLogin
{
    public class ExternalLoginRequestDto
    {
        public string Token { get; set; } = string.Empty;

        public string? Nonce { get; set; }
    }
}
