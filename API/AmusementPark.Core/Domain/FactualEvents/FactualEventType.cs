namespace AmusementPark.Core.Domain.FactualEvents;

/// <summary>
/// Stable vocabulary of factual changes that a member may explicitly follow.
/// Numeric ranges are intentionally separated by target family for durable storage.
/// </summary>
public enum FactualEventType
{
    OpeningCalendarPublished = 1,
    OpeningCalendarChanged = 2,
    SeasonOpeningConfirmed = 3,
    SeasonClosingConfirmed = 4,
    ParkTemporaryClosureConfirmed = 5,
    ParkPermanentClosureConfirmed = 6,
    ParkReopeningConfirmed = 7,
    ParkNameChanged = 8,
    OperatorChanged = 9,
    TicketPricePublishedOrChanged = 10,
    MajorDataCompletionImproved = 11,

    AttractionAnnouncedOfficially = 101,
    OpeningDateConfirmed = 102,
    OpeningDateChanged = 103,
    OpenedConfirmed = 104,
    TemporarilyClosedConfirmed = 105,
    ReopenedConfirmed = 106,
    PermanentClosureConfirmed = 107,
    Renamed = 108,
    MajorRestrictionChanged = 109,
    LocationOrCategoryCorrected = 110,

    HistoryPublished = 201,
    MajorHistoryUpdate = 202,
    VerifiedSourceAdded = 203,
    CorrectionAfterUserReport = 204,
}
