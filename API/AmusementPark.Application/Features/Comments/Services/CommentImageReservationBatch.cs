namespace AmusementPark.Application.Features.Comments.Services;

public sealed record CommentImageReservationBatch(
    IReadOnlyCollection<string> ReservedImageIds,
    string ReservationToken,
    IReadOnlyCollection<string> PreparedCleanupImageIds,
    long PendingCommentRevision = 0);
