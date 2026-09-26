using System.Text.Json;
using AmusementPark.Core.Domain.History;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.History;
using AmusementPark.Infrastructure.Persistence.Mongo.Migrations;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Migrations;

public sealed class HistoricalLegacyFactConverterTests
{
    [Fact]
    public void BuildHistoricalDate_ShouldKeepTheOriginalPrecision()
    {
        HistoryEventDocument historyEvent = new HistoryEventDocument
        {
            Year = 1998,
            Month = 5,
            Day = null,
            DatePrecision = HistoryDatePrecision.Month,
        };

        HistoricalDate result = HistoricalLegacyFactConverter.BuildHistoricalDate(historyEvent);

        Assert.Equal(1998, result.Year);
        Assert.Equal(5, result.Month);
        Assert.Null(result.Day);
        Assert.Equal(HistoryDatePrecision.Month, result.Precision);
    }

    [Fact]
    public void BuildStructuredValue_ShouldPreserveBothNames()
    {
        HistoryEventDocument historyEvent = new HistoryEventDocument
        {
            PreviousName = "Ancien nom",
            NewName = "Nouveau nom",
        };
        LegacyHistoryEventTypeMapping mapping = new LegacyHistoryEventTypeMapping(
            HistoricalFactType.Renaming,
            null,
            HistoricalAttributeKind.Name,
            AttributeBoundaryMeaning.Unspecified);

        string? result = HistoricalLegacyFactConverter.BuildStructuredValue(historyEvent, mapping);
        Dictionary<string, string>? values = JsonSerializer.Deserialize<Dictionary<string, string>>(result!);

        Assert.Equal("Ancien nom", values!["previous"]);
        Assert.Equal("Nouveau nom", values["next"]);
    }

    [Fact]
    public void BuildStructuredValue_WhenAttributeHasNoKnownValue_ShouldRemainUnknown()
    {
        HistoryEventDocument historyEvent = new HistoryEventDocument();
        LegacyHistoryEventTypeMapping mapping = new LegacyHistoryEventTypeMapping(
            HistoricalFactType.ManufacturerChange,
            null,
            HistoricalAttributeKind.Manufacturer,
            AttributeBoundaryMeaning.Unspecified);

        string? result = HistoricalLegacyFactConverter.BuildStructuredValue(historyEvent, mapping);

        Assert.Null(result);
    }

    [Fact]
    public void BuildStructuredValue_WhenOwnershipEventOnlyHasOperatorIds_ShouldRemainUnknown()
    {
        HistoryEventDocument historyEvent = new HistoryEventDocument
        {
            PreviousOperatorId = "operator-before",
            NewOperatorId = "operator-after",
        };
        LegacyHistoryEventTypeMapping mapping = new LegacyHistoryEventTypeMapping(
            HistoricalFactType.OwnerChange,
            null,
            HistoricalAttributeKind.Owner,
            AttributeBoundaryMeaning.Unspecified);

        string? result = HistoricalLegacyFactConverter.BuildStructuredValue(historyEvent, mapping);

        Assert.Null(result);
    }

    [Fact]
    public void BuildLegacyWarnings_ShouldCoverEverySupportedLanguage()
    {
        IReadOnlyCollection<HistoricalLocalizedText> warnings =
            HistoricalLegacyFactConverter.BuildLegacyWarnings();

        Assert.Equal(
            HistoricalLocalizationPolicy.SupportedLanguageCodes.OrderBy(static code => code),
            warnings.Select(static warning => warning.LanguageCode).OrderBy(static code => code));
    }

    [Fact]
    public void ResolveHistoricalLabel_ShouldNeverExposeTheTechnicalIdentifierAsFallback()
    {
        HistoryEventDocument historyEvent = new HistoryEventDocument
        {
            OwnerId = "technical-id-1",
        };

        string result = HistoricalLegacyFactConverter.ResolveHistoricalLabel(
            historyEvent,
            "Cible historique introuvable");

        Assert.Equal("Cible historique introuvable", result);
        Assert.DoesNotContain(historyEvent.OwnerId, result, StringComparison.Ordinal);
    }
}
