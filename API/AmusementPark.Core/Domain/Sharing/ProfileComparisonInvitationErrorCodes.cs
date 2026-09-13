namespace AmusementPark.Core.Domain.Sharing;

public static class ProfileComparisonInvitationErrorCodes
{
    public const string InvalidCategory = "profile-comparison-invitation.category-invalid";

    public const string InvalidCategoryCount = "profile-comparison-invitation.category-count-invalid";

    public const string InvalidLifetime = "profile-comparison-invitation.lifetime-invalid";

    public const string InvalidState = "profile-comparison-invitation.state-invalid";

    public const string Expired = "profile-comparison-invitation.expired";

    public const string AlreadyAccepted = "profile-comparison-invitation.already-accepted";

    public const string SelfAcceptance = "profile-comparison-invitation.self-acceptance";
}
