using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Commands;
using AmusementPark.Application.Features.Passport.Handlers;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Passport.Results;
using AmusementPark.Core.Domain.Visits;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Passport.Handlers;

public sealed class VisitParkAssessmentCommandHandlersTests
{
    private static readonly DateTime NowUtc =
        new DateTime(2026, 9, 3, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Upsert_ShouldPersistTheAssessmentWithTheVisitVersionFence()
    {
        Visit visit = CreateVisit();
        Mock<IUserVisitRepository> repository = new Mock<IUserVisitRepository>(MockBehavior.Strict);
        repository.Setup(value => value.GetOwnedAsync(
                VisitId.Parse("visit-1"),
                "user-1",
                CancellationToken.None))
            .ReturnsAsync(visit);
        repository.Setup(value => value.TryUpdateOwnedAsync(
                It.Is<Visit>(candidate =>
                    candidate.Version == 2
                    && candidate.ParkAssessment != null
                    && candidate.ParkAssessment.Value.DoubleValue == 4.5d
                    && candidate.ParkAssessment.PrivateComment == "Belle journée"),
                1,
                CancellationToken.None))
            .ReturnsAsync(true);
        UpsertVisitParkAssessmentCommandHandler handler = new UpsertVisitParkAssessmentCommandHandler(
            repository.Object,
            CreateClock());

        ApplicationResult<VisitResult> result = await handler.HandleAsync(
            new UpsertVisitParkAssessmentCommand(
                " user-1 ",
                "visit-1",
                4.5d,
                " Belle journée ",
                1));

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value?.Version);
        Assert.Equal(4.5d, result.Value?.ParkAssessment?.Value);
        Assert.Equal(1, result.Value?.ParkAssessment?.Revision);
        repository.VerifyAll();
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(5.5d)]
    [InlineData(4.2d)]
    public async Task Upsert_WhenRatingIsInvalid_ShouldFailBeforePersistence(double value)
    {
        Mock<IUserVisitRepository> repository = new Mock<IUserVisitRepository>(MockBehavior.Strict);
        UpsertVisitParkAssessmentCommandHandler handler = new UpsertVisitParkAssessmentCommandHandler(
            repository.Object,
            CreateClock());

        ApplicationResult<VisitResult> result = await handler.HandleAsync(
            new UpsertVisitParkAssessmentCommand("user-1", "visit-1", value, null, 1));

        Assert.False(result.IsSuccess);
        Assert.StartsWith("rating.invalid-", Assert.Single(result.Errors).Code);
        repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Upsert_WhenParentVersionChanged_ShouldReturnAConflict()
    {
        Visit visit = CreateVisit();
        Mock<IUserVisitRepository> repository = new Mock<IUserVisitRepository>(MockBehavior.Strict);
        repository.Setup(value => value.GetOwnedAsync(
                VisitId.Parse("visit-1"),
                "user-1",
                CancellationToken.None))
            .ReturnsAsync(visit);
        UpsertVisitParkAssessmentCommandHandler handler = new UpsertVisitParkAssessmentCommandHandler(
            repository.Object,
            CreateClock());

        ApplicationResult<VisitResult> result = await handler.HandleAsync(
            new UpsertVisitParkAssessmentCommand("user-1", "visit-1", 4d, null, 2));

        Assert.False(result.IsSuccess);
        Assert.Equal("visit-park-assessment.version-conflict", Assert.Single(result.Errors).Code);
        repository.VerifyAll();
    }

    [Fact]
    public async Task Delete_ShouldAtomicallyRemoveTheAssessment()
    {
        Visit visit = CreateVisit();
        visit.UpsertParkAssessment(
            AmusementPark.Core.Domain.Ratings.RatingValue.FromDouble(4d),
            null,
            NowUtc.AddMinutes(-1));
        Mock<IUserVisitRepository> repository = new Mock<IUserVisitRepository>(MockBehavior.Strict);
        repository.Setup(value => value.GetOwnedAsync(
                VisitId.Parse("visit-1"),
                "user-1",
                CancellationToken.None))
            .ReturnsAsync(visit);
        repository.Setup(value => value.TryUpdateOwnedAsync(
                It.Is<Visit>(candidate => candidate.Version == 3 && candidate.ParkAssessment == null),
                2,
                CancellationToken.None))
            .ReturnsAsync(true);
        DeleteVisitParkAssessmentCommandHandler handler = new DeleteVisitParkAssessmentCommandHandler(
            repository.Object,
            CreateClock());

        ApplicationResult<VisitResult> result = await handler.HandleAsync(
            new DeleteVisitParkAssessmentCommand("user-1", "visit-1", 2));

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value?.ParkAssessment);
        Assert.Equal(3, result.Value?.Version);
        repository.VerifyAll();
    }

    [Fact]
    public async Task Delete_WhenAssessmentIsAlreadyAbsent_ShouldConfirmTheParentVersion()
    {
        Visit visit = CreateVisit();
        Mock<IUserVisitRepository> repository = new Mock<IUserVisitRepository>(MockBehavior.Strict);
        repository.Setup(value => value.GetOwnedAsync(
                VisitId.Parse("visit-1"),
                "user-1",
                CancellationToken.None))
            .ReturnsAsync(visit);
        repository.Setup(value => value.TryConfirmOwnedVersionAsync(
                VisitId.Parse("visit-1"),
                "user-1",
                1,
                CancellationToken.None))
            .ReturnsAsync(true);
        DeleteVisitParkAssessmentCommandHandler handler = new DeleteVisitParkAssessmentCommandHandler(
            repository.Object,
            CreateClock());

        ApplicationResult<VisitResult> result = await handler.HandleAsync(
            new DeleteVisitParkAssessmentCommand("user-1", "visit-1", 1));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value?.Version);
        repository.VerifyAll();
    }

    private static Visit CreateVisit()
    {
        return Visit.Create(
            VisitId.Parse("visit-1"),
            "user-1",
            "park-1",
            VisitDate.ForDay(2026, 9, 3),
            "Europe/Paris",
            LocalServiceDayConvention.VisitStartLocalDate,
            null,
            null,
            NowUtc.AddHours(-1));
    }

    private static IPassportClock CreateClock()
    {
        Mock<IPassportClock> clock = new Mock<IPassportClock>(MockBehavior.Strict);
        clock.SetupGet(value => value.UtcNow).Returns(NowUtc);
        return clock.Object;
    }
}
