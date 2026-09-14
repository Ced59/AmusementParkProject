using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkFit.Models;
using AmusementPark.Application.Features.ParkFit.Ports;
using AmusementPark.Application.Features.ParkFit.Results;
using AmusementPark.Application.Features.ParkFit.Services;
using AmusementPark.Application.Tests.Features.Sharing.Handlers;
using AmusementPark.Core.Domain.ParkFit;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.ParkFit.Services;

public sealed class ParkFitGroupProfileLifecycleServiceTests
{
    private static readonly DateTime NowUtc =
        new DateTime(2026, 9, 14, 14, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task CreateAsync_ShouldPersistPrivateProfileAndReturnOpaqueId()
    {
        Mock<IParkFitGroupProfileRepository> repository =
            new Mock<IParkFitGroupProfileRepository>(MockBehavior.Strict);
        repository.Setup(value => value.CountOwnedAsync("user-1", CancellationToken.None))
            .ReturnsAsync(0);
        repository.Setup(value => value.CreateAsync(
                It.Is<ParkFitGroupProfile>(profile =>
                    profile.OwnerUserId == "user-1"
                    && profile.Alias == "Alex"
                    && profile.Version == 1),
                CancellationToken.None))
            .ReturnsAsync(ParkFitGroupProfileWriteOutcome.Success);
        ParkFitGroupProfileLifecycleService service = CreateService(repository.Object);

        ApplicationResult<ParkFitGroupProfileResult> result = await service.CreateAsync(
            "user-1",
            Input("Alex"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Value!.ProfileId);
        Assert.Equal(NowUtc, result.Value.CreatedAtUtc);
        repository.VerifyAll();
    }

    [Fact]
    public async Task CreateAsync_AtOwnerLimit_ShouldRejectWithoutWriting()
    {
        Mock<IParkFitGroupProfileRepository> repository =
            new Mock<IParkFitGroupProfileRepository>(MockBehavior.Strict);
        repository.Setup(value => value.CountOwnedAsync("user-1", CancellationToken.None))
            .ReturnsAsync(ParkFitGroupProfile.MaximumProfilesPerOwner);
        ParkFitGroupProfileLifecycleService service = CreateService(repository.Object);

        ApplicationResult<ParkFitGroupProfileResult> result = await service.CreateAsync(
            "user-1",
            Input("Alex"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error =>
            error.Code == "park-fit.group-profile.limit-reached");
        repository.VerifyAll();
    }

    [Fact]
    public async Task UpdateAsync_ForAnotherOwner_ShouldReturnNotFoundWithoutWriting()
    {
        ParkFitGroupProfile profile = CreateProfile("owner-1");
        Mock<IParkFitGroupProfileRepository> repository =
            new Mock<IParkFitGroupProfileRepository>(MockBehavior.Strict);
        repository.Setup(value => value.GetOwnedAsync(
                profile.Id,
                "outsider",
                CancellationToken.None))
            .ReturnsAsync((ParkFitGroupProfile?)null);
        ParkFitGroupProfileLifecycleService service = CreateService(repository.Object);

        ApplicationResult<ParkFitGroupProfileResult> result = await service.UpdateAsync(
            "outsider",
            profile.Id.Value,
            profile.Version,
            Input("Intrus"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error =>
            error.Code == "park-fit.group-profile.not-found");
        repository.VerifyAll();
    }

    [Fact]
    public async Task UpdateAsync_WithStaleVersion_ShouldReturnConflictWithoutWriting()
    {
        ParkFitGroupProfile profile = CreateProfile("user-1");
        Mock<IParkFitGroupProfileRepository> repository =
            new Mock<IParkFitGroupProfileRepository>(MockBehavior.Strict);
        repository.Setup(value => value.GetOwnedAsync(
                profile.Id,
                "user-1",
                CancellationToken.None))
            .ReturnsAsync(profile);
        ParkFitGroupProfileLifecycleService service = CreateService(repository.Object);

        ApplicationResult<ParkFitGroupProfileResult> result = await service.UpdateAsync(
            "user-1",
            profile.Id.Value,
            0,
            Input("Alex"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error =>
            error.Code == "park-fit.group-profile.changed-concurrently");
        repository.VerifyAll();
    }

    [Fact]
    public async Task DeleteAsync_ShouldFenceOwnerAndVersion()
    {
        ParkFitGroupProfile profile = CreateProfile("user-1");
        Mock<IParkFitGroupProfileRepository> repository =
            new Mock<IParkFitGroupProfileRepository>(MockBehavior.Strict);
        repository.Setup(value => value.GetOwnedAsync(
                profile.Id,
                "user-1",
                CancellationToken.None))
            .ReturnsAsync(profile);
        repository.Setup(value => value.DeleteOwnedAsync(
                profile.Id,
                "user-1",
                1,
                CancellationToken.None))
            .ReturnsAsync(ParkFitGroupProfileWriteOutcome.Success);
        ParkFitGroupProfileLifecycleService service = CreateService(repository.Object);

        ApplicationResult result = await service.DeleteAsync(
            "user-1",
            profile.Id.Value,
            1,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        repository.VerifyAll();
    }

    private static ParkFitGroupProfileLifecycleService CreateService(
        IParkFitGroupProfileRepository repository)
    {
        return new ParkFitGroupProfileLifecycleService(
            repository,
            new SharePublicationFixedTimeProvider(NowUtc));
    }

    private static ParkFitGroupProfileInput Input(string alias)
    {
        return new ParkFitGroupProfileInput(alias, 170, 30, true, 40);
    }

    private static ParkFitGroupProfile CreateProfile(string ownerUserId)
    {
        return ParkFitGroupProfile.Create(
            ParkFitGroupProfileId.Parse("profile-1"),
            ownerUserId,
            "Alex",
            170,
            30,
            true,
            40,
            NowUtc);
    }
}
