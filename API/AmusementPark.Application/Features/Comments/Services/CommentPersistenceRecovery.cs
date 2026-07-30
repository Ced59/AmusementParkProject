using AmusementPark.Application.Features.Comments.Ports;
using AmusementPark.Core.Domain.Comments;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.Comments.Services;

internal static class CommentPersistenceRecovery
{
    public static Task<Comment?> TryResolveCreateAsync(
        ICommentRepository commentRepository,
        Comment expected)
    {
        ArgumentNullException.ThrowIfNull(commentRepository);
        ArgumentNullException.ThrowIfNull(expected);

        return TryResolveExactAsync(commentRepository, expected);
    }

    public static async Task<Comment?> UpdateAsync(
        ICommentRepository commentRepository,
        Comment expected,
        long expectedRevision,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(commentRepository);
        ArgumentNullException.ThrowIfNull(expected);

        try
        {
            return await commentRepository.UpdateAsync(
                expected,
                expectedRevision,
                cancellationToken);
        }
        catch
        {
            Comment? committed = await TryResolveExactAsync(
                commentRepository,
                expected);
            if (committed is null)
            {
                throw;
            }

            return committed;
        }
    }

    private static async Task<Comment?> TryResolveExactAsync(
        ICommentRepository commentRepository,
        Comment expected)
    {
        try
        {
            Comment? candidate = await commentRepository.GetByIdAsync(
                expected.Id,
                CancellationToken.None);
            return candidate is not null && MatchesExpectedState(expected, candidate)
                ? candidate
                : null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static bool MatchesExpectedState(Comment expected, Comment candidate)
    {
        return string.Equals(candidate.Id, expected.Id, StringComparison.Ordinal)
            && candidate.Revision == expected.Revision
            && candidate.TargetType == expected.TargetType
            && string.Equals(candidate.TargetId, expected.TargetId, StringComparison.Ordinal)
            && string.Equals(candidate.ParkId, expected.ParkId, StringComparison.Ordinal)
            && string.Equals(candidate.AuthorUserId, expected.AuthorUserId, StringComparison.Ordinal)
            && string.Equals(
                candidate.AuthorDisplayName,
                expected.AuthorDisplayName,
                StringComparison.Ordinal)
            && string.Equals(
                candidate.AuthorAvatarUrl,
                expected.AuthorAvatarUrl,
                StringComparison.Ordinal)
            && candidate.AuthorRole == expected.AuthorRole
            && candidate.IsOfficial == expected.IsOfficial
            && candidate.ModerationStatus == expected.ModerationStatus
            && candidate.ImageIds.SequenceEqual(expected.ImageIds, StringComparer.Ordinal)
            && BodiesMatch(expected.Bodies, candidate.Bodies);
    }

    private static bool BodiesMatch(
        IReadOnlyList<LocalizedText> expected,
        IReadOnlyList<LocalizedText> candidate)
    {
        return expected.Count == candidate.Count
            && expected
                .Zip(
                    candidate,
                    static (expectedBody, candidateBody) =>
                        string.Equals(
                            expectedBody.LanguageCode,
                            candidateBody.LanguageCode,
                            StringComparison.Ordinal)
                        && string.Equals(
                            expectedBody.Value,
                            candidateBody.Value,
                            StringComparison.Ordinal))
                .All(static matches => matches);
    }
}
