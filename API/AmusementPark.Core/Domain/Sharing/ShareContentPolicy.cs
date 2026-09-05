namespace AmusementPark.Core.Domain.Sharing;

/// <summary>
/// Liste blanche immuable des catégories de données qu'une publication peut exposer.
/// </summary>
public sealed class ShareContentPolicy
{
    public const int CurrentSchemaVersion = 1;

    private readonly IReadOnlyList<ShareContentField> includedFields;

    private ShareContentPolicy(
        SharePublicationType publicationType,
        int schemaVersion,
        ShareDatePrecision datePrecision,
        IEnumerable<ShareContentField>? includedFields)
    {
        ValidatePublicationType(publicationType);
        ValidateSchemaVersion(schemaVersion);
        ValidateDatePrecision(publicationType, datePrecision);

        ShareContentField[] normalizedFields = (includedFields ?? Array.Empty<ShareContentField>())
            .Distinct()
            .OrderBy(static field => field)
            .ToArray();
        foreach (ShareContentField field in normalizedFields)
        {
            ValidateContentField(publicationType, field);
        }

        this.PublicationType = publicationType;
        this.SchemaVersion = schemaVersion;
        this.DatePrecision = datePrecision;
        this.includedFields = Array.AsReadOnly(normalizedFields);
    }

    public SharePublicationType PublicationType { get; }

    public int SchemaVersion { get; }

    public ShareDatePrecision DatePrecision { get; }

    public IReadOnlyList<ShareContentField> IncludedFields => this.includedFields;

    public static ShareContentPolicy CreatePrivateDefault(SharePublicationType publicationType)
    {
        return new ShareContentPolicy(
            publicationType,
            CurrentSchemaVersion,
            ShareDatePrecision.Hidden,
            Array.Empty<ShareContentField>());
    }

    public static ShareContentPolicy Create(
        SharePublicationType publicationType,
        ShareDatePrecision datePrecision,
        IEnumerable<ShareContentField>? includedFields)
    {
        return new ShareContentPolicy(
            publicationType,
            CurrentSchemaVersion,
            datePrecision,
            includedFields);
    }

    public static ShareContentPolicy Restore(
        SharePublicationType publicationType,
        int schemaVersion,
        ShareDatePrecision datePrecision,
        IEnumerable<ShareContentField>? includedFields)
    {
        return new ShareContentPolicy(
            publicationType,
            schemaVersion,
            datePrecision,
            includedFields);
    }

    public bool Includes(ShareContentField field)
    {
        return this.includedFields.Contains(field);
    }

    public bool HasSameSelectionAs(ShareContentPolicy other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return this.PublicationType == other.PublicationType
            && this.SchemaVersion == other.SchemaVersion
            && this.DatePrecision == other.DatePrecision
            && this.includedFields.SequenceEqual(other.includedFields);
    }

    private static void ValidatePublicationType(SharePublicationType publicationType)
    {
        if (!Enum.IsDefined(publicationType))
        {
            throw CreateValidationException(
                ShareContentPolicyErrorCodes.InvalidPublicationType,
                "The share publication type is invalid.",
                nameof(publicationType));
        }
    }

    private static void ValidateSchemaVersion(int schemaVersion)
    {
        if (schemaVersion != CurrentSchemaVersion)
        {
            throw CreateValidationException(
                ShareContentPolicyErrorCodes.UnsupportedSchemaVersion,
                "The share content policy schema version is not supported.",
                nameof(schemaVersion));
        }
    }

    private static void ValidateDatePrecision(
        SharePublicationType publicationType,
        ShareDatePrecision datePrecision)
    {
        if (!Enum.IsDefined(datePrecision))
        {
            throw CreateValidationException(
                ShareContentPolicyErrorCodes.InvalidDatePrecision,
                "The share date precision is invalid.",
                nameof(datePrecision));
        }

        ShareDatePrecision maximumPrecision = publicationType switch
        {
            SharePublicationType.VisitRecap => ShareDatePrecision.Day,
            SharePublicationType.YearRecap => ShareDatePrecision.Year,
            SharePublicationType.PassportProfile => ShareDatePrecision.Year,
            SharePublicationType.PersonalRanking => ShareDatePrecision.Hidden,
            SharePublicationType.ProfileComparison => ShareDatePrecision.Day,
            _ => throw CreateValidationException(
                ShareContentPolicyErrorCodes.InvalidPublicationType,
                "The share publication type is invalid.",
                nameof(publicationType)),
        };
        if (datePrecision > maximumPrecision)
        {
            throw CreateValidationException(
                ShareContentPolicyErrorCodes.DatePrecisionNotAllowed,
                "The selected date precision is not allowed for this publication type.",
                nameof(datePrecision));
        }
    }

    private static void ValidateContentField(
        SharePublicationType publicationType,
        ShareContentField field)
    {
        if (!Enum.IsDefined(field))
        {
            throw CreateValidationException(
                ShareContentPolicyErrorCodes.InvalidContentField,
                "The share content field is invalid.",
                nameof(field));
        }

        if (!IsContentFieldAllowed(publicationType, field))
        {
            throw CreateValidationException(
                ShareContentPolicyErrorCodes.ContentFieldNotAllowed,
                "The selected content field is not allowed for this publication type.",
                nameof(field));
        }
    }

    private static bool IsContentFieldAllowed(
        SharePublicationType publicationType,
        ShareContentField field)
    {
        return field switch
        {
            ShareContentField.PublicDisplayName => true,
            ShareContentField.Avatar => true,
            ShareContentField.RideCount => publicationType is not SharePublicationType.PersonalRanking,
            ShareContentField.TemporalRatings => publicationType is not SharePublicationType.PersonalRanking,
            ShareContentField.GlobalRatings => true,
            ShareContentField.PublicCaption => publicationType is
                SharePublicationType.VisitRecap
                or SharePublicationType.YearRecap
                or SharePublicationType.PassportProfile,
            ShareContentField.GeographicStatistics => publicationType is
                SharePublicationType.YearRecap
                or SharePublicationType.PassportProfile,
            ShareContentField.MissedItems => publicationType is
                SharePublicationType.VisitRecap
                or SharePublicationType.YearRecap
                or SharePublicationType.PassportProfile,
            _ => false,
        };
    }

    private static ShareContentPolicyValidationException CreateValidationException(
        string errorCode,
        string message,
        string parameterName)
    {
        return new ShareContentPolicyValidationException(errorCode, message, parameterName);
    }
}
