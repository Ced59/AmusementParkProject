using System.Text.Json;
using System.Text.Json.Serialization;
using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Common.Measurements;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkFounders.Ports;
using AmusementPark.Application.Features.ParkGraphUpserts.Contracts;
using AmusementPark.Application.Features.ParkGraphUpserts.Ports;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;
using AmusementPark.Application.Features.ParkGraphUpserts.Services;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkOperators.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Core.Domain.Parks;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.ParkGraphUpserts.Services;

public sealed class ParkGraphUpsertParkStatusTests
{
    [Theory]
    [InlineData("Planned", ParkStatus.Planned)]
    [InlineData("announced", ParkStatus.Planned)]
    [InlineData("projet annoncé", ParkStatus.Planned)]
    [InlineData("UnderConstruction", ParkStatus.UnderConstruction)]
    [InlineData("construction-started", ParkStatus.UnderConstruction)]
    [InlineData("en travaux", ParkStatus.UnderConstruction)]
    [InlineData("TemporarilyClosed", ParkStatus.TemporarilyClosed)]
    [InlineData("temporary closure", ParkStatus.TemporarilyClosed)]
    [InlineData("fermé temporairement", ParkStatus.TemporarilyClosed)]
    [InlineData("Cancelled", ParkStatus.Cancelled)]
    [InlineData("canceled", ParkStatus.Cancelled)]
    [InlineData("abandoned", ParkStatus.Cancelled)]
    [InlineData("annulé", ParkStatus.Cancelled)]
    public async Task ApplyAsync_ShouldNormalizeCanonicalAndToleratedStatusValues(string input, ParkStatus expected)
    {
        Park existingPark = new Park { Id = "park-1", Name = "Status Park", Status = ParkStatus.Operating };
        ProcessorContext context = CreateProcessorContext(existingPark);
        ParkGraphUpsertRequest request = CreateRequest($$"""
        {
          "park": { "status": "{{input}}" }
        }
        """, "park-1", false);

        ApplicationResult<ParkGraphUpsertResult> result = await context.Processor.ApplyAsync(request, "user-1", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, context.SavedPark?.Status);
    }

    [Fact]
    public async Task ApplyAsync_WhenCreatingPlannedPark_ShouldKeepItHiddenByDefaultWithoutStatusWarning()
    {
        ProcessorContext context = CreateProcessorContext(null);
        ParkGraphUpsertRequest request = CreateRequest("""
        {
          "identity": { "name": "Future Park", "countryCode": "RO" },
          "park": { "name": "Future Park", "countryCode": "RO", "status": "Planned" }
        }
        """, null, true);

        ApplicationResult<ParkGraphUpsertResult> result = await context.Processor.ApplyAsync(request, "user-1", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(context.SavedPark);
        Assert.Equal(ParkStatus.Planned, context.SavedPark.Status);
        Assert.False(context.SavedPark.IsVisible);
        Assert.DoesNotContain(result.Value!.Warnings, warning => warning.Contains("statut", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(result.Value.Warnings, warning => warning.Contains("status", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(ParkStatus.Planned)]
    [InlineData(ParkStatus.UnderConstruction)]
    [InlineData(ParkStatus.TemporarilyClosed)]
    [InlineData(ParkStatus.ClosedDefinitively)]
    [InlineData(ParkStatus.Cancelled)]
    public async Task PreviewAsync_WhenNonOperatingParkContainsOpeningHours_ShouldRejectSchedule(ParkStatus status)
    {
        Park existingPark = new Park { Id = "park-1", Name = "Lifecycle Park", Status = status };
        ProcessorContext context = CreateProcessorContext(existingPark);
        ParkGraphUpsertRequest request = CreateRequest("""
        {
          "openingHours": {
            "regularRules": [ {} ]
          }
        }
        """, "park-1", false);

        ApplicationResult<ParkGraphUpsertResult> result = await context.Processor.PreviewAsync(
            request,
            "user-1",
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Contains(result.Value!.Errors, error => error.Contains("Operating", StringComparison.Ordinal));
        ParkGraphUpsertChange change = Assert.Single(
            result.Value.Changes,
            item => item.EntityType == "ParkOpeningHours");
        Assert.Equal("Skipped", change.ChangeType);
    }

    [Theory]
    [InlineData(ParkStatus.Operating)]
    [InlineData(ParkStatus.ClosedDefinitively)]
    [InlineData(ParkStatus.Planned)]
    [InlineData(ParkStatus.UnderConstruction)]
    [InlineData(ParkStatus.TemporarilyClosed)]
    [InlineData(ParkStatus.Cancelled)]
    public async Task ExportThenReimport_ShouldPreserveCanonicalStatus(ParkStatus status)
    {
        Park exportedPark = new Park { Id = "source-park", Name = "Exported Park", CountryCode = "FR", Status = status };
        Dictionary<string, object?> exportedDocument = ParkGraphJsonExportDocumentFactory.BuildDocument(
            new ParkGraphJsonParkExportData { Park = exportedPark },
            new HashSet<ParkGraphExportSection> { ParkGraphExportSection.ParkBasics },
            new DateTime(2026, 8, 4, 0, 0, 0, DateTimeKind.Utc),
            "test");
        JsonSerializerOptions options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        string json = JsonSerializer.Serialize(exportedDocument, options);
        using JsonDocument document = JsonDocument.Parse(json);

        Assert.Equal(status.ToString(), document.RootElement.GetProperty("park").GetProperty("status").GetString());

        Park targetPark = new Park { Id = "target-park", Name = "Target Park", Status = ParkStatus.Operating };
        ProcessorContext context = CreateProcessorContext(targetPark);
        ParkGraphUpsertRequest request = new ParkGraphUpsertRequest
        {
            TargetParkId = "target-park",
            CreateIfMissing = false,
            ReplaceCollections = false,
            Document = document.RootElement.Clone(),
            RawJson = json,
        };

        ApplicationResult<ParkGraphUpsertResult> result = await context.Processor.ApplyAsync(request, "user-1", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(status, context.SavedPark?.Status);
    }

    [Theory]
    [InlineData(ParkStatus.Planned)]
    [InlineData(ParkStatus.UnderConstruction)]
    [InlineData(ParkStatus.TemporarilyClosed)]
    [InlineData(ParkStatus.ClosedDefinitively)]
    [InlineData(ParkStatus.Cancelled)]
    public void Export_WhenParkIsNotOperating_ShouldNotExposeStoredOpeningHours(ParkStatus status)
    {
        Park park = new Park { Id = "park-1", Name = "Lifecycle Park", Status = status };
        Dictionary<string, object?> exportedDocument = ParkGraphJsonExportDocumentFactory.BuildDocument(
            new ParkGraphJsonParkExportData
            {
                Park = park,
                OpeningHours = new ParkOpeningHoursSchedule { ParkId = "park-1", TimeZoneId = "UTC" },
            },
            new HashSet<ParkGraphExportSection>
            {
                ParkGraphExportSection.ParkBasics,
                ParkGraphExportSection.OpeningHours,
            },
            new DateTime(2026, 8, 4, 0, 0, 0, DateTimeKind.Utc),
            "test");

        Assert.Null(exportedDocument["openingHours"]);
    }

    private static ParkGraphUpsertRequest CreateRequest(string json, string? targetParkId, bool createIfMissing)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        return new ParkGraphUpsertRequest
        {
            TargetParkId = targetParkId,
            CreateIfMissing = createIfMissing,
            ReplaceCollections = false,
            Document = document.RootElement.Clone(),
            RawJson = json,
        };
    }

    private static ProcessorContext CreateProcessorContext(Park? existingPark)
    {
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Loose);
        Park? savedPark = null;
        if (existingPark is not null)
        {
            parkRepository
                .Setup(repository => repository.GetByIdAsync(existingPark.Id, true, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingPark);
        }

        parkRepository
            .Setup(repository => repository.UpdateAsync(It.IsAny<string>(), It.IsAny<Park>(), It.IsAny<CancellationToken>()))
            .Callback<string, Park, CancellationToken>((_, park, _) => savedPark = park)
            .ReturnsAsync((string _, Park park, CancellationToken _) => park);
        parkRepository
            .Setup(repository => repository.CreateAsync(It.IsAny<Park>(), It.IsAny<CancellationToken>()))
            .Callback<Park, CancellationToken>((park, _) =>
            {
                park.Id = "created-park";
                savedPark = park;
            })
            .ReturnsAsync((Park park, CancellationToken _) => park);

        Mock<IParkGraphUpsertHistoryRepository> historyRepository = new Mock<IParkGraphUpsertHistoryRepository>(MockBehavior.Loose);
        historyRepository
            .Setup(repository => repository.SaveAsync(It.IsAny<ParkGraphUpsertHistoryEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        Mock<ISearchProjectionWriter> searchProjectionWriter = new Mock<ISearchProjectionWriter>(MockBehavior.Loose);
        searchProjectionWriter
            .Setup(writer => writer.UpsertAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        Mock<IPublicSeoUpdateNotifier> publicSeoUpdateNotifier = new Mock<IPublicSeoUpdateNotifier>(MockBehavior.Loose);
        publicSeoUpdateNotifier
            .Setup(notifier => notifier.NotifyAsync(It.IsAny<PublicSeoUpdate>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        ParkGraphUpsertProcessor processor = new ParkGraphUpsertProcessor(
            parkRepository.Object,
            Mock.Of<IParkZoneRepository>(),
            Mock.Of<IParkItemRepository>(),
            Mock.Of<IParkFounderRepository>(),
            Mock.Of<IParkOperatorRepository>(),
            Mock.Of<IAttractionManufacturerRepository>(),
            Mock.Of<IImageRepository>(),
            Mock.Of<IRemoteImageImporter>(),
            searchProjectionWriter.Object,
            historyRepository.Object,
            publicSeoUpdateNotifier.Object,
            MeasurementConversionService.Instance);

        return new ProcessorContext(processor, () => savedPark);
    }

    private sealed class ProcessorContext
    {
        private readonly Func<Park?> savedParkAccessor;

        public ProcessorContext(ParkGraphUpsertProcessor processor, Func<Park?> savedParkAccessor)
        {
            this.Processor = processor;
            this.savedParkAccessor = savedParkAccessor;
        }

        public ParkGraphUpsertProcessor Processor { get; }

        public Park? SavedPark => this.savedParkAccessor();
    }
}
