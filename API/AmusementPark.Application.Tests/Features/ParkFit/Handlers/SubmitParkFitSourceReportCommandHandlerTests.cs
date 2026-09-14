using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkFit.Commands;
using AmusementPark.Application.Features.ParkFit.Handlers;
using AmusementPark.Application.Features.ParkFit.Ports;
using AmusementPark.Application.Features.ParkFit.Services;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkOpeningHours.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Core.Domain.ParkFit;
using AmusementPark.Core.Domain.Parks;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.ParkFit.Handlers;

public sealed class SubmitParkFitSourceReportCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldResolvePublicParkNameAndPersistReport()
    {
        Park park = new Park
        {
            Id = "park-1",
            Name = "Parc témoin",
            IsVisible = true,
            Status = ParkStatus.Operating,
            AdminReviewStatus = AdminReviewStatus.Validated,
        };
        Mock<IParkRepository> parks = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkFitSourceReportRepository> reports =
            new Mock<IParkFitSourceReportRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> items = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<IParkOpeningHoursRepository> openingHours =
            new Mock<IParkOpeningHoursRepository>(MockBehavior.Strict);
        parks.Setup(repository => repository.GetByIdAsync(
                park.Id,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);
        items.Setup(repository => repository.GetVisibleOpenAttractionsByParkIdsAsync(
                It.Is<IReadOnlyCollection<string>>(parkIds => parkIds.SequenceEqual(new[] { park.Id })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new ParkItem
                {
                    ParkId = park.Id,
                    AttractionDetails = new AttractionDetails
                    {
                        AccessConditions = new List<AttractionAccessCondition>
                        {
                            new AttractionAccessCondition
                            {
                                SourceUrl = "https://example.org/access",
                            },
                        },
                    },
                },
            });
        reports.Setup(repository => repository.CreateAsync(
                It.Is<ParkFitSourceReport>(report =>
                    report.ParkName == park.Name
                    && report.Status == ParkFitSourceReportStatus.Pending),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ParkFitSourceReportWriteOutcome.Success);
        SubmitParkFitSourceReportCommandHandler handler =
            new SubmitParkFitSourceReportCommandHandler(
                parks.Object,
                reports.Object,
                new ParkFitEvidenceSourceResolver(items.Object, openingHours.Object));

        ApplicationResult result = await handler.HandleAsync(new SubmitParkFitSourceReportCommand(
            park.Id,
            ParkFitEvidenceKind.AccessCondition,
            "https://example.org/access",
            null,
            ParkFitSourceReportReason.Outdated,
            "La taille a changé"));

        Assert.True(result.IsSuccess);
        parks.VerifyAll();
        reports.VerifyAll();
        items.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_ShouldRejectSourceThatDoesNotBelongToCurrentParkEvidence()
    {
        Park park = new Park
        {
            Id = "park-1",
            Name = "Parc témoin",
            IsVisible = true,
            Status = ParkStatus.Operating,
            AdminReviewStatus = AdminReviewStatus.Validated,
        };
        Mock<IParkRepository> parks = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkFitSourceReportRepository> reports =
            new Mock<IParkFitSourceReportRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> items = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<IParkOpeningHoursRepository> openingHours =
            new Mock<IParkOpeningHoursRepository>(MockBehavior.Strict);
        parks.Setup(repository => repository.GetByIdAsync(
                park.Id,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);
        items.Setup(repository => repository.GetVisibleOpenAttractionsByParkIdsAsync(
                It.Is<IReadOnlyCollection<string>>(parkIds => parkIds.SequenceEqual(new[] { park.Id })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new ParkItem
                {
                    ParkId = park.Id,
                    AttractionDetails = new AttractionDetails
                    {
                        AccessConditions = new List<AttractionAccessCondition>
                        {
                            new AttractionAccessCondition
                            {
                                SourceUrl = "https://example.org/trusted-access",
                            },
                        },
                    },
                },
            });
        SubmitParkFitSourceReportCommandHandler handler =
            new SubmitParkFitSourceReportCommandHandler(
                parks.Object,
                reports.Object,
                new ParkFitEvidenceSourceResolver(items.Object, openingHours.Object));

        ApplicationResult result = await handler.HandleAsync(new SubmitParkFitSourceReportCommand(
            park.Id,
            ParkFitEvidenceKind.AccessCondition,
            "https://attacker.example/spoofed-proof",
            null,
            ParkFitSourceReportReason.Incorrect,
            "Source incorrecte"));

        Assert.False(result.IsSuccess);
        Assert.Equal("park-fit.report.invalid", Assert.Single(result.Errors).Code);
        parks.VerifyAll();
        items.VerifyAll();
        reports.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_ShouldResolveCurrentOpeningCalendarSource()
    {
        Park park = new Park
        {
            Id = "park-1",
            Name = "Parc témoin",
            IsVisible = true,
            Status = ParkStatus.Operating,
            AdminReviewStatus = AdminReviewStatus.Validated,
        };
        Mock<IParkRepository> parks = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkFitSourceReportRepository> reports =
            new Mock<IParkFitSourceReportRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> items = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<IParkOpeningHoursRepository> openingHours =
            new Mock<IParkOpeningHoursRepository>(MockBehavior.Strict);
        parks.Setup(repository => repository.GetByIdAsync(
                park.Id,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);
        openingHours.Setup(repository => repository.GetByParkIdAsync(
                park.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ParkOpeningHoursSchedule
            {
                ParkId = park.Id,
                SourceUrl = "https://example.org/opening-calendar",
            });
        reports.Setup(repository => repository.CreateAsync(
                It.Is<ParkFitSourceReport>(report =>
                    report.EvidenceKind == ParkFitEvidenceKind.OpeningCalendar
                    && report.SourceUrl == "https://example.org/opening-calendar"
                    && report.SourceReference == null),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ParkFitSourceReportWriteOutcome.Success);
        SubmitParkFitSourceReportCommandHandler handler =
            new SubmitParkFitSourceReportCommandHandler(
                parks.Object,
                reports.Object,
                new ParkFitEvidenceSourceResolver(items.Object, openingHours.Object));

        ApplicationResult result = await handler.HandleAsync(new SubmitParkFitSourceReportCommand(
            park.Id,
            ParkFitEvidenceKind.OpeningCalendar,
            "https://example.org/opening-calendar",
            null,
            ParkFitSourceReportReason.Unavailable,
            null));

        Assert.True(result.IsSuccess);
        parks.VerifyAll();
        openingHours.VerifyAll();
        reports.VerifyAll();
    }
}
