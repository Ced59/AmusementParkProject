# Étape 4 — Descriptions longues localisées

Objectif : produire les descriptions publiques longues, naturelles et utiles dans les 8 langues, sans saturer le contexte.

## Lire avant de commencer

- `description-guidelines-r2.md`
- `park-graph-upsert-json-guideline-r10.md`

## Export requis

Utiliser l’export actualisé après l’inventaire des parkItems. Les descriptions doivent viser les IDs, noms, zones et statuts réellement présents.

## Langues attendues

Les 8 langues publiques sont :

- `fr`
- `en`
- `de`
- `nl`
- `it`
- `es`
- `pl`
- `pt`

Reprendre les codes de langue présents dans l’export si l’état existant diffère.

## Découpage anti-saturation

Ordre recommandé :

1. Description du parc seul.
2. Descriptions des zones.
3. ParkItems majeurs par lots de 5 à 8.
4. ParkItems secondaires par lots de 8 à 12.
5. Restaurants, boutiques, services, parkings et hôtels par familles.

Ne pas rédiger toutes les descriptions d’un grand parc en une seule réponse.

## Niveau de longueur

- Parc majeur : description riche, structurée en plusieurs paragraphes courts.
- Parc local : description plus courte, mais spécifique et utile.
- Zone : ambiance, rôle dans la visite, repères concrets.
- Attraction : expérience observée, rythme, ambiance, place dans le parc.
- Restaurant, boutique, service : utilité réelle et identité visible, sans phrase vide.
- Référence : biographie réutilisable et non centrée uniquement sur le parc du lot.

## Règles rédactionnelles à préserver

Ne pas modifier la charte pour rendre les textes techniques. Les descriptions restent publiques, naturelles et user friendly.

Interdit dans les descriptions :

- restrictions d’accès ;
- tailles ;
- tarifs ;
- horaires ;
- dates d’ouverture ;
- coordonnées GPS ;
- notes de complétude ;
- jargon admin ;
- explication d’upsert, de SEO ou de base de données.

## JSON attendu

Sections possibles :

- `park.descriptions`
- `zones[].descriptions`
- `items[].descriptions`
- `references.*.biography` ou `references.operators[].description` si le lot cible des références

```json
{
  "documentType": "AmusementParkParkGraphUpsert",
  "schemaVersion": "2026-06-30",
  "mode": "merge",
  "metadata": {
    "source": "codex-rich-descriptions",
    "targetParkId": "id-du-parc",
    "targetParkName": "Nom du parc",
    "step": "04-descriptions-items-lot-1",
    "notes": "Lot limité à 6 attractions majeures."
  },
  "identity": {
    "parkId": "id-du-parc",
    "name": "Nom du parc"
  },
  "items": [
    {
      "id": "id-item-exporte",
      "key": "item-key",
      "name": "Nom de l’item",
      "descriptions": [
        { "languageCode": "fr", "value": "<p>Description française naturelle.</p>" },
        { "languageCode": "en", "value": "<p>Natural English description.</p>" }
      ]
    }
  ]
}
```

## Contrôles avant livraison

- Les 8 langues sont présentes pour chaque entité du lot, sauf décision explicitement documentée.
- Les traductions sont naturelles, pas mot à mot.
- Le français public utilise un ton direct et informel quand le contexte s’y prête.
- Aucun texte ne réemploie mécaniquement la même structure d’un item à l’autre.
- Le HTML reste simple : `<p>`, `<h2>`, `<h3>`, `<ul>`, `<li>`, `<strong>` si utile.
- Aucune information structurée ne pollue la narration.

## Après Apply

Demander l’export actualisé avant le lot de descriptions suivant ou avant les images.
