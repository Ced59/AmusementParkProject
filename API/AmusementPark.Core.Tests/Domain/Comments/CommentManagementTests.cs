using AmusementPark.Core.Domain.Comments;
using AmusementPark.Core.Domain.Users;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Comments;

public sealed class CommentManagementTests
{
    [Fact]
    public void CanBeManagedBy_WhenActorIsAdministrator_ShouldAllowAnotherAuthorsComment()
    {
        Comment comment = new Comment { AuthorUserId = "moderator-1" };
        User administrator = new User
        {
            Id = "admin-1",
            Roles = new List<Role> { Role.Admin },
        };

        bool result = comment.CanBeManagedBy(administrator);

        Assert.True(result);
    }

    [Fact]
    public void CanBeManagedBy_WhenActorOwnsTheComment_ShouldAllowIt()
    {
        Comment comment = new Comment { AuthorUserId = "moderator-1" };
        User moderator = new User
        {
            Id = "moderator-1",
            Roles = new List<Role> { Role.Moderator },
        };

        bool result = comment.CanBeManagedBy(moderator);

        Assert.True(result);
    }

    [Fact]
    public void CanBeManagedBy_WhenActorIsNotAdministratorOrOwner_ShouldRejectIt()
    {
        Comment comment = new Comment { AuthorUserId = "moderator-2" };
        User moderator = new User
        {
            Id = "moderator-1",
            Roles = new List<Role> { Role.Moderator },
        };

        bool result = comment.CanBeManagedBy(moderator);

        Assert.False(result);
    }

    [Theory]
    [InlineData(Role.Admin, true)]
    [InlineData(Role.Moderator, true)]
    [InlineData(Role.User, false)]
    public void CanManageOfficialStatus_ShouldRequireAStaffRole(Role role, bool expected)
    {
        User actor = new User
        {
            Id = "user-1",
            Roles = new List<Role> { role },
        };

        bool result = Comment.CanManageOfficialStatus(actor);

        Assert.Equal(expected, result);
    }
}
