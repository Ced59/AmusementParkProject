namespace AmusementPark.Application.Features.Sharing.Services;

public static class SharePublicationCacheInvalidationJob
{
    public const string Kind = "sharing.invalidate-publication-caches";

    public const int PayloadVersion = 1;
}
