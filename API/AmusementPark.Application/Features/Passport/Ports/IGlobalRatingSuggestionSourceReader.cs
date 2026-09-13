using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Passport.Ports;

public interface IGlobalRatingSuggestionSourceReader
{
    Task<IReadOnlyCollection<GlobalRatingSuggestionSource>> ReadAsync(
        string userId,
        CancellationToken cancellationToken);
}
