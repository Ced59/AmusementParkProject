namespace Dtos.Users.ChangePassword
{
    public class ChangePasswordDto
    {
        public string ActualPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string NewPasswordConfirm { get; set; } = string.Empty;
    }
}