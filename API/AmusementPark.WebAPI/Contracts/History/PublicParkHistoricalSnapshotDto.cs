namespace AmusementPark.WebAPI.Contracts.History;

public sealed class PublicParkHistoricalSnapshotDto
{
    public string ParkId { get; set; } = string.Empty;

    public string ParkName { get; set; } = string.Empty;

    public PublicHistoricalDateDto RequestedInstant { get; set; } = new PublicHistoricalDateDto();

    public IReadOnlyCollection<PublicHistoricalSubjectSnapshotDto> Subjects { get; set; } =
        Array.Empty<PublicHistoricalSubjectSnapshotDto>();

    public PublicHistoricalCoverageDto Coverage { get; set; } = new PublicHistoricalCoverageDto();

    public IReadOnlyCollection<PublicHistoricalAmbiguityDto> Ambiguities { get; set; } =
        Array.Empty<PublicHistoricalAmbiguityDto>();

    public string MethodologyVersion { get; set; } = string.Empty;
}
