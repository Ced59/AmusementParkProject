using System.Text.Json.Serialization;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Models;

public sealed record RatingRankingRebuildScopePayload(
    [property: JsonPropertyName("scopeKey")] string ScopeKey,
    [property: JsonPropertyName("requestedSourceRevision")] long RequestedSourceRevision,
    [property: JsonPropertyName("methodologyVersion")] string MethodologyVersion);

public static class RatingRankingRebuildScopeJob
{
    public const string Kind = "ratings.rebuild-scope";
    public const int PayloadVersion = 1;

    public static string BuildNaturalKey(RankingScopeKey scopeKey)
    {
        return $"{Kind}:{scopeKey.Value}";
    }
}

public static class RatingRankingRebuildErrorCodes
{
    public const string InvalidPayload = "ranking-snapshot.invalid-rebuild-payload";
    public const string UnknownScope = "ranking-snapshot.unknown-scope";
    public const string SourceRevisionUnavailable = "ranking-snapshot.source-revision-unavailable";
    public const string SourceSetTruncated = "ranking-snapshot.source-set-truncated";
    public const string BelowMinimumEligibleEntries = "ranking-snapshot.below-minimum-eligible-entries";
    public const string BuildConflict = "ranking-snapshot.build-conflict";
    public const string ChunkWriteConflict = "ranking-snapshot.chunk-write-conflict";
    public const string ValidationFailed = "ranking-snapshot.validation-failed";
    public const string PublicationConflict = "ranking-snapshot.publication-conflict";
    public const string RetirementConflict = "ranking-snapshot.retirement-conflict";
}

public sealed record RatingRankingMutationLease
{
    public RatingRankingMutationLease(
        RankingScopeKey scopeKey,
        string token)
    {
        if (!Guid.TryParseExact(token, "N", out Guid parsedToken))
        {
            throw new ArgumentException("The ranking mutation lease token is invalid.", nameof(token));
        }

        this.ScopeKey = scopeKey;
        this.Token = parsedToken.ToString("N");
    }

    public RankingScopeKey ScopeKey { get; }

    public string Token { get; }

    public static RatingRankingMutationLease Create(RankingScopeKey scopeKey)
    {
        return new RatingRankingMutationLease(
            scopeKey,
            Guid.NewGuid().ToString("N"));
    }
}

public sealed record RatingRankingSnapshotBuildPlan(
    int TotalEntryCount,
    IReadOnlyCollection<RankingSnapshotEntry> EligibleEntries,
    bool IsSourceTruncated);

public sealed class RatingRankingMutationPreparation
{
    public RatingRankingMutationPreparation(
        IReadOnlyCollection<RatingRankingMutationLease> mutationLeases)
    {
        ArgumentNullException.ThrowIfNull(mutationLeases);
        this.MutationLeases = Array.AsReadOnly(mutationLeases
            .DistinctBy(static lease => lease.ScopeKey)
            .OrderBy(static lease => lease.ScopeKey.Value, StringComparer.Ordinal)
            .ToArray());
    }

    public IReadOnlyCollection<RatingRankingMutationLease> MutationLeases { get; }
}
