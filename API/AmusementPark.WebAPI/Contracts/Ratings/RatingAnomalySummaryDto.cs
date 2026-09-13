namespace AmusementPark.WebAPI.Contracts.Ratings;

public sealed class RatingAnomalySummaryDto
{
    public long NonNumericValueCount { get; set; }

    public long UnexpectedValueStorageTypeCount { get; set; }

    public long OutOfRangeValueCount { get; set; }

    public long NonHalfStepValueCount { get; set; }

    public long NearHalfStepValueCount { get; set; }

    public long MissingUserIdCount { get; set; }

    public long MissingTargetCount { get; set; }

    public long DuplicateVoteKeyCount { get; set; }

    public long ExtraDuplicateDocumentCount { get; set; }
}
