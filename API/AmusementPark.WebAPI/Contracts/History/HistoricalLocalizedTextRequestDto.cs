namespace AmusementPark.WebAPI.Contracts.History;

public sealed class HistoricalLocalizedTextRequestDto
{
    public string LanguageCode { get; init; } = string.Empty;

    public string Value { get; init; } = string.Empty;
}
