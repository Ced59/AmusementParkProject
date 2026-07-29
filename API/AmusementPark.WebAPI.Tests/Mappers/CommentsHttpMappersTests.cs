using AmusementPark.Application.Features.Comments.Results;
using AmusementPark.Core.Domain.Comments;
using AmusementPark.Core.Domain.Users;
using AmusementPark.Core.Localization;
using AmusementPark.WebAPI.Contracts.Comments;
using AmusementPark.WebAPI.Mappers;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Mappers;

public sealed class CommentsHttpMappersTests
{
    [Theory]
    [InlineData(null, false, false)]
    [InlineData("another-user", false, false)]
    [InlineData("author-1", false, true)]
    [InlineData("another-user", true, true)]
    public void ToHttp_ShouldExposeOnlyTheCurrentActorsManagementRights(
        string? actorUserId,
        bool canManageAll,
        bool expected)
    {
        CommentResult result = new CommentResult(
            "comment-1",
            CommentTargetType.Park,
            "park-1",
            "author-1",
            "Alice",
            Role.Moderator,
            new[] { new LocalizedText("fr", "<p>Avis</p>") },
            false,
            new DateTime(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc));

        CommentDto dto = result.ToHttp(actorUserId, canManageAll);

        Assert.Equal(expected, dto.CanUpdate);
        Assert.Equal(expected, dto.CanDelete);
    }
}
