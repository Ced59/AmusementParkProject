using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Core.Domain.Trips;

public readonly record struct TripItemDecisionId
{
    private TripItemDecisionId(string value)
    {
        this.Value = value;
    }

    public string Value { get; }

    public static TripItemDecisionId New()
    {
        return new TripItemDecisionId(Guid.NewGuid().ToString("D"));
    }

    public static TripItemDecisionId Parse(string value)
    {
        return new TripItemDecisionId(IdentifierRules.NormalizeRequired(value, nameof(value)));
    }

    public override string ToString()
    {
        return this.Value;
    }
}
