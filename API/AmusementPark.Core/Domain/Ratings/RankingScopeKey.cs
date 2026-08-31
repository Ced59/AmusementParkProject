namespace AmusementPark.Core.Domain.Ratings;

/// <summary>
/// Clé stable d'un périmètre de classement publiable.
/// </summary>
public readonly record struct RankingScopeKey
{
    public const int MaximumLength = 128;

    private readonly string? value;

    private RankingScopeKey(string value)
    {
        this.value = value;
    }

    public string Value => this.value
        ?? throw new InvalidOperationException("An uninitialized ranking scope key has no value.");

    public static RankingScopeKey Parse(string? value)
    {
        if (!TryParse(value, out RankingScopeKey key))
        {
            throw new ArgumentException(
                "The ranking scope key must contain at least two lowercase ASCII segments separated by colons.",
                nameof(value));
        }

        return key;
    }

    public static bool TryParse(string? value, out RankingScopeKey key)
    {
        key = default;
        if (value is null || value.Length == 0 || value.Length > MaximumLength)
        {
            return false;
        }

        int segmentCount = 1;
        int segmentLength = 0;
        bool previousCharacterWasHyphen = false;
        foreach (char character in value)
        {
            if (character == ':')
            {
                if (segmentLength == 0 || previousCharacterWasHyphen)
                {
                    return false;
                }

                segmentCount++;
                segmentLength = 0;
                previousCharacterWasHyphen = false;
                continue;
            }

            bool isLowercaseAsciiLetter = character is >= 'a' and <= 'z';
            bool isAsciiDigit = character is >= '0' and <= '9';
            if (!isLowercaseAsciiLetter && !isAsciiDigit && character != '-')
            {
                return false;
            }

            if (character == '-')
            {
                if (segmentLength == 0 || previousCharacterWasHyphen)
                {
                    return false;
                }

                previousCharacterWasHyphen = true;
            }
            else
            {
                previousCharacterWasHyphen = false;
            }

            segmentLength++;
        }

        if (segmentCount < 2 || segmentLength == 0 || previousCharacterWasHyphen)
        {
            return false;
        }

        key = new RankingScopeKey(value);
        return true;
    }

    public override string ToString()
    {
        return this.Value;
    }
}
