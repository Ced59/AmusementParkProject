using System.Text.Json;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkGraphUpserts.Contracts;
using AmusementPark.Application.Features.ParkGraphUpserts.Handlers;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;
using AmusementPark.Application.Features.ParkGraphUpserts.Services;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Parks;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.ParkGraphUpserts.Services;

public sealed class ParkGraphOwnerTransitionTests
{
    [Theory]
    [InlineData("OwnershipChange")]
    [InlineData("Acquisition")]
    [InlineData("Sale")]
    public async Task OwnerTransition_ShouldPreviewApplyAndRoundTripThroughBothExportPaths(string eventType)
    {
        HistoryUpsertTestContext context = new HistoryUpsertTestContext(CreateExistingEvent());
        string json = BuildDocument(eventType, "\"previousOwnerName\": \" Founding family \", \"newOwnerName\": \" Purchasing group \"");

        ApplicationResult<ParkGraphUpsertResult> preview = await context.PreviewAsync(json);

        Assert.True(preview.IsSuccess);
        Assert.True(preview.Value!.CanApply);
        Assert.Null(context.ReadPersistedEvent().NewOwnerName);
        Assert.Contains(preview.Value.Changes.SelectMany(static change => change.Fields), static field => field.Field == "newOwnerName");

        ApplicationResult<ParkGraphUpsertResult> apply = await context.ApplyAsync(json);

        Assert.True(apply.IsSuccess);
        Assert.True(apply.Value!.IsApplied);
        HistoryEvent persisted = context.ReadPersistedEvent();
        Assert.Equal("Founding family", persisted.PreviousOwnerName);
        Assert.Equal("Purchasing group", persisted.NewOwnerName);
        Assert.Equal("operator-1", persisted.NewOperatorId);
        ParkGraphExportHistoryEvent mapped = ExportParkGraphJsonQueryHandlerHistoryMappingExtensions.MapHistoryEvent(persisted);
        Assert.Equal(persisted.PreviousOwnerName, mapped.PreviousOwnerName);
        Assert.Equal(persisted.NewOwnerName, mapped.NewOwnerName);

        ParkGraphJsonParkExportData data = new ParkGraphJsonParkExportData
        {
            Park = new Park { Id = "park-1", Name = "Park", CountryCode = "FR" },
            HistoryEvents = new[] { persisted },
        };
        Dictionary<string, object?> exported = ParkGraphJsonExportDocumentFactory.BuildDocument(
            data,
            new HashSet<ParkGraphExportSection> { ParkGraphExportSection.History },
            DateTime.UtcNow,
            "test");
        string exportedJson = JsonSerializer.Serialize(exported, ExportParkGraphJsonQueryHandler.ExportJsonOptions);
        HistoryUpsertTestContext roundTripContext = new HistoryUpsertTestContext(CreateExistingEvent());

        ApplicationResult<ParkGraphUpsertResult> roundTrip = await roundTripContext.ApplyAsync(exportedJson);

        Assert.True(roundTrip.IsSuccess);
        Assert.True(roundTrip.Value!.IsApplied);
        Assert.Equal(persisted.PreviousOwnerName, roundTripContext.ReadPersistedEvent().PreviousOwnerName);
        Assert.Equal(persisted.NewOwnerName, roundTripContext.ReadPersistedEvent().NewOwnerName);
    }

    [Theory]
    [InlineData(false, "\"newOwnerName\": null")]
    [InlineData(true, "\"newOwnerName\": \" \"")]
    [InlineData(false, "\"previousOwnerName\": \"Founding family\"")]
    [InlineData(true, "\"newOperatorId\": \"operator-2\"")]
    public async Task MissingNewOwner_ShouldRejectPreviewAndLeaveHistoryUnchangedOnApply(bool isVisible, string patch)
    {
        HistoryUpsertTestContext context = new HistoryUpsertTestContext(CreateExistingEvent());
        string json = BuildDocument("Acquisition", $"\"isVisible\": {JsonSerializer.Serialize(isVisible)}, {patch}");

        ApplicationResult<ParkGraphUpsertResult> preview = await context.PreviewAsync(json);
        ApplicationResult<ParkGraphUpsertResult> apply = await context.ApplyAsync(json);

        Assert.False(preview.Value!.CanApply);
        Assert.NotEmpty(preview.Value.Errors);
        Assert.False(apply.Value!.CanApply);
        Assert.NotEmpty(apply.Value.Errors);
        Assert.Null(context.ReadPersistedEvent().NewOwnerName);
        Assert.Equal("Opening", context.ReadPersistedEvent().EventType);
        context.HistoryEventRepository.Verify(repository => repository.UpdateAsync(
            It.IsAny<string>(), It.IsAny<HistoryEvent>(), It.IsAny<DateTime>(),
            It.IsAny<Guid?>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        context.HistoricalNarrativeCanonicalizer.Verify(repository => repository.CanonicalizeAsync(
            It.IsAny<HistoryEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OmittedOwnerPatch_ShouldPreserveExistingOwner()
    {
        HistoryEvent existing = CreateExistingEvent();
        existing.EventType = "Acquisition";
        existing.NewOwnerName = "Purchasing group";
        HistoryUpsertTestContext context = new HistoryUpsertTestContext(existing);

        ApplicationResult<ParkGraphUpsertResult> result = await context.ApplyAsync(BuildDocument("Acquisition", "\"isMajor\": true"));

        Assert.True(result.IsSuccess);
        Assert.Equal("Purchasing group", context.ReadPersistedEvent().NewOwnerName);
    }

    private static string BuildDocument(string eventType, string patch)
    {
        return $$"""
        { "mode": "merge", "historyEvents": [{
          "owner": "park", "ownerId": "park-1", "key": "history-1979",
          "eventType": "{{eventType}}", "date": "1979", {{patch}}
        }] }
        """;
    }

    private static HistoryEvent CreateExistingEvent()
    {
        return new HistoryEvent
        {
            Id = "history-1", Key = "history-1979", EntityType = HistoryEntityType.Park,
            OwnerId = "park-1", ParkId = "park-1", Year = 1979,
            EventType = "Opening", IsVisible = true, NewOperatorId = "operator-1",
            Sources = new List<HistorySourceReference> { new HistorySourceReference { Url = "https://example.com/acquisition" } },
        };
    }
}
