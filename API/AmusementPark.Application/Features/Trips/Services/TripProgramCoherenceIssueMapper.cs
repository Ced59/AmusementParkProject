using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Trips.Services;

public sealed class TripProgramCoherenceIssueMapper
{
    public IReadOnlyCollection<TripProgramCoherenceIssueResult> Map(
        IReadOnlyCollection<TripProgramCoherenceIssue> issues,
        IReadOnlyDictionary<string, Park> parksById,
        IReadOnlyDictionary<string, ParkItem> itemsById,
        IReadOnlyDictionary<string, ParkOpeningHoursSchedule> schedulesByParkId)
    {
        ArgumentNullException.ThrowIfNull(issues);
        ArgumentNullException.ThrowIfNull(parksById);
        ArgumentNullException.ThrowIfNull(itemsById);
        ArgumentNullException.ThrowIfNull(schedulesByParkId);
        return issues.Select(issue => MapIssue(issue, parksById, itemsById, schedulesByParkId)).ToArray();
    }

    private static TripProgramCoherenceIssueResult MapIssue(
        TripProgramCoherenceIssue issue,
        IReadOnlyDictionary<string, Park> parksById,
        IReadOnlyDictionary<string, ParkItem> itemsById,
        IReadOnlyDictionary<string, ParkOpeningHoursSchedule> schedulesByParkId)
    {
        Park? park = issue.ParkId is not null ? parksById.GetValueOrDefault(issue.ParkId) : null;
        ParkItem? item = issue.ParkItemId is not null ? itemsById.GetValueOrDefault(issue.ParkItemId) : null;
        ParkOpeningHoursSchedule? schedule = issue.ParkId is not null
            ? schedulesByParkId.GetValueOrDefault(issue.ParkId)
            : null;
        bool openingEvidence = issue.Code is TripProgramCoherenceCode.OpeningHoursUnknown
            or TripProgramCoherenceCode.OpeningHoursClosed
            or TripProgramCoherenceCode.OpeningHoursStale
            or TripProgramCoherenceCode.OpeningHoursVerifiedAfterPlanning;
        bool attractionEvidence = issue.Code is TripProgramCoherenceCode.AttractionClosed
            or TripProgramCoherenceCode.AttractionUnavailable;
        bool itemAvailable = TripProgramAttractionFactBuilder.IsAvailable(item, parksById);
        return new TripProgramCoherenceIssueResult(
            issue.Code,
            issue.Severity,
            issue.LocalDate,
            issue.ParkId,
            park?.IsPubliclyDiscoverable() == true ? park.Name?.Trim() : null,
            issue.ParkItemId,
            itemAvailable ? NormalizeOptional(item!.Name) : null,
            attractionEvidence && itemAvailable
                ? ParkItemStatusNormalizer.Normalize(item?.AttractionDetails?.Status)
                : null,
            openingEvidence
                ? NormalizeOptional(schedule?.SourceUrl)
                : attractionEvidence && itemAvailable
                    ? NormalizeOptional(item?.AttractionDetails?.SourceUrl)
                    : null,
            openingEvidence ? schedule?.LastVerifiedAtUtc : null);
    }

    private static string? NormalizeOptional(string? value)
    {
        string? normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
