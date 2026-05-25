using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionAccessConditionTypes.Commands;
using AmusementPark.Application.Features.AttractionAccessConditionTypes.Contracts;
using AmusementPark.Application.Features.AttractionAccessConditionTypes.Ports;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.AttractionAccessConditionTypes.Handlers;

public sealed class UpsertAttractionAccessConditionTypeDefinitionCommandHandler
    : ICommandHandler<UpsertAttractionAccessConditionTypeDefinitionCommand, ApplicationResult<AttractionAccessConditionTypeDefinition>>
{
    private readonly IAttractionAccessConditionTypeDefinitionRepository repository;

    public UpsertAttractionAccessConditionTypeDefinitionCommandHandler(IAttractionAccessConditionTypeDefinitionRepository repository)
    {
        this.repository = repository;
    }

    public async Task<ApplicationResult<AttractionAccessConditionTypeDefinition>> HandleAsync(
        UpsertAttractionAccessConditionTypeDefinitionCommand command,
        CancellationToken cancellationToken = default)
    {
        AttractionAccessConditionTypeDefinitionWriteModel normalized = Normalize(command.TypeDefinition);
        if (string.IsNullOrWhiteSpace(normalized.Key))
        {
            return ApplicationResult<AttractionAccessConditionTypeDefinition>.Failure(AttractionAccessConditionTypeApplicationErrors.InvalidKey());
        }

        if (normalized.Labels.Count == 0)
        {
            return ApplicationResult<AttractionAccessConditionTypeDefinition>.Failure(AttractionAccessConditionTypeApplicationErrors.MissingLabels());
        }

        AttractionAccessConditionTypeDefinition value = await this.repository.UpsertAsync(normalized, cancellationToken);
        return ApplicationResult<AttractionAccessConditionTypeDefinition>.Success(value);
    }

    private static AttractionAccessConditionTypeDefinitionWriteModel Normalize(AttractionAccessConditionTypeDefinitionWriteModel model)
    {
        return new AttractionAccessConditionTypeDefinitionWriteModel
        {
            Key = AttractionAccessConditionTypeKeyNormalizer.Normalize(model.Key),
            LegacyType = model.LegacyType,
            IsSystem = model.IsSystem,
            IsActive = model.IsActive,
            SortOrder = model.SortOrder,
            Labels = NormalizeLocalizedValues(model.Labels),
            Descriptions = NormalizeLocalizedValues(model.Descriptions),
        };
    }

    private static IReadOnlyCollection<LocalizedTextValue> NormalizeLocalizedValues(IEnumerable<LocalizedTextValue>? values)
    {
        Dictionary<string, LocalizedTextValue> normalized = new Dictionary<string, LocalizedTextValue>(StringComparer.OrdinalIgnoreCase);
        foreach (LocalizedTextValue value in values ?? Array.Empty<LocalizedTextValue>())
        {
            if (string.IsNullOrWhiteSpace(value.LanguageCode) || string.IsNullOrWhiteSpace(value.Value))
            {
                continue;
            }

            string languageCode = value.LanguageCode.Trim().ToLowerInvariant();
            normalized[languageCode] = new LocalizedTextValue(languageCode, value.Value.Trim());
        }

        return normalized.Values.OrderBy(static value => value.LanguageCode, StringComparer.OrdinalIgnoreCase).ToList();
    }
}
