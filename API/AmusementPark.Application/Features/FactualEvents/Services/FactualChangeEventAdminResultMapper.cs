using AmusementPark.Application.Features.FactualEvents.Results;
using AmusementPark.Core.Domain.FactualEvents;

namespace AmusementPark.Application.Features.FactualEvents.Services;

public static class FactualChangeEventAdminResultMapper
{
    public static FactualChangeEventAdminResult Map(
        FactualChangeEvent factualEvent,
        IReadOnlyDictionary<string, string?> parkNames,
        IReadOnlyDictionary<string, string?> parkItemNames)
    {
        ArgumentNullException.ThrowIfNull(factualEvent);
        ArgumentNullException.ThrowIfNull(parkNames);
        ArgumentNullException.ThrowIfNull(parkItemNames);

        string? targetName = factualEvent.Target.Type == FactualTargetType.Park
            ? FindName(parkNames, factualEvent.Target.TargetId)
            : FindName(parkItemNames, factualEvent.Target.TargetId);
        string? parentParkName = factualEvent.Target.ParentParkId is null
            ? null
            : FindName(parkNames, factualEvent.Target.ParentParkId);
        return new FactualChangeEventAdminResult(
            factualEvent.Id.Value,
            factualEvent.Type,
            factualEvent.DefinitionVersion,
            new FactualChangeTargetAdminResult(
                factualEvent.Target.Type,
                factualEvent.Target.TargetId,
                targetName,
                factualEvent.Target.ParentParkId,
                parentParkName),
            Map(factualEvent.PreviousValue),
            Map(factualEvent.NewValue),
            new FactualSourceReferenceAdminResult(
                factualEvent.Source.Type,
                factualEvent.Source.PublisherName,
                factualEvent.Source.Title,
                factualEvent.Source.Url,
                factualEvent.Source.PublishedAtUtc),
            factualEvent.Confidence,
            factualEvent.OccurredAtUtc,
            factualEvent.Revision,
            factualEvent.Status,
            factualEvent.CreatedAtUtc,
            factualEvent.UpdatedAtUtc,
            factualEvent.VerifiedAtUtc,
            factualEvent.PublishedAtUtc,
            factualEvent.TerminalAtUtc,
            factualEvent.SupersededByEventId?.Value,
            factualEvent.ReasonCode,
            factualEvent.Version,
            factualEvent.CanBeDistributed);
    }

    private static FactualFactValueAdminResult? Map(FactValue? value)
    {
        return value is null
            ? null
            : new FactualFactValueAdminResult(
                value.Kind,
                value.CanonicalValue,
                value.UnitCode);
    }

    private static string? FindName(
        IReadOnlyDictionary<string, string?> names,
        string id)
    {
        return names.TryGetValue(id, out string? name) && !string.IsNullOrWhiteSpace(name)
            ? name.Trim()
            : null;
    }
}
