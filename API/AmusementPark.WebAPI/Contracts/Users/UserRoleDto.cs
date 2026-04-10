using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.Users;

/// <summary>
/// Rôle HTTP utilisateur aligné sur le legacy.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UserRoleDto
{
    USER,
    MODERATOR,
    ADMIN,
}
