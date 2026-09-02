using AmusementPark.Application.Features.Ratings.Models;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.WebAPI.Contracts.Ratings;

namespace AmusementPark.WebAPI.Mappers;

internal static class RatingRankingAdministrationHttpMappers
{
    public static RatingRankingPolicyCandidate ToApplication(
        this RatingRankingPolicyCandidateRequestDto value)
    {
        return new RatingRankingPolicyCandidate(
            value.Version,
            value.ProvisionalMinUniqueContributors,
            value.EligibleMinUniqueContributors,
            value.EstablishedMinUniqueContributors,
            value.StrongEvidenceMinUniqueContributors,
            value.MinimumEligibleEntriesPerRanking,
            value.MinimumEligibleItemsForParkItemComponent,
            value.MinimumEligibleItemsPerCategory,
            value.MinimumEligibleCategories,
            value.ScoreTieEpsilon);
    }

    public static RatingRankingAdministrationDto ToHttp(
        this RatingRankingAdministrationResult value)
    {
        return new RatingRankingAdministrationDto
        {
            GeneratedAtUtc = value.GeneratedAtUtc,
            CurrentMethodology = value.CurrentMethodology.ToHttp(),
            PreparingMethodology = value.PreparingMethodology?.ToHttp(),
            DataDiagnostics = value.DataDiagnostics.ToHttp(),
            Scopes = value.Scopes.Select(static scope => new RatingRankingScopeDiagnosticsDto
            {
                ScopeKey = scope.ScopeKey,
                TargetFamily = scope.TargetFamily.ToString(),
                ParkItemCategory = scope.ParkItemCategory?.ToString(),
                MethodologyVersion = scope.MethodologyVersion,
                CurrentSnapshotId = scope.CurrentSnapshotId,
                GeneratedAtUtc = scope.GeneratedAtUtc,
                PublishedAtUtc = scope.PublishedAtUtc,
                RebuildDurationMilliseconds = scope.RebuildDurationMilliseconds,
                TotalEntryCount = scope.TotalEntryCount,
                EligibleEntryCount = scope.EligibleEntryCount,
                SourceRevision = scope.SourceRevision,
                PublishedSourceRevision = scope.PublishedSourceRevision,
                IsRebuildOutstanding = scope.IsRebuildOutstanding,
                IsDiagnosticSourceTruncated = scope.IsDiagnosticSourceTruncated,
                LastJobStatus = scope.LastJobStatus,
                LastErrorCode = scope.LastErrorCode,
                LastJobUpdatedAtUtc = scope.LastJobUpdatedAtUtc,
            }).ToArray(),
            EvidenceDistribution = value.EvidenceDistribution.Select(static distribution =>
                new RatingRankingEvidenceDistributionDto
                {
                    TargetType = distribution.TargetType.ToString(),
                    Level = distribution.Level.ToString(),
                    TargetCount = distribution.TargetCount,
                    UniqueContributorCount = distribution.UniqueContributorCount,
                    RatingObservationCount = distribution.RatingObservationCount,
                }).ToArray(),
            NearThresholdTargets = value.NearThresholdTargets.Select(static target =>
                new RatingRankingNearThresholdTargetDto
                {
                    ScopeKey = target.ScopeKey,
                    TargetType = target.TargetType.ToString(),
                    TargetId = target.TargetId,
                    TargetName = target.TargetName,
                    UniqueContributorCount = target.UniqueContributorCount,
                    EligibilityThreshold = target.EligibilityThreshold,
                    RemainingContributorCount = target.RemainingContributorCount,
                }).ToArray(),
            Exclusions = value.Exclusions.Select(static exclusion =>
                new RatingRankingExclusionDistributionDto
                {
                    TargetType = exclusion.TargetType.ToString(),
                    Reason = exclusion.Reason.ToString(),
                    TargetCount = exclusion.TargetCount,
                }).ToArray(),
            CategoryCoverage = value.CategoryCoverage.Select(static coverage =>
                new RatingRankingCategoryCoverageDto
                {
                    ScopeKey = coverage.ScopeKey,
                    Category = coverage.Category.ToString(),
                    CandidateCount = coverage.CandidateCount,
                    EligibleCount = coverage.EligibleCount,
                    HasMinimumComparableEntries = coverage.HasMinimumComparableEntries,
                }).ToArray(),
        };
    }

    public static RatingRankingPolicyImpactDto ToHttp(this RatingRankingPolicyImpactResult value)
    {
        return new RatingRankingPolicyImpactDto
        {
            GeneratedAtUtc = value.GeneratedAtUtc,
            Candidate = value.Candidate.ToHttp(),
            GainedEligibilityCount = value.GainedEligibilityCount,
            LostEligibilityCount = value.LostEligibilityCount,
            ComparedRankCount = value.ComparedRankCount,
            TotalAbsoluteRankChange = value.TotalAbsoluteRankChange,
            AverageRankChange = value.AverageRankChange,
            MaximumRankChange = value.MaximumRankChange,
            ScopeCountBelowMinimum = value.ScopeCountBelowMinimum,
            IncompleteParkCompositionCount = value.IncompleteParkCompositionCount,
            EstimatedTargetCount = value.EstimatedTargetCount,
            EstimatedChunkCount = value.EstimatedChunkCount,
            Scopes = value.Scopes.Select(static scope => new RatingRankingPolicyScopeImpactDto
            {
                ScopeKey = scope.ScopeKey,
                TargetFamily = scope.TargetFamily.ToString(),
                ParkItemCategory = scope.ParkItemCategory?.ToString(),
                HasCurrentSnapshot = scope.HasCurrentSnapshot,
                IsImpactAvailable = scope.IsImpactAvailable,
                IsSourceTruncated = scope.IsSourceTruncated,
                CurrentEligibleCount = scope.CurrentEligibleCount,
                CandidateEligibleCount = scope.CandidateEligibleCount,
                GainedEligibilityCount = scope.GainedEligibilityCount,
                LostEligibilityCount = scope.LostEligibilityCount,
                ComparedRankCount = scope.ComparedRankCount,
                TotalAbsoluteRankChange = scope.TotalAbsoluteRankChange,
                AverageRankChange = scope.AverageRankChange,
                MaximumRankChange = scope.MaximumRankChange,
                HasMinimumComparableEntries = scope.HasMinimumComparableEntries,
                IncompleteParkCompositionCount = scope.IncompleteParkCompositionCount,
                EstimatedTargetCount = scope.EstimatedTargetCount,
                EstimatedChunkCount = scope.EstimatedChunkCount,
                GainedTargets = scope.GainedTargets.Select(ToHttp).ToArray(),
                LostTargets = scope.LostTargets.Select(ToHttp).ToArray(),
            }).ToArray(),
        };
    }

    public static RatingRankingRebuildRequestResultDto ToHttp(
        this RatingRankingRebuildRequestResult value)
    {
        return new RatingRankingRebuildRequestResultDto
        {
            RequestedAtUtc = value.RequestedAtUtc,
            ScheduledScopeCount = value.ScheduledScopeCount,
            Scopes = value.Scopes.Select(static scope => new RatingRankingScheduledScopeDto
            {
                ScopeKey = scope.ScopeKey,
                RequestedSourceRevision = scope.RequestedSourceRevision,
            }).ToArray(),
        };
    }

    private static RatingRankingPolicyCandidateRequestDto ToHttp(
        this RatingRankingPolicyCandidate value)
    {
        return new RatingRankingPolicyCandidateRequestDto
        {
            Version = value.Version,
            ProvisionalMinUniqueContributors = value.ProvisionalMinUniqueContributors,
            EligibleMinUniqueContributors = value.EligibleMinUniqueContributors,
            EstablishedMinUniqueContributors = value.EstablishedMinUniqueContributors,
            StrongEvidenceMinUniqueContributors = value.StrongEvidenceMinUniqueContributors,
            MinimumEligibleEntriesPerRanking = value.MinimumEligibleEntriesPerRanking,
            MinimumEligibleItemsForParkItemComponent = value.MinimumEligibleItemsForParkItemComponent,
            MinimumEligibleItemsPerCategory = value.MinimumEligibleItemsPerCategory,
            MinimumEligibleCategories = value.MinimumEligibleCategories,
            ScoreTieEpsilon = value.ScoreTieEpsilon,
        };
    }

    private static RatingRankingPolicyTargetChangeDto ToHttp(
        RatingRankingPolicyTargetChangeResult value)
    {
        return new RatingRankingPolicyTargetChangeDto
        {
            TargetType = value.TargetType.ToString(),
            TargetId = value.TargetId,
            TargetName = value.TargetName,
            PreviousRank = value.PreviousRank,
            CandidateRank = value.CandidateRank,
        };
    }
}
