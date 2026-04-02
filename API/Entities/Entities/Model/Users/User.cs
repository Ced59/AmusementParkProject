using Common.General;
using Common.Users;

namespace Entities.Model.Users
{
    public class User : ModelBase
    {
        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        public string? Email { get; set; }

        public string? HashedPassword { get; set; }

        public bool IsActivated { get; set; }

        public bool IsBlocked { get; set; }

        public string? PreferredLanguage { get; set; }

        public string? AvatarUrl { get; set; }

        public List<Role> Roles { get; set; } = new();

        public List<ExternalLogin> ExternalLogins { get; set; } = new();

        public DateTime LastLogin { get; set; }

        public DateTime LastActivity { get; set; }
    }
}
