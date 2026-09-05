namespace AmusementPark.WebAPI.Contracts.PassportBeta;

public sealed class PassportBetaMetricsDto
{
    public DateTime GeneratedAtUtc { get; set; }

    public DateTime FromUtc { get; set; }

    public DateTime ToUtc { get; set; }

    public long CreatedVisits { get; set; }

    public long CompletedVisits { get; set; }

    public long UsersWithCompletedVisit { get; set; }

    public long UsersWithSecondCompletedVisit { get; set; }

    public decimal RepeatUsageRatePercent { get; set; }

    public string RepeatUsageSignal { get; set; } = string.Empty;

    public bool RequiresQualitativeValidation { get; set; }

    public IReadOnlyCollection<PassportBetaDailyMetricsDto> Daily { get; set; } =
        Array.Empty<PassportBetaDailyMetricsDto>();
}
