namespace AmusementPark.Core.Domain.Sharing;

public static class ShareModerationErrorCodes
{
    public const string InvalidState = "share-moderation.invalid-state";
    public const string InvalidTransition = "share-moderation.invalid-transition";
    public const string UnsafeText = "share-moderation.unsafe-text";
}
