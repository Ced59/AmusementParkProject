namespace AmusementPark.Application.Features.LiveData.Models;

public sealed class LivePublicTargetCoverage
{
    public LivePublicTargetCoverage(string internalTargetId, string externalTargetId)
    {
        if (string.IsNullOrWhiteSpace(internalTargetId))
        {
            throw new ArgumentException("The internal live target identifier is required.", nameof(internalTargetId));
        }

        if (string.IsNullOrWhiteSpace(externalTargetId))
        {
            throw new ArgumentException("The external live target identifier is required.", nameof(externalTargetId));
        }

        this.InternalTargetId = internalTargetId.Trim();
        this.ExternalTargetId = externalTargetId.Trim();
    }

    public string InternalTargetId { get; }

    public string ExternalTargetId { get; }
}
