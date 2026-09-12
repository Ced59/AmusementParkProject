namespace AmusementPark.Application.Features.Sharing.Models;

public sealed record PassportProfileShareSourceRevisionSnapshot(
    ShareSourceRevision Passport,
    ShareSourceRevision Identity,
    ShareSourceRevision Ratings,
    ShareSourceRevision Catalog)
{
    public bool IsStable => this.Passport.IsStable
        && this.Identity.IsStable
        && this.Ratings.IsStable
        && this.Catalog.IsStable;

    public bool IsStableFor(bool includeRatings)
    {
        return this.Passport.IsStable
            && this.Identity.IsStable
            && (!includeRatings || this.Ratings.IsStable)
            && this.Catalog.IsStable;
    }

    public bool HasSameRevisionsAs(
        PassportProfileShareSourceRevisionSnapshot other,
        bool includeRatings)
    {
        ArgumentNullException.ThrowIfNull(other);
        return this.Passport.Revision == other.Passport.Revision
            && this.Identity.Revision == other.Identity.Revision
            && (!includeRatings || this.Ratings.Revision == other.Ratings.Revision)
            && this.Catalog.Revision == other.Catalog.Revision;
    }
}
