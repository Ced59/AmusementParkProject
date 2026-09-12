using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Models;

public sealed record PassportProfileShareSourceRevisionSnapshot(
    ShareSourceRevision Coordination,
    ShareSourceRevision Passport,
    ShareSourceRevision DisplayName,
    ShareSourceRevision Avatar,
    ShareSourceRevision Ratings,
    IReadOnlyDictionary<string, ShareSourceRevision> Catalog)
{
    public bool IsStable => this.Coordination.IsStable
        && this.Passport.IsStable
        && this.DisplayName.IsStable
        && this.Avatar.IsStable
        && this.Ratings.IsStable
        && this.Catalog.Values.All(static revision => revision.IsStable);

    public bool IsStableFor(ShareContentPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        return this.Coordination.IsStable
            && this.Passport.IsStable
            && (!policy.Includes(ShareContentField.PublicDisplayName)
                || this.DisplayName.IsStable)
            && (!policy.Includes(ShareContentField.Avatar) || this.Avatar.IsStable)
            && (!policy.Includes(ShareContentField.GlobalRatings) || this.Ratings.IsStable)
            && this.Catalog.Values.All(static revision => revision.IsStable);
    }

    public bool HasSameRevisionsAs(
        PassportProfileShareSourceRevisionSnapshot other,
        ShareContentPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(other);
        ArgumentNullException.ThrowIfNull(policy);
        return this.Coordination.Revision == other.Coordination.Revision
            && this.Passport.Revision == other.Passport.Revision
            && (!policy.Includes(ShareContentField.PublicDisplayName)
                || this.DisplayName.Revision == other.DisplayName.Revision)
            && (!policy.Includes(ShareContentField.Avatar)
                || this.Avatar.Revision == other.Avatar.Revision)
            && (!policy.Includes(ShareContentField.GlobalRatings)
                || this.Ratings.Revision == other.Ratings.Revision)
            && HasSameCatalogRevisions(this.Catalog, other.Catalog);
    }

    private static bool HasSameCatalogRevisions(
        IReadOnlyDictionary<string, ShareSourceRevision> before,
        IReadOnlyDictionary<string, ShareSourceRevision> after)
    {
        return before.Count == after.Count
            && before.All(pair => after.TryGetValue(
                    pair.Key,
                    out ShareSourceRevision? revision)
                && revision.Revision == pair.Value.Revision);
    }
}
