using AmusementPark.Application.Features.ParkFit.Models;
using AmusementPark.Application.Features.ParkFit.Queries;
using AmusementPark.Application.Features.ParkFit.Results;
using AmusementPark.Core.Domain.ParkFit;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.WebAPI.Contracts.ParkFit;
using AmusementPark.WebAPI.Contracts.ParkItems;

namespace AmusementPark.WebAPI.Mappers;

public static class ParkFitSearchHttpMapper
{
    public static SearchParksByFitQuery ToApplication(this ParkFitSearchRequestDto request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new SearchParksByFitQuery(
            request.EvaluationDate,
            (request.Members ?? Array.Empty<ParkFitSearchMemberCriteriaDto>())
                .Select(static (member, index) => member is null
                    ? new ParkFitSearchMemberCriteria(
                        string.Empty,
                        null,
                        null,
                        null,
                        null,
                        null,
                        null)
                    : new ParkFitSearchMemberCriteria(
                        $"member-{index + 1}",
                        member.HeightCentimeters,
                        member.MinimumAgeYears,
                        member.MaximumAgeYears,
                        member.CanBeAccompanied,
                        member.CompanionMinimumAgeYears,
                        member.CompanionMaximumAgeYears))
                .ToList(),
            (request.PreferredAttractionTypes ?? Array.Empty<ParkItemTypeDto>())
                .Select(static type => (ParkItemType)(int)type)
                .ToList(),
            request.PreferIndoor,
            request.CountryCode,
            (ParkFitUnknownDataPolicy)(int)request.UnknownDataPolicy,
            request.MaximumResults);
    }

    public static ParkFitSearchResponseDto ToHttp(this ParkFitSearchResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return new ParkFitSearchResponseDto
        {
            MethodVersion = result.MethodVersion,
            EvaluationDate = result.EvaluationDate,
            EvaluatedAtUtc = result.EvaluatedAtUtc,
            TotalCandidateCount = result.TotalCandidateCount,
            InspectedCandidateCount = result.InspectedCandidateCount,
            QualityEligibleCandidateCount = result.QualityEligibleCandidateCount,
            QualityRejectedCandidateCount = result.QualityRejectedCandidateCount,
            CandidatePoolTruncated = result.CandidatePoolTruncated,
            QualityStatusCounts = result.QualityStatusCounts.ToDictionary(
                static item => item.Key.ToString(),
                static item => item.Value,
                StringComparer.Ordinal),
            QualityIssueCounts = result.QualityIssueCounts.ToDictionary(
                static item => item.Key.ToString(),
                static item => item.Value,
                StringComparer.Ordinal),
            Parks = result.Parks.Select(ToHttp).ToList(),
        };
    }

    private static ParkFitSearchParkDto ToHttp(ParkFitSearchParkResult result)
    {
        int unknownCount = result.Score.UnknownHardFilterCount
            + (result.Score.DateAvailabilityState == ParkFitDateAvailabilityState.Unknown ? 1 : 0)
            + result.Score.Components.Count(static component =>
                component.State == ParkFitSubscoreState.Unknown);

        return new ParkFitSearchParkDto
        {
            ParkId = result.Park.Id,
            ParkName = result.Park.Name?.Trim() ?? string.Empty,
            CountryCode = result.Park.CountryCode,
            ParkType = result.Park.Type?.ToString(),
            ScoreState = result.Score.State.ToString(),
            ComparativeScore = result.Score.ComparativeScore,
            RawKnownScore = result.Score.RawKnownScore,
            CoveragePercent = result.Score.CoveragePercent,
            KnownWeightPercent = result.Score.KnownWeightPercent,
            ScoreCeilingPercent = result.Score.ScoreCeilingPercent,
            Confidence = result.Score.Confidence.ToString(),
            DateAvailabilityState = result.Score.DateAvailabilityState.ToString(),
            UnknownCount = unknownCount,
            EveryoneTogetherAttractionCount = result.EveryoneTogetherAttractionCount,
            SplitRequiredAttractionCount = result.SplitRequiredAttractionCount,
            PartialAttractionCount = result.PartialAttractionCount,
            NoCompatibleMemberAttractionCount = result.NoCompatibleMemberAttractionCount,
            UnknownAttractionCount = result.UnknownAttractionCount,
            DataQualityStatus = result.DataQuality.Status.ToString(),
            DataQualityCoveragePercent = result.DataQuality.CoveragePercent,
            LastVerifiedAtUtc = result.DataQuality.LastVerifiedAtUtc,
            Reasons = result.Score.Reasons.Select(static reason => reason.ToString()).ToList(),
            Components = result.Score.Components.Select(static component =>
                new ParkFitScoreComponentDto
                {
                    Kind = component.Kind.ToString(),
                    State = component.State.ToString(),
                    Value = component.Value,
                    CoveragePercent = component.CoveragePercent,
                    Confidence = component.Confidence.ToString(),
                    BaseWeightPercent = component.BaseWeightPercent,
                    ApplicableWeightPercent = component.ApplicableWeightPercent,
                    KnownScoreWeightPercent = component.KnownScoreWeightPercent,
                    Contribution = component.Contribution,
                    Reasons = component.Reasons.Select(static reason => reason.ToString()).ToList(),
                }).ToList(),
            MemberSummaries = result.MemberSummaries.Select(static member =>
                new ParkFitSearchMemberSummaryDto
                {
                    MemberNumber = member.MemberNumber,
                    CompatibleAloneAttractionCount = member.CompatibleAloneAttractionCount,
                    CompatibleWithCompanionAttractionCount = member.CompatibleWithCompanionAttractionCount,
                    IncompatibleAttractionCount = member.IncompatibleAttractionCount,
                    UnknownAttractionCount = member.UnknownAttractionCount,
                    NotApplicableAttractionCount = member.NotApplicableAttractionCount,
                }).ToList(),
            CriticalSources = result.CriticalSources.Select(static source =>
                new ParkFitCriticalSourceDto
                {
                    Kind = source.Kind.ToString(),
                    Url = source.Url,
                    Reference = source.Reference,
                    LanguageCode = source.LanguageCode,
                    CollectedAtUtc = source.CollectedAtUtc,
                    VerifiedAtUtc = source.VerifiedAtUtc,
                    Confidence = source.Confidence.ToString(),
                    Summaries = source.Summaries.ToHttp(),
                }).ToList(),
        };
    }
}
