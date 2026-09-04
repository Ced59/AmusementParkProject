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

public readonly record struct RankingSnapshotChecksum
{
    public const int HexadecimalLength = 64;

    private readonly string? value;

    private RankingSnapshotChecksum(string value)
    {
        this.value = value;
    }

    public string Value => this.value
        ?? throw new InvalidOperationException("An uninitialized ranking snapshot checksum has no value.");

    public static RankingSnapshotChecksum Parse(string? value)
    {
        if (!TryParse(value, out RankingSnapshotChecksum checksum))
        {
            throw new ArgumentException(
                "A ranking snapshot checksum must contain exactly 64 lowercase hexadecimal characters.",
                nameof(value));
        }

        return checksum;
    }

    public static bool TryParse(string? value, out RankingSnapshotChecksum checksum)
    {
        checksum = default;
        if (value is null || value.Length != HexadecimalLength)
        {
            return false;
        }

        foreach (char character in value)
        {
            bool isLowercaseHexadecimalLetter = character is >= 'a' and <= 'f';
            bool isDigit = character is >= '0' and <= '9';
            if (!isLowercaseHexadecimalLetter && !isDigit)
            {
                return false;
            }
        }

        checksum = new RankingSnapshotChecksum(value);
        return true;
    }

    public override string ToString()
    {
        return this.Value;
    }
}

public enum RankingSnapshotStatus
{
    Building,
    Validated,
    Current,
    Superseded,
    Failed,
}
