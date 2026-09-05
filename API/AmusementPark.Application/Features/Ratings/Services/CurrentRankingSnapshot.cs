namespace AmusementPark.Application.Features.Ratings.Services;

internal sealed record CurrentRankingSnapshot(
    bool IsAvailable,
    IReadOnlyDictionary<string, int> Ranks)
{
    public static CurrentRankingSnapshot Unavailable { get; } =
        new CurrentRankingSnapshot(false, new Dictionary<string, int>(StringComparer.Ordinal));
}
