using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Core.Domain.Visits;

public sealed record PassportRideStatisticsObservation
{
    public PassportRideStatisticsObservation(
        string rideOccurrenceId,
        string visitId,
        string parkId,
        string parkItemId,
        VisitDate visitDate,
        RideOccurrenceStatus status,
        RatingValue? assessment,
        string? historicalCategory,
        string? currentCategory,
        string? historicalName = null)
    {
        this.RideOccurrenceId = IdentifierRules.NormalizeRequired(
            rideOccurrenceId,
            nameof(rideOccurrenceId));
        this.VisitId = IdentifierRules.NormalizeRequired(visitId, nameof(visitId));
        this.ParkId = IdentifierRules.NormalizeRequired(parkId, nameof(parkId));
        this.ParkItemId = IdentifierRules.NormalizeRequired(parkItemId, nameof(parkItemId));
        this.VisitDate = visitDate ?? throw new ArgumentNullException(nameof(visitDate));
        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status));
        }

        this.Status = status;
        this.Assessment = assessment;
        this.HistoricalCategory = NormalizeOptional(historicalCategory);
        this.CurrentCategory = NormalizeOptional(currentCategory);
        this.HistoricalName = NormalizeOptional(historicalName);
    }

    public string RideOccurrenceId { get; }

    public string VisitId { get; }

    public string ParkId { get; }

    public string ParkItemId { get; }

    public VisitDate VisitDate { get; }

    public RideOccurrenceStatus Status { get; }

    public RatingValue? Assessment { get; }

    public string? HistoricalCategory { get; }

    public string? HistoricalName { get; }

    public string? CurrentCategory { get; }

    private static string? NormalizeOptional(string? value)
    {
        string normalized = value?.Trim() ?? string.Empty;
        return normalized.Length == 0 ? null : normalized;
    }
}
