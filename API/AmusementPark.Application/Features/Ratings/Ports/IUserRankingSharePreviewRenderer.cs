using AmusementPark.Application.Features.Ratings.Results;

namespace AmusementPark.Application.Features.Ratings.Ports;

public interface IUserRankingSharePreviewRenderer
{
    Task<byte[]> RenderPngAsync(
        UserRankingSharePreviewResult preview,
        CancellationToken cancellationToken);
}
