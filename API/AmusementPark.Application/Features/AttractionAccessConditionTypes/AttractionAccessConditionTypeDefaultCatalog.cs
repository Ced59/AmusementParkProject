using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Features.AttractionAccessConditionTypes.Contracts;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.AttractionAccessConditionTypes;

/// <summary>
/// Catalogue système minimal des types historiques de conditions d'accès.
/// </summary>
public static class AttractionAccessConditionTypeDefaultCatalog
{
    public static IReadOnlyCollection<AttractionAccessConditionTypeDefinitionWriteModel> BuildSystemDefinitions()
    {
        return new[]
        {
            System("min-height", AttractionAccessConditionType.MinHeight, 10, "Taille minimale", "Minimum height", "Altura mínima", "Mindestgröße", "Altezza minima", "Minimalny wzrost", "Minimumlengte", "Altura mínima"),
            System("min-height-accompanied", AttractionAccessConditionType.MinHeightAccompanied, 20, "Taille minimale accompagné", "Minimum height with accompaniment", "Altura mínima acompañado", "Mindestgröße in Begleitung", "Altezza minima con accompagnatore", "Minimalny wzrost z opiekunem", "Minimumlengte met begeleiding", "Altura mínima acompanhado"),
            System("max-height", AttractionAccessConditionType.MaxHeight, 30, "Taille maximale", "Maximum height", "Altura máxima", "Maximalgröße", "Altezza massima", "Maksymalny wzrost", "Maximumlengte", "Altura máxima"),
            System("min-age", AttractionAccessConditionType.MinAge, 40, "Âge minimum", "Minimum age", "Edad mínima", "Mindestalter", "Età minima", "Minimalny wiek", "Minimumleeftijd", "Idade mínima"),
            System("min-age-accompanied", AttractionAccessConditionType.MinAgeAccompanied, 50, "Âge minimum accompagné", "Minimum age with accompaniment", "Edad mínima acompañado", "Mindestalter in Begleitung", "Età minima con accompagnatore", "Minimalny wiek z opiekunem", "Minimumleeftijd met begeleiding", "Idade mínima acompanhado"),
            System("pregnancy-restriction", AttractionAccessConditionType.PregnancyRestriction, 60, "Restriction grossesse", "Pregnancy restriction", "Restricción embarazo", "Einschränkung Schwangerschaft", "Restrizione gravidanza", "Ograniczenie ciąży", "Zwangerschapsbeperking", "Restrição gravidez"),
            System("heart-restriction", AttractionAccessConditionType.HeartRestriction, 70, "Restriction cardiaque", "Heart condition restriction", "Restricción cardíaca", "Einschränkung Herzprobleme", "Restrizione cardiaca", "Ograniczenie choroby serca", "Hartbeperking", "Restrição cardíaca"),
            System("back-neck-restriction", AttractionAccessConditionType.BackNeckRestriction, 80, "Restriction dos / cou", "Back or neck restriction", "Restricción espalda / cuello", "Einschränkung Rücken / Nacken", "Restrizione schiena / collo", "Ograniczenie plecy / szyja", "Rug / nek beperking", "Restrição costas / pescoço"),
            System("wheelchair-transfer-required", AttractionAccessConditionType.WheelchairTransferRequired, 90, "Transfert fauteuil requis", "Wheelchair transfer required", "Transferencia de silla requerida", "Rollstuhltransfer erforderlich", "Trasferimento carrozzina richiesto", "Wymagany transfer z wózka", "Rolstoeltransfer vereist", "Transferência de cadeira necessária"),
            System("access-pass-required", AttractionAccessConditionType.AccessPassRequired, 100, "Access pass requis", "Access pass required", "Access pass requerido", "Access Pass erforderlich", "Access pass richiesto", "Wymagany access pass", "Access pass vereist", "Access pass obrigatório"),
        };
    }

    public static IReadOnlyCollection<LocalizedTextValue> FallbackLabels(string key, string? rawName)
    {
        string label = string.IsNullOrWhiteSpace(rawName) ? key : rawName.Trim();
        return new[]
        {
            new LocalizedTextValue("fr", label),
            new LocalizedTextValue("en", label),
        };
    }

    private static AttractionAccessConditionTypeDefinitionWriteModel System(
        string key,
        AttractionAccessConditionType legacyType,
        int sortOrder,
        string fr,
        string en,
        string es,
        string de,
        string it,
        string pl,
        string nl,
        string pt)
    {
        return new AttractionAccessConditionTypeDefinitionWriteModel
        {
            Key = key,
            LegacyType = legacyType,
            IsSystem = true,
            IsActive = true,
            SortOrder = sortOrder,
            Labels = new[]
            {
                new LocalizedTextValue("fr", fr),
                new LocalizedTextValue("en", en),
                new LocalizedTextValue("es", es),
                new LocalizedTextValue("de", de),
                new LocalizedTextValue("it", it),
                new LocalizedTextValue("pl", pl),
                new LocalizedTextValue("nl", nl),
                new LocalizedTextValue("pt", pt),
            },
        };
    }
}
