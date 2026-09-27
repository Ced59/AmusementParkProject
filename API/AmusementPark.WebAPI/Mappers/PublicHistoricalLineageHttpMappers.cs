using AmusementPark.Application.Features.History.Results;
using AmusementPark.Core.Domain.History;
using AmusementPark.WebAPI.Contracts.Common;
using AmusementPark.WebAPI.Contracts.History;

namespace AmusementPark.WebAPI.Mappers;

internal static class PublicHistoricalLineageHttpMappers
{
    public static PublicHistoricalLineageDto ToHttp(this PublicHistoricalLineageResult result)
    {
        Dictionary<(HistoricalSubjectType Type, string Id, string? ContextParkId), string> publicKeys = result.Subjects
            .Select((subject, index) => new
            {
                Subject = subject,
                Key = $"subject-{index + 1}",
            })
            .ToDictionary(
                static item => (item.Subject.Type, item.Subject.Id, item.Subject.ContextParkId),
                static item => item.Key);
        return new PublicHistoricalLineageDto
        {
            Root = result.Root.ToLineageHttp(publicKeys),
            ContextPark = result.ContextPark is null
                ? null
                : new PublicHistoricalLineageContextParkDto
                {
                    Id = result.ContextPark.Id,
                    Name = result.ContextPark.Name,
                },
            Subjects = result.Subjects.Select(subject => subject.ToLineageHttp(publicKeys)).ToArray(),
            Relations = result.Relations.Select(relation => relation.ToLineageHttp(publicKeys)).ToArray(),
            HasDirectedCycle = result.HasDirectedCycle,
            IsTruncated = result.IsTruncated,
            MaximumDepth = result.MaximumDepth,
        };
    }

    private static PublicHistoricalLineageSubjectDto ToLineageHttp(
        this HistoricalSubject subject,
        IReadOnlyDictionary<(HistoricalSubjectType Type, string Id, string? ContextParkId), string> publicKeys)
    {
        return new PublicHistoricalLineageSubjectDto
        {
            Key = publicKeys[(subject.Type, subject.Id, subject.ContextParkId)],
            Type = subject.Type.ToString(),
            Label = subject.HistoricalLabel,
            IsHistoricalOnly = subject.PublicationPolicy == HistoricalSubjectPublicationPolicy.HistoricalOnly,
        };
    }

    private static PublicHistoricalLineageRelationDto ToLineageHttp(
        this PublicHistoricalRelationResult result,
        IReadOnlyDictionary<(HistoricalSubjectType Type, string Id, string? ContextParkId), string> publicKeys)
    {
        HistoricalRelation relation = result.Relation;
        return new PublicHistoricalLineageRelationDto
        {
            SourceKey = publicKeys[(
                relation.Source.Type,
                relation.Source.Id,
                relation.Source.ContextParkId)],
            TargetKey = publicKeys[(
                relation.Target.Type,
                relation.Target.Id,
                relation.Target.ContextParkId)],
            Type = relation.Type.ToString(),
            Direction = relation.Direction.ToString(),
            Period = relation.Period.ToLineageHttp(),
            EvidenceState = relation.State.ToString(),
            UncertaintyExplanations = relation.PublicUncertaintyExplanation.Select(static explanation =>
                new LocalizedTextDto
                {
                    LanguageCode = explanation.LanguageCode,
                    Value = explanation.Value,
                }).ToArray(),
            Sources = result.Sources.Select(static source => new PublicHistoricalSourceDto
            {
                Type = source.Type.ToString(),
                Title = source.Title,
                PublisherOrAuthor = source.PublisherOrAuthor,
                Url = source.Url,
                BibliographicReference = source.BibliographicReference,
                PublishedOn = source.PublishedOn,
                AccessedOn = source.AccessedOn,
                LanguageCode = source.LanguageCode,
                ArchiveUrl = source.ArchiveUrl,
                Accessibility = source.Accessibility.ToString(),
            }).ToArray(),
        };
    }

    private static PublicHistoricalPeriodDto ToLineageHttp(this HistoricalPeriod period)
    {
        return new PublicHistoricalPeriodDto
        {
            Start = period.Start?.ToLineageHttp(),
            End = period.End?.ToLineageHttp(),
            StartConfidence = period.StartConfidence.ToString(),
            EndConfidence = period.EndConfidence.ToString(),
        };
    }

    private static PublicHistoricalDateDto ToLineageHttp(this HistoricalDate date)
    {
        return new PublicHistoricalDateDto
        {
            Year = date.Year,
            Month = date.Month,
            Day = date.Day,
            Precision = date.Precision.ToString(),
            IsApproximate = date.IsApproximate,
            Qualifier = date.Qualifier?.ToString(),
        };
    }
}
