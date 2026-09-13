using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AmusementPark.WebAPI.Contracts.DataSources;

public sealed class DataSourceComparisonItemDto
{
    public string Id { get; set; } = string.Empty;

    public string EntityType { get; set; } = string.Empty;

    public string ChangeType { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string? LocalEntityId { get; set; }

    public string? ExternalEntityId { get; set; }

    public string MatchConfidence { get; set; } = string.Empty;

    public bool IsApplied { get; set; }

    public bool HasExternalDuplicates { get; set; }

    public bool RequiresManualResolution { get; set; }

    public string ResolutionStatus { get; set; } = string.Empty;

    public string? AppliedExternalVariantId { get; set; }

    public IReadOnlyCollection<DataSourceComparisonFieldChangeDto> Changes { get; set; } = Array.Empty<DataSourceComparisonFieldChangeDto>();

    public IReadOnlyCollection<DataSourceComparisonVariantDto> ExternalVariants { get; set; } = Array.Empty<DataSourceComparisonVariantDto>();
}
