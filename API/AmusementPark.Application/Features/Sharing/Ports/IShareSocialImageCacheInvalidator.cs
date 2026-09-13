namespace AmusementPark.Application.Features.Sharing.Ports;

public interface IShareSocialImageCacheInvalidator
{
    void Invalidate(IReadOnlyCollection<string> shareIds);
}
