using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Model.Errors
{
    public static class ErrorCodes
    {
        public readonly struct ErrorDetail
        {
            public int StatusCode { get; }
            public string Message { get; }

            public ErrorDetail(int statusCode, string message)
            {
                StatusCode = statusCode;
                Message = message;
            }
        }

        public static readonly ErrorDetail PasswordsNotSames = new(400, "Passwords do not match.");
        public static readonly ErrorDetail InvalidEmailAddress = new(400, "Invalid email address format.");
        public static readonly ErrorDetail UserAlreadyExists = new(400, "User Already Exist.");
        public static readonly ErrorDetail UserNotExists = new(404, "User not Exist");
        public static readonly ErrorDetail
            InvalidPassword = new(400, "Password does not meet complexity requirements.");
        public static readonly ErrorDetail UserUpdateFailed = new(500, "Update of the user failed.");
        public static readonly ErrorDetail LoginFailed = new(403, "Username or password invalid");
        public static readonly ErrorDetail TokenRefreshFailed = new(400, "Refresh token failed");
        public static readonly ErrorDetail TokenRefreshFailedInactivity =
            new(400, "Refresh token failed due to inactivity");

        public static readonly ErrorDetail RoleAlreadyAssigned = new(400, "Role already assigned to the user");
        public static readonly ErrorDetail AssignRoleFailed = new(500, "Role assignation failed");
        public static readonly ErrorDetail RoleNotAssigned = new(404, "Role to remove is not assigned to this user");
        public static readonly ErrorDetail RemoveRoleFailed = new(500, "Role deleting failed");
        public static readonly ErrorDetail CannotDeleteUserRole = new(400, "Cannot suppress User Role");
    }
}
