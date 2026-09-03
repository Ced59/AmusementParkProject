using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Core.Domain.Visits;

/// <summary>
/// Observation privée et temporelle d'une occurrence de ride précise.
/// Elle ne constitue jamais une note communautaire courante.
/// </summary>
public sealed class RideAssessment
{
    public const int MaximumPrivateCommentLength = 4000;

    private RideAssessment(
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

    public static RideAssessment Create(
        RatingValue value,
        string? privateComment,
        DateTime nowUtc)
    {
        return new RideAssessment(value, privateComment, 1, nowUtc, nowUtc);
    }

    public static RideAssessment Restore(
        RatingValue value,
        string? privateComment,
        int revision,
        DateTime createdAtUtc,
        DateTime updatedAtUtc)
    {
        return new RideAssessment(
            value,
            privateComment,
            revision,
            createdAtUtc,
            updatedAtUtc);
    }

    public RideAssessment Update(
        RatingValue value,
        string? privateComment,
        DateTime nowUtc)
    {
        if (this.Revision == int.MaxValue)
        {
            throw CreateValidationException(
                RideAssessmentErrorCodes.InvalidRevision,
                "The assessment revision cannot be incremented further.");
        }

        return new RideAssessment(
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
                RideAssessmentErrorCodes.PrivateCommentTooLong,
                $"The private assessment comment cannot exceed {MaximumPrivateCommentLength} characters.");
        }

        return normalizedValue;
    }

    private static void ValidateRevision(int revision)
    {
        if (revision < 1)
        {
            throw CreateValidationException(
                RideAssessmentErrorCodes.InvalidRevision,
                "The assessment revision must be positive.");
        }
    }

    private static void ValidateTimestamps(DateTime createdAtUtc, DateTime updatedAtUtc)
    {
        if (createdAtUtc.Kind != DateTimeKind.Utc || updatedAtUtc.Kind != DateTimeKind.Utc)
        {
            throw CreateValidationException(
                RideAssessmentErrorCodes.TimestampNotUtc,
                "Assessment timestamps must be expressed in UTC.");
        }

        if (updatedAtUtc < createdAtUtc)
        {
            throw CreateValidationException(
                RideAssessmentErrorCodes.InvalidTimestampOrder,
                "Assessment timestamps are not chronologically consistent.");
        }
    }

    private static RideAssessmentValidationException CreateValidationException(
        string errorCode,
        string message)
    {
        return new RideAssessmentValidationException(errorCode, message);
    }
}
