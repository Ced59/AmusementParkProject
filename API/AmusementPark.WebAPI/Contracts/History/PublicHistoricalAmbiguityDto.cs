namespace AmusementPark.WebAPI.Contracts.History;

public sealed class PublicHistoricalAmbiguityDto
{
    public string SubjectType { get; set; } = string.Empty;

    public string SubjectId { get; set; } = string.Empty;

    public string SubjectLabel { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public string? AttributeKind { get; set; }
}
