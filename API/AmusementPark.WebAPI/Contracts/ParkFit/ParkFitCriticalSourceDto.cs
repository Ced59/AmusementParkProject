using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.ParkFit;

public sealed class ParkFitCriticalSourceDto
{
    public string Kind { get; init; } = string.Empty;

    public string? Url { get; init; }

    public string? Reference { get; init; }

    public string? LanguageCode { get; init; }

    public DateTime? CollectedAtUtc { get; init; }

    public DateTime? VerifiedAtUtc { get; init; }

    public string Confidence { get; init; } = string.Empty;

    public IReadOnlyCollection<LocalizedTextDto> Summaries { get; init; } =
        Array.Empty<LocalizedTextDto>();
}
