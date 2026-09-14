using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkFit.Commands;
using AmusementPark.Application.Features.ParkFit.Handlers;
using AmusementPark.Application.Features.ParkFit.Ports;
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
        parks.Setup(repository => repository.GetByIdAsync(
                park.Id,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);
        reports.Setup(repository => repository.CreateAsync(
                It.Is<ParkFitSourceReport>(report =>
                    report.ParkName == park.Name
                    && report.Status == ParkFitSourceReportStatus.Pending),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ParkFitSourceReportWriteOutcome.Success);
        SubmitParkFitSourceReportCommandHandler handler =
            new SubmitParkFitSourceReportCommandHandler(parks.Object, reports.Object);

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
    }
}
