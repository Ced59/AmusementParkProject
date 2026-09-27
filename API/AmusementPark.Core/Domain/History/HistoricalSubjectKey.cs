namespace AmusementPark.Core.Domain.History;

public sealed record HistoricalSubjectKey
{
    public HistoricalSubjectKey(HistoricalSubjectType type, string id, string? contextParkId = null)
    {
        if (!Enum.IsDefined(type))
        {
            throw new HistoricalPersistenceValidationException(
                HistoricalPersistenceErrorCodes.InvalidEnum,
                "A historical subject key requires a valid type.");
        }

        string normalizedId = id?.Trim() ?? string.Empty;
        if (normalizedId.Length == 0
            || normalizedId.Length > 200
            || normalizedId.Any(char.IsControl))
        {
            throw new HistoricalPersistenceValidationException(
                HistoricalPersistenceErrorCodes.InvalidIdentifier,
                "A historical subject key requires a valid identifier.");
        }

        this.Type = type;
        this.Id = normalizedId;
        this.ContextParkId = type == HistoricalSubjectType.Park
            ? normalizedId
            : NormalizeOptionalContextParkId(contextParkId);
    }

    public HistoricalSubjectType Type { get; }

    public string Id { get; }

    public string? ContextParkId { get; }

    private static string? NormalizeOptionalContextParkId(string? contextParkId)
    {
        if (string.IsNullOrWhiteSpace(contextParkId))
        {
            return null;
        }

        string normalizedContextParkId = contextParkId.Trim();
        if (normalizedContextParkId.Length > 200 || normalizedContextParkId.Any(char.IsControl))
        {
            throw new HistoricalPersistenceValidationException(
                HistoricalPersistenceErrorCodes.InvalidIdentifier,
                "A historical subject key requires a valid context park identifier.");
        }

        return normalizedContextParkId;
    }
}
