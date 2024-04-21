using Common.Users;

namespace Dtos.Users;

public class UserCreatedDto : ModelBaseDto
{
    public string? Email { get; set;}
    public bool? IsActivated { get; set;}
    public bool? IsBlocked { get; set;}
    public List<Role> Roles { get; set; } = new List<Role>();
    public string? PreferredLanguage { get; set; }
}