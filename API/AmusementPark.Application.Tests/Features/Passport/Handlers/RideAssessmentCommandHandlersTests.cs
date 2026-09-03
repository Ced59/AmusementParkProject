using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Commands;
using AmusementPark.Application.Features.Passport.Handlers;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Passport.Results;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Core.Domain.Visits;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Passport.Handlers;

public sealed class RideAssessmentCommandHandlersTests
{
    private static readonly DateTime NowUtc =
        new DateTime(2026, 9, 3, 14, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Upsert_ShouldPersistTheAssessmentWithTheOccurrenceVersionFence()
    {
        RideOccurrence occurrence = CreateOccurrence();
        Mock<IRideOccurrenceRepository> repository = new Mock<IRideOccurrenceRepository>(MockBehavior.Strict);
        repository.Setup(value => value.GetOwnedByIdAsync(
                RideOccurrenceId.Parse("occurrence-1"),
                "user-1",
                CancellationToken.None))
            .ReturnsAsync(occurrence);
        repository.Setup(value => value.TryUpdateOwnedAsync(
                It.Is<RideOccurrence>(candidate =>
                    candidate.Version == 2
                    && candidate.Assessment != null
                    && candidate.Assessment.Value.DoubleValue == 4.5d
                    && candidate.Assessment.PrivateComment == "Tour mémorable"),
                1,
                CancellationToken.None))
            .ReturnsAsync(true);
        UpsertRideAssessmentCommandHandler handler = new UpsertRideAssessmentCommandHandler(
            repository.Object,
            CreateClock());

        ApplicationResult<RideOccurrenceResult> result = await handler.HandleAsync(
            new UpsertRideAssessmentCommand(
                " user-1 ",
                "occurrence-1",
                4.5d,
                " Tour mémorable ",
                1));

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value?.Version);
        Assert.Equal(4.5d, result.Value?.Assessment?.Value);
        Assert.Equal(1, result.Value?.Assessment?.Revision);
        repository.VerifyAll();
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(5.5d)]
    [InlineData(4.2d)]
    public async Task Upsert_WhenRatingIsInvalid_ShouldFailBeforePersistence(double value)
    {
        Mock<IRideOccurrenceRepository> repository = new Mock<IRideOccurrenceRepository>(MockBehavior.Strict);
        UpsertRideAssessmentCommandHandler handler = new UpsertRideAssessmentCommandHandler(
            repository.Object,
            CreateClock());

        ApplicationResult<RideOccurrenceResult> result = await handler.HandleAsync(
            new UpsertRideAssessmentCommand("user-1", "occurrence-1", value, null, 1));

        Assert.False(result.IsSuccess);
        Assert.StartsWith("rating.invalid-", Assert.Single(result.Errors).Code);
        repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Upsert_WhenOwnerDoesNotOwnOccurrence_ShouldReturnNotFound()
    {
        Mock<IRideOccurrenceRepository> repository = new Mock<IRideOccurrenceRepository>(MockBehavior.Strict);
        repository.Setup(value => value.GetOwnedByIdAsync(
                RideOccurrenceId.Parse("occurrence-1"),
                "other-user",
                CancellationToken.None))
            .ReturnsAsync((RideOccurrence?)null);
        UpsertRideAssessmentCommandHandler handler = new UpsertRideAssessmentCommandHandler(
            repository.Object,
            CreateClock());

        ApplicationResult<RideOccurrenceResult> result = await handler.HandleAsync(
            new UpsertRideAssessmentCommand("other-user", "occurrence-1", 4d, null, 1));

        Assert.False(result.IsSuccess);
        Assert.Equal("ride-occurrence.not-found", Assert.Single(result.Errors).Code);
        repository.VerifyAll();
    }

    [Fact]
    public async Task Upsert_WhenParentVersionChanged_ShouldReturnAConflict()
    {
        RideOccurrence occurrence = CreateOccurrence();
        Mock<IRideOccurrenceRepository> repository = new Mock<IRideOccurrenceRepository>(MockBehavior.Strict);
        repository.Setup(value => value.GetOwnedByIdAsync(
                RideOccurrenceId.Parse("occurrence-1"),
                "user-1",
                CancellationToken.None))
            .ReturnsAsync(occurrence);
        UpsertRideAssessmentCommandHandler handler = new UpsertRideAssessmentCommandHandler(
            repository.Object,
            CreateClock());

        ApplicationResult<RideOccurrenceResult> result = await handler.HandleAsync(
            new UpsertRideAssessmentCommand("user-1", "occurrence-1", 4d, null, 2));

        Assert.False(result.IsSuccess);
        Assert.Equal("ride-assessment.version-conflict", Assert.Single(result.Errors).Code);
        repository.VerifyAll();
    }

    [Fact]
    public async Task Delete_ShouldAtomicallyRemoveTheAssessment()
    {
        RideOccurrence occurrence = CreateOccurrence();
        occurrence.UpsertAssessment(RatingValue.FromDouble(4d), null, NowUtc.AddMinutes(-1));
        Mock<IRideOccurrenceRepository> repository = new Mock<IRideOccurrenceRepository>(MockBehavior.Strict);
        repository.Setup(value => value.GetOwnedByIdAsync(
                RideOccurrenceId.Parse("occurrence-1"),
                "user-1",
                CancellationToken.None))
            .ReturnsAsync(occurrence);
        repository.Setup(value => value.TryUpdateOwnedAsync(
                It.Is<RideOccurrence>(candidate => candidate.Version == 3 && candidate.Assessment == null),
                2,
                CancellationToken.None))
            .ReturnsAsync(true);
        DeleteRideAssessmentCommandHandler handler = new DeleteRideAssessmentCommandHandler(
            repository.Object,
            CreateClock());

        ApplicationResult<RideOccurrenceResult> result = await handler.HandleAsync(
            new DeleteRideAssessmentCommand("user-1", "occurrence-1", 2));

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value?.Assessment);
        Assert.Equal(3, result.Value?.Version);
        repository.VerifyAll();
    }

    [Fact]
    public async Task Delete_WhenAssessmentIsAlreadyAbsent_ShouldConfirmTheParentVersion()
    {
        RideOccurrence occurrence = CreateOccurrence();
        Mock<IRideOccurrenceRepository> repository = new Mock<IRideOccurrenceRepository>(MockBehavior.Strict);
        repository.Setup(value => value.GetOwnedByIdAsync(
                RideOccurrenceId.Parse("occurrence-1"),
                "user-1",
                CancellationToken.None))
            .ReturnsAsync(occurrence);
        repository.Setup(value => value.TryConfirmOwnedVersionAsync(
                occurrence.Id,
                occurrence.VisitId,
                "user-1",
                1,
                CancellationToken.None))
            .ReturnsAsync(true);
        DeleteRideAssessmentCommandHandler handler = new DeleteRideAssessmentCommandHandler(
            repository.Object,
            CreateClock());

        ApplicationResult<RideOccurrenceResult> result = await handler.HandleAsync(
            new DeleteRideAssessmentCommand("user-1", "occurrence-1", 1));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value?.Version);
        repository.VerifyAll();
    }

    private static RideOccurrence CreateOccurrence()
    {
        Visit visit = Visit.Create(
            VisitId.Parse("visit-1"),
            "user-1",
            "park-1",
            VisitDate.ForDay(2026, 9, 3),
            "Europe/Paris",
            LocalServiceDayConvention.VisitStartLocalDate,
            null,
            null,
            NowUtc.AddHours(-1));
        return RideOccurrence.Create(
            RideOccurrenceId.Parse("occurrence-1"),
            visit,
            "item-1",
            1024,
            new OccurrenceMoment(null, false),
            RideOccurrenceStatus.Completed,
            RideLogSource.Manual,
            HistoricalConsistency.Verified,
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
