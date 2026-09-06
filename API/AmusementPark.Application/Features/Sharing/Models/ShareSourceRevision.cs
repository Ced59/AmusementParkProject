namespace AmusementPark.Application.Features.Sharing.Models;

public sealed record ShareSourceRevision(
    long Revision,
    int PendingMutationCount,
    DateTime UpdatedAtUtc)
{
    public bool IsStable => this.PendingMutationCount == 0;
}
