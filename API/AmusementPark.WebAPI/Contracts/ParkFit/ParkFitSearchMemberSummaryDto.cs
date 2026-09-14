namespace AmusementPark.WebAPI.Contracts.ParkFit;

public sealed class ParkFitSearchMemberSummaryDto
{
    public int MemberNumber { get; init; }

    public int CompatibleAloneAttractionCount { get; init; }

    public int CompatibleWithCompanionAttractionCount { get; init; }

    public int IncompatibleAttractionCount { get; init; }

    public int UnknownAttractionCount { get; init; }

    public int NotApplicableAttractionCount { get; init; }
}
