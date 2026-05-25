# Import JSON des contenus localisés — 1.0.1

## Objectif

Ajout d'une fonctionnalité admin permettant de choisir une entité localisable par recherche lisible, puis d'appliquer un JSON de localisation à cette entité sans manipuler directement la base.

## Architecture

- **WebAPI** : `LocalizedContentController` expose uniquement les contrats HTTP et délègue à des handlers Application.
- **Application** : `Features/LocalizedContent` contient la logique de sélection, de validation JSON, de mapping des champs supportés et d'application métier.
- **Infrastructure** : aucun accès Mongo direct depuis le contrôleur ; les repositories existants restent les ports de persistance. Seul `IParkZoneRepository.GetAllAsync` a été ajouté pour permettre la recherche admin globale des zones.
- **Front Angular** : nouvelle page admin `/:lang/admin/localized-content` avec type d'entité, recherche, sélection explicite, éditeur JSON et feedback.

## Entités et champs localisables couverts

- `park` : `descriptions`
- `parkZone` : `names`, `descriptions`
- `parkItem` : `descriptions`, `accessConditions[].label`, `accessConditions[].description`
- `parkOperator` : `description`
- `parkFounder` : `biography`
- `attractionManufacturer` : `biography`
- `image` : `altTexts`, `captions`, `credits`
- `imageTag` : `labels`, `descriptions`

## Format JSON

Par défaut, les langues fournies sont fusionnées avec les valeurs existantes. Les langues absentes du JSON sont conservées.

```json
{
  "descriptions": [
    { "languageCode": "fr", "value": "<p>Description française.</p>" },
    { "languageCode": "en", "value": "<p>English description.</p>" }
  ]
}
```

Pour remplacer entièrement un champ localisé :

```json
{
  "replaceExisting": true,
  "descriptions": {
    "fr": "<p>Description française.</p>",
    "en": "<p>English description.</p>"
  }
}
```

Pour les conditions d'accès d'une attraction :

```json
{
  "accessConditions": [
    {
      "type": "MinHeight",
      "label": [
        { "languageCode": "fr", "value": "Taille minimale" },
        { "languageCode": "en", "value": "Minimum height" }
      ],
      "description": [
        { "languageCode": "fr", "value": "Accès soumis à une taille minimale." },
        { "languageCode": "en", "value": "Access is subject to a minimum height." }
      ]
    }
  ]
}
```

Le sélecteur de condition peut utiliser `type` et/ou `displayOrder`.
