namespace AmusementPark.Application.Features.TechnicalPages.Results;

public sealed class TechnicalPageJsonUpsertResult
{
    public int CreatedCount { get; init; }

    public int UpdatedCount { get; init; }

    public IReadOnlyCollection<TechnicalPageResult> Pages { get; init; } = Array.Empty<TechnicalPageResult>();
}
