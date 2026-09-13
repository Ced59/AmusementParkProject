using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AmusementPark.WebAPI.Contracts.DataSources;

public sealed class DataSourceComparisonVariantDto
{
    public string ExternalVariantId { get; set; } = string.Empty;

    public string DisplayLabel { get; set; } = string.Empty;

    public string? CandidateLocalEntityId { get; set; }

    public string? SourceUrl { get; set; }

    public bool IsSuggested { get; set; }

    public IReadOnlyCollection<DataSourceComparisonFieldChangeDto> Changes { get; set; } = Array.Empty<DataSourceComparisonFieldChangeDto>();
}
