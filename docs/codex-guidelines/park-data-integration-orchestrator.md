# AmusementPark — Orchestrateur d’intégration des données d’un parc

Version : **2026-07-03-r1**
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

## Règle de livraison visible

Avant chaque fichier JSON upsert livré, la réponse visible doit récapituler :

- les ajouts, corrections, suppressions contrôlées, éléments masqués et éléments explicitement conservés ;
- le périmètre du lot, avec les entités incluses et les entités exclues ou reportées ;
- un compteur d’avancement de l’étape au format traité / total, même si le lot est complet ;
- le reste à traiter avant le prochain lot ou la prochaine étape officielle ;
- les sources principales et les limites connues, sans noyer la réponse dans le JSON.

Pour l’étape 3, distinguer au minimum le compteur de tous les parkItems et, quand c’est utile, le sous-compteur des attractions. Ne pas annoncer le passage à l’étape suivante si le compteur ou les éléments restants montrent que l’étape en cours n’est pas terminée.

## Règle de parcours strict

Le parcours officiel est uniquement celui défini ci-dessous, de l’étape 0 à l’étape 8. Ne jamais inventer une nouvelle étape, renommer une étape, insérer une étape intermédiaire, fusionner deux étapes ou réordonner le parcours pendant l’intégration d’un parc.

Quand l’utilisateur demande `Go étape N`, lire l’orchestrateur puis le fichier exact de l’étape N, et produire seulement le livrable de cette étape. Ne pas recommencer une étape précédente, ne pas anticiper une étape future et ne pas remplacer l’étape demandée par un découpage jugé plus logique.

Avant de produire un JSON qui contient des enums, lire `park-graph-upsert-enums.md` et utiliser uniquement les valeurs canoniques listées. Ne jamais envoyer de valeur numérique ni d’alias legacy dans un nouveau JSON.

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

## Contrôle bloquant — propriétaires et clés résolues

Avant de livrer un JSON upsert, vérifier toutes les relations qui dépendent d’un propriétaire ou d’une clé indirecte.

Règle générale : quand l’entité existe déjà dans l’export actualisé, préférer l’ID explicite plutôt qu’une clé indirecte :

- image de parc existant : `ownerType: "Park"` + `ownerId` ou `ownerKey: "park"` ;
- image de parkItem existant : `ownerType: "ParkItem"` + `ownerId` égal à l’ID du parkItem, et éventuellement `ownerKey` égal au même ID ;
- image de constructeur existant : `ownerType: "AttractionManufacturer"` + `ownerId`, ou `ownerKey: "manufacturer:<id-or-key>"` seulement si la référence est dans le JSON ou déjà résolue ;
- événement d’histoire de parkItem existant : `owner: "parkItem"` + `entityType: "ParkItem"` + `ownerId` + `parkItemId` + `itemId`, tous égaux à l’ID du parkItem ciblé ;
- événement d’histoire de parc : `owner: "park"` + `entityType: "Park"` + `ownerId` ou `parkId` égal à l’ID du parc.

Ne jamais compter sur `itemKey`, `parkItemKey`, `ownerKey` ou `imageKey` seuls quand le JSON ne contient pas aussi la section qui enregistre cette clé pendant le traitement.

Cas autorisé pour les clés :

- `itemKey` / `parkItemKey` peut être utilisé seulement si le même JSON contient aussi une section `items[]` minimale qui permet de remplir le dictionnaire des parkItems avant le traitement dépendant ;
- `imageKey` peut être utilisé dans un article seulement si l’image est créée ou mise à jour dans le même JSON avec un `key` stable ;
- sinon utiliser `imageId` depuis l’export actualisé.

Tout Preview qui retourne :

- `owner could not be resolved`,
- `Remote image ignored: owner could not be resolved`,
- `clé image introuvable`,
- `Impossible de résoudre le propriétaire de l’événement history`,

est bloquant. Ne pas appliquer. Corriger le JSON et relivrer une version numérotée.

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

## Flux attraction fixe isolée

Si l’étape 0 conclut que l’entité est une attraction fixe isolée, ne pas continuer le parcours parc 1 à 8. Lire `standalone-attraction-data-integration.md` et utiliser un document `standaloneAttractionGraph`.

Règles spécifiques :

- ne pas créer de faux parc pour porter une seule attraction ;
- ne pas rattacher artificiellement les activités voisines du domaine touristique ;
- utiliser `standaloneAttraction` pour l’identité, l’adresse, l’exploitant, les descriptions et les données techniques ;
- utiliser `migration` pour convertir une ancienne fiche parc mono-attraction en attraction autonome ;
- utiliser `ImageOwnerType: "StandaloneAttraction"` et `ImageCategory: "StandaloneAttraction"` pour les images de l’attraction ;
- ne pas renseigner les horaires sur l’ancien parc legacy. Les horaires autonomes seront traités seulement quand le modèle d’horaires générique sera disponible.

## Mode bulk JSON upsert

Le mode bulk utilise une enveloppe racine `AmusementParkBulkParkGraphUpsert` avec un tableau `parks`. Chaque entrée de `parks` est un document `AmusementParkParkGraphUpsert` normal, avec son `identity` minimal (`id`/`parkId`, `name`, `countryCode`) et les sections exportées explicitement.

Toutes les règles de cet orchestrateur restent valables en mode bulk : enums canoniques, sources fiables, résolution des clés, previews obligatoires, lots bornés, prudence sur les images, horaires et historiques, et interdiction de copier un export complet quand seules quelques propriétés doivent changer.

Règle spécifique bulk : ne jamais ajouter de propriété absente du JSON exporté. Le travail consiste à vérifier, corriger ou renseigner uniquement les propriétés déjà présentes dans le JSON fourni par l'export bulk. Si une propriété utile n'est pas dans l'export, demander un nouvel export qui inclut la section correspondante au lieu d'inventer ou d'ajouter la propriété manuellement.

Un champ demandé à l'export doit rester visible même lorsqu'il n'est pas renseigné : valeur vide, tableau vide ou `null` selon le contrat. Ce `null` est le signal attendu pour pouvoir renseigner ce champ sans ajouter une propriété nouvelle.

Le bulk est un flux de mise à jour de parcs existants. Ne pas créer de parc, zone, parkItem, référence, image ou événement nouveau dans un JSON bulk, sauf demande explicite de sortir du mode bulk update-only et de revenir à un upsert ciblé classique. Si la preview signale une création (`Created`), corriger le JSON pour ne garder que les entités présentes dans l'export ou demander un autre flux.

L'enveloppe bulk doit rester cohérente : elle peut venir d'une sélection explicite de parcs ou d'un filtre admin documenté, mais elle ne doit pas devenir un dump massif non borné sans demande explicite. Pour des mises à jour larges, préférer plusieurs exports bulk par critère lisible : par exemple par statut, pays, rayonnement, visibilité ou état d'horaires.

## Règle de résolution des clés

Avant de livrer un fichier JSON upsert, vérifier toutes les clés de rattachement :

- `zoneKey` ;
- `manufacturerKey` ;
- `operatorKey` ;
- `founderKey` ;
- `ownerKey` ;
- `itemKey` ;
- `imageKey`.

Chaque clé utilisée doit être résolue par l’importeur pendant le traitement du JSON courant. Une clé vue seulement dans l’export actualisé sert à identifier la bonne entité, mais elle ne suffit pas toujours comme clé de rattachement si la section qui construit le dictionnaire n’est pas présente dans le JSON courant.

Résoudre une clé par l’une de ces voies :

- la clé est créée ou redéclarée dans le même JSON, dans la section adaptée (`references`, `zones`, `items`, `images`) ;
- la clé appartient à une entité déjà présente dans une section du même JSON ;
- un champ d’ID direct supporté par le contrat est utilisé à la place, par exemple `zoneId` ou `attractionDetails.manufacturerId` pour rattacher un parkItem à une entité déjà exportée.

Ne jamais utiliser un UUID, un ID interne ou une valeur devinée comme `manufacturerKey`, `operatorKey`, `founderKey`, `zoneKey`, `itemKey` ou `ownerKey` si l’export ne prouve pas que cette valeur est bien la clé attendue. Une valeur visible, un nom localisé ou un slug probable ne suffit pas.

Pour les zones, ne pas utiliser `items[].zoneKey` uniquement parce que la clé existe dans l’export actualisé. Dans un lot d’items, utiliser `zoneId` pour une zone déjà exportée, ou ajouter dans le même JSON une entrée minimale `zones` avec cette `key` avant de l’utiliser dans `items[].zoneKey`. Si la zone n’est pas fiable, retirer tout rattachement de zone.

Pour les constructeurs, ne pas utiliser `attractionDetails.manufacturerKey` uniquement parce que la clé existe dans l’export actualisé. Dans un lot d’items, utiliser `attractionDetails.manufacturerId` pour un constructeur déjà exporté, ou ajouter dans le même JSON une entrée minimale dans `references.manufacturers` avec cette `key` avant de l’utiliser dans `attractionDetails.manufacturerKey`. Si le constructeur n’est pas fiable, retirer tout rattachement constructeur.

Les zones minimales et constructeurs minimaux nécessaires au lot doivent être embarqués dans le même JSON que les parkItems qui les utilisent. Ne pas livrer un fichier qui dépend d’un futur lot pour résoudre ses `zoneKey` ou `manufacturerKey`.

Les alertes suivantes ne sont jamais acceptables dans un livrable final : `ZoneKey non résolue`, `ManufacturerKey non résolue` et `Remote image ignored: owner could not be resolved`. Corriger le JSON, retirer le rattachement incertain ou retirer l’image avant de régénérer le fichier.

Si une Preview signale une clé non résolue après livraison, arrêter le flux courant : ne pas commencer le lot suivant et ne pas passer à l’étape suivante. Fournir d’abord une nouvelle version du même JSON, avec un nom traçable de type `v2-resolved-keys`, puis récapituler précisément les clés ajoutées, retirées ou corrigées.

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

Objectif : créer ou corriger le parc avec ses données stables : nom, pays, type, rayonnement, statut, dates d’ouverture et de fermeture, adresse, coordonnées, site officiel, fondateur, exploitant et visibilité prudente.

Sortie attendue : un JSON upsert centré sur `park` et, si nécessaire, `references.founders` ou `references.operators`.

### Étape 2 — Zones

Lire `park-data-integration-steps/02-zones-upsert.md`.

Objectif : ajouter uniquement les zones officielles ou clairement établies, avec noms localisés, ordre de visite et descriptions si la taille du lot le permet.

Sortie attendue : un JSON upsert centré sur `zones`.

### Étape 3 — Inventaire des parkItems

Lire `park-data-integration-steps/03-park-items-inventory-upsert.md`.

Objectif : intégrer les attractions, restaurants, boutiques, hôtels, services, parkings, entrées, spectacles fixes, animaux/enclos et autres éléments nommables, avec dates, statuts, conditions d’accès et contraintes structurées quand ils sont fiables.

Sortie attendue : un ou plusieurs JSON upsert centrés sur `items` et `references.manufacturers`. Les longues descriptions peuvent être reportées à l’étape 4 pour éviter la saturation.

### Étape 4 — Descriptions longues localisées

Lire `park-data-integration-steps/04-rich-descriptions-localization.md`.

Objectif : produire les descriptions longues du parc, des zones et des parkItems dans les 8 langues, avec un style public naturel, spécifique et non technique.

Sortie attendue : plusieurs JSON upsert bornés par lot de descriptions.

### Étape 5 — Images et références

Lire `park-data-integration-steps/05-images-and-reference-enrichment.md`.

Objectif : enrichir logos, images du parc, images d’items, biographies de fondateurs, descriptions d’exploitants et biographies de constructeurs, avec des sources d’image techniquement importables et éditorialement fiables.

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
- Vérifier réellement chaque URL utilisée comme source d’article ou d’événement avant livraison. La page finale après redirection doit répondre et rester pertinente ; ne jamais livrer de source 404, 410, erreur serveur, soft-404, page d’accueil utilisée comme remplacement, ou URL inventée.
- Ne jamais inventer une date complète quand seule l’année ou le mois est fiable.
- Si seule l’année est fiable pour une ouverture ou fermeture, renseigner l’année seule dans le JSON plutôt que laisser vide ou inventer un jour.
- Ne jamais transformer une rumeur, une page non sourcée ou une mention isolée en donnée publique validée.

## Règles globales intégrées

Ces règles remplacent les anciennes guidelines séparées et s’appliquent à toutes les étapes.

- Vérifier la pertinence avant tout enrichissement.
- Ne jamais enrichir artificiellement une entité douteuse.
- Pour un parc majeur, viser un traitement complet : parc, zones, attractions, restaurants, boutiques, services, hôtels, parkings, références, images, horaires et histoire.
- Ne pas se limiter aux coasters.
- Résoudre toutes les clés utilisées : `zoneKey`, `manufacturerKey`, `operatorKey`, `founderKey`, `ownerKey`, `itemKey`, `imageKey`.
- Chercher systématiquement les conditions d’accès de chaque attraction et les intégrer dans `items[].attractionDetails.accessConditions[]` quand elles sont fiables.
- Ne livrer aucune image dont le propriétaire ne peut pas être résolu à partir de l’export actualisé ou des références/items créés dans le même JSON.
- Vérifier les descriptions ou biographies manquantes des constructeurs, fondateurs et exploitants associés au parc ; les compléter à l’étape 5 ou signaler explicitement l’absence de source fiable.
- Préserver les données existantes en mode `merge` : IDs, images, rattachements, coordonnées, biographies et contenus validés.
- Garder les éléments fermés mais confirmés visibles quand ils sont pertinents pour la fiche ou l’histoire.
- Renseigner une année seule quand c’est la seule précision fiable pour une date d’ouverture ou de fermeture ; ne jamais fabriquer `01-01` ou un premier jour de mois.
- Mettre les restrictions, tailles, tarifs, horaires, dates, coordonnées et données techniques dans les champs structurés, pas dans les descriptions.
- Utiliser uniquement les valeurs enum listées dans `park-graph-upsert-enums.md`.
- Renseigner `park.audienceClassification` dans les nouveaux JSON d’infos générales de parc et vérifier son absence uniquement comme dette legacy à corriger.
- Utiliser uniquement des images externes importables par le flux technique du projet : URL HTTP(S) publique, réponse image réelle, taille acceptée et propriétaire résolu.
- Garder les horaires et événements datés sourcés, actuels et séparés des tarifs.
- Réserver les libellés et raisons visibles dans le calendrier aux événements nommés, exceptions datées ou informations temporaires utiles ; ne jamais répéter un commentaire général sur tous les jours normaux.
- Créer un article seulement si le sujet a une vraie valeur éditoriale durable.
- Pour un incident ou accident trouvé sur un parkItem, créer obligatoirement un article associé quand l’événement est sourcé et retenu, avec une photo contextualisée si une image acceptable est trouvable.
- Rédiger les événements et articles historiques pour les visiteurs, sans note d’audit interne, justification de méthode, “repère documentaire prudent” ou formulation mécanique sur une présence seulement documentée.
- Pour les articles et événements historiques, utiliser uniquement des sources dont les liens répondent au moment de la génération. Si la page d’origine ne répond plus, utiliser une archive fiable ou une autre source valide ; sinon retirer la source et documenter la limite.

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
