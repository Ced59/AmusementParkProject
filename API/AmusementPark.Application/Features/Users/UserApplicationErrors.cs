using AmusementPark.Application.Errors;

namespace AmusementPark.Application.Features.Users;

/// <summary>
/// Erreurs applicatives dédiées à la feature Users avec messages alignés sur le legacy.
/// </summary>
internal static class UserApplicationErrors
{
    public static ApplicationError PasswordsNotSames() => ApplicationError.RuleViolation("user.passwords.not-same", "Passwords do not match.");
    public static ApplicationError InvalidEmailAddress() => ApplicationError.RuleViolation("user.email.invalid", "Invalid email address format.");
    public static ApplicationError UserAlreadyExists() => ApplicationError.RuleViolation("user.already-exists", "User Already Exist.");
    public static ApplicationError UserNotExists() => ApplicationError.NotFound("user.not-found", "User not Exist");
    public static ApplicationError InvalidPassword() => ApplicationError.RuleViolation("user.password.invalid", "Password does not meet complexity requirements.");
    public static ApplicationError UserUpdateFailed() => ApplicationError.Technical("user.update.failed", "Update of the user failed.");
    public static ApplicationError UserCannotUpdateOtherUser() => ApplicationError.RuleViolation("user.update.other.forbidden", "You cannot update other user");
    public static ApplicationError LoginFailed() => ApplicationError.Forbidden("auth.login.failed", "Username or password invalid");
    public static ApplicationError InvalidExternalIdentityToken() => ApplicationError.RuleViolation("auth.external.token.invalid", "Invalid external identity token.");
    public static ApplicationError ExternalAuthenticationProviderNotSupported() => ApplicationError.RuleViolation("auth.external.provider.unsupported", "External authentication provider is not supported.");
    public static ApplicationError ExternalLoginRequiresAccountLinking() => ApplicationError.Conflict("auth.external.linking.required", "An account already exists with this email. Sign in with your existing method first, then link Google from your profile.");
    public static ApplicationError TokenRefreshFailed() => ApplicationError.RuleViolation("auth.refresh.failed", "Refresh token failed");
    public static ApplicationError TokenRefreshFailedInactivity() => ApplicationError.RuleViolation("auth.refresh.inactive", "Refresh token failed due to inactivity");
    public static ApplicationError RoleAlreadyAssigned() => ApplicationError.RuleViolation("user.role.already-assigned", "Role already assigned to the user");
    public static ApplicationError AssignRoleFailed() => ApplicationError.Technical("user.role.assign.failed", "Role assignation failed");
    public static ApplicationError RoleNotAssigned() => ApplicationError.NotFound("user.role.not-assigned", "Role to remove is not assigned to this user");
    public static ApplicationError RemoveRoleFailed() => ApplicationError.Technical("user.role.remove.failed", "Role deleting failed");
    public static ApplicationError CannotLockUser() => ApplicationError.Technical("user.lock.failed", "User locking failed");
    public static ApplicationError CannotUnlockUser() => ApplicationError.Technical("user.unlock.failed", "User unlocking failed");
    public static ApplicationError Unauthorized() => ApplicationError.Unauthorized("auth.unauthorized", "Need be logged to access at this resource");
    public static ApplicationError UserNotActivated() => ApplicationError.Forbidden("user.not-activated", "Need be activated to access at this resource");
    public static ApplicationError UserBlocked() => ApplicationError.Forbidden("user.blocked", "User is blocked. You cannot access this resource");
    public static ApplicationError UserCannotChangeOtherPasswordUser() => ApplicationError.Forbidden("user.password.change.other.forbidden", "You cannot update other user password");
    public static ApplicationError IncorrectPassword() => ApplicationError.Forbidden("user.password.incorrect", "The actual password is incorrect");
    public static ApplicationError ChangePasswordFailed() => ApplicationError.Technical("user.password.change.failed", "Change Password Failed");
    public static ApplicationError LocalLoginNotAvailable() => ApplicationError.Forbidden("auth.local.not-available", "Local login is not available for this account.");
    public static ApplicationError EmailConfirmationTokenInvalid() => ApplicationError.RuleViolation("user.email-confirmation.token.invalid", "The email confirmation token is invalid.");
    public static ApplicationError EmailConfirmationTokenExpired() => ApplicationError.RuleViolation("user.email-confirmation.token.expired", "The email confirmation token has expired.");
    public static ApplicationError AccountAlreadyActivated() => ApplicationError.RuleViolation("user.account.already-activated", "The account is already activated.");
    public static ApplicationError ConfirmationEmailResendFailed() => ApplicationError.Technical("user.email-confirmation.resend.failed", "The confirmation email could not be sent.");
    public static ApplicationError PasswordResetTokenInvalid() => ApplicationError.RuleViolation("user.password-reset.token.invalid", "The password reset token is invalid.");
    public static ApplicationError PasswordResetTokenExpired() => ApplicationError.RuleViolation("user.password-reset.token.expired", "The password reset token has expired.");
    public static ApplicationError PasswordResetEmailSendFailed() => ApplicationError.Technical("user.password-reset.email.failed", "The password reset email could not be sent.");
    public static ApplicationError PasswordResetFailed() => ApplicationError.Technical("user.password-reset.failed", "Password reset failed.");
}
