namespace AmusementPark.Core.Domain.History;

public sealed record HistoricalSubject
{
    public HistoricalSubject(
        HistoricalSubjectType type,
        string id,
        string historicalLabel,
        HistoricalSubjectPublicationPolicy publicationPolicy,
        string? contextParkId = null)
    {
        if (!Enum.IsDefined(type) || !Enum.IsDefined(publicationPolicy))
        {
            throw Invalid(
                HistoricalPersistenceErrorCodes.InvalidEnum,
                "The historical subject type or publication policy is invalid.");
        }

        this.Type = type;
        this.Id = Normalize(id, 200, nameof(id));
        this.HistoricalLabel = Normalize(historicalLabel, 300, nameof(historicalLabel));
        this.PublicationPolicy = publicationPolicy;
        this.ContextParkId = type == HistoricalSubjectType.Park
            ? this.Id
            : NormalizeOptional(contextParkId, 200, nameof(contextParkId));
    }

    public HistoricalSubjectType Type { get; }

    public string Id { get; }

    public string HistoricalLabel { get; }

    public HistoricalSubjectPublicationPolicy PublicationPolicy { get; }

    /// <summary>
    /// Identifiant durable du parc auquel rattacher ce sujet dans les vues historiques.
    /// Il permet de retrouver une cible retirée sans dépendre de son document courant.
    /// </summary>
    public string? ContextParkId { get; }

    private static string Normalize(string value, int maximumLength, string parameterName)
    {
        string normalizedValue = value?.Trim() ?? string.Empty;
        if (normalizedValue.Length == 0
            || normalizedValue.Length > maximumLength
            || normalizedValue.Any(char.IsControl))
        {
            throw Invalid(
                HistoricalPersistenceErrorCodes.InvalidIdentifier,
                "A historical subject requires a valid identifier and label.",
                parameterName);
        }

        return normalizedValue;
    }

    private static string? NormalizeOptional(string? value, int maximumLength, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Normalize(value, maximumLength, parameterName);
    }

    private static HistoricalPersistenceValidationException Invalid(
        string code,
        string message,
        string? parameterName = null)
    {
        return new HistoricalPersistenceValidationException(code, message, parameterName);
    }
}
