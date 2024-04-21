namespace Dtos.Users.Creating
{
    public class UserCreateDto
    {
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? VerifyPassword { get; set; }
        public string? PreferredLanguage { get; set; }
    }
}
