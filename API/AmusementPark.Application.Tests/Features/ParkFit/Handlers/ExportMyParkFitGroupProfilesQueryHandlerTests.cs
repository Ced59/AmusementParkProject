using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkFit.Handlers;
using AmusementPark.Application.Features.ParkFit.Ports;
using AmusementPark.Application.Features.ParkFit.Queries;
using AmusementPark.Application.Features.ParkFit.Results;
using AmusementPark.Application.Tests.Features.Sharing.Handlers;
using AmusementPark.Core.Domain.ParkFit;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.ParkFit.Handlers;

public sealed class ExportMyParkFitGroupProfilesQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldExportHumanFactsWithoutTechnicalIdentity()
    {
        DateTime nowUtc = new DateTime(2026, 9, 14, 14, 0, 0, DateTimeKind.Utc);
        ParkFitGroupProfile profile = ParkFitGroupProfile.Create(
            ParkFitGroupProfileId.Parse("internal-profile-id"),
            "internal-owner-id",
            "Enfant",
            120,
            8,
            true,
            40,
            nowUtc);
        Mock<IParkFitGroupProfileRepository> repository =
            new Mock<IParkFitGroupProfileRepository>(MockBehavior.Strict);
        repository.Setup(value => value.ListOwnedAsync("internal-owner-id", CancellationToken.None))
            .ReturnsAsync(new[] { profile });
        ExportMyParkFitGroupProfilesQueryHandler handler = new(
            repository.Object,
            new SharePublicationFixedTimeProvider(nowUtc.AddMinutes(1)));

        ApplicationResult<ParkFitGroupProfileExportResult> result = await handler.HandleAsync(
            new ExportMyParkFitGroupProfilesQuery("internal-owner-id"),
            CancellationToken.None);

        ParkFitGroupProfileExportItemResult item = Assert.Single(result.Value!.Profiles);
        Assert.Equal("Enfant", item.Alias);
        Assert.DoesNotContain(
            item.GetType().GetProperties(),
            static property => property.Name.Contains("Id", StringComparison.Ordinal)
                || property.Name.Contains("Version", StringComparison.Ordinal));
        repository.VerifyAll();
    }
}
