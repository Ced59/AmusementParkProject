using AmusementPark.Core.Domain.Parks;
using AmusementPark.Application.Features.ParkFit.Results;
using AmusementPark.WebAPI.Contracts.ParkFit;

namespace AmusementPark.WebAPI.Mappers;

public static class ParkFitDataQualityHttpMapper
{
    public static ParkFitDataQualityDto ToHttp(this ParkFitDataQualityOperationsResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        ParkFitDataQualityAssessment assessment = result.Assessment;

        return new ParkFitDataQualityDto
        {
            ParkId = assessment.ParkId,
            ParkName = assessment.ParkName,
            Status = assessment.Status.ToString(),
            CoveragePercent = assessment.CoveragePercent,
            VisibleAttractionCount = assessment.VisibleAttractionCount,
            AttractionWithConditionsCount = assessment.AttractionWithConditionsCount,
            DecisionEligibleAttractionCount = assessment.DecisionEligibleAttractionCount,
            ConditionCount = assessment.ConditionCount,
            DecisionEligibleConditionCount = assessment.DecisionEligibleConditionCount,
            IssueItemCount = assessment.IssueItemCount,
            MissingSourceItemCount = assessment.MissingSourceItemCount,
            MissingTimestampItemCount = assessment.MissingTimestampItemCount,
            StaleEvidenceItemCount = assessment.StaleEvidenceItemCount,
            AmbiguousItemCount = assessment.AmbiguousItemCount,
            LastVerifiedAtUtc = assessment.LastVerifiedAtUtc,
            Issues = assessment.Issues.Select(static issue => issue.ToString()).ToList(),
            IssueSamples = assessment.IssueSamples.Select(static item => new ParkFitDataQualityItemDto
            {
                ParkItemId = item.ParkItemId,
                ParkItemName = item.ParkItemName,
                Issues = item.Issues.Select(static issue => issue.ToString()).ToList(),
            }).ToList(),
            RecommendationState = result.RecommendationState.ToString(),
            OperationalRevision = result.OperationalRevision,
            OperationalUpdatedAtUtc = result.OperationalUpdatedAtUtc,
            PendingReportCount = result.PendingReportCount,
            RecentDecisions = result.RecentDecisions.Select(static decision =>
                new ParkFitOperationalDecisionDto
                {
                    Type = decision.Type.ToString(),
                    Reason = decision.Reason,
                    DecidedAtUtc = decision.DecidedAtUtc,
                    Revision = decision.Revision,
                }).ToList(),
        };
    }
}
