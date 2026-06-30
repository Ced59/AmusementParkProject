# Étape 2 — Zones officielles

Objectif : structurer le parc avec ses zones officielles ou clairement établies avant de rattacher les parkItems.

## Lire avant de commencer

- `park-data-integration-orchestrator.md`
- `04-rich-descriptions-localization.md` si des descriptions de zones sont rédigées

## Export requis

Utiliser l’export actualisé après l’étape 1. Ne pas continuer avec l’export initial si le parc vient d’être créé ou corrigé.

## Données à rechercher

- Plan officiel du parc.
- Pages officielles des zones.
- Noms localisés officiels s’ils existent.
- Ordre de visite ou ordre de présentation stable.
- Coordonnées approximatives seulement si la zone est clairement localisable.
- Attractions ou points d’intérêt qui confirment l’existence de la zone.

## Règles zones

- Créer seulement des zones officielles ou très clairement établies.
- Ne pas créer de zone à partir d’un événement saisonnier.
- Ne pas créer de zone à partir d’une catégorie inventée comme “zone restaurants” ou “zone familiale” si le parc ne l’emploie pas.
- Ne pas placer un hôtel dans une zone sauf rattachement officiel.
- Si le parc n’a pas de zones publiques fiables, ne pas inventer de structure : passer à l’inventaire sans `zoneKey`.
- Ne pas déduire une zone d’une localisation approximative.
- Ne pas renommer une zone existante sans source claire.
- Garder les zones fermées ou historiques si elles sont utiles à l’histoire du parc et documentées.

## JSON attendu

Section principale : `zones`.

Chaque zone doit avoir une `key` stable, un `name`, des `names` localisés si possible, `isVisible`, `sortOrder` et, si le lot le permet, des `descriptions`.

```json
{
  "documentType": "AmusementParkParkGraphUpsert",
  "schemaVersion": "2026-06-30",
  "mode": "merge",
  "metadata": {
    "source": "codex-park-zones",
    "targetParkId": "id-du-parc",
    "targetParkName": "Nom du parc",
    "step": "02-zones",
    "notes": "Zones reprises depuis le plan officiel."
  },
  "identity": {
    "parkId": "id-du-parc",
    "name": "Nom du parc"
  },
  "zones": [
    {
      "key": "zone-official-name",
      "name": "Official Name",
      "names": [
        { "languageCode": "fr", "value": "Nom français" },
        { "languageCode": "en", "value": "English name" }
      ],
      "isVisible": true,
      "sortOrder": 10
    }
  ]
}
```

## Contrôles avant livraison

- Chaque `zone.key` est unique et lisible.
- Les noms ne traduisent pas artificiellement une marque ou un nom propre.
- Les zones présentes dans l’export sont préservées sauf correction claire.
- Les futurs `items[].zoneKey` pourront correspondre exactement aux clés créées.
- Les descriptions de zones, si présentes, respectent la charte publique.

## Après Apply

Demander l’export actualisé avant de créer les parkItems, afin de reprendre les vrais IDs et les clés acceptées.
