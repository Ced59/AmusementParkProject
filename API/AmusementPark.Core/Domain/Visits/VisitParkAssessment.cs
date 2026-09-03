using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Core.Domain.Visits;

/// <summary>
/// Observation privée et temporelle de la qualité d'un parc pour une visite précise.
/// Elle ne constitue jamais une note communautaire courante.
/// </summary>
public sealed class VisitParkAssessment
{
    public const int MaximumPrivateCommentLength = 4000;

    private VisitParkAssessment(
        RatingValue value,
        string? privateComment,
        int revision,
        DateTime createdAtUtc,
        DateTime updatedAtUtc)
    {
        _ = value.HalfSteps;
        ValidateRevision(revision);
        ValidateTimestamps(createdAtUtc, updatedAtUtc);

        this.Value = value;
        this.PrivateComment = NormalizePrivateComment(privateComment);
        this.Revision = revision;
        this.CreatedAtUtc = createdAtUtc;
        this.UpdatedAtUtc = updatedAtUtc;
    }

    public RatingValue Value { get; }

    public string? PrivateComment { get; }

    public int Revision { get; }

    public DateTime CreatedAtUtc { get; }

    public DateTime UpdatedAtUtc { get; }

    public static VisitParkAssessment Create(
        RatingValue value,
        string? privateComment,
        DateTime nowUtc)
    {
        return new VisitParkAssessment(value, privateComment, 1, nowUtc, nowUtc);
    }

    public static VisitParkAssessment Restore(
        RatingValue value,
        string? privateComment,
        int revision,
        DateTime createdAtUtc,
        DateTime updatedAtUtc)
    {
        return new VisitParkAssessment(
            value,
            privateComment,
            revision,
            createdAtUtc,
            updatedAtUtc);
    }

    public VisitParkAssessment Update(
        RatingValue value,
        string? privateComment,
        DateTime nowUtc)
    {
        if (this.Revision == int.MaxValue)
        {
            throw CreateValidationException(
                VisitParkAssessmentErrorCodes.InvalidRevision,
                "The assessment revision cannot be incremented further.");
        }

        return new VisitParkAssessment(
            value,
            privateComment,
            this.Revision + 1,
            this.CreatedAtUtc,
            nowUtc);
    }

    private static string? NormalizePrivateComment(string? value)
    {
        string normalizedValue = value?.Trim() ?? string.Empty;
        if (normalizedValue.Length == 0)
        {
            return null;
        }

        if (normalizedValue.Length > MaximumPrivateCommentLength)
        {
            throw CreateValidationException(
                VisitParkAssessmentErrorCodes.PrivateCommentTooLong,
                $"The private assessment comment cannot exceed {MaximumPrivateCommentLength} characters.");
        }

        return normalizedValue;
    }

    private static void ValidateRevision(int revision)
    {
        if (revision < 1)
        {
            throw CreateValidationException(
                VisitParkAssessmentErrorCodes.InvalidRevision,
                "The assessment revision must be positive.");
        }
    }

    private static void ValidateTimestamps(DateTime createdAtUtc, DateTime updatedAtUtc)
    {
        if (createdAtUtc.Kind != DateTimeKind.Utc || updatedAtUtc.Kind != DateTimeKind.Utc)
        {
            throw CreateValidationException(
                VisitParkAssessmentErrorCodes.TimestampNotUtc,
                "Assessment timestamps must be expressed in UTC.");
        }

        if (updatedAtUtc < createdAtUtc)
        {
            throw CreateValidationException(
                VisitParkAssessmentErrorCodes.InvalidTimestampOrder,
                "Assessment timestamps are not chronologically consistent.");
        }
    }

    private static VisitParkAssessmentValidationException CreateValidationException(
        string errorCode,
        string message)
    {
        return new VisitParkAssessmentValidationException(errorCode, message);
    }
}
