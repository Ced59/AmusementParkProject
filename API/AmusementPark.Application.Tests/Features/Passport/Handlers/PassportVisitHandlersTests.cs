using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Passport.Commands;
using AmusementPark.Application.Features.Passport.Handlers;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Passport.Queries;
using AmusementPark.Application.Features.Passport.Results;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Visits;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Passport.Handlers;

public sealed class PassportVisitHandlersTests
{
    private static readonly DateTime NowUtc =
        new DateTime(2026, 9, 3, 10, 30, 0, DateTimeKind.Utc);

    [Fact]
    public async Task CreateVisit_ShouldValidateTheParkAndPersistAnOwnerBoundDraft()
    {
        Mock<IUserVisitRepository> visits = new Mock<IUserVisitRepository>(MockBehavior.Strict);
        Mock<IParkRepository> parks = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IPassportTimeZoneValidator> timeZones =
            new Mock<IPassportTimeZoneValidator>(MockBehavior.Strict);
        parks.Setup(repository => repository.GetByIdAsync("park-1", true, CancellationToken.None))
            .ReturnsAsync(new Park { Id = "park-1", Name = "Park" });
        timeZones.Setup(validator => validator.IsValid("Europe/Paris"))
            .Returns(true);
        Visit? captured = null;
        visits.Setup(repository => repository.CreateIdempotentAsync(
                It.IsAny<Visit>(),
                "request-1",
                CancellationToken.None))
            .Callback((Visit visit, string _, CancellationToken _) => captured = visit)
            .ReturnsAsync(() => new IdempotentVisitCreationResult(
                IdempotentVisitCreationStatus.Created,
                captured));
        CreateVisitCommandHandler handler = CreateHandler(visits.Object, parks.Object, timeZones.Object);

        ApplicationResult<CreateVisitResult> result = await handler.HandleAsync(
            CreateCommand(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(captured);
        Assert.Equal("owner-1", captured.UserId);
        Assert.Equal("park-1", captured.ParkId);
        Assert.Equal(VisitStatus.Draft, captured.Status);
        Assert.Equal(VisitPrivacy.Private, captured.Privacy);
        Assert.Equal(NowUtc, captured.CreatedAtUtc);
        Assert.Equal("Journée d'été", result.Value?.Visit.Title);
        Assert.False(result.Value?.WasReplayed);
        visits.VerifyAll();
        parks.VerifyAll();
        timeZones.VerifyAll();
    }

    [Fact]
    public async Task CreateVisit_WhenTheSameOperationIsReplayed_ShouldExposeTheOriginalVisit()
    {
        Visit existing = CreateVisit("existing-visit", "owner-1");
        Mock<IUserVisitRepository> visits = new Mock<IUserVisitRepository>(MockBehavior.Strict);
        Mock<IParkRepository> parks = CreateParkRepository();
        Mock<IPassportTimeZoneValidator> timeZones = CreateTimeZoneValidator();
        visits.Setup(repository => repository.CreateIdempotentAsync(
                It.IsAny<Visit>(),
                "request-1",
                CancellationToken.None))
            .ReturnsAsync(new IdempotentVisitCreationResult(
                IdempotentVisitCreationStatus.Replayed,
                existing));
        CreateVisitCommandHandler handler = CreateHandler(visits.Object, parks.Object, timeZones.Object);

        ApplicationResult<CreateVisitResult> result = await handler.HandleAsync(CreateCommand());

        Assert.True(result.IsSuccess);
        Assert.True(result.Value?.WasReplayed);
        Assert.Equal("existing-visit", result.Value?.Visit.Id);
    }

    [Fact]
    public async Task CreateVisit_WhenTheOperationPayloadDiffers_ShouldReturnAConflict()
    {
        Mock<IUserVisitRepository> visits = new Mock<IUserVisitRepository>(MockBehavior.Strict);
        Mock<IParkRepository> parks = CreateParkRepository();
        Mock<IPassportTimeZoneValidator> timeZones = CreateTimeZoneValidator();
        visits.Setup(repository => repository.CreateIdempotentAsync(
                It.IsAny<Visit>(),
                "request-1",
                CancellationToken.None))
            .ReturnsAsync(new IdempotentVisitCreationResult(
                IdempotentVisitCreationStatus.Conflict,
                null));
        CreateVisitCommandHandler handler = CreateHandler(visits.Object, parks.Object, timeZones.Object);

        ApplicationResult<CreateVisitResult> result = await handler.HandleAsync(CreateCommand());

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorType.Conflict, Assert.Single(result.Errors).Type);
        Assert.Equal("visit.idempotency-key-conflict", Assert.Single(result.Errors).Code);
    }

    [Fact]
    public async Task CreateVisit_WhenDateIsInvalid_ShouldFailBeforeAnyRepositoryCall()
    {
        Mock<IUserVisitRepository> visits = new Mock<IUserVisitRepository>(MockBehavior.Strict);
        Mock<IParkRepository> parks = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IPassportTimeZoneValidator> timeZones =
            new Mock<IPassportTimeZoneValidator>(MockBehavior.Strict);
        CreateVisitCommandHandler handler = CreateHandler(visits.Object, parks.Object, timeZones.Object);
        CreateVisitCommand command = CreateCommand() with
        {
            Year = 2026,
            Month = 2,
            Day = 30,
        };

        ApplicationResult<CreateVisitResult> result = await handler.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal("visit-date.invalid-day", Assert.Single(result.Errors).Code);
        visits.VerifyNoOtherCalls();
        parks.VerifyNoOtherCalls();
        timeZones.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateVisit_WhenTimeZoneIsInvalid_ShouldFailBeforeLookingUpThePark()
    {
        Mock<IUserVisitRepository> visits = new Mock<IUserVisitRepository>(MockBehavior.Strict);
        Mock<IParkRepository> parks = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IPassportTimeZoneValidator> timeZones =
            new Mock<IPassportTimeZoneValidator>(MockBehavior.Strict);
        timeZones.Setup(validator => validator.IsValid("Mars/Olympus"))
            .Returns(false);
        CreateVisitCommandHandler handler = CreateHandler(visits.Object, parks.Object, timeZones.Object);

        ApplicationResult<CreateVisitResult> result = await handler.HandleAsync(
            CreateCommand() with { TimeZoneId = "Mars/Olympus" });

        Assert.False(result.IsSuccess);
        Assert.Equal("visit.time-zone-id-invalid", Assert.Single(result.Errors).Code);
        visits.VerifyNoOtherCalls();
        parks.VerifyNoOtherCalls();
        timeZones.VerifyAll();
    }

    [Fact]
    public async Task CreateVisit_WhenParkDoesNotExist_ShouldReturnNotFound()
    {
        Mock<IUserVisitRepository> visits = new Mock<IUserVisitRepository>(MockBehavior.Strict);
        Mock<IParkRepository> parks = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IPassportTimeZoneValidator> timeZones = CreateTimeZoneValidator();
        parks.Setup(repository => repository.GetByIdAsync("park-1", true, CancellationToken.None))
            .ReturnsAsync((Park?)null);
        CreateVisitCommandHandler handler = CreateHandler(visits.Object, parks.Object, timeZones.Object);

        ApplicationResult<CreateVisitResult> result = await handler.HandleAsync(CreateCommand());

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorType.NotFound, Assert.Single(result.Errors).Type);
        Assert.Equal("visit.park-not-found", Assert.Single(result.Errors).Code);
        visits.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetVisit_ShouldAlwaysQueryByVisitAndOwner()
    {
        Visit visit = CreateVisit("visit-1", "owner-1");
        Mock<IUserVisitRepository> visits = new Mock<IUserVisitRepository>(MockBehavior.Strict);
        visits.Setup(repository => repository.GetOwnedAsync(
                VisitId.Parse("visit-1"),
                "owner-1",
                CancellationToken.None))
            .ReturnsAsync(visit);
        GetVisitQueryHandler handler = new GetVisitQueryHandler(visits.Object);

        ApplicationResult<VisitResult> result = await handler.HandleAsync(
            new GetVisitQuery(" owner-1 ", "visit-1"));

        Assert.True(result.IsSuccess);
        Assert.Equal("visit-1", result.Value?.Id);
        visits.VerifyAll();
    }

    [Fact]
    public async Task GetVisit_WhenOwnerCannotSeeIt_ShouldReturnTheSameNotFoundAsAnAbsentVisit()
    {
        Mock<IUserVisitRepository> visits = new Mock<IUserVisitRepository>(MockBehavior.Strict);
        visits.Setup(repository => repository.GetOwnedAsync(
                VisitId.Parse("visit-1"),
                "other-owner",
                CancellationToken.None))
            .ReturnsAsync((Visit?)null);
        GetVisitQueryHandler handler = new GetVisitQueryHandler(visits.Object);

        ApplicationResult<VisitResult> result = await handler.HandleAsync(
            new GetVisitQuery("other-owner", "visit-1"));

        Assert.False(result.IsSuccess);
        Assert.Equal("visit.not-found", Assert.Single(result.Errors).Code);
    }

    [Fact]
    public async Task ListVisits_ShouldForwardOwnerFiltersAndCursorAndMapThePage()
    {
        Visit visit = CreateVisit("visit-1", "owner-1");
        UserVisitListCursor cursor = new UserVisitListCursor(
            visit.Date,
            visit.UpdatedAtUtc,
            visit.Id);
        Mock<IUserVisitRepository> visits = new Mock<IUserVisitRepository>(MockBehavior.Strict);
        visits.Setup(repository => repository.ListOwnedAsync(
                It.Is<UserVisitListCriteria>(criteria =>
                    criteria.UserId == "owner-1"
                    && criteria.Limit == 10
                    && criteria.ParkId == "park-1"
                    && criteria.Year == 2026
                    && criteria.Status == VisitStatus.Draft
                    && criteria.After == cursor),
                CancellationToken.None))
            .ReturnsAsync(new UserVisitPage(new[] { visit }, cursor));
        ListUserVisitsQueryHandler handler = new ListUserVisitsQueryHandler(visits.Object);

        ApplicationResult<VisitPageResult> result = await handler.HandleAsync(
            new ListUserVisitsQuery(
                " owner-1 ",
                10,
                " park-1 ",
                2026,
                VisitStatus.Draft,
                cursor));

        Assert.True(result.IsSuccess);
        Assert.Equal("visit-1", Assert.Single(result.Value!.Items).Id);
        Assert.Equal(cursor, result.Value.NextCursor);
        visits.VerifyAll();
    }

    [Fact]
    public async Task ListVisits_WhenLimitIsUnbounded_ShouldFailBeforePersistence()
    {
        Mock<IUserVisitRepository> visits = new Mock<IUserVisitRepository>(MockBehavior.Strict);
        ListUserVisitsQueryHandler handler = new ListUserVisitsQueryHandler(visits.Object);

        ApplicationResult<VisitPageResult> result = await handler.HandleAsync(
            new ListUserVisitsQuery("owner-1", UserVisitListCriteria.MaximumLimit + 1));

        Assert.False(result.IsSuccess);
        Assert.Equal("visit.list-limit-invalid", Assert.Single(result.Errors).Code);
        visits.VerifyNoOtherCalls();
    }

    private static CreateVisitCommandHandler CreateHandler(
        IUserVisitRepository visits,
        IParkRepository parks,
        IPassportTimeZoneValidator timeZones)
    {
        Mock<IPassportClock> clock = new Mock<IPassportClock>(MockBehavior.Strict);
        clock.SetupGet(value => value.UtcNow).Returns(NowUtc);
        return new CreateVisitCommandHandler(visits, parks, clock.Object, timeZones);
    }

    private static Mock<IParkRepository> CreateParkRepository()
    {
        Mock<IParkRepository> parks = new Mock<IParkRepository>(MockBehavior.Strict);
        parks.Setup(repository => repository.GetByIdAsync("park-1", true, CancellationToken.None))
            .ReturnsAsync(new Park { Id = "park-1", Name = "Park" });
        return parks;
    }

    private static Mock<IPassportTimeZoneValidator> CreateTimeZoneValidator()
    {
        Mock<IPassportTimeZoneValidator> timeZones =
            new Mock<IPassportTimeZoneValidator>(MockBehavior.Strict);
        timeZones.Setup(validator => validator.IsValid("Europe/Paris"))
            .Returns(true);
        return timeZones;
    }

    private static CreateVisitCommand CreateCommand()
    {
        return new CreateVisitCommand(
            " owner-1 ",
            " request-1 ",
            " park-1 ",
            2026,
            8,
            31,
            VisitDatePrecision.Day,
            false,
            " Europe/Paris ",
            LocalServiceDayConvention.VisitStartLocalDate,
            " Journée d'été ",
            " Note privée ");
    }

    private static Visit CreateVisit(string visitId, string userId)
    {
        return Visit.Create(
            VisitId.Parse(visitId),
            userId,
            "park-1",
            VisitDate.ForDay(2026, 8, 31),
            "Europe/Paris",
            LocalServiceDayConvention.VisitStartLocalDate,
            "Journée d'été",
            "Note privée",
            NowUtc);
    }
}
