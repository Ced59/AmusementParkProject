namespace AmusementPark.Application.Features.ParkOpeningHours.Models;

public sealed record ParkOpeningHoursFactualChangeCursor(
    DateTime SourceUpdatedAtUtc,
    string ParkId,
    DateTime EntryRecordedAtUtc,
    string EntryId);
