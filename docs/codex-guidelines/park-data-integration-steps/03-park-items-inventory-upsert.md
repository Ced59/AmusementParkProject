# Étape 3 — Inventaire des parkItems

Objectif : intégrer tous les contenus visiteurs nommables et fiables, avec dates, statuts et rattachements, sans se limiter aux coasters.

## Lire avant de commencer

- `park-data-integration-orchestrator.md`
- `04-rich-descriptions-localization.md` seulement si des descriptions sont incluses dans ce lot

## Export requis

Utiliser l’export actualisé après les zones. Vérifier les `zone.key`, les IDs existants et les items déjà présents pour éviter les doublons.

## Contenus à rechercher

Inclure quand c’est fiable :

- attractions mécaniques ;
- montagnes russes ;
- dark rides ;
- parcours scéniques ;
- manèges familiaux ;
- attractions aquatiques ;
- restaurants ;
- boutiques ;
- hôtels ;
- parkings ;
- entrées et points d’accès ;
- services visiteurs nommables ;
- spectacles fixes ou lieux de spectacle stables ;
- animaux, enclos ou espaces animaliers nommables ;
- anciens parkItems utiles à l’histoire.

## Découpage recommandé

Pour un grand parc :

- un lot par zone officielle ;
- ou un lot attractions, un lot restauration/boutiques, un lot services/hôtels/parkings ;
- 15 à 30 items maximum par JSON ;
- descriptions longues reportées à l’étape 4.

## Données à renseigner

Pour chaque item :

- `key` stable ;
- `name` ;
- `category` et `type` cohérents avec l’export existant ;
- `zoneKey` si la zone est sûre ;
- `isVisible` pour les éléments confirmés, même fermés ;
- `adminReviewStatus` prudent ;
- `attractionDetails.status` si l’état est connu ;
- `attractionDetails.openingDate` ou `openingDateText` ;
- `attractionDetails.closingDate` ou `closingDateText` ;
- constructeur via `manufacturerKey` si fiable ;
- modèle, source externe, dimensions ou contraintes seulement si les sources sont fiables ;
- coordonnées uniquement si l’emplacement est précis.

## Règles dates et statuts

- Un item fermé mais confirmé reste visible si son intérêt public ou historique est réel.
- Ajouter un statut de fermeture définitive quand il est fiable, et un tag ou une note `closed-definitively` si le modèle ou le lot le prévoit.
- Ne pas utiliser une date complète sans source complète.
- Ne pas confondre annonce, soft opening, ouverture publique et réouverture.
- Pour une attraction déplacée, renseigner l’état dans le parc courant et réserver les autres vies à l’étape histoire.
- Les restrictions d’accès vont dans `accessConditions`, jamais dans la description.
- Ne pas supprimer une attraction fermée simplement parce qu’elle n’existe plus physiquement.
- Utiliser une suppression contrôlée seulement pour un doublon, une erreur ou une entité hors cible déjà identifiée.
- Ne pas inventer un constructeur, un modèle ou une zone à partir d’une supposition.
- Ne pas transformer une information saisonnière en statut permanent.
- Ne pas ajouter de données techniques brutes sans source fiable.

## Références constructeurs

Si un `manufacturerKey` est utilisé, la référence doit être résolue dans le même JSON ou déjà exister sûrement dans l’export.

Ne pas créer un constructeur doublon. Si un constructeur semble déjà présent sous un nom proche, documenter le doute dans `metadata.notes`.

Ne pas créer `Anton Schwarzkopf` si une fiche `Schwarzkopf` doit plutôt être utilisée, renommée ou fusionnée. Ne pas modifier une biographie déjà validée explicitement, notamment Vekoma, sauf demande directe.

## JSON attendu

Sections possibles :

- `references.manufacturers`
- `items`

```json
{
  "documentType": "AmusementParkParkGraphUpsert",
  "schemaVersion": "2026-06-30",
  "mode": "merge",
  "metadata": {
    "source": "codex-park-items",
    "targetParkId": "id-du-parc",
    "targetParkName": "Nom du parc",
    "step": "03-park-items-zone-a",
    "notes": "Lot limité à la zone A. Descriptions longues reportées à l’étape 4."
  },
  "identity": {
    "parkId": "id-du-parc",
    "name": "Nom du parc"
  },
  "references": {
    "manufacturers": [
      {
        "key": "manufacturer-key",
        "name": "Manufacturer Name",
        "isVisible": true,
        "adminReviewStatus": "ToReview"
      }
    ]
  },
  "items": [
    {
      "key": "item-key",
      "name": "Nom de l’item",
      "category": "Attraction",
      "type": "RollerCoaster",
      "zoneKey": "zone-official-name",
      "isVisible": true,
      "adminReviewStatus": "ToReview",
      "attractionDetails": {
        "manufacturerKey": "manufacturer-key",
        "status": "Operating",
        "openingDate": "2001-04-07",
        "sourceUrl": "https://source.example/item"
      }
    }
  ]
}
```

## Contrôles avant livraison

- Aucun doublon évident avec l’export.
- Toutes les `zoneKey` sont résolues.
- Toutes les `manufacturerKey` sont résolues.
- Les dates sont exactes ou restent textuelles.
- Les anciens items importants ne sont pas supprimés.
- Les items sans source fiable restent absents ou `ToReview`.

## Après Apply

Demander l’export actualisé avant de rédiger les descriptions longues.
