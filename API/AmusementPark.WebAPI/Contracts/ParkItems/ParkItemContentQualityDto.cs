using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.ParkItems;

public sealed class ParkItemContentQualityDto
{
    public bool StructureComplete { get; set; }

    public bool HasAnyDescription { get; set; }

    public bool HasFrenchDescription { get; set; }

    public bool HasEnglishDescription { get; set; }

    public bool HasZone { get; set; }

    public bool HasPreciseType { get; set; }

    public bool HasLocation { get; set; }

    public bool HasAccessConditions { get; set; }

    public bool IsPublishable { get; set; }

    public IReadOnlyCollection<string> AvailableLanguageCodes { get; set; } = Array.Empty<string>();

    public IReadOnlyCollection<string> MissingRequirementKeys { get; set; } = Array.Empty<string>();
}
