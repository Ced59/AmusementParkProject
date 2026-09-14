namespace AmusementPark.Application.Features.Sharing.Results;

public sealed record ShareAccountDeletionResult(
    int RevokedPublicationCount,
    long ExpiredInvitationCount,
    int RevokedComparisonCount,
    long PurgedDocumentCount);
