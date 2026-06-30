# Étape 6 — Horaires et événements nommés

Objectif : intégrer les horaires vérifiés et les exceptions datées sans inventer de calendrier ni transformer des périodes génériques en événements éditoriaux.

## Lire avant de commencer

- `park-data-integration-orchestrator.md`
- `park-graph-upsert-enums.md`
- `04-rich-descriptions-localization.md` pour les libellés publics

## Export requis

Utiliser l’export actualisé après les étapes précédentes. Si des images doivent illustrer un événement historique, elles doivent déjà être dans l’export ou créées dans le même JSON.

## Sources à privilégier

- Calendrier officiel du parc.
- Page officielle d’horaires.
- Communiqué officiel pour un événement nommé.
- Source locale fiable si le parc n’a pas de calendrier détaillé.

Pour les horaires actuels ou futurs, vérifier les sources au moment de la génération.

## Ce qui va dans `openingHours`

Utiliser `openingHours` pour :

- règles régulières sur une période ;
- jours d’ouverture ;
- jours de fermeture ;
- horaires d’ouverture et fermeture ;
- admissions tardives si disponibles ;
- exceptions datées ;
- libellés et raisons localisés.

Ne pas mettre ces informations dans les descriptions du parc.

Ne pas inventer un calendrier complet à partir d’une information partielle. Distinguer période d’ouverture, jours d’ouverture, horaires, dernières admissions et fermetures exceptionnelles. Documenter les incertitudes dans `metadata.notes`.

## Libellés visibles dans le calendrier

Les champs `openingHours.regularRules[].labels`, `openingHours.regularRules[].reasons`, `openingHours.dateOverrides[].labels` et `openingHours.dateOverrides[].reasons` alimentent les informations visibles sur les jours du calendrier public.

Ils doivent servir à faire ressortir une information spéciale utile au visiteur :

- événement nommé ;
- nocturne nommée ;
- horaires Halloween, Noël ou festival identifié ;
- fermeture exceptionnelle datée ;
- ouverture exceptionnelle datée ;
- condition temporaire vraiment spécifique à la période affichée.

Ne jamais remplir ces champs avec des commentaires généraux répétés sur tous les jours ou sur une longue période normale. Ces commentaires noient les vrais événements dans le calendrier.

Interdits dans `labels` et `reasons` :

- “horaires officiels actuels” ;
- “horaires officiels des attractions” ;
- “parc ouvert selon le calendrier officiel” ;
- “période renseignée” ;
- “haute saison” ou “saison estivale” sans nom d’événement ;
- commentaire de source, d’audit ou de prudence ;
- information qui serait identique sur presque tous les jours d’ouverture.

Si une règle ne fait que décrire les horaires normaux, laisser `labels` et `reasons` vides. Mettre les remarques de source ou de prudence dans `openingHours.notes` ou `metadata.notes`, pas dans les informations affichées par jour.

Si un libellé apparaît sur de nombreux jours, il doit correspondre à un événement nommé ou à une exception clairement identifiable. Sinon, le retirer.

## Événements nommés

Un événement nommé peut être intégré s’il est clairement identifié par le parc ou une source fiable :

- Halloween ;
- Noël ;
- nocturne nommée ;
- festival nommé ;
- saison ou célébration officielle avec nom propre ;
- fermeture exceptionnelle datée et documentée.

Ne pas créer d’événement pour :

- “ouverture estivale” générique ;
- simple haute saison ;
- week-end prolongé non nommé ;
- variation horaire sans identité publique ;
- promotion tarifaire si les tarifs ne sont pas implémentés.

Les événements nommés liés au calendrier vont d’abord dans `openingHours.labels` ou `openingHours.reasons`. Ils deviennent des événements `history` seulement s’ils ont une valeur durable pour l’histoire du parc.

## Forme des horaires

- `timeZoneId` obligatoire et cohérent avec le pays.
- Dates au format `YYYY-MM-DD`.
- Heures au format `HH:mm`.
- `daysOfWeek` en valeurs anglaises compatibles enum, par exemple `Monday`.
- Les valeurs possibles de `daysOfWeek` sont listées dans `park-graph-upsert-enums.md`.
- `labels` et `reasons` localisés avec les 8 langues quand le libellé est public.
- `lastVerifiedAtUtc` renseigné si la date de vérification est connue.
- Ne pas utiliser les anciens champs singuliers `label` ou `reason` : utiliser toujours `labels` et `reasons` avec `languageCode` et `value`.

## JSON attendu

Section principale : `openingHours`.

```json
{
  "documentType": "AmusementParkParkGraphUpsert",
  "schemaVersion": "2026-06-30",
  "mode": "merge",
  "metadata": {
    "source": "codex-opening-hours",
    "targetParkId": "id-du-parc",
    "targetParkName": "Nom du parc",
    "step": "06-opening-hours",
    "notes": "Horaires vérifiés sur le calendrier officiel le 2026-06-30."
  },
  "identity": {
    "parkId": "id-du-parc",
    "name": "Nom du parc"
  },
  "openingHours": {
    "parkId": "id-du-parc",
    "timeZoneId": "Europe/Paris",
    "sourceUrl": "https://example.com/horaires",
    "notes": "Ne contient pas les tarifs.",
    "lastVerifiedAtUtc": "2026-06-30T00:00:00Z",
    "regularRules": [
      {
        "startDate": "2026-10-01",
        "endDate": "2026-10-31",
        "daysOfWeek": ["Saturday", "Sunday"],
        "isClosed": false,
        "labels": [
          { "languageCode": "fr", "value": "Week-ends d’Halloween" }
        ],
        "reasons": [
          { "languageCode": "fr", "value": "Horaires liés à l’événement Halloween du parc." }
        ],
        "sortOrder": 10,
        "timeRanges": [
          { "opensAt": "10:00", "closesAt": "22:00", "closesNextDay": false }
        ]
      }
    ],
    "dateOverrides": []
  }
}
```

## Contrôles avant livraison

- Aucun tarif n’est ajouté.
- Aucune saison générique n’est transformée en événement historique.
- Les exceptions datées ont une source.
- Les libellés publics sont localisés.
- `labels` et `reasons` ne contiennent que des événements nommés, exceptions datées ou informations temporaires vraiment utiles sur le jour affiché.
- Aucun commentaire général n’est répété sur les jours normaux du calendrier.
- Les `daysOfWeek` utilisent uniquement les valeurs canoniques de `park-graph-upsert-enums.md`.
- Le calendrier n’est pas extrapolé au-delà des sources.
- Les fermetures exceptionnelles ne sont pas confondues avec une fermeture définitive du parc.

## Après Apply

Demander l’export actualisé avant de créer la timeline historique.

À la fin de la réponse, ajouter `Pertinence de la prochaine étape` pour l’étape 7 — Histoire du parc et des parkItems. Si le parc est trop peu documenté pour une timeline fiable, indiquer `probablement inutile` ou `à décider` avec la raison. Si l’étape 7 est `probablement inutile`, appliquer la règle de proche en proche de l’orchestrateur jusqu’à la prochaine étape officielle `utile` ou `à décider`, puis attendre la décision utilisateur.
