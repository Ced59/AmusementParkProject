using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Core.Domain.Trips;

public readonly record struct TripParkCandidateId
{
    private readonly string? value;

    private TripParkCandidateId(string value)
    {
        this.value = value;
    }

    public string Value => this.value
        ?? throw new InvalidOperationException("An uninitialized trip park candidate identifier has no value.");

    public static TripParkCandidateId New()
    {
        return new TripParkCandidateId(Guid.NewGuid().ToString("N"));
    }

    public static TripParkCandidateId Parse(string? value)
    {
        return new TripParkCandidateId(IdentifierRules.NormalizeRequired(value, nameof(value)));
    }

    public static bool TryParse(string? value, out TripParkCandidateId candidateId)
    {
        try
        {
            candidateId = Parse(value);
            return true;
        }
        catch (ArgumentException)
        {
            candidateId = default;
            return false;
        }
        catch (InvalidOperationException)
        {
            candidateId = default;
            return false;
        }
    }

    public override string ToString()
    {
        return this.Value;
    }
}
