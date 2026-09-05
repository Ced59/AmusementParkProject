namespace AmusementPark.Core.Domain.Sharing;

/// <summary>
/// Jeton public canonique de 256 bits, distinct de tout identifiant métier ou utilisateur.
/// </summary>
public readonly record struct ShareToken
{
    public const int EntropyByteLength = 32;

    public const int EncodedLength = 43;

    private readonly string? value;

    private ShareToken(string value)
    {
        this.value = value;
    }

    public string Value => this.value
        ?? throw new InvalidOperationException("An uninitialized share token has no value.");

    public static ShareToken Parse(string? value)
    {
        if (value is null || value.Length != EncodedLength)
        {
            throw new ArgumentException(
                $"A share token must contain exactly {EncodedLength} Base64 URL characters.",
                nameof(value));
        }

        foreach (char character in value)
        {
            if (!IsBase64UrlCharacter(character))
            {
                throw new ArgumentException(
                    "A share token must use the unpadded Base64 URL alphabet.",
                    nameof(value));
            }
        }

        byte[] decoded;
        try
        {
            decoded = Convert.FromBase64String(
                value.Replace('-', '+').Replace('_', '/') + "=");
        }
        catch (FormatException exception)
        {
            throw new ArgumentException(
                "A share token must be a valid Base64 URL value.",
                nameof(value),
                exception);
        }

        if (decoded.Length != EntropyByteLength
            || !string.Equals(Encode(decoded), value, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "A share token must use the canonical 256-bit Base64 URL encoding.",
                nameof(value));
        }

        return new ShareToken(value);
    }

    public static bool TryParse(string? value, out ShareToken shareToken)
    {
        try
        {
            shareToken = Parse(value);
            return true;
        }
        catch (ArgumentException)
        {
            shareToken = default;
            return false;
        }
    }

    public override string ToString()
    {
        return this.Value;
    }

    private static string Encode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    private static bool IsBase64UrlCharacter(char character)
    {
        return character is >= 'A' and <= 'Z'
            or >= 'a' and <= 'z'
            or >= '0' and <= '9'
            or '-'
            or '_';
    }
}
