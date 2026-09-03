namespace AmusementPark.Core.Domain.Visits;

/// <summary>
/// Codes métier stables associés à l'évaluation privée d'une occurrence de ride.
/// </summary>
public static class RideAssessmentErrorCodes
{
    public const string PrivateCommentTooLong = "ride-assessment.private-comment-too-long";

    public const string InvalidRevision = "ride-assessment.invalid-revision";

    public const string TimestampNotUtc = "ride-assessment.timestamp-not-utc";

    public const string InvalidTimestampOrder = "ride-assessment.invalid-timestamp-order";
}
