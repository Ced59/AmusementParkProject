# Étape 5 — Images et enrichissement des références

Objectif : ajouter les images fiables et enrichir les fondateurs, exploitants ou constructeurs sans créer de doublons ni d’images non importables.

## Lire avant de commencer

- `park-data-integration-orchestrator.md`
- `park-graph-upsert-enums.md`
- `04-rich-descriptions-localization.md` pour les biographies et descriptions de références

## État de référence requis

Dans les deux modes, utiliser le registre consolidé et les réponses validées des items et descriptions concernés, sans nouvel export complet. Les `ownerKey` doivent correspondre aux clés déjà présentes dans cet état ou aux références créées dans le même JSON ; un export ciblé est réservé à un identifiant réellement manquant ou à une réponse ambiguë.

Avant toute recherche, produire la liste des propriétaires à couvrir et noter pour chacun l’image courante déjà présente, les images secondaires et l’absence éventuelle. L’objectif n’est pas d’ajouter quelques photos faciles, mais de fermer méthodiquement les lacunes de couverture.

## Ce que l’import permet techniquement

Le flux d’import d’images remote accepte plus qu’une URL de fichier “directe” classique. D’après le code actuel, une image peut être importée si toutes ces conditions sont vraies :

- `sourceUrl` est une URL absolue `http` ou `https` ;
- l’URL ne contient pas d’identifiants dans `UserInfo` ;
- l’hôte n’est pas `localhost` ;
- le DNS ou l’adresse IP résout vers une adresse publique ;
- les adresses loopback, privées, link-local, multicast, réservées ou équivalentes sont rejetées ;
- les redirections HTTP(S) sont acceptées jusqu’à 5 redirections ;
- chaque URL de redirection doit rester publique et HTTP(S) ;
- la réponse finale est un succès HTTP ;
- `Content-Length`, s’il est fourni, est strictement positif et inférieur ou égal à 10 Mo ;
- le flux téléchargé ne dépasse pas 10 Mo ;
- le contenu téléchargé n’est pas vide ;
- ImageSharp détecte un vrai format d’image dans les octets téléchargés ;
- l’extension de l’URL n’est pas obligatoire : le format détecté peut corriger ou ajouter l’extension ;
- le `Content-Type` peut être imprécis, par exemple `application/octet-stream`, si les octets sont bien une image ;
- les serveurs CDN, URLs signées, URLs avec paramètres ou URLs de transformation peuvent passer si elles respectent les conditions ci-dessus ;
- l’import envoie des headers proches d’un navigateur, mais une protection anti-hotlinking peut quand même bloquer le téléchargement ;
- les logos ne reçoivent jamais de watermark, même si `withWatermark` est demandé.

Les propriétaires importables par JSON upsert sont :

- `Park` ;
- `ParkItem` ;
- `ParkOperator` ;
- `AttractionManufacturer` ;
- `ParkFounder` ;
- `StandaloneAttraction` pour une attraction fixe isolée traitée hors parc.

## Images à privilégier éditorialement

### Contrat de couverture

Rechercher systématiquement, quand l’entité est applicable :

- le logo officiel actuel du parc, distinct de toute photo principale ;
- au moins une image représentative du parc ;
- au moins une image fidèle de chaque attraction actuelle ;
- au moins une image fidèle de chaque attraction annoncée ou en construction, en distinguant chantier, rendu officiel et attraction ouverte ;
- au moins une image historique contextualisée de chaque attraction définitivement fermée ;
- une image contextualisée pour chaque jalon historique visible et chaque article, à acquérir ici ou dans le lot d’étape 7 qui crée le contenu ;
- les images utiles des références importantes quand une source adaptée existe.

Une même image peut illustrer plusieurs contenus seulement si elle reste réellement pertinente dans chacun de ces contextes. Ne pas réutiliser mécaniquement une vue générale du parc pour masquer l’absence d’une photo de l’attraction, du jalon ou de l’article concerné.

L’absence d’image est acceptable uniquement après une recherche réelle dans les sources officielles, espaces presse, archives, presse, fonds photographiques et sources spécialisées adaptées. Consigner le propriétaire, les familles de sources vérifiées et la raison de l’échec dans le registre des lacunes. Ne jamais compenser avec une image générique, une mauvaise attraction ou un rendu présenté comme une photographie.

### Vérification visuelle et éditoriale

Avant tout import, inspecter visuellement le fichier final, pas seulement sa page, son URL, son nom ou ses métadonnées :

- la bonne attraction, le bon parc ou le bon sujet doit être identifiable sans ambiguïté ;
- l’époque et l’état représentés doivent être compatibles avec le contexte annoncé ;
- la résolution et le cadrage doivent rester utiles sur la fiche publique ;
- aucun watermark, logo incrusté ou signature promotionnelle d’un site tiers ne doit apparaître ;
- le contenu ne doit être ni trompeur, ni graphique, ni intrusif.

Une photo non officielle peut être utilisée si elle satisfait ces contrôles, possède une source joignable, peut être créditée correctement et respecte le contexte d’utilisation. Préférer les sources officielles et presse lorsqu’elles offrent un visuel équivalent, sans transformer cette préférence en interdiction des bonnes images d’archives ou de contributeurs.

Le logo officiel visible comme sujet de son propre fichier n’est pas un watermark. En revanche, une photographie portant le logo ou le filigrane d’un autre site reste refusée.

### Image courante ou image de contexte

- Le logo officiel actuel devient le logo courant du parc et n’est jamais watermarqué par le projet.
- La meilleure vue représentative peut devenir l’image principale du parc ou de l’attraction lorsqu’aucune image courante plus juste n’existe.
- Une image historique, un rendu, une vue de chantier ou une illustration d’article reste secondaire et ne remplace pas automatiquement l’image courante.
- Vérifier `setAsCurrent` ou `isCurrent` pour chaque import ; ne pas laisser la valeur par défaut choisir la hiérarchie éditoriale.

### Parcours selon l’opérateur

- ChatGPT conserve le flux d’images distantes Park Graph Upsert décrit dans ce fichier.
- Codex suit le téléchargement, l’inspection, l’upload, le rattachement et la mise à jour des métadonnées décrits dans `../codex-park-data-editor-api-workflow.md`. Il n’utilise pas l’administration comme solution de secours.

Pour `Planned`, `UnderConstruction` ou `Cancelled`, distinguer clairement photographies du site, images officielles du chantier et rendus de conception. Un rendu ne doit jamais être légendé comme une vue d’un parc existant ou ouvert. Pour `TemporarilyClosed` et `ClosedDefinitively`, dater ou contextualiser les images quand leur apparence ne reflète plus l’état actuel.

Une image externe doit être :

- une URL stable quand c’est possible ;
- téléchargeable ;
- fidèle au parc ou à l’item ;
- créditable ;
- sans watermark d’un site tiers ;
- issue d’une source fiable ou librement exploitable selon le contexte du projet.

Refuser ou éviter :

- page HTML qui ne renvoie pas directement une image au téléchargement ;
- preview qui ne renvoie pas les octets de l’image finale ;
- miniature trop petite quand une image de meilleure qualité est disponible ;
- image générique ;
- image dont l’élément représenté est douteux.
- photographie avec watermark, logo incrusté ou signature d’un site tiers.

Ne pas utiliser de lien CDN interne du site comme source externe pour réimporter une image déjà stockée. Utiliser l’ID d’image existant dans ce cas.

## Propriétaires d’images

### Fonctionnement réel du processeur

Le processeur traite `references`, puis `items[]`, puis `images[]`. Il construit pendant ce traitement des dictionnaires de clés qui contiennent seulement les références, parkItems ou attractions autonomes redéclarés dans le JSON courant. Le fait qu’une entité existe déjà en base ou dans un export précédent ne suffit pas à enregistrer sa clé.

Le parc cible est le seul propriétaire résolu directement par le contexte du parc. Pour les livrables produits par ChatGPT, chaque objet de `images[]` doit préciser explicitement `ownerType` et utiliser le mécanisme suivant :

| Propriétaire | `ownerType` de l’image | `ownerKey` sûr | Déclaration requise dans le JSON courant | Rôle de `ownerId` dans la résolution |
| --- | --- | --- | --- | --- |
| Parc cible | `Park` | `park` | parc cible résolu par la sélection admin, `identity.parkId` ou l’identifiant équivalent du document | ignoré dès que `ownerKey: "park"` est reconnu |
| ParkItem | `ParkItem` | valeur exacte de `items[].key` | entrée correspondante dans `items[]` | non utilisé pour résoudre ce type |
| Exploitant | `ParkOperator` | `operator:<key>` | entrée correspondante dans `references.operators` | non utilisé pour résoudre ce type |
| Fondateur | `ParkFounder` | `founder:<key>` | entrée correspondante dans `references.founders` | non utilisé pour résoudre ce type |
| Constructeur | `AttractionManufacturer` | `manufacturer:<key>` | entrée correspondante dans `references.manufacturers` | non utilisé pour résoudre ce type |
| Attraction autonome | `StandaloneAttraction` | clé enregistrée par le bloc singulier, notamment `standaloneAttraction` dans l’export standard | bloc `standaloneAttraction` correspondant | un `ownerId` exact non vide est accepté directement pour ce type |

Le processeur sait déduire certains `ownerType` depuis un préfixe, mais ChatGPT doit toujours l’écrire explicitement pour empêcher un repli accidentel vers `Park`.

Pour un propriétaire existant, recopier depuis le même état de référence :

- `id`, `key` et `name` dans l’entrée minimale de `items[]` ou de `references` ;
- exactement la même valeur de `key` dans `images[].ownerKey`, précédée du préfixe requis pour une référence ;
- l’`ownerId` uniquement s’il est conservé comme information redondante issue de l’export.

Dans l’export standard, la clé d’un parkItem est son ID. Une image de parkItem utilise donc généralement cet ID comme `ownerKey`, mais parce qu’il est aussi présent dans `items[].key`, pas parce que `ownerId` permettrait de le résoudre.

Pour un propriétaire créé dans le même JSON, utiliser une clé locale stable et strictement identique entre sa déclaration et l’image. Ne jamais inventer un UUID ou un ID interne.

`ownerId` n’est pas une clé de secours universelle. En particulier, un `ownerId` fourni seul pour un parkItem, un exploitant, un fondateur ou un constructeur ne remplace jamais `ownerKey` ni la déclaration du propriétaire dans le JSON courant.

Avant de livrer un JSON avec des images, faire un contrôle croisé simple :

- lister chaque `sourceUrl` ;
- indiquer son `ownerType` explicite ;
- indiquer son `ownerKey` exact ;
- pointer vers l’objet précis du JSON courant qui enregistre cette clé, ou vers le parc cible pour `ownerKey: "park"` ;
- vérifier caractère par caractère l’égalité entre les clés ;
- vérifier tout `ownerId` conservé contre l’export, sans compter sur lui pour remplacer la clé ;
- retirer toute image dont le propriétaire ne peut pas être prouvé.

Ne jamais utiliser le nom de fichier, le dossier de galerie, l’URL, une légende approximative ou un slug deviné comme `ownerKey`. Une image d’une galerie source doit être rattachée à `park` si elle représente vraiment le parc dans son ensemble, ou à un `ParkItem` seulement si l’item est déjà présent et que le lien est certain.

Chaque image est résolue indépendamment. Aucun champ propriétaire n’est hérité de l’image précédente, du parc cible, de l’article qui la référence ou d’une autre section.

Une alerte Preview du type `Remote image ignored: owner could not be resolved` indique une erreur de livrable. Ne pas demander à l’utilisateur d’appliquer quand même : corriger `ownerType`, `ownerKey` ou la déclaration manquante dans `items[]` ou `references`, puis fournir un nouveau fichier téléchargeable.

## Métadonnées image

Chaque nouvelle image distante doit avoir :

- `key` stable, sans espaces de bord et unique sans tenir compte de la casse ;
- `sourceUrl` ;
- `ownerType` ;
- l’`ownerKey` défini dans le tableau ci-dessus, sauf attraction autonome volontairement résolue par son `ownerId` exact ;
- `category` ;
- `isPublished` ;
- `withWatermark` ;
- `setAsCurrent` si elle doit devenir logo ou image principale ;
- `description` interne courte ;
- `altTexts`, `captions`, `credits` dans les 8 langues quand l’image est publique.

Pendant une création ou une commande de complétude avant autorisation de publication, toute nouvelle image doit utiliser `isPublished: false`. Cette règle vaut aussi pour l’enrichissement d’un parc déjà public : sa visibilité existante est préservée, mais le nouveau média reste privé jusqu’à la phase de publication. Ne jamais se fier à une valeur par défaut du client ou du fichier de métadonnées.

Les textes visiteurs de l’image suivent la charte éditoriale de l’étape 4 :

- `altTexts` décrit avec concision ce qui est réellement visible et utile à comprendre ;
- `captions` situe naturellement la scène, l’attraction ou la période quand ce contexte apporte quelque chose ;
- `credits` contient l’auteur, la source et la licence ou l’autorisation utile ;
- `description` reste factuelle et lisible si elle peut apparaître dans un outil éditorial.

La description et la légende commencent par le sujet réellement visible et son contexte : attraction, scène, époque, décor ou point de vue utile. Des libellés comme « image officielle », « visuel supplémentaire », « onride », « photo importée » ou « image contextualisée de l’élément » ne décrivent pas la scène et doivent être remplacés. Le caractère officiel, l’auteur, la licence et la provenance restent dans `credits` lorsqu’ils sont utiles.

Ne jamais écrire dans `altTexts`, `captions` ou `description` : URL source, méthode d’import, statut de vérification, résolution, justification de droits, absence d’une autre photo, propriétaire technique, catégorie de base, score de complétude ou formulation mécanique du type « image contextualisée de l’élément ». Les crédits et notes internes portent ces informations à leur place.

Si une image est techniquement importable mais éditorialement fragile, ne pas l’ajouter : préférer une absence d’image à une image trompeuse, instable ou mal créditée.

## Références à enrichir

Enrichir seulement les références utiles :

- constructeurs réellement liés à des items ;
- exploitants du parc ;
- fondateurs documentés ;
- propriétaires si le modèle ou le contexte les prend en charge.

Les biographies doivent être génériques et réutilisables. Ne pas écrire une bio de constructeur centrée uniquement sur le parc en cours.

Pour les constructeurs majeurs, une bonne biographie peut couvrir l’origine, la période d’activité, les spécialités, les modèles marquants, l’influence dans l’industrie et des exemples connus. Pour une source limitée, rester prudent et plus court plutôt que remplir.

Pour les fondateurs, une bonne biographie peut couvrir l’identité, le rôle dans la création du parc, le parcours public documenté, les dates de vie si fiables, la nationalité, la fonction ou occupation, et le lien réel avec le projet. Ne pas romancer une personne peu documentée.

Pour les exploitants, utiliser `description` plutôt que `biography`. Une bonne description peut couvrir le nom légal, la période d’activité, le rôle exact dans le parc, les autres parcs ou activités connues, les changements de propriétaire, le site officiel et les coordonnées publiques si elles sont fiables.

Avant de décider que l’étape 5 est inutile, auditer l’état de référence :

- constructeur lié à un item sans `biography` fiable ;
- fondateur lié au parc sans `biography` fiable ;
- exploitant lié au parc sans `description` fiable ;
- référence avec dates, nom légal, site officiel, contact ou pays manquant alors qu’une source fiable existe ;
- référence sans logo ou image pertinente alors qu’une source techniquement importable et créditable existe.

S’il reste une référence importante incomplète et sourçable, l’étape 5 est `utile`. Si les sources manquent, l’étape 5 est au minimum `à décider`, avec la liste des références concernées.

Champs utiles par type de référence :

- fondateur : `key`, `name`, `occupation`, `birthDate`, `deathDate`, `birthPlace`, `nationalityCountryCode`, `websiteUrl`, `biography` ;
- exploitant : `key`, `name`, `legalName`, `foundedYear`, `closedYear`, `contactDetails`, `description`, `adminReviewStatus` ;
- constructeur : `key`, `name`, `legalName`, `foundedYear`, `closedYear`, `contactDetails`, `biography`, `isVisible`, `adminReviewStatus`.

`contactDetails` peut contenir `websiteUrl`, `email`, `phoneNumber`, `street`, `city`, `postalCode`, `countryCode`, `latitude`, `longitude`.

Ne pas modifier une biographie déjà validée explicitement, notamment Vekoma, sauf demande directe.

Ne pas transformer ces enrichissements en étape autonome. Cette étape 5 est le bloc prévu pour compléter les références déjà nécessaires au parc, aux parkItems ou aux images. Si une référence minimale manque parce qu’elle aurait dû être créée à l’étape 1 ou 3, la résoudre ici seulement si elle est indispensable au livrable d’images ou de biographies, et signaler l’écart dans `metadata.notes`.

## JSON attendu

Sections possibles :

- `images`
- `items` quand une image appartient à un parkItem
- `references.founders`
- `references.operators`
- `references.manufacturers`

```json
{
  "documentType": "AmusementParkParkGraphUpsert",
  "schemaVersion": "2026-06-30",
  "mode": "merge",
  "metadata": {
    "source": "codex-images-references",
    "targetParkId": "id-du-parc",
    "targetParkName": "Nom du parc",
    "step": "05-images",
    "notes": "Images Wikimedia Commons directes avec crédits localisés."
  },
  "identity": {
    "parkId": "id-du-parc",
    "name": "Nom du parc"
  },
  "images": [
    {
      "key": "park-main-image",
      "sourceUrl": "https://upload.wikimedia.org/example/image.jpg",
      "ownerType": "Park",
      "ownerKey": "park",
      "category": "Park",
      "isPublished": true,
      "withWatermark": false,
      "setAsCurrent": true,
      "description": "Vue du parc - source et licence.",
      "altTexts": [
        { "languageCode": "fr", "value": "Vue du parc." }
      ],
      "credits": [
        { "languageCode": "fr", "value": "Photo : auteur, source, licence." }
      ]
    }
  ]
}
```

## Exemples de résolution sûre

### Image d’un parkItem existant

Même si aucune donnée métier du parkItem ne change, le redéclarer pour enregistrer sa clé avant le traitement de l’image :

```json
{
  "items": [
    {
      "id": "id-exporte-du-parkItem",
      "key": "id-exporte-du-parkItem",
      "name": "Nom exact exporté du parkItem"
    }
  ],
  "images": [
    {
      "key": "park-item-main-image",
      "sourceUrl": "https://example.org/photo.jpg",
      "ownerType": "ParkItem",
      "ownerKey": "id-exporte-du-parkItem",
      "category": "ParkItem"
    }
  ]
}
```

L’`ownerId` de l’image peut être recopié de l’export comme redondance, mais il n’est pas consulté pour résoudre un propriétaire `ParkItem`.

### Image d’un constructeur existant

La référence doit être traitée avant l’image et la partie située après `manufacturer:` doit correspondre à sa clé :

```json
{
  "references": {
    "manufacturers": [
      {
        "id": "id-exporte-du-constructeur",
        "key": "id-exporte-du-constructeur",
        "name": "Nom exact exporté du constructeur"
      }
    ]
  },
  "images": [
    {
      "key": "manufacturer-logo",
      "sourceUrl": "https://example.org/logo.png",
      "ownerType": "AttractionManufacturer",
      "ownerKey": "manufacturer:id-exporte-du-constructeur",
      "category": "Logo"
    }
  ]
}
```

Appliquer la même règle avec `operator:<key>` et `references.operators`, ou `founder:<key>` et `references.founders`.

### Contrôle bloquant

- aucune image distante ne produit `Remote image ignored: owner could not be resolved` ;
- chaque `ownerKey` est prouvé par le parc cible ou par une déclaration du JSON courant ;
- aucune image n’utilise seulement `ownerId` pour un parkItem ou une référence ;
- aucun `ownerKey` ne provient d’une URL, d’un chemin CDN, d’un nom de fichier, d’un nom affiché ou d’une valeur reconstruite ;
- le nombre d’images contrôlées est égal au nombre d’objets de `images[]`.

## Contrôles avant livraison

- Toutes les URLs images sont techniquement importables selon les règles ci-dessus.
- Tous les propriétaires sont résolus par le mécanisme exact décrit dans le tableau.
- Tous les parkItems propriétaires d’images sont redéclarés dans `items[]`.
- Toutes les références propriétaires d’images sont redéclarées dans leur section `references` correspondante.
- Aucune URL image ne peut produire `Remote image ignored: owner could not be resolved`.
- Les constructeurs liés aux items ont une biographie ou une limite de source documentée.
- Les fondateurs liés au parc ont une biographie ou une limite de source documentée.
- Les exploitants liés au parc ont une description et des informations utiles ou une limite de source documentée.
- Les crédits sont lisibles pour un visiteur.
- Les logos ne sont pas confondus avec des photos.
- Le logo officiel courant est présent, sans watermark ajouté, rattaché au parc et marqué comme courant, ou son absence après recherche est documentée comme lacune.
- Les nouvelles images du lot restent non publiées tant que l’autorisation explicite de l’étape 8 n’a pas été donnée.
- Chaque attraction actuelle, annoncée, en construction ou définitivement fermée a au moins une image fidèle, ou une exception de recherche précisément documentée.
- Chaque image a été inspectée visuellement et ne porte aucun watermark ou logo incrusté d’un site tiers.
- Chaque jalon et article déjà présent possède une image contextualisée quand une image acceptable est trouvable ; les images manquantes sont inscrites au registre de reprise de l’étape 7.
- Les `altTexts`, `captions` et `description` sont naturels et éditoriaux, sans formulation technique, mécanique ou justificative.
- Les huit versions des `altTexts` et `captions` décrivent le même sujet avec une langue naturelle ; aucune traduction ne retombe sur un libellé générique du seul nom de l’attraction si la scène permet davantage.
- Les images historiques ne prétendent pas montrer une date ou un état qu’elles ne montrent pas.
- Les biographies ne créent pas de doublons de références.
- Toutes les valeurs enum utilisées sont listées dans `park-graph-upsert-enums.md`.

## Après Apply

Pour récupérer les IDs d’images avant de les référencer dans l’histoire, utiliser les IDs retournés par les imports et conservés dans le registre consolidé ; demander un export ciblé de la section `Images` seulement si la réponse ne fournit pas l’identifiant indispensable. Aucun export complet intermédiaire n’est nécessaire.

Calculer provisoirement la couverture depuis le registre consolidé, puis confirmer les valeurs avec l’export complet frais précédant l’étape 8 :

- logo officiel : présent et courant / absent ;
- image principale du parc : présente / absente ;
- attractions actuelles avec image / total ;
- attractions annoncées ou en construction avec image / total ;
- attractions définitivement fermées avec image / total ;
- jalons et articles existants avec image / total ;
- exceptions restantes, avec preuve de recherche.

Ne pas déclarer l’étape terminée sur la seule réussite technique des imports.

À la fin de la réponse, ajouter `Pertinence de la prochaine étape` pour l’étape 6 — Horaires et événements nommés. Si aucun calendrier fiable n’existe ou si le parc est fermé sans horaires utiles, indiquer `probablement inutile` ou `à décider` avec la raison. Si l’étape 6 est `probablement inutile`, appliquer la règle de proche en proche de l’orchestrateur jusqu’à la prochaine étape officielle `utile` ou `à décider`. En mode ChatGPT, attendre la décision utilisateur ; en mode Codex autonome, consigner la non-applicabilité et continuer selon l’orchestrateur.
