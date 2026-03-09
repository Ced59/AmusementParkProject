namespace Entities.Model.Errors
{
    public static class ErrorCodes
    {
        public static readonly ErrorDetail PasswordsNotSames = new(400, "Passwords do not match.");
        public static readonly ErrorDetail InvalidEmailAddress = new(400, "Invalid email address format.");
        public static readonly ErrorDetail UserAlreadyExists = new(400, "User Already Exist.");
        public static readonly ErrorDetail UserNotExists = new(404, "User not Exist");

        public static readonly ErrorDetail InvalidPassword = new(400, "Password does not meet complexity requirements.");

        public static readonly ErrorDetail UserUpdateFailed = new(500, "Update of the user failed.");
        public static readonly ErrorDetail UserCannotUpdateOtherUser = new(400, "You cannot update other user");
        public static readonly ErrorDetail LoginFailed = new(403, "Username or password invalid");
        public static readonly ErrorDetail TokenRefreshFailed = new(400, "Refresh token failed");

        public static readonly ErrorDetail TokenRefreshFailedInactivity = new(400, "Refresh token failed due to inactivity");

        public static readonly ErrorDetail RoleAlreadyAssigned = new(400, "Role already assigned to the user");
        public static readonly ErrorDetail AssignRoleFailed = new(500, "Role assignation failed");
        public static readonly ErrorDetail RoleNotAssigned = new(404, "Role to remove is not assigned to this user");
        public static readonly ErrorDetail RemoveRoleFailed = new(500, "Role deleting failed");
        public static readonly ErrorDetail CannotLockUser = new(500, "User locking failed");
        public static readonly ErrorDetail CannotUnlockUser = new(500, "User unlocking failed");
        public static readonly ErrorDetail Unauthorized = new(401, "Need be logged to access at this resource");
        public static readonly ErrorDetail UserNotActivated = new(403, "Need be activated to access at this resource");
        public static readonly ErrorDetail UserBlocked = new(403, "User is blocked. You cannot access this resource");

        public static readonly ErrorDetail UserCannotChangeOtherPasswordUser = new(403, "You cannot update other user password");

        public static readonly ErrorDetail IncorrectPassword = new(403, "The actual password is incorrect");
        public static readonly ErrorDetail ChangePasswordFailed = new(500, "Change Password Failed");

        public static readonly ErrorDetail ParkNotExists = new(404, "Park not exists");
        public static readonly ErrorDetail NoParkInThisLocation = new(404, "They are no park in this location");
        public static readonly ErrorDetail ErrorCreatingPark = new(500, "Error while creating park");

        public static readonly ErrorDetail ParkFounderNotExists = new(404, "Park founder not exists");
        public static readonly ErrorDetail ErrorCreatingParkFounder = new(500, "Error while creating park founder");
        public static readonly ErrorDetail ErrorUpdatingParkFounder = new(500, "Error while updating park founder");

        public static readonly ErrorDetail ParkOperatorNotExists = new(404, "Park operator not exists");
        public static readonly ErrorDetail ErrorCreatingParkOperator = new(500, "Error while creating park operator");
        public static readonly ErrorDetail ErrorUpdatingParkOperator = new(500, "Error while updating park operator");

        public static readonly ErrorDetail AttractionManufacturerNotExists = new(404, "Attraction manufacturer not exists");
        public static readonly ErrorDetail ErrorCreatingAttractionManufacturer = new(500, "Error while creating attraction manufacturer");
        public static readonly ErrorDetail ErrorUpdatingAttractionManufacturer = new(500, "Error while updating attraction manufacturer");

        public static readonly ErrorDetail ParkZoneNotExists = new(404, "Park zone not exists");
        public static readonly ErrorDetail ErrorCreatingParkZone = new(500, "Error while creating park zone");
        public static readonly ErrorDetail ErrorUpdatingParkZone = new(500, "Error while updating park zone");
        public static readonly ErrorDetail ErrorDeletingParkZone = new(500, "Error while deleting park zone");

        public static readonly ErrorDetail ParkItemNotExists = new(404, "Park item not exists");
        public static readonly ErrorDetail ErrorCreatingParkItem = new(500, "Error while creating park item");
        public static readonly ErrorDetail ErrorUpdatingParkItem = new(500, "Error while updating park item");
        public static readonly ErrorDetail ErrorDeletingParkItem = new(500, "Error while deleting park item");

        public static readonly ErrorDetail NoImageFileProvided = new(404, "No image filename provided.");
        public static readonly ErrorDetail NoImageCategoryProvided = new(404, "No image category provided.");
        public static readonly ErrorDetail ImageServorInternalError = new(500, "Image processing Internal Server Error");

        public static readonly ErrorDetail ImageNotExists = new(404, "Image does not exist.");
        public static readonly ErrorDetail ImageNotLinkedToOwner = new(400, "Image is not linked to any owner.");
        public static readonly ErrorDetail ErrorUpdatingImageLink = new(500, "Error while updating image link.");
        public static readonly ErrorDetail ErrorSettingCurrentImage = new(500, "Error while setting current image.");
        public static readonly ErrorDetail ErrorDeletingImage = new(500, "Error while deleting image.");

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
    }
}