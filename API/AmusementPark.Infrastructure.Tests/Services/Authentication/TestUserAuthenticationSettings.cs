using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Application.Ports;
using AmusementPark.Core.Domain.Users;
using AmusementPark.Infrastructure.Services.Authentication;
using AmusementPark.Infrastructure.Services.Email;
using Moq;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Services.Authentication;

internal sealed class TestUserAuthenticationSettings : IUserAuthenticationSettings
{
    public int EmailConfirmationTokenExpirationHours => 24;

    public int PasswordResetTokenExpirationMinutes => 60;

    public int TokenRefreshLimitMinutes => 45;

    public string FrontendBaseUrl => "https://amusement-parks.fun";
}
