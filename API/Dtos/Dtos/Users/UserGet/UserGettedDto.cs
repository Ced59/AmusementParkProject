using Common.General;
using Common.Users;

namespace Dtos.Users.UserGet
{
    public class UserGettedDto : ModelBase
    {
        public string? Email { get; set; } = string.Empty;
        public string? FirstName { get; set; } = string.Empty;
        public string? LastName { get; set; } = string.Empty;
        public bool? IsActivated { get; set; } = false;
        public bool? IsBlocked { get; set; } = false;
        public List<Role> Roles { get; set; } = new();
        public string? PreferredLanguage { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
    }
}