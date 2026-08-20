using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Ports;

public interface IUserRankingShareRepository
{
    Task<UserRankingShare?> GetByUserIdAsync(string userId, CancellationToken cancellationToken);

    Task<UserRankingShare?> GetPublicByShareIdAsync(string shareId, CancellationToken cancellationToken);

    Task<UserRankingShare> UpsertAsync(UserRankingShare share, CancellationToken cancellationToken);
}
