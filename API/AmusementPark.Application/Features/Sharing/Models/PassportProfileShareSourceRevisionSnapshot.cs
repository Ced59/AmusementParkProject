namespace AmusementPark.Application.Features.Sharing.Models;

public sealed record PassportProfileShareSourceRevisionSnapshot(
    ShareSourceRevision Passport,
    ShareSourceRevision Identity,
    ShareSourceRevision Catalog)
{
    public bool IsStable => this.Passport.IsStable
        && this.Identity.IsStable
        && this.Catalog.IsStable;

    public bool HasSameRevisionsAs(PassportProfileShareSourceRevisionSnapshot other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return this.Passport.Revision == other.Passport.Revision
            && this.Identity.Revision == other.Identity.Revision
            && this.Catalog.Revision == other.Catalog.Revision;
    }
}
