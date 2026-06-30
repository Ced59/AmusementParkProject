# AmusementPark — Orchestrateur d’intégration des données d’un parc

Version : **2026-06-30-r3**
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

Quand le livrable principal est un JSON upsert, le JSON doit être fourni sous forme de fichier `.json` téléchargeable, pas comme un long bloc texte à copier-coller. La réponse visible doit seulement résumer le contenu du fichier, les sources, les limites et la suite. Si l’interface ne permet pas de joindre un fichier, ne pas contourner en collant tout le JSON : prévenir l’utilisateur et demander explicitement le format de secours souhaité.

Nommer les fichiers de façon lisible et traçable, par exemple `park-slug-step-03-items-lot-1-YYYYMMDD.json`.

## Règle de parcours strict

Le parcours officiel est uniquement celui défini ci-dessous, de l’étape 0 à l’étape 8. Ne jamais inventer une nouvelle étape, renommer une étape, insérer une étape intermédiaire, fusionner deux étapes ou réordonner le parcours pendant l’intégration d’un parc.

Quand l’utilisateur demande `Go étape N`, lire l’orchestrateur puis le fichier exact de l’étape N, et produire seulement le livrable de cette étape. Ne pas recommencer une étape précédente, ne pas anticiper une étape future et ne pas remplacer l’étape demandée par un découpage jugé plus logique.

Les références ne forment pas une étape autonome :

- les fondateurs et exploitants nécessaires à la fiche parc se traitent à l’étape 1 ;
- les constructeurs nécessaires aux parkItems se traitent à l’étape 3 ;
- les biographies de références et les images de références se traitent à l’étape 5 ou dans un lot de descriptions prévu par l’étape 4 ;
- les références utiles à l’histoire se réutilisent à l’étape 7, sans créer un nouveau bloc de workflow.

Si une information utile à l’étape demandée exige une référence, résoudre cette référence dans le JSON de l’étape en cours ou vérifier qu’elle existe déjà dans l’export. Ne pas créer une étape “références” ou “pré-références”.

Si la prochaine étape officielle semble peu pertinente pour le parc en cours, ne pas la sauter seul. À la fin de l’étape en cours, ajouter une section `Pertinence de la prochaine étape` avec :

- le numéro et le nom de la prochaine étape officielle ;
- un statut clair : `utile`, `probablement inutile` ou `à décider` ;
- la raison concrète liée au parc et aux sources ;
- la décision attendue de l’utilisateur : continuer cette étape, la sauter, ou demander un complément.

Si la prochaine étape officielle est `probablement inutile`, continuer l’analyse de proche en proche jusqu’à identifier la prochaine étape officielle `utile` ou `à décider`. Lister brièvement chaque étape intermédiaire jugée peu pertinente et pourquoi. Ne pas exécuter ni sauter automatiquement ces étapes : l’utilisateur tranche.

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

## Règle de résolution des clés

Avant de livrer un fichier JSON upsert, vérifier toutes les clés de rattachement :

- `zoneKey` ;
- `manufacturerKey` ;
- `operatorKey` ;
- `founderKey` ;
- `ownerKey` ;
- `itemKey` ;
- `imageKey`.

Chaque clé utilisée doit être résolue par l’une de ces deux voies :

- la clé existe déjà clairement dans l’export actualisé fourni par l’utilisateur ;
- la clé est créée dans le même JSON, dans la section adaptée (`references`, `zones`, `items`, `images`).

Ne jamais utiliser un UUID, un ID interne ou une valeur devinée comme `manufacturerKey`, `operatorKey`, `founderKey`, `zoneKey`, `itemKey` ou `ownerKey` si l’export ne prouve pas que cette valeur est bien la clé attendue. Pour les constructeurs, `manufacturerKey` doit correspondre exactement à une clé de `references.manufacturers` créée dans le même JSON ou déjà présente dans l’export. Une alerte du type “ManufacturerKey non résolue” n’est jamais acceptable dans un livrable final : corriger le JSON et régénérer le fichier avant de le livrer.

Si une clé ne peut pas être résolue :

- créer la référence minimale dans le même JSON si elle est fiable ;
- sinon retirer le rattachement incertain ;
- sinon livrer une analyse de blocage, pas un JSON qui produira une alerte prévisible.

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

Objectif : enrichir logos, images du parc, images d’items et biographies de fondateurs, exploitants ou constructeurs, avec des sources d’image techniquement importables et éditorialement fiables.

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

Le passage à l’étape suivante se fait seulement après validation utilisateur. Même si une étape semble inutile pour le parc, la décision appartient à l’utilisateur après lecture de la section `Pertinence de la prochaine étape`.

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
- Utiliser uniquement des images externes importables par le flux technique du projet : URL HTTP(S) publique, réponse image réelle, taille acceptée et propriétaire résolu.
- Garder les horaires et événements datés sourcés, actuels et séparés des tarifs.
- Créer un article seulement si le sujet a une vraie valeur éditoriale durable.

## Règles de livraison

Avant de livrer un JSON, appliquer le fichier d’étape concerné et les règles globales intégrées ci-dessus.

La réponse doit indiquer clairement :

- `Étape traitée` ;
- `Livrable`, avec le nom du fichier `.json` téléchargeable quand un upsert est généré ;
- les sources utilisées ou les limites de source ;
- `Ce qui reste volontairement hors étape` ;
- les points nécessitant relecture humaine ;
- `Pertinence de la prochaine étape`.

La section `Ce qui reste volontairement hors étape` doit expliquer ce qui est reporté parce que cela appartient à une étape officielle future. Elle ne doit pas proposer un nouveau découpage.

Ne pas coller le JSON complet dans la réponse visible quand un fichier téléchargeable a été généré. Un court extrait ou un résumé des sections incluses suffit.
