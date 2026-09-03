namespace AmusementPark.Core.Domain.Visits;

/// <summary>
/// Codes métier stables associés à l'évaluation privée d'un parc pendant une visite.
/// </summary>
public static class VisitParkAssessmentErrorCodes
{
    public const string PrivateCommentTooLong = "visit-park-assessment.private-comment-too-long";

    public const string InvalidRevision = "visit-park-assessment.invalid-revision";

    public const string TimestampNotUtc = "visit-park-assessment.timestamp-not-utc";

    public const string InvalidTimestampOrder = "visit-park-assessment.invalid-timestamp-order";
}
