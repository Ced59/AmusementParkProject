namespace AmusementPark.Core.Domain.Sharing;

public enum ShareModerationReason
{
    PersonalData = 1,
    HarassmentOrHate = 2,
    Impersonation = 3,
    InappropriateContent = 4,
    SpamOrUnsafeLink = 5,
    MisleadingContent = 6,
    Other = 7,
}
