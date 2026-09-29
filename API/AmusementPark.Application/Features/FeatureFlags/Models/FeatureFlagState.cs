namespace AmusementPark.Application.Features.FeatureFlags.Models;

public sealed record FeatureFlagState(
    Guid Id,
    string Key,
    string Environment,
    bool? EnabledOverride,
    int Revision,
    int? SupersedesRevision,
    string ChangedByUserId,
    string Reason,
    DateTime RecordedAtUtc);
