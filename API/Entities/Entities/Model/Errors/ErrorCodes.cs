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
    }
}
