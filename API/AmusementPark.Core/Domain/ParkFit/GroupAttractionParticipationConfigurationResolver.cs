namespace AmusementPark.Core.Domain.ParkFit;

/// <summary>
/// N'affirme une organisation commune que lorsqu'un groupe ne contient qu'un membre.
/// </summary>
public static class GroupAttractionParticipationConfigurationResolver
{
    public static GroupAttractionParticipationConfiguration Resolve(
        IReadOnlyCollection<GroupAttractionMemberCompatibility> members)
    {
        ArgumentNullException.ThrowIfNull(members);

        if (members.Count == 0 || members.Any(static member => member is null))
        {
            throw new ArgumentException(
                "A group must contain at least one non-null member.",
                nameof(members));
        }

        if (members.Count != 1)
        {
            return GroupAttractionParticipationConfiguration.Unknown;
        }

        GroupAttractionMemberCompatibility member = members.Single();
        return member.Compatibility.State is AttractionCompatibilityState.CompatibleAlone
                or AttractionCompatibilityState.CompatibleWithCompanion
            ? GroupAttractionParticipationConfiguration.EveryoneTogether
            : GroupAttractionParticipationConfiguration.Unknown;
    }
}
