# Étape 3 — Inventaire des parkItems

Objectif : intégrer tous les contenus visiteurs nommables et fiables, avec dates, statuts et rattachements, sans se limiter aux coasters.

## Lire avant de commencer

- `park-data-integration-orchestrator.md`
- `park-graph-upsert-enums.md`
- `04-rich-descriptions-localization.md` seulement si des descriptions sont incluses dans ce lot

## Export requis

Utiliser l’export actualisé après les zones. Vérifier les `zone.key`, les `references.manufacturers[].key`, les IDs existants et les items déjà présents pour éviter les doublons et les rattachements non résolus.

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
- `zoneId` pour rattacher une zone déjà exportée, ou `zoneKey` seulement si la zone est créée/redéclarée dans le même JSON ;
- `isVisible` pour les éléments confirmés, même fermés ;
- `adminReviewStatus` prudent ;
- `attractionDetails.status` si l’état est connu ;
- `attractionDetails.openingDate` ou `openingDateText` ;
- `attractionDetails.closingDate` ou `closingDateText` ;
- constructeur via `attractionDetails.manufacturerId` s’il existe déjà dans l’export, ou via `manufacturerKey` seulement si la référence est créée/redéclarée dans le même JSON ;
- modèle, source externe, dimensions ou contraintes seulement si les sources sont fiables ;
- conditions d’accès dans `attractionDetails.accessConditions` si elles sont disponibles ;
- coordonnées uniquement si l’emplacement est précis.

## Conditions d’accès des attractions

Pour chaque attraction, rechercher systématiquement les conditions d’accès publiées par le parc ou une source fiable. Ces données sont importantes et ne doivent pas être oubliées.

Inclure dans `attractionDetails.accessConditions` quand c’est fiable :

- taille minimum ;
- taille minimum avec accompagnement ;
- taille maximum ;
- âge minimum ;
- âge minimum avec accompagnement ;
- restrictions grossesse ;
- restrictions cardiaques ;
- restrictions dos/cou ;
- transfert fauteuil requis ;
- accès spécial ou pass d’accessibilité requis ;
- condition spécifique en `Custom` seulement si aucune enum dédiée ne convient.

Utiliser les types et unités listés dans `park-graph-upsert-enums.md`. Pour une taille, utiliser `Centimeter` ou `Inch` selon la source. Pour un âge, utiliser `Year`. Si la source exprime une condition avec accompagnant, renseigner `requiresAccompaniment` et `minimumCompanionAge` quand l’âge de l’accompagnant est connu.

Ne jamais mettre ces conditions dans les descriptions longues. Si les conditions d’accès ne sont pas trouvées, ne pas les inventer : indiquer dans `metadata.notes` que les conditions n’ont pas été trouvées ou restent à vérifier.

## Règles dates et statuts

- Un item fermé mais confirmé reste visible si son intérêt public ou historique est réel.
- Ajouter un statut de fermeture définitive quand il est fiable, et un tag ou une note `closed-definitively` si le modèle ou le lot le prévoit.
- Utiliser `attractionDetails.openingDate` ou `attractionDetails.closingDate` avec une date complète fiable au format `YYYY-MM-DD`.
- Si seule l’année est fiable, renseigner l’année seule dans le champ date, par exemple `"openingDate": "1988"`. L’import la conserve comme précision textuelle et ne doit jamais l’interpréter comme le 1er janvier.
- Si seul le mois est fiable, utiliser une précision textuelle, par exemple `openingDateText: "mai 1988"` ou `openingDate: "1988-05"` si le mois numérique est sûr.
- Ne pas laisser une date vide si l’année est fiable : l’année seule est une information utile.
- Ne pas utiliser une date complète sans source complète.
- Ne pas confondre annonce, soft opening, ouverture publique et réouverture.
- Pour une attraction déplacée, renseigner l’état dans le parc courant et réserver les autres vies à l’étape histoire.
- Les restrictions d’accès vont dans `accessConditions`, jamais dans la description.
- Ne pas supprimer une attraction fermée simplement parce qu’elle n’existe plus physiquement.
- Utiliser une suppression contrôlée seulement pour un doublon, une erreur ou une entité hors cible déjà identifiée.
- Ne pas inventer un constructeur, un modèle ou une zone à partir d’une supposition.
- Ne pas transformer une information saisonnière en statut permanent.
- Ne pas ajouter de données techniques brutes sans source fiable.

## Règles `zoneKey` et `manufacturerKey`

Avant de livrer un lot de parkItems, faire un contrôle croisé explicite des rattachements :

- lister toutes les valeurs `items[].zoneId` et `items[].zoneKey` utilisées dans le lot ;
- utiliser `zoneId` pour une zone déjà présente dans l’export actualisé ;
- vérifier que chaque `zoneKey` existe dans les `zones[].key` du même JSON ;
- lister toutes les valeurs `attractionDetails.manufacturerKey` utilisées dans les items du lot ;
- utiliser `attractionDetails.manufacturerId` pour un constructeur déjà présent dans l’export actualisé ;
- vérifier que chaque `manufacturerKey` existe dans `references.manufacturers[].key` du même JSON ;
- corriger le fichier avant livraison si une clé manque.

Si une zone fiable est nécessaire mais absente de l’export, ajouter une zone minimale dans le même JSON avec `key`, `name`, `isVisible`, `adminReviewStatus` et, si possible, `displayOrder`. Si la zone existe déjà dans l’export, préférer `zoneId` dans l’item plutôt que `zoneKey` isolé. Si la zone n’est pas fiable, ne pas renseigner de rattachement de zone.

Si un constructeur fiable est nécessaire mais absent de l’export, ajouter un constructeur minimal dans le même JSON avec `key`, `name`, `isVisible` et `adminReviewStatus`. Si le constructeur existe déjà dans l’export, préférer `attractionDetails.manufacturerId` plutôt que `manufacturerKey` isolé. Si le constructeur n’est pas fiable, ne pas renseigner de rattachement constructeur.

Ne jamais livrer un lot d’items qui suppose qu’une zone ou un constructeur sera créé dans un futur JSON. Le Preview doit pouvoir résoudre toutes les clés avec l’export actualisé et le fichier courant seulement.

Une alerte Preview du type `ZoneKey non résolue` ou `ManufacturerKey non résolue` indique une erreur de livrable. Corriger immédiatement le JSON, fournir une version corrigée et ne pas continuer le lot suivant tant que cette alerte existe.

## Références constructeurs

Si un `manufacturerKey` est utilisé, la référence doit être résolue dans le même JSON. Pour réutiliser un constructeur déjà exporté sans redéclarer la référence, utiliser `attractionDetails.manufacturerId`.

`manufacturerKey` doit être une clé de constructeur déclarée dans `references.manufacturers` du même JSON, pas un UUID deviné ni un ID interne copié sans preuve. Si l’export montre un constructeur existant, utiliser son `manufacturerId`; si la référence doit être créée ou redéclarée dans le lot, créer une entrée minimale dans `references.manufacturers` avec une `key` stable et réutiliser exactement cette même valeur dans `attractionDetails.manufacturerKey`.

Avant de livrer le fichier JSON, faire un contrôle croisé simple :

- lister toutes les valeurs `attractionDetails.manufacturerKey` utilisées dans les items du lot ;
- vérifier que chaque valeur existe dans `references.manufacturers[].key` du même JSON ;
- corriger le fichier si une valeur manque.

Une alerte Preview du type `ManufacturerKey non résolue` indique une erreur de livrable. Ne pas demander à l’utilisateur de l’appliquer quand même : corriger le JSON et fournir un nouveau fichier téléchargeable.

Ne pas créer un constructeur doublon. Si un constructeur semble déjà présent sous un nom proche, documenter le doute dans `metadata.notes`.

Ne pas créer `Anton Schwarzkopf` si une fiche `Schwarzkopf` doit plutôt être utilisée, renommée ou fusionnée. Ne pas modifier une biographie déjà validée explicitement, notamment Vekoma, sauf demande directe.

Ne pas créer une étape séparée pour les constructeurs. Les références minimales de constructeurs nécessaires aux parkItems appartiennent à cette étape. Les biographies, images et enrichissements plus longs appartiennent à l’étape 5 ou à un lot de descriptions prévu par l’étape 4.

## JSON attendu

Sections possibles :

- `zones`
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
  "zones": [
    {
      "key": "zone-official-name",
      "name": "Zone officielle",
      "isVisible": true,
      "adminReviewStatus": "ToReview",
      "displayOrder": 1
    }
  ],
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
      "zoneId": "zone-id-from-export-or-null",
      "isVisible": true,
      "adminReviewStatus": "ToReview",
      "attractionDetails": {
        "manufacturerKey": "manufacturer-key",
        "status": "Operating",
        "openingDate": "2001-04-07",
        "accessConditions": [
          {
            "type": "MinHeight",
            "value": 120,
            "unit": "Centimeter",
            "displayOrder": 1
          }
        ],
        "sourceUrl": "https://source.example/item"
      }
    }
  ]
}
```

## Contrôles avant livraison

- Aucun doublon évident avec l’export.
- Tous les `zoneId` réutilisés proviennent de l’export actualisé, et toutes les `zoneKey` sont résolues par `zones` dans le même JSON.
- Tous les `manufacturerId` réutilisés proviennent de l’export actualisé, et toutes les `manufacturerKey` sont résolues par `references.manufacturers` dans le même JSON.
- Les conditions d’accès trouvées sont dans `attractionDetails.accessConditions`, pas dans les descriptions.
- Toutes les valeurs enum utilisées sont listées dans `park-graph-upsert-enums.md`.
- Les dates sont exactes ou restent textuelles ; aucune année seule n’est transformée en date complète inventée.
- Les anciens items importants ne sont pas supprimés.
- Les items sans source fiable restent absents ou `ToReview`.

## Récap avant livraison

Avant le fichier JSON, résumer systématiquement :

- ce qui est ajouté, corrigé, masqué et conservé ;
- les parkItems inclus dans le lot, avec leur type principal quand le lot porte sur l’inventaire ;
- le compteur d’avancement de l’étape 3, par exemple `parkItems : 30 / 147` et `attractions uniquement : 30 / 49` quand ces totaux sont connus ;
- le reste à traiter avant l’étape 4, notamment les lots d’items non encore intégrés ;
- les sources principales et les limites du lot.

Si un JSON est une correction d’un lot déjà livré, le récap doit préciser les seules modifications structurelles apportées, par exemple ajout de zones minimales, ajout de constructeurs minimaux, retrait d’un `zoneKey` incertain ou correction d’une clé.

## Après Apply

Demander l’export actualisé avant de rédiger les descriptions longues.

À la fin de la réponse, ajouter `Pertinence de la prochaine étape` pour l’étape 4 — Descriptions longues localisées. Si le parc est très mineur ou trop peu documenté pour des textes longs, indiquer `à décider` ou `probablement inutile` avec la raison. Si l’étape 4 est `probablement inutile`, appliquer la règle de proche en proche de l’orchestrateur jusqu’à la prochaine étape officielle `utile` ou `à décider`, puis attendre la décision utilisateur.
