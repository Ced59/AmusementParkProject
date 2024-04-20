using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Model.Users
{
    public class UserCreate
    {
        public string? UserName { get; set; }
        public string? Password { get; set; }
        public string? VerifyPassword { get; set; }
        public string? PreferredLanguage { get; set; }
    }
}
