using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Ports;

public interface IRankingScopeRegistry
{
    string Version { get; }

    IReadOnlyCollection<RankingScopeDefinition> Definitions { get; }

    bool TryResolve(
        string? scopeKey,
        RatingMethodologyVersion methodologyVersion,
        out RankingScopeDefinition? definition);
}
