using AmusementPark.Application.Features.Users.Contracts;
using AmusementPark.Core.Domain.Users;
using AmusementPark.WebAPI.Contracts.Users;
using AmusementPark.WebAPI.Mappers;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Mappers;

public sealed class UsersHttpMappersTests
{
    [Fact]
    public void ToApplication_ShouldMapThePublicDisplayName()
    {
        UserUpdateDto request = new UserUpdateDto
        {
            PublicDisplayName = "CoasterFan",
        };

        UserProfileUpdate result = request.ToApplication();

        Assert.Equal("CoasterFan", result.PublicDisplayName);
    }

    [Fact]
    public void UserResponses_ShouldExposeThePublicDisplayNameToItsOwner()
    {
        User user = new User
        {
            Id = "user-1",
            PublicDisplayName = "CoasterFan",
            PublicAccountNumber = 1,
            Roles = new List<Role> { Role.Admin },
        };

        Assert.Equal("CoasterFan", user.ToGettedDto().PublicDisplayName);
        Assert.Equal("CoasterFan", user.ToUpdatedDto().PublicDisplayName);
        Assert.Equal("CoasterFan", user.ToListDto().PublicDisplayName);
        Assert.Equal("CoasterFan", user.ToCreatedDto().PublicDisplayName);
        Assert.Equal("Admin01", user.ToGettedDto().PublicIdentifier);
        Assert.Equal("Admin01", user.ToUpdatedDto().PublicIdentifier);
        Assert.Equal("Admin01", user.ToListDto().PublicIdentifier);
        Assert.Equal("Admin01", user.ToCreatedDto().PublicIdentifier);
    }
}
