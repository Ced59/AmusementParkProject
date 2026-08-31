namespace AmusementPark.Core.Domain.Identifiers;

/// <summary>
/// Codes métier stables associés à la validation des identifiants opaques.
/// </summary>
public static class IdentifierErrorCodes
{
    public const string Required = "identifier.required";

    public const string TooLong = "identifier.too-long";

    public const string ControlCharacter = "identifier.control-character";
}
