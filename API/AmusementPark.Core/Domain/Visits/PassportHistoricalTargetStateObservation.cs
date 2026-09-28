using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Core.Domain.Visits;

public sealed record PassportHistoricalTargetStateObservation
{
    public PassportHistoricalTargetStateObservation(
        string parkItemId,
        string name,
        string category,
        HistoricalOperationalState operationalState,
        bool isCanonical,
        bool hasCanonicalNameEvidence = false,
        bool hasCanonicalCategoryEvidence = false)
    {
        this.ParkItemId = IdentifierRules.NormalizeRequired(parkItemId, nameof(parkItemId));
        this.Name = NormalizeRequired(name, nameof(name));
        this.Category = NormalizeRequired(category, nameof(category));
        if (!Enum.IsDefined(operationalState))
        {
            throw new ArgumentOutOfRangeException(nameof(operationalState));
        }

        this.OperationalState = operationalState;
        this.IsCanonical = isCanonical;
        this.HasCanonicalNameEvidence = hasCanonicalNameEvidence;
        this.HasCanonicalCategoryEvidence = hasCanonicalCategoryEvidence;
    }

    public string ParkItemId { get; }

    public string Name { get; }

    public string Category { get; }

    public HistoricalOperationalState OperationalState { get; }

    public bool IsCanonical { get; }

    public bool HasCanonicalNameEvidence { get; }

    public bool HasCanonicalCategoryEvidence { get; }

    private static string NormalizeRequired(string value, string parameterName)
    {
        string normalized = value?.Trim() ?? string.Empty;
        return normalized.Length > 0
            ? normalized
            : throw new ArgumentException("The historical target value is required.", parameterName);
    }
}
