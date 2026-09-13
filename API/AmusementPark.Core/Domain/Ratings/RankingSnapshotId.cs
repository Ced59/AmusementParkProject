using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Core.Domain.Ratings;

public readonly record struct RankingSnapshotId
{
    private readonly string? value;

    private RankingSnapshotId(string value)
    {
        this.value = value;
    }

    public string Value => this.value
        ?? throw new InvalidOperationException("An uninitialized ranking snapshot identifier has no value.");

    public static RankingSnapshotId Parse(string? value)
    {
        return new RankingSnapshotId(IdentifierRules.NormalizeRequired(value, nameof(value)));
    }

    public override string ToString()
    {
        return this.Value;
    }
}
