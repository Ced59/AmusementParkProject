using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.ParkItems;

/// <summary>
/// Contrainte d'accès HTTP d'une attraction.
/// </summary>
public sealed class AttractionAccessConditionDto : IValidatableObject
{
    public AttractionAccessConditionTypeDto Type { get; set; }

    public string? TypeKey { get; set; }

    public bool? IsCustom { get; set; }

    public string? CustomTypeKey { get; set; }

    public List<LocalizedTextDto>? CustomTypeLabel { get; set; }

    public double? Value { get; set; }

    public AttractionAccessConditionUnitDto? Unit { get; set; }

    public bool? RequiresAccompaniment { get; set; }

    public int? MinimumCompanionAge { get; set; }

    public List<LocalizedTextDto>? Label { get; set; }

    public List<LocalizedTextDto>? Description { get; set; }

    public int? DisplayOrder { get; set; }

    [Required]
    [Range(1, 1)]
    public int? ProvenanceSchemaVersion { get; set; }

    public AttractionAccessConditionSourceKindDto SourceKind { get; set; }

    public string? SourceUrl { get; set; }

    public string? SourceReference { get; set; }

    public DateTime? CollectedAtUtc { get; set; }

    public DateTime? VerifiedAtUtc { get; set; }

    public string? SourceLanguageCode { get; set; }

    public List<LocalizedTextDto>? SourceSummary { get; set; }

    public AttractionAccessConditionConfidenceDto SourceConfidence { get; set; }

    public AttractionAccessConditionScopeDto Scope { get; set; }

    public string? ScopeDetail { get; set; }

    public DateOnly? EffectiveFrom { get; set; }

    public DateOnly? EffectiveTo { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (this.CollectedAtUtc.HasValue && this.CollectedAtUtc.Value.Kind == DateTimeKind.Unspecified)
        {
            yield return new ValidationResult(
                "CollectedAtUtc must include a timezone offset.",
                new[] { nameof(this.CollectedAtUtc) });
        }

        if (this.VerifiedAtUtc.HasValue && this.VerifiedAtUtc.Value.Kind == DateTimeKind.Unspecified)
        {
            yield return new ValidationResult(
                "VerifiedAtUtc must include a timezone offset.",
                new[] { nameof(this.VerifiedAtUtc) });
        }

        if (!Enum.IsDefined(this.SourceKind))
        {
            yield return new ValidationResult(
                "SourceKind is invalid.",
                new[] { nameof(this.SourceKind) });
        }

        if (!Enum.IsDefined(this.SourceConfidence))
        {
            yield return new ValidationResult(
                "SourceConfidence is invalid.",
                new[] { nameof(this.SourceConfidence) });
        }

        if (!Enum.IsDefined(this.Scope))
        {
            yield return new ValidationResult(
                "Scope is invalid.",
                new[] { nameof(this.Scope) });
        }
    }
}
