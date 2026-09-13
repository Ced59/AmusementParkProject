using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Users.Commands;
using AmusementPark.Application.Features.Users.Contracts;
using AmusementPark.Application.Features.Users.Handlers;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Application.Features.Users.Results;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Ports;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Users;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Users;

internal sealed class ExternalUserHandlerMocks
{
    public Mock<IUserRepository> UserRepository { get; } =
        new Mock<IUserRepository>(MockBehavior.Strict);

    public Mock<IImageRepository> ImageRepository { get; } =
        new Mock<IImageRepository>(MockBehavior.Strict);

    public Mock<IExternalIdentityVerifier> ExternalIdentityVerifier { get; } =
        new Mock<IExternalIdentityVerifier>(MockBehavior.Strict);

    public Mock<IUserAvatarImporter> UserAvatarImporter { get; } =
        new Mock<IUserAvatarImporter>(MockBehavior.Strict);

    public Mock<ITokenService> TokenService { get; } =
        new Mock<ITokenService>(MockBehavior.Strict);

    public Mock<IRefreshTokenFactory> RefreshTokenFactory { get; } =
        new Mock<IRefreshTokenFactory>(MockBehavior.Strict);

    public Mock<IRefreshTokenRepository> RefreshTokenRepository { get; } =
        new Mock<IRefreshTokenRepository>(MockBehavior.Strict);

    public Mock<IUserAuthenticationSettings> AuthenticationSettings { get; } =
        new Mock<IUserAuthenticationSettings>(MockBehavior.Strict);

    public void VerifyAll()
    {
        UserRepository.VerifyAll();
        ImageRepository.VerifyAll();
        ExternalIdentityVerifier.VerifyAll();
        UserAvatarImporter.VerifyAll();
        TokenService.VerifyAll();
        RefreshTokenFactory.VerifyAll();
        RefreshTokenRepository.VerifyAll();
        AuthenticationSettings.VerifyAll();
    }
}
