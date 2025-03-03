namespace Dtos.Users.Updating;

public class UserUpdateDto
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? NewEmail { get; set; }
    public string? PreferredLanguage { get; set; }
    public string? AvatarUrl { get; set; }
}