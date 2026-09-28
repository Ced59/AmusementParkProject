namespace AmusementPark.WebAPI.Contracts.History;

public sealed class ReviewHistoricalEditorialResourceRequestDto
{
    public int ExpectedRevision { get; init; }

    public string? ReviewNote { get; init; }
}
