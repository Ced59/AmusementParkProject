namespace AmusementPark.Application.Features.Ratings.Results;

public sealed record RatingAnomalySummaryResult(
    long NonNumericValueCount,
    long UnexpectedValueStorageTypeCount,
    long OutOfRangeValueCount,
    long NonHalfStepValueCount,
    long NearHalfStepValueCount,
    long MissingUserIdCount,
    long MissingTargetCount,
    long DuplicateVoteKeyCount,
    long ExtraDuplicateDocumentCount);
