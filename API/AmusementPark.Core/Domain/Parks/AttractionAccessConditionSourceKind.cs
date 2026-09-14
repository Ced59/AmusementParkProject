namespace AmusementPark.Core.Domain.Parks;

/// <summary>
/// Nature de la source qui atteste une condition d'accès.
/// </summary>
public enum AttractionAccessConditionSourceKind
{
    Unknown,
    Official,
    OperatorProvided,
    VerifiedSecondary,
    CommunityUnverified,
}
