using Entities.Model.Users.Enums;

namespace Entities.Model.Users;

public class UserCreated : ModelBase
{
    public string? Email { get; set;}
    public bool? IsActivated { get; set;}
    public bool? IsBlocked { get; set;}
    public List<Role> Roles { get; set; } = new List<Role>();
    public string? PreferredLanguage { get; set; }
}