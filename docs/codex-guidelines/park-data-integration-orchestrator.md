# AmusementPark — Orchestrateur d’intégration des données d’un parc

Version : **2026-06-30-r1**  
Projet : **amusement-parks.fun**  
Usage : fichier d’entrée à donner à ChatGPT/Codex pour intégrer progressivement les données d’un parc avec des JSON Park Graph Upsert.

Cet orchestrateur sert à éviter les JSON trop gros, les oublis de cohérence et les réponses qui saturent à cause d’un parc trop riche. Il ne remplace pas les règles détaillées : il indique quoi lire, dans quel ordre, et quand s’arrêter pour demander un export actualisé.

## Règle de contexte obligatoire

Avant l’étape 0, l’utilisateur fournit l’export actuel du parc s’il existe déjà, ou confirme qu’il faut créer le parc depuis zéro.

Avant chaque nouvelle étape, l’utilisateur fournit l’export actualisé après Preview/Apply de l’étape précédente. Sans cet export actualisé, ne pas continuer : demander l’export plutôt que produire un JSON basé sur un état ancien.

Chaque réponse doit produire un seul livrable principal :

- soit une analyse de blocage ;
- soit un JSON upsert borné pour l’étape en cours ;
- soit une checklist de validation si l’étape ne nécessite pas encore de JSON.

## Règles anti-saturation

- Traiter une seule étape à la fois.
- Ne pas copier l’export complet dans le JSON upsert : ne fournir que les sections modifiées.
- Pour un grand parc, découper les parkItems par zone, par famille ou par lot de 15 à 30 items.
- Pour les descriptions longues, découper les lots encore plus finement : parc seul, puis zones, puis 5 à 12 parkItems maximum selon la longueur.
- Pour l’histoire, séparer la timeline du parc, puis les timelines des parkItems majeurs, puis les articles longs.
- Ne jamais mélanger horaires, inventaire d’items, descriptions longues, images et histoire détaillée dans un même JSON si le parc est dense.
- Conserver une section `metadata.notes` claire avec les incertitudes, les sources faibles et les décisions de prudence.

## Structure JSON commune

Utiliser le mode `merge` sauf demande contraire. Sélectionner aussi le parc cible dans l’écran admin quand il existe.

```json
{
  "documentType": "AmusementParkParkGraphUpsert",
  "schemaVersion": "2026-06-30",
  "mode": "merge",
  "metadata": {
    "source": "codex-park-data-integration",
    "targetParkId": "export-park-id-if-known",
    "targetParkName": "Nom du parc",
    "step": "01-park-core",
    "notes": "Résumé court des sources, limites et choix de prudence."
  },
  "identity": {
    "parkId": "export-park-id-if-known",
    "name": "Nom du parc",
    "countryCode": "FR"
  }
}
```

Ajouter seulement les sections utiles à l’étape : `references`, `park`, `zones`, `items`, `images`, `openingHours`, `history`.

Les textes localisés des upserts actuels utilisent les codes courts présents dans les exports : `fr`, `en`, `de`, `nl`, `it`, `es`, `pl`, `pt`. Si un export existant utilise une autre forme, garder la forme déjà présente.

## Parcours recommandé

### Étape 0 — Cadrage et export

Lire `park-data-integration-steps/00-intake-and-export.md`.

Objectif : décider si le parc est pertinent, s’il est majeur, s’il existe déjà, quelles sources sont acceptables, et comment découper le travail.

Sortie attendue : une décision de pertinence et un plan de lots. Pas de JSON massif.

### Étape 1 — Infos générales du parc

Lire `park-data-integration-steps/01-park-core-upsert.md`.

Objectif : créer ou corriger le parc avec ses données stables : nom, pays, type, statut, dates d’ouverture et de fermeture, adresse, coordonnées, site officiel, fondateur, exploitant et visibilité prudente.

Sortie attendue : un JSON upsert centré sur `park` et, si nécessaire, `references.founders` ou `references.operators`.

### Étape 2 — Zones

Lire `park-data-integration-steps/02-zones-upsert.md`.

Objectif : ajouter uniquement les zones officielles ou clairement établies, avec noms localisés, ordre de visite et descriptions si la taille du lot le permet.

Sortie attendue : un JSON upsert centré sur `zones`.

### Étape 3 — Inventaire des parkItems

Lire `park-data-integration-steps/03-park-items-inventory-upsert.md`.

Objectif : intégrer les attractions, restaurants, boutiques, hôtels, services, parkings, entrées, spectacles fixes, animaux/enclos et autres éléments nommables, avec dates et statuts quand ils sont fiables.

Sortie attendue : un ou plusieurs JSON upsert centrés sur `items` et `references.manufacturers`. Les longues descriptions peuvent être reportées à l’étape 4 pour éviter la saturation.

### Étape 4 — Descriptions longues localisées

Lire `park-data-integration-steps/04-rich-descriptions-localization.md`.

Objectif : produire les descriptions longues du parc, des zones et des parkItems dans les 8 langues, avec un style public naturel, spécifique et non technique.

Sortie attendue : plusieurs JSON upsert bornés par lot de descriptions.

### Étape 5 — Images et références

Lire `park-data-integration-steps/05-images-and-reference-enrichment.md`.

Objectif : enrichir logos, images du parc, images d’items et biographies de fondateurs, exploitants ou constructeurs, sans lien image indirect.

Sortie attendue : JSON upsert avec `images` et/ou `references`.

### Étape 6 — Horaires et événements nommés

Lire `park-data-integration-steps/06-opening-hours-and-named-events.md`.

Objectif : intégrer les horaires vérifiés et les exceptions datées. Les événements nommés comme Halloween peuvent apparaître dans les libellés ou raisons localisés, mais les périodes génériques comme une ouverture estivale ne doivent pas devenir des événements éditoriaux artificiels.

Sortie attendue : JSON upsert centré sur `openingHours`, et éventuellement quelques événements `history` seulement s’ils ont une vraie valeur durable.

### Étape 7 — Histoire du parc et des parkItems

Lire `park-data-integration-steps/07-history-timelines-and-articles.md`.

Objectif : créer la timeline du parc, puis les timelines des parkItems importants, avec articles seulement quand le sujet le mérite.

Sortie attendue : JSON upsert centré sur `history.events`, en plusieurs lots.

### Étape 8 — Audit final

Lire `park-data-integration-steps/08-final-audit-and-publication.md`.

Objectif : vérifier cohérence, sources, localisations, références, images, statut de visibilité, SEO public et absence de données inventées.

Sortie attendue : checklist de corrections ou dernier JSON upsert ciblé.

## Règles de passage entre étapes

Une étape est terminée seulement quand :

- le JSON a été prévisualisé sans erreur bloquante ;
- les warnings ont été expliqués ou corrigés ;
- l’application a été faite si l’utilisateur valide ;
- l’utilisateur fournit l’export actualisé ;
- les nouvelles clés créées sont reprises dans l’étape suivante.

Si l’export montre une divergence avec le JSON précédent, l’export gagne. Ne pas réutiliser un ancien `id`, `zoneKey`, `manufacturerKey`, `itemKey` ou `imageKey` qui n’existe plus dans l’état actualisé.

## Règles de recherche

- Utiliser les sources officielles quand elles existent.
- Croiser les données historiques avec des sources spécialisées ou archivées quand les sources officielles sont incomplètes.
- Vérifier les informations récentes ou changeantes au moment de l’étape.
- Ne jamais inventer une date complète quand seule l’année ou le mois est fiable.
- Ne jamais transformer une rumeur, une page non sourcée ou une mention isolée en donnée publique validée.

## Règles globales intégrées

Ces règles remplacent les anciennes guidelines séparées et s’appliquent à toutes les étapes.

- Vérifier la pertinence avant tout enrichissement.
- Ne jamais enrichir artificiellement une entité douteuse.
- Pour un parc majeur, viser un traitement complet : parc, zones, attractions, restaurants, boutiques, services, hôtels, parkings, références, images, horaires et histoire.
- Ne pas se limiter aux coasters.
- Résoudre toutes les clés utilisées : `zoneKey`, `manufacturerKey`, `operatorKey`, `founderKey`, `ownerKey`, `itemKey`, `imageKey`.
- Préserver les données existantes en mode `merge` : IDs, images, rattachements, coordonnées, biographies et contenus validés.
- Garder les éléments fermés mais confirmés visibles quand ils sont pertinents pour la fiche ou l’histoire.
- Mettre les restrictions, tailles, tarifs, horaires, dates, coordonnées et données techniques dans les champs structurés, pas dans les descriptions.
- Utiliser uniquement des images externes directes vers de vrais fichiers image.
- Garder les horaires et événements datés sourcés, actuels et séparés des tarifs.
- Créer un article seulement si le sujet a une vraie valeur éditoriale durable.

## Règles de livraison

Avant de livrer un JSON, appliquer le fichier d’étape concerné et les règles globales intégrées ci-dessus.

La réponse doit indiquer clairement :

- l’étape traitée ;
- les sources utilisées ou les limites de source ;
- ce qui est volontairement laissé de côté pour l’étape suivante ;
- les points nécessitant relecture humaine.
