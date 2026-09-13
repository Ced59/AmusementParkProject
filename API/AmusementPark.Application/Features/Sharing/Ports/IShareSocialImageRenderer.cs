using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Results;

namespace AmusementPark.Application.Features.Sharing.Ports;

public interface IShareSocialImageRenderer
{
    Task<ShareSocialImageRenderResult> RenderAsync(
        ShareSocialImageModel model,
        CancellationToken cancellationToken);
}
