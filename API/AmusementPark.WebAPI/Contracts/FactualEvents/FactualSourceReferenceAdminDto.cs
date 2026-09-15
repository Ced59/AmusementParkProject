namespace AmusementPark.WebAPI.Contracts.FactualEvents;

public sealed class FactualSourceReferenceAdminDto
{
    public string Type { get; init; } = string.Empty;

    public string PublisherName { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string Url { get; init; } = string.Empty;

    public DateTime PublishedAtUtc { get; init; }
}
