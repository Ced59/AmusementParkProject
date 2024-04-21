using Common.Users;
using Entities.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dtos.Users.Updating
{
    public class UserUpdatedDto : ModelBase
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public bool? IsActivated { get; set; }
        public bool? IsBlocked { get; set; }
        public List<Role> Roles { get; set; } = new List<Role>();
        public string? PreferredLanguage { get; set; }
    }
}
