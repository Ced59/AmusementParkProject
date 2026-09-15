using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Features.FactualEvents.Models;
using AmusementPark.Application.Features.FactualEvents.Results;
using AmusementPark.Core.Domain.FactualEvents;
using AmusementPark.WebAPI.Contracts.FactualEvents;

namespace AmusementPark.WebAPI.Mappers;

public static class FactualChangeEventHttpMapper
{
    public static bool TryToCriteria(
        this FactualChangeEventSearchRequestDto request,
        out FactualChangeEventSearchCriteria? criteria)
    {
        ArgumentNullException.ThrowIfNull(request);
        criteria = null;
        if (!TryParseOptional(request.Status, out FactualChangeStatus? status)
            || !TryParseOptional(request.TargetType, out FactualTargetType? targetType)
            || !TryParseOptional(request.EventType, out FactualEventType? eventType)
            || !TryParseOptional(request.Confidence, out DataConfidence? confidence))
        {
            return false;
        }

        criteria = new FactualChangeEventSearchCriteria(
            new PagedQuery(request.Page, request.Size),
            status,
            targetType,
            eventType,
            confidence);
        return true;
    }

    public static FactualChangeEventAdminDto ToHttp(this FactualChangeEventAdminResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new FactualChangeEventAdminDto
        {
            EventId = result.EventId,
            Type = result.Type.ToString(),
            DefinitionVersion = result.DefinitionVersion,
            Target = new FactualChangeTargetAdminDto
            {
                Type = result.Target.Type.ToString(),
                Name = result.Target.TargetName,
                ParentParkName = result.Target.ParentParkName,
            },
            PreviousValue = ToHttp(result.PreviousValue),
            NewValue = ToHttp(result.NewValue),
            Source = new FactualSourceReferenceAdminDto
            {
                Type = result.Source.Type.ToString(),
                PublisherName = result.Source.PublisherName,
                Title = result.Source.Title,
                Url = result.Source.Url,
                PublishedAtUtc = result.Source.PublishedAtUtc,
            },
            Confidence = result.Confidence.ToString(),
            OccurredAtUtc = result.OccurredAtUtc,
            Revision = result.Revision,
            Status = result.Status.ToString(),
            CreatedAtUtc = result.CreatedAtUtc,
            UpdatedAtUtc = result.UpdatedAtUtc,
            VerifiedAtUtc = result.VerifiedAtUtc,
            PublishedAtUtc = result.PublishedAtUtc,
            Version = result.Version,
            CanBeDistributed = result.CanBeDistributed,
        };
    }

    private static FactualFactValueAdminDto? ToHttp(FactualFactValueAdminResult? value)
    {
        return value is null
            ? null
            : new FactualFactValueAdminDto
            {
                Kind = value.Kind.ToString(),
                CanonicalValue = value.CanonicalValue,
                UnitCode = value.UnitCode,
            };
    }

    private static bool TryParseOptional<TEnum>(string? value, out TEnum? parsed)
        where TEnum : struct, Enum
    {
        parsed = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (!Enum.TryParse(value.Trim(), true, out TEnum candidate)
            || !Enum.IsDefined(candidate))
        {
            return false;
        }

        parsed = candidate;
        return true;
    }
}
