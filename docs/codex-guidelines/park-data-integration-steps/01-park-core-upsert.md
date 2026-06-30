# Étape 1 — Infos générales du parc

Objectif : créer ou corriger la fiche parc minimale fiable avant tout enrichissement lourd.

## Lire avant de commencer

- `00-intake-and-export.md`
- `park-data-integration-orchestrator.md`

## Export requis

Utiliser l’export initial ou l’export actualisé fourni par l’utilisateur. Si l’export manque, le demander avant de générer le JSON.

## Données à rechercher

- Nom officiel actuel.
- Anciens noms importants si utiles pour l’histoire, pas forcément dans cette étape.
- Pays, ville, adresse et site officiel.
- Type de parc.
- Statut : en activité, fermé définitivement ou autre statut réellement supporté.
- Date d’ouverture.
- Date de fermeture si le parc est fermé.
- Précisions textuelles si seule l’année ou le mois est fiable.
- Fondateur si fiable.
- Exploitant actuel ou dernier exploitant si le parc est fermé.
- Coordonnées GPS du parc ou de l’entrée principale.
- Logo officiel seulement si l’image est techniquement importable et fiable.

## Références incluses dans cette étape

Cette étape inclut les références nécessaires à la fiche parc. Ne pas créer une étape séparée pour les références.

- Si un `founderKey` est utilisé dans `park`, créer ou corriger la référence dans `references.founders`, sauf si elle existe déjà sûrement dans l’export actualisé.
- Si un `operatorKey` est utilisé dans `park`, créer ou corriger la référence dans `references.operators`, sauf si elle existe déjà sûrement dans l’export actualisé.
- Ne jamais utiliser un UUID, un ID interne ou un nom approximatif comme `founderKey` ou `operatorKey` si l’export ne prouve pas que c’est bien la clé attendue.
- Ne pas ajouter ici les constructeurs liés aux parkItems : ils appartiennent à l’étape 3, ou à l’étape 5 pour l’enrichissement de référence.
- Ne pas rédiger de biographies longues ici sauf besoin minimal de désambiguïsation. Les biographies publiques complètes appartiennent à l’étape 4 ou 5 selon le lot.

## Règles dates

- Utiliser `openingDate` ou `closingDate` seulement avec une date complète fiable au format `YYYY-MM-DD`.
- Si seule l’année ou le mois est fiable, utiliser `openingDateText` ou `closingDateText`.
- Ne pas inventer `01-01` ou le premier jour d’un mois pour rendre une date compatible.
- Pour un parc disparu, conserver la visibilité si le parc est pertinent historiquement, mais garder `adminReviewStatus: "ToReview"` tant que la fiche n’est pas auditée.

## Règles merge et prudence

- Ne pas effacer une donnée existante fiable en mode `merge`.
- Préserver les IDs, rattachements, images, coordonnées et contenus validés.
- Si une correction remplace une donnée existante, expliquer la raison dans `metadata.notes`.
- Ne pas confondre fondateur, exploitant, propriétaire et opérateur historique.
- Ne pas ajouter de tarif, même si la source consultée contient des prix.
- Ne pas inclure de descriptions longues dans cette étape si le parc est dense.

## JSON attendu

Sections possibles :

- `identity`
- `park`
- `references.founders`
- `references.operators`
- `images` pour le logo uniquement si l’URL respecte les règles techniques de l’étape 5

Exemple de forme :

```json
{
  "documentType": "AmusementParkParkGraphUpsert",
  "schemaVersion": "2026-06-30",
  "mode": "merge",
  "metadata": {
    "source": "codex-park-core",
    "targetParkName": "Nom du parc",
    "step": "01-park-core",
    "notes": "Dates vérifiées sur le site officiel et une source historique."
  },
  "identity": {
    "parkId": "id-si-connu",
    "name": "Nom du parc",
    "countryCode": "FR"
  },
  "park": {
    "name": "Nom du parc",
    "countryCode": "FR",
    "type": "ThemePark",
    "status": "Operating",
    "openingDate": "1992-04-12",
    "openingDateText": null,
    "websiteUrl": "https://example.com",
    "city": "Ville",
    "latitude": 48.123456,
    "longitude": 2.123456,
    "isVisible": false,
    "adminReviewStatus": "ToReview"
  }
}
```

## Contrôles avant livraison

- Le parc est pertinent.
- La date complète n’est utilisée que si elle est sûre.
- Les coordonnées pointent sur le parc ou l’entrée principale, pas sur une ville.
- Le fondateur et l’exploitant ne sont pas confondus.
- Les `founderKey` et `operatorKey` utilisés sont résolus dans le même JSON ou déjà présents dans l’export.
- Les descriptions longues ne sont pas forcées dans cette étape si elles risquent de saturer le lot.
- Le parc reste masqué tant que les données publiques ne sont pas prêtes, sauf demande explicite.

## Après Apply

Demander l’export actualisé avant de passer aux zones.

À la fin de la réponse, ajouter `Pertinence de la prochaine étape` pour l’étape 2 — Zones. Si aucune zone officielle ou clairement établie n’existe, indiquer `probablement inutile` avec la raison, puis appliquer la règle de proche en proche de l’orchestrateur jusqu’à la prochaine étape officielle `utile` ou `à décider`. Attendre la décision utilisateur : ne pas passer directement à l’étape 3 sans accord.
