namespace AmusementPark.Core.Domain.ParkFit;

/// <summary>
/// Codes stables traduisibles qui expliquent un verdict de groupe.
/// </summary>
public enum GroupAttractionCompatibilityReasonCode
{
    EveryoneTogetherKnown,
    SplitRequiredKnown,
    PartialParticipation,
    NoMemberCompatible,
    ParticipationConfigurationUnknown,
    MemberCompatibilityUnknown,
    MemberCompatibilityNotApplicable,
}
