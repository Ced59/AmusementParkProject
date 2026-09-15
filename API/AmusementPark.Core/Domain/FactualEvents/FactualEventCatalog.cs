using System.Collections.Frozen;

namespace AmusementPark.Core.Domain.FactualEvents;

public static class FactualEventCatalog
{
    public const int CurrentSchemaVersion = 1;

    private static readonly FrozenDictionary<FactualEventType, FactualEventDefinition> Definitions =
        BuildDefinitions();

    public static IReadOnlyDictionary<FactualEventType, FactualEventDefinition> All => Definitions;

    public static FactualEventDefinition Get(FactualEventType type)
    {
        return Definitions.TryGetValue(type, out FactualEventDefinition? definition)
            ? definition
            : throw new FactualEventValidationException(
                FactualEventErrorCodes.InvalidType,
                "The factual event type is not registered in the catalogue.");
    }

    public static bool TryGet(
        FactualEventType type,
        out FactualEventDefinition? definition)
    {
        return Definitions.TryGetValue(type, out definition);
    }

    private static FrozenDictionary<FactualEventType, FactualEventDefinition> BuildDefinitions()
    {
        FactualTargetType[] park = { FactualTargetType.Park };
        FactualTargetType[] parkItem = { FactualTargetType.ParkItem };
        FactualTargetType[] editorial =
        {
            FactualTargetType.Park,
            FactualTargetType.ParkItem,
        };
        FactualEventDefinition[] definitions =
        {
            Define(FactualEventType.OpeningCalendarPublished, "opening-calendar-published", park),
            Define(FactualEventType.OpeningCalendarChanged, "opening-calendar-changed", park),
            Define(FactualEventType.SeasonOpeningConfirmed, "season-opening-confirmed", park),
            Define(FactualEventType.SeasonClosingConfirmed, "season-closing-confirmed", park),
            Define(FactualEventType.ParkTemporaryClosureConfirmed, "park-temporary-closure-confirmed", park),
            Define(FactualEventType.ParkPermanentClosureConfirmed, "park-permanent-closure-confirmed", park),
            Define(FactualEventType.ParkReopeningConfirmed, "park-reopening-confirmed", park),
            Define(FactualEventType.ParkNameChanged, "park-name-changed", park),
            Define(FactualEventType.OperatorChanged, "operator-changed", park),
            Define(FactualEventType.TicketPricePublishedOrChanged, "ticket-price-published-or-changed", park),
            Define(FactualEventType.MajorDataCompletionImproved, "major-data-completion-improved", park),
            Define(FactualEventType.AttractionAnnouncedOfficially, "attraction-announced-officially", parkItem),
            Define(FactualEventType.OpeningDateConfirmed, "opening-date-confirmed", parkItem),
            Define(FactualEventType.OpeningDateChanged, "opening-date-changed", parkItem),
            Define(FactualEventType.OpenedConfirmed, "opened-confirmed", parkItem),
            Define(FactualEventType.TemporarilyClosedConfirmed, "temporarily-closed-confirmed", parkItem),
            Define(FactualEventType.ReopenedConfirmed, "reopened-confirmed", parkItem),
            Define(FactualEventType.PermanentClosureConfirmed, "permanent-closure-confirmed", parkItem),
            Define(FactualEventType.Renamed, "renamed", parkItem),
            Define(FactualEventType.MajorRestrictionChanged, "major-restriction-changed", parkItem),
            Define(FactualEventType.LocationOrCategoryCorrected, "location-or-category-corrected", parkItem),
            Define(FactualEventType.HistoryPublished, "history-published", editorial),
            Define(FactualEventType.MajorHistoryUpdate, "major-history-update", editorial),
            Define(FactualEventType.VerifiedSourceAdded, "verified-source-added", editorial),
            Define(FactualEventType.CorrectionAfterUserReport, "correction-after-user-report", editorial),
        };

        if (definitions.Length != Enum.GetValues<FactualEventType>().Length
            || definitions.Select(static definition => definition.Code)
                .Distinct(StringComparer.Ordinal).Count() != definitions.Length)
        {
            throw new InvalidOperationException(
                "Every factual event type must have exactly one unique catalogue definition.");
        }

        return definitions.ToFrozenDictionary(static definition => definition.Type);
    }

    private static FactualEventDefinition Define(
        FactualEventType type,
        string code,
        IReadOnlyCollection<FactualTargetType> targetTypes)
    {
        return new FactualEventDefinition(type, code, CurrentSchemaVersion, targetTypes);
    }
}
