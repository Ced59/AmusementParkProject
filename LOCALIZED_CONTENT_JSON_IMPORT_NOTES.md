# Import JSON des contenus localisés — 1.0.1

## Objectif

Ajout d'une fonctionnalité admin permettant de choisir une entité localisable par recherche lisible, puis d'appliquer un JSON de localisation à cette entité sans manipuler directement la base.

## Architecture

- **WebAPI** : `LocalizedContentController` expose les endpoints admin `admin/localized-content/targets` et `admin/localized-content/{entityType}/{entityId}`, uniquement les contrats HTTP et délègue à des handlers Application.
- **Application** : `Features/LocalizedContent` contient la logique de sélection, de validation JSON, de mapping des champs supportés et d'application métier.
- **Infrastructure** : aucun accès Mongo direct depuis le contrôleur ; les repositories existants restent les ports de persistance. Seul `IParkZoneRepository.GetAllAsync` a été ajouté pour permettre la recherche admin globale des zones.
- **Front Angular** : nouvelle page admin `/:lang/admin/localized-content` avec type d'entité, recherche, sélection explicite, éditeur JSON et feedback.

## Entités et champs localisables couverts

- `park` : `descriptions`
- `parkZone` : `names`, `descriptions`
- `parkItem` : `descriptions`, `accessConditions[].type`, `accessConditions[].customTypeKey`, `accessConditions[].customTypeLabel`, `accessConditions[].value`, `accessConditions[].unit`, `accessConditions[].requiresAccompaniment`, `accessConditions[].minimumCompanionAge`, `accessConditions[].displayOrder`, `accessConditions[].label`, `accessConditions[].description`
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

Pour les conditions d'accès d'une attraction, le JSON peut maintenant créer ou mettre à jour les valeurs métier en plus des textes localisés :

```json
{
  "accessConditions": [
    {
      "type": "MinHeight",
      "value": 105,
      "unit": "Centimeter",
      "displayOrder": 1,
      "label": {
        "fr": "Taille minimale",
        "en": "Minimum height"
      },
      "description": {
        "fr": "Accessible à partir de 105 cm.",
        "en": "Accessible from 105 cm."
      }
    },
    {
      "type": "AdultSupervisionRequired",
      "requiresAccompaniment": true,
      "displayOrder": 2,
      "customTypeLabel": {
        "fr": "Surveillance adulte",
        "en": "Adult supervision"
      },
      "label": {
        "fr": "Accompagnement requis",
        "en": "Accompaniment required"
      }
    }
  ]
}
```

Un `type` inconnu est automatiquement traité comme une condition personnalisée : l'API génère un `customTypeKey` stable à partir de ce type, conserve les textes localisés et crée la condition si elle n'existe pas. Pour cibler explicitement une condition personnalisée déjà créée, utiliser `customTypeKey`.

Le sélecteur de condition peut utiliser `customTypeKey`, `type` et/ou `displayOrder`. Si aucune condition ne correspond mais que le JSON contient un type ou un `customTypeKey`, la condition est créée. Si plusieurs conditions correspondent, l'API refuse la mise à jour et demande un sélecteur plus précis.


## Correction v27

- Alignement de la route API sur le préfixe admin : `admin/localized-content`.
- Construction d'URL front normalisée pour éviter les doubles slashs entre `apiBaseUrl` et endpoint.
- Reprise de l'interface avec les classes partagées `admin-list-card`, `app-field`, `app-input`, `app-select` et `app-button` pour un rendu cohérent avec le back-office existant.


## Évolution v28

- `accessConditions` supporte désormais les valeurs métier : `value`, `unit`, `requiresAccompaniment`, `minimumCompanionAge`, `displayOrder` et `isCustom`.
- Les conditions absentes peuvent être créées par JSON si un `type` ou un `customTypeKey` est fourni.
- Les types inconnus sont transformés en conditions personnalisées avec `customTypeKey` généré, ce qui évite de modifier l'enum pour chaque cas terrain.
- Les conditions personnalisées peuvent exposer un libellé de type via `customTypeLabel`, en plus du `label` propre à la condition.
