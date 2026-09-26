namespace AmusementPark.WebAPI.Contracts.History;

public sealed class PublicHistoricalSubjectSnapshotDto
{
    public string SubjectType { get; set; } = string.Empty;

    public string SubjectId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string NameOrigin { get; set; } = string.Empty;

    public string OperationalState { get; set; } = string.Empty;

    public string PresenceExtent { get; set; } = string.Empty;

    public IReadOnlyCollection<PublicHistoricalAttributeDto> Attributes { get; set; } =
        Array.Empty<PublicHistoricalAttributeDto>();

    public IReadOnlyCollection<string> ReasonCodes { get; set; } = Array.Empty<string>();

    public int SupportingSourceCount { get; set; }
}
