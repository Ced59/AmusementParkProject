# AmusementPark — Orchestrateur d’intégration des données d’un parc

Version : **2026-08-09-r4**
Projet : **amusement-parks.fun**  
Usage : fichier d’entrée à donner à ChatGPT/Codex pour intégrer progressivement les données d’un parc avec des JSON Park Graph Upsert.

Cet orchestrateur sert à éviter les JSON trop gros, les oublis de cohérence et les réponses qui saturent à cause d’un parc trop riche. Il ne remplace pas les règles détaillées : il indique quoi lire, dans quel ordre et comment vérifier qu’un parc est réellement complet.

## Deux modes d’exécution, une seule exigence éditoriale

- **ChatGPT guidé** : l’utilisateur fournit les informations existantes et les résultats actualisés utiles, exécute ou valide Preview/Apply et décide du passage à l’étape suivante. ChatGPT livre un seul lot à la fois.
- **Codex autonome par API** : la commande `Complète le parc <nom>` autorise Codex à exécuter de bout en bout toutes les étapes applicables avec `codex-park-data-editor-api-workflow.md`, sans demander une validation intermédiaire à chaque lot. Codex recherche l’existant, ne demande que les sections précises dont il a besoin, prévisualise, applique, contrôle les reçus, consolide localement les résultats, intègre les tarifs actuels quand le parc est `Operating`, effectue l’unique export complet obligatoire juste avant l’étape 9 et audite lui-même.

Les deux modes utilisent les mêmes étapes 0 à 9, les mêmes règles métier, la même qualité éditoriale et le même seuil de complétude. Seuls l’opérateur technique et les points de pause diffèrent.

`Complète le parc <nom>` n’autorise pas la publication. Codex s’arrête après l’étape 9 avec un état `prêt pour publication` ou une liste précise de lacunes. Une demande distincte et explicite est nécessaire pour publier les nouvelles images, rendre visibles le parc et les nouveaux contenus, valider leur statut ou déclencher toute annonce de publication.

## Règle de contexte obligatoire

En mode ChatGPT, avant l’étape 0, l’utilisateur fournit les informations existantes utiles dont il dispose ou confirme qu’il faut créer le parc depuis zéro ; un export complet n’est pas exigé. En mode Codex, Codex recherche les doublons et récupère seulement les sections nécessaires par l’API technique ; il ne demande un choix que si plusieurs identités plausibles subsistent.

En mode ChatGPT guidé, avant chaque nouvelle étape, l’état de référence est le registre consolidé avec les résultats de Preview/Apply ou d’import de l’étape précédente. ChatGPT ne demande un export ciblé que si une information précise manque pour produire le lot suivant.

En mode Codex autonome, l’état de référence des étapes 0 à 8 est un registre local consolidé à partir de la recherche du parc, des éventuels exports ciblés et de chaque réponse réussie de Preview, Apply et d’import d’image. Codex ne lance aucun export complet au cadrage, après un Apply ou un import, entre deux lots, ni lors du passage d’une étape à la suivante. Il effectue l’unique export complet obligatoire immédiatement avant l’audit de l’étape 9. Avant ce jalon, un export limité aux seules sections nécessaires est admis uniquement pour résoudre une identité, une incohérence précise, récupérer une réponse de mutation manquante ou lever un doute qui ne peut pas être résolu par les reçus locaux.

En mode ChatGPT, chaque réponse doit produire un seul livrable principal :

- soit une analyse de blocage ;
- soit un JSON upsert borné pour l’étape en cours ;
- soit une checklist de validation si l’étape ne nécessite pas encore de JSON.

Quand le livrable principal est un JSON upsert, le JSON doit être fourni sous forme de fichier `.json` téléchargeable, pas comme un long bloc texte à copier-coller. La réponse visible doit seulement résumer le contenu du fichier, les sources, les limites et la suite. Si l’interface ne permet pas de joindre un fichier, ne pas contourner en collant tout le JSON : prévenir l’utilisateur et demander explicitement le format de secours souhaité.

En mode Codex autonome, la même limite s’applique à chaque lot API, mais Codex peut enchaîner plusieurs lots et étapes dans le même tour. Il conserve les artefacts de travail hors du dépôt, informe brièvement l’utilisateur de sa progression et livre à la fin un bilan consolidé plutôt qu’un fichier à appliquer manuellement pour chaque lot.

Pour limiter la charge lorsque plusieurs intégrations travaillent en parallèle, ne pas demander un export complet après chaque lot. Le reçu d’Apply, ses erreurs, warnings et compteurs constituent le contrôle normal entre deux lots. Un export ciblé sur les seules sections utiles reste nécessaire si la réponse d’Apply est ambiguë, si un lot suivant dépend d’un identifiant nouvellement créé ou si une vérification précise l’exige. Un export complet frais est obligatoire une seule fois, immédiatement avant l’audit de l’étape 9.

Nommer les fichiers de façon lisible et traçable, par exemple `park-slug-step-03-items-lot-1-YYYYMMDD.json`.

## Règle de livraison visible

Avant chaque fichier JSON upsert livré par ChatGPT, la réponse visible doit récapituler :

- les ajouts, corrections, suppressions contrôlées, éléments masqués et éléments explicitement conservés ;
- le périmètre du lot, avec les entités incluses et les entités exclues ou reportées ;
- un compteur d’avancement de l’étape au format traité / total, même si le lot est complet ;
- le reste à traiter avant le prochain lot ou la prochaine étape officielle ;
- les sources principales et les limites connues, sans noyer la réponse dans le JSON.

Pour l’étape 3, distinguer au minimum le compteur de tous les parkItems et, quand c’est utile, le sous-compteur des attractions. Ne pas annoncer le passage à l’étape suivante si le compteur ou les éléments restants montrent que l’étape en cours n’est pas terminée.

Codex reprend ces informations dans ses points d’avancement et dans le bilan final consolidé, avec les compteurs avant/après et les lots réellement appliqués.

## Règle de parcours strict

Le parcours officiel est uniquement celui défini ci-dessous, de l’étape 0 à l’étape 9. Ne jamais inventer une nouvelle étape, renommer une étape, insérer une étape intermédiaire, fusionner deux étapes ou réordonner le parcours pendant l’intégration d’un parc.

Quand l’utilisateur demande `Go étape N`, lire l’orchestrateur puis le fichier exact de l’étape N, et produire seulement le livrable de cette étape. Ne pas recommencer une étape précédente, ne pas anticiper une étape future et ne pas remplacer l’étape demandée par un découpage jugé plus logique.

Avant de produire un JSON qui contient des enums, lire `park-graph-upsert-enums.md` et utiliser uniquement les valeurs canoniques listées. Ne jamais envoyer de valeur numérique ni d’alias legacy dans un nouveau JSON.

Les références ne forment pas une étape autonome :

- les fondateurs et exploitants nécessaires à la fiche parc se traitent à l’étape 1 ;
- les constructeurs nécessaires aux parkItems se traitent à l’étape 3 ;
- les biographies de références et les images de références se traitent à l’étape 5 ou dans un lot de descriptions prévu par l’étape 4 ;
- les références utiles à l’histoire se réutilisent à l’étape 8, sans créer un nouveau bloc de workflow.

Si une information utile à l’étape demandée exige une référence, résoudre cette référence dans le JSON de l’étape en cours ou vérifier qu’elle existe déjà dans l’export. Ne pas créer une étape “références” ou “pré-références”.

Si la prochaine étape officielle semble peu pertinente pour le parc en cours, l’évaluer explicitement. À la fin de l’étape en cours, ajouter une section `Pertinence de la prochaine étape` avec :

- le numéro et le nom de la prochaine étape officielle ;
- un statut clair : `utile`, `probablement inutile` ou `à décider` ;
- la raison concrète liée au parc et aux sources ;
- la décision attendue de l’utilisateur : continuer cette étape, la sauter, ou demander un complément.

Si la prochaine étape officielle est `probablement inutile`, continuer l’analyse de proche en proche jusqu’à identifier la prochaine étape officielle `utile` ou `à décider`. Lister brièvement chaque étape intermédiaire jugée peu pertinente et pourquoi. En mode ChatGPT, l’utilisateur tranche avant de poursuivre. En mode Codex autonome, consigner la non-applicabilité et continuer ; ne jamais classer une étape applicable comme inutile pour éviter son travail.

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

Règle générale : utiliser les IDs explicites des entités existantes, sauf pour les propriétaires d’images dont le processeur résout plusieurs types uniquement grâce aux clés enregistrées dans le JSON courant :

- image du parc cible : `ownerType: "Park"` + `ownerKey: "park"`, avec un parc cible effectivement résolu ;
- image de parkItem : `ownerType: "ParkItem"` + `ownerKey` égal exactement à une valeur `items[].key`, avec ce parkItem redéclaré dans le même JSON ;
- image d’exploitant, de fondateur ou de constructeur : `ownerType` explicite + `ownerKey` préfixé, avec la référence correspondante redéclarée dans le même JSON ;
- image d’attraction autonome : `ownerType: "StandaloneAttraction"` + clé enregistrée par le bloc d’attraction ; un `ownerId` exact non vide est aussi accepté directement pour ce seul type ;
- événement d’histoire de parkItem existant : `owner: "parkItem"` + `entityType: "ParkItem"` + `ownerId` + `parkItemId` + `itemId`, tous égaux à l’ID du parkItem ciblé ;
- événement d’histoire de parc : `owner: "park"` + `entityType: "Park"` + `ownerId` ou `parkId` égal à l’ID du parc.

Pour une image de parkItem ou de référence, `ownerId` ne remplace jamais `ownerKey` ni la section qui enregistre cette clé pendant le traitement. Voir l’étape 5 pour la matrice complète de résolution.

Cas autorisé pour les clés :

- `itemKey` / `parkItemKey` peut être utilisé seulement si le même JSON contient aussi une section `items[]` minimale qui permet de remplir le dictionnaire des parkItems avant le traitement dépendant ;
- `imageKey` peut être utilisé dans un article pour une image créée dans le même JSON avec un `key` stable et unique après suppression des espaces de bord et sans tenir compte de la casse ;
- sinon utiliser `imageId` depuis l’état de référence validé.

Tout Preview qui retourne :

- `owner could not be resolved`,
- `Remote image ignored: owner could not be resolved`,
- `Impossible de résoudre le propriétaire de l’événement history`,

est bloquant. Ne pas appliquer. Corriger le JSON et relivrer une version numérotée.

Le Preview ne signale pas les clés d’images d’articles introuvables et ne peut pas enregistrer la clé d’une image distante qui n’a pas encore été importée. Avant livraison, comparer donc statiquement chaque `mainImageKey`, `imageKey` et `imageKeys` avec les `images[].key` du même JSON. Après Apply, tout avertissement `clé image introuvable` constitue une erreur de livrable.

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

Ajouter seulement les sections utiles à l’étape : `references`, `park`, `zones`, `items`, `images`, `openingHours`, `pricing`, `history`.

Les textes localisés des upserts actuels utilisent les codes courts présents dans les exports : `fr`, `en`, `de`, `nl`, `it`, `es`, `pl`, `pt`. Si un export existant utilise une autre forme, garder la forme déjà présente.

## Flux attraction fixe isolée

Si l’étape 0 conclut que l’entité est une attraction fixe isolée, ne pas continuer le parcours parc 1 à 9. Lire `standalone-attraction-data-integration.md` et utiliser un document `standaloneAttractionGraph`.

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

Chaque clé utilisée doit être résolue par l’importeur pendant le traitement du JSON courant. Une clé vue seulement dans l’état de référence sert à identifier la bonne entité, mais elle ne suffit pas toujours comme clé de rattachement si la section qui construit le dictionnaire n’est pas présente dans le JSON courant.

Résoudre une clé par l’une de ces voies :

- la clé est créée ou redéclarée dans le même JSON, dans la section adaptée (`references`, `zones`, `items`, `images`) ;
- la clé appartient à une entité déjà présente dans une section du même JSON ;
- un champ d’ID direct supporté par le contrat est utilisé à la place, par exemple `zoneId` ou `attractionDetails.manufacturerId` pour rattacher un parkItem à une entité déjà exportée.

Ne jamais utiliser un UUID, un ID interne ou une valeur devinée comme `manufacturerKey`, `operatorKey`, `founderKey`, `zoneKey`, `itemKey` ou `ownerKey` si l’export ne prouve pas que cette valeur est bien la clé attendue. Une valeur visible, un nom localisé ou un slug probable ne suffit pas.

Pour les zones, ne pas utiliser `items[].zoneKey` uniquement parce que la clé existe dans l’état de référence. Dans un lot d’items, utiliser `zoneId` pour une zone déjà connue, ou ajouter dans le même JSON une entrée minimale `zones` avec cette `key` avant de l’utiliser dans `items[].zoneKey`. Si la zone n’est pas fiable, retirer tout rattachement de zone.

Pour les constructeurs, ne pas utiliser `attractionDetails.manufacturerKey` uniquement parce que la clé existe dans l’état de référence. Dans un lot d’items, utiliser `attractionDetails.manufacturerId` pour un constructeur déjà connu, ou ajouter dans le même JSON une entrée minimale dans `references.manufacturers` avec cette `key` avant de l’utiliser dans `attractionDetails.manufacturerKey`. Si le constructeur n’est pas fiable, retirer tout rattachement constructeur.

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

Le cadrage doit aussi classifier le cycle de vie avec une valeur canonique : `Planned`, `UnderConstruction`, `Operating`, `TemporarilyClosed`, `ClosedDefinitively` ou `Cancelled`. Cette décision pilote l’applicabilité des étapes suivantes ; elle ne doit jamais être déduite de la seule présence d’une fiche ou d’une date annoncée.

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

Objectif : produire les descriptions longues du parc, des zones et des parkItems dans les 8 langues, avec un style public naturel, spécifique et non technique. Pour un parc majeur, contrôler aussi la profondeur visible : plusieurs axes éditoriaux et paragraphes développés pour le parc comme pour chaque parkItem, selon le contrat précis de l’étape 4.

Sortie attendue : plusieurs JSON upsert bornés par lot de descriptions.

### Étape 5 — Images et références

Lire `park-data-integration-steps/05-images-and-reference-enrichment.md`.

Objectif : enrichir logos, images du parc, images d’items, biographies de fondateurs, descriptions d’exploitants et biographies de constructeurs, avec des sources d’image techniquement importables et éditorialement fiables.

Sortie attendue : JSON upsert avec `images` et/ou `references`.

### Étape 6 — Horaires et événements nommés

Lire `park-data-integration-steps/06-opening-hours-and-named-events.md`.

Objectif : intégrer les horaires vérifiés et les exceptions datées. Les événements nommés comme Halloween peuvent apparaître dans les libellés ou raisons localisés, mais les périodes génériques comme une ouverture estivale ne doivent pas devenir des événements éditoriaux artificiels.

Cette étape ne produit un bloc `openingHours` que pour `Operating`. Pour les cinq autres statuts, constater explicitement que l’étape est non applicable et passer à l’évaluation de l’étape 7 sans fabriquer de calendrier.

Sortie attendue : JSON upsert centré sur `openingHours`, et éventuellement quelques événements `history` seulement s’ils ont une vraie valeur durable.

### Étape 7 — Tarifs actuels et historique annuel

Lire `park-data-integration-steps/07-pricing.md`.

Objectif : intégrer, uniquement pour un parc `Operating`, une grille actuelle vérifiée comprenant les billets d’entrée, pass annuels et offres de parking, avec devise source locale, canaux en ligne/guichet, périodes, conditions localisées et liens officiels. Rechercher aussi les relevés fiables des années précédentes et les conserver avec leur devise propre et des codes produit stables afin d’alimenter les graphiques d’évolution.

Sortie attendue : un JSON upsert centré sur `pricing`, prévisualisé puis appliqué par le flux normal. Pour un autre statut, consigner que l’étape est non applicable et ne pas créer de grille actuelle.

### Étape 8 — Histoire du parc et des parkItems

Lire `park-data-integration-steps/08-history-timelines-and-articles.md`.

Objectif : créer la timeline du parc, puis les timelines des parkItems importants, avec articles seulement quand le sujet le mérite. Les résumés expliquent le fait et sa portée ; les articles durables développent plusieurs sections localisées et ne se réduisent pas à deux paragraphes courts.

Sortie attendue : JSON upsert centré sur `history.events`, en plusieurs lots.

### Étape 9 — Audit final

Lire `park-data-integration-steps/09-final-audit-and-publication.md`.

Objectif : vérifier cohérence, sources, localisations, références, images, statut de visibilité, SEO public et absence de données inventées.

Sortie attendue : checklist de corrections ou dernier JSON upsert ciblé.

## Règles de passage entre étapes

En mode ChatGPT guidé, une étape est terminée seulement quand :

- le JSON a été prévisualisé sans erreur bloquante ;
- les warnings ont été expliqués ou corrigés ;
- l’application a été faite si l’utilisateur valide ;
- l’utilisateur fournit la réponse actualisée de l’opération et, seulement si nécessaire, la section ciblée demandée ;
- les nouvelles clés créées sont reprises dans l’étape suivante.

Si une réponse actualisée ou un export ciblé montre une divergence avec le JSON précédent, l’état le plus récent gagne. Ne pas réutiliser un ancien `id`, `zoneKey`, `manufacturerKey`, `itemKey` ou `imageKey` qui n’existe plus dans l’état actualisé.

En mode ChatGPT, le passage à l’étape suivante se fait seulement après validation utilisateur. Même si une étape semble inutile pour le parc, la décision appartient à l’utilisateur après lecture de la section `Pertinence de la prochaine étape`.

En mode Codex autonome, une étape est terminée quand ses lots ont été prévisualisés et appliqués sans erreur bloquante, que leurs réponses ont été contrôlées et intégrées au registre local, et que les compteurs et lacunes ont été actualisés. Aucun export complet intermédiaire n’est requis. Codex peut constater qu’une étape est non applicable ou réellement inutile, consigner la raison et continuer vers l’étape officielle suivante. Il ne peut pas sauter un travail applicable pour gagner du temps. Tout doute qui changerait matériellement l’identité du parc, supprimerait une donnée, masquerait un contenu public ou exigerait une décision éditoriale du propriétaire reste bloquant.

## Règles de recherche

- Utiliser les sources officielles quand elles existent.
- Croiser les données historiques avec des sources spécialisées ou archivées quand les sources officielles sont incomplètes.
- Pour un parc majeur ou ancien, effectuer une recherche dédiée sur les attractions définitivement fermées, les anciens noms, les remplacements, les démolitions et les relocalisations. L’absence d’un élément sur le site officiel actuel ne prouve pas qu’il n’a jamais existé.
- Rechercher les annonces récentes et les actualités à effet durable au moment de l’intégration : nouveauté importante, fermeture, remplacement, transformation, acquisition ou projet structurant. Les intégrer à l’histoire et créer un article quand elles possèdent un vrai angle éditorial durable.
- Vérifier les informations récentes ou changeantes au moment de l’étape.
- Vérifier réellement chaque URL utilisée comme source d’article ou d’événement avant livraison. La page finale après redirection doit répondre et rester pertinente ; ne jamais livrer de source 404, 410, erreur serveur, soft-404, page d’accueil utilisée comme remplacement, ou URL inventée.
- Ne jamais inventer une date complète quand seule l’année ou le mois est fiable.
- Si seule l’année est fiable pour une ouverture ou fermeture, renseigner l’année seule dans le JSON plutôt que laisser vide ou inventer un jour.
- Ne jamais transformer une rumeur, une page non sourcée ou une mention isolée en donnée publique validée.

## Règles globales intégrées

Ces règles remplacent les anciennes guidelines séparées et s’appliquent à toutes les étapes.

- Vérifier la pertinence avant tout enrichissement.
- Ne jamais enrichir artificiellement une entité douteuse.
- Le degré d’exigence est élevé pour chaque parc complété. La profondeur et le volume s’adaptent à son importance, son statut et aux sources, mais jamais la rigueur de recherche, de vérification, de localisation, d’imagerie ou d’audit.
- Pour un parc majeur `Operating`, viser un traitement exhaustif : parc, zones, attractions actuelles et historiques, restaurants, boutiques, services, hôtels, parkings, références, images, horaires, histoire et actualités durables. Pour un projet ou un parc non exploité, adapter le traitement à ce qui existe réellement sans simuler une offre visiteurs.
- Ne pas se limiter aux coasters.
- Résoudre toutes les clés utilisées : `zoneKey`, `manufacturerKey`, `operatorKey`, `founderKey`, `ownerKey`, `itemKey`, `imageKey`.
- Chercher systématiquement les conditions d’accès de chaque attraction et les intégrer dans `items[].attractionDetails.accessConditions[]` quand elles sont fiables.
- Ne livrer aucune image dont le propriétaire ne peut pas être résolu à partir de l’état de référence ou des références/items créés dans le même JSON.
- Pendant une commande de complétude, conserver toute nouvelle image en `isPublished: false`, y compris pour un parc déjà public. La publication des médias appartient à la phase explicitement autorisée de l’étape 9.
- Vérifier les descriptions ou biographies manquantes des constructeurs, fondateurs et exploitants associés au parc ; les compléter à l’étape 5 ou signaler explicitement l’absence de source fiable.
- Préserver les données existantes en mode `merge` : IDs, images, rattachements, coordonnées, biographies et contenus validés.
- Garder les éléments fermés mais confirmés visibles quand ils sont pertinents pour la fiche ou l’histoire.
- Rechercher explicitement les attractions définitivement fermées et documenter leur statut, leurs dates ou périodes, leurs descriptions, leur histoire et leur image quand les sources le permettent.
- Renseigner une année seule quand c’est la seule précision fiable pour une date d’ouverture ou de fermeture ; ne jamais fabriquer `01-01` ou un premier jour de mois.
- Mettre les restrictions, tailles, tarifs, horaires, dates, coordonnées et données techniques dans les champs structurés, pas dans les descriptions.
- Refuser aussi la fiche technique déguisée en prose : ne pas dérouler le tracé, le nombre de véhicules ou de sièges, les rotations, l’accélération, la vitesse, la durée ou le principe de fonctionnement. Les rares noms physiques nécessaires restent naturels et isolés ; une densité de vocabulaire de rails, voies, trains, véhicules, sièges, structures ou mouvements commande une relecture manuelle et une réécriture autour du monde raconté et de l’expérience ressentie.
- Décrire l’identité et l’expérience propres à chaque entité, jamais l’organisation de la journée du visiteur. Sont notamment interdits les conseils d’itinéraire, les injonctions à « garder » ou « placer » une attraction, les pauses suggérées entre files et véhicules et les paragraphes génériques présentés comme un rôle dans le parcours.
- Pour un parc majeur, utiliser par défaut le contrat de profondeur de l’étape 4 : au moins trois intertitres et cinq paragraphes pour le parc ; au moins deux intertitres spécifiques et trois paragraphes développés pour chaque parkItem publiable. Les seuils de mots sont des alertes de relecture, pas une cible de remplissage.
- Relire le corpus par langue après retrait des titres, noms et balises de mise en forme. Une phrase complète, un corps de paragraphe ou un squelette éditorial répété sur plusieurs entités distinctes est une dette bloquante, même si le nom injecté rend le HTML techniquement unique.
- Utiliser uniquement les valeurs enum listées dans `park-graph-upsert-enums.md`.
- Renseigner `park.audienceClassification` dans les nouveaux JSON d’infos générales de parc et vérifier son absence uniquement comme dette legacy à corriger.
- Utiliser uniquement des images externes importables par le flux technique du projet : URL HTTP(S) publique, réponse image réelle, taille acceptée et propriétaire résolu.
- Rechercher le logo officiel actuel, une image représentative du parc et au moins une image fidèle pour chaque attraction actuelle, annoncée, en construction ou définitivement fermée quand elle est trouvable. Vérifier aussi une image contextualisée pour chaque jalon et article historique.
- Une photo non officielle est acceptable si elle représente sans ambiguïté la bonne entité, peut être correctement créditée et ne porte aucun watermark d’un autre site. Toujours inspecter visuellement l’image ; ne jamais se fier au seul nom de fichier, à l’URL ou à la légende source.
- Les textes alternatifs, légendes et descriptions d’images sont des contenus éditoriaux destinés au visiteur. Ils décrivent naturellement ce qui est visible et son contexte, sans jargon d’import, formulation mécanique, note d’audit, justification de source ou commentaire sur une image manquante.
- Codex reste l’auteur et le responsable éditorial de chaque traduction publique. Il rédige ou réécrit substantiellement puis valide lui-même chaque langue, sans déléguer le résultat final à un moteur, une API de traduction ou un autre texte généré. Un outil peut uniquement fournir un élément de comparaison ponctuel ; sa sortie brute ne doit jamais être conservée ni envoyée dans un lot.
- Chaque traduction est rédigée comme un texte naturel dans sa propre langue. Ne pas conserver un jargon anglais, une tournure littérale ou un paragraphe de secours simplement parce que les huit codes de langue sont présents.
- Garder les horaires et événements datés sourcés, actuels et séparés des tarifs.
- Réserver les libellés et raisons visibles dans le calendrier aux événements nommés, exceptions datées ou informations temporaires utiles ; ne jamais répéter un commentaire général sur tous les jours normaux.
- Créer un article seulement si le sujet a une vraie valeur éditoriale durable.
- Développer les résumés de timeline autour du fait et de sa conséquence historique. Pour les sujets durables d’un parc majeur, appliquer les bandes de profondeur et la structure de l’étape 8 ; une succession de blocs présents mais trop courts reste une dette éditoriale.
- Pour un incident ou accident trouvé sur un parkItem, créer obligatoirement un article associé quand l’événement est sourcé et retenu, avec une photo contextualisée si une image acceptable est trouvable.
- Rédiger les événements et articles historiques pour les visiteurs, sans note d’audit interne, justification de méthode, “repère documentaire prudent” ou formulation mécanique sur une présence seulement documentée.
- Pour les articles et événements historiques, utiliser uniquement des sources dont les liens répondent au moment de la génération. Si la page d’origine ne répond plus, utiliser une archive fiable ou une autre source valide ; sinon retirer la source et documenter la limite.
- Ne considérer une absence comme acceptable qu’après une recherche réelle. L’audit final doit nommer chaque lacune résiduelle et les familles de sources vérifiées, sans la masquer derrière un score global.
- Auditer les références globales liées au parc, mais ne pas corriger leur texte dans un lot strictement local si la modification affecterait d’autres parcs. Une reprise de référence partagée exige un périmètre transversal explicite et une vérification factuelle adaptée.

## Règles de livraison

Avant de livrer un JSON, appliquer le fichier d’étape concerné et les règles globales intégrées ci-dessus.

Pour ChatGPT, chaque réponse d’étape doit indiquer clairement :

- `Étape traitée` ;
- `Livrable`, avec le nom du fichier `.json` téléchargeable quand un upsert est généré ;
- les sources utilisées ou les limites de source ;
- `Ce qui reste volontairement hors étape` ;
- les points nécessitant relecture humaine ;
- `Pertinence de la prochaine étape`.

La section `Ce qui reste volontairement hors étape` doit expliquer ce qui est reporté parce que cela appartient à une étape officielle future. Elle ne doit pas proposer un nouveau découpage.

Ne pas coller le JSON complet dans la réponse visible quand un fichier téléchargeable a été généré. Un court extrait ou un résumé des sections incluses suffit.

Pour Codex autonome, le bilan final remplace les livraisons intermédiaires : il indique les étapes exécutées ou non applicables, les lots appliqués, le tableau de couverture, les lacunes après recherche, les éventuels warnings acceptés et la décision `prêt pour publication` ou les corrections restantes.
