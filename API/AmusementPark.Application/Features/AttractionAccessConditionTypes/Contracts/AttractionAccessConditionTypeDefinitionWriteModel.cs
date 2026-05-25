using AmusementPark.Application.Common.Contracts;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.AttractionAccessConditionTypes.Contracts;

/// <summary>
/// Modèle d'écriture d'un type réutilisable de condition d'accès.
/// </summary>
public sealed class AttractionAccessConditionTypeDefinitionWriteModel
{
    public string Key { get; set; } = string.Empty;
    public AttractionAccessConditionType LegacyType { get; set; } = AttractionAccessConditionType.Custom;
    public bool IsSystem { get; set; }
    public bool IsActive { get; set; } = true;
    public IReadOnlyCollection<LocalizedTextValue> Labels { get; set; } = Array.Empty<LocalizedTextValue>();
    public IReadOnlyCollection<LocalizedTextValue> Descriptions { get; set; } = Array.Empty<LocalizedTextValue>();
    public int SortOrder { get; set; }
}
