using Common.Users;

namespace Dtos.Users.Roles;

public class RoleRemovedDto
{
    public string UserId { get; set; } = string.Empty;
    public IEnumerable<Role> Roles { get; set; } = Enumerable.Empty<Role>();
}