namespace Dtos.Users.LockUser;

public class UserLockedDto
{
    public string UserId { get; set; } = string.Empty;
    public string? FirstName { get; set; } = string.Empty;
    public string? LastName { get; set; } = string.Empty;
}