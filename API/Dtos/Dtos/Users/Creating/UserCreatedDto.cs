using Common.General;
using Common.Users;

namespace Dtos.Users.Creating;

public class UserCreatedDto : ModelBase
{
    public string? Email { get; set; }
    public bool? IsActivated { get; set; }
    public bool? IsBlocked { get; set; }
    public List<Role> Roles { get; set; } = new();
    public string? PreferredLanguage { get; set; }
}