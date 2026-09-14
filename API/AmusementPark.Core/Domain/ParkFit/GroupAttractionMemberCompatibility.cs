namespace AmusementPark.Core.Domain.ParkFit;

/// <summary>
/// Associe un identifiant opaque de la requête à son verdict individuel.
/// </summary>
public sealed class GroupAttractionMemberCompatibility
{
    public GroupAttractionMemberCompatibility(
        string memberKey,
        AttractionCompatibility compatibility)
    {
        if (string.IsNullOrWhiteSpace(memberKey))
        {
            throw new ArgumentException(
                "The member key is required.",
                nameof(memberKey));
        }

        ArgumentNullException.ThrowIfNull(compatibility);

        this.MemberKey = memberKey.Trim();
        this.Compatibility = compatibility;
    }

    public string MemberKey { get; }

    public AttractionCompatibility Compatibility { get; }
}
