namespace AmusementPark.WebAPI.Contracts.Passport;

public sealed class PassportHistoricalRideTargetPageDto
{
    public IReadOnlyCollection<PassportVisitRideTargetEvaluationDto> Items { get; init; } =
        Array.Empty<PassportVisitRideTargetEvaluationDto>();

    public int CurrentPage { get; init; }

    public int PageSize { get; init; }

    public int TotalItems { get; init; }

    public int TotalPages { get; init; }

    public int KnownOpenCount { get; init; }

    public int PossiblyOpenCount { get; init; }

    public int AllHistoryCount { get; init; }

    public string CoverageStatus { get; init; } = string.Empty;

    public int CoveragePercent { get; init; }

    public string MethodologyVersion { get; init; } = string.Empty;
}
