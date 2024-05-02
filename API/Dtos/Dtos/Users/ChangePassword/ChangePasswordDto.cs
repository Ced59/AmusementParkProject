using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dtos.Users.ChangePassword
{
    public class ChangePasswordDto
    {
        public string ActualPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string NewPasswordConfirm { get; set; } = string.Empty;
    }
}
