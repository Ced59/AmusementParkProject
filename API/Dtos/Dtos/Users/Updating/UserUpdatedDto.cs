using Common.General;
using Common.Users;

namespace Dtos.Users.Updating;

public class UserUpdatedDto : ModelBase
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public bool? IsActivated { get; set; }
    public bool? IsBlocked { get; set; }
    public List<Role> Roles { get; set; } = new();
    public string? PreferredLanguage { get; set; }
}