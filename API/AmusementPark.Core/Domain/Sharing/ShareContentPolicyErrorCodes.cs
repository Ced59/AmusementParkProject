namespace AmusementPark.Core.Domain.Sharing;

/// <summary>
/// Codes métier stables associés à la liste blanche d'une publication.
/// </summary>
public static class ShareContentPolicyErrorCodes
{
    public const string InvalidPublicationType = "share-content-policy.invalid-publication-type";

    public const string UnsupportedSchemaVersion = "share-content-policy.unsupported-schema-version";

    public const string InvalidDatePrecision = "share-content-policy.invalid-date-precision";

    public const string DatePrecisionNotAllowed = "share-content-policy.date-precision-not-allowed";

    public const string InvalidContentField = "share-content-policy.invalid-content-field";

    public const string ContentFieldNotAllowed = "share-content-policy.content-field-not-allowed";
}
