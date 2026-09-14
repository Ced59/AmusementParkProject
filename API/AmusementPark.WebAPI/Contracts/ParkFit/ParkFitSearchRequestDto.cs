using AmusementPark.WebAPI.Contracts.ParkItems;

namespace AmusementPark.WebAPI.Contracts.ParkFit;

public sealed class ParkFitSearchRequestDto
{
    public DateOnly EvaluationDate { get; set; }

    public IReadOnlyCollection<ParkFitSearchMemberCriteriaDto> Members { get; set; } =
        Array.Empty<ParkFitSearchMemberCriteriaDto>();

    public IReadOnlyCollection<ParkItemTypeDto> PreferredAttractionTypes { get; set; } =
        Array.Empty<ParkItemTypeDto>();

    public bool PreferIndoor { get; set; }

    public string? CountryCode { get; set; }

    public ParkFitUnknownDataPolicyDto UnknownDataPolicy { get; set; } =
        ParkFitUnknownDataPolicyDto.KeepWithWarning;

    public int MaximumResults { get; set; } = 10;
}
