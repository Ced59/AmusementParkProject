namespace AmusementPark.Application.Features.Sharing.Results;

public enum ProfileComparisonInvitationPreviewStatus
{
    Ready = 0,
    Accepted = 1,
    Expired = 2,
    OwnInvitation = 3,
    InviterPassportUnavailable = 4,
    InviteePassportUnavailable = 5,
    InviteeCategoriesUnavailable = 6,
}
