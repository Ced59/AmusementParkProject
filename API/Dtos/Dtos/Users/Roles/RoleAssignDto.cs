using Common.Users;

namespace Dtos.Users.Roles;

public class RoleAssignDto
{
    public Role Role { get; set; } = new();
}