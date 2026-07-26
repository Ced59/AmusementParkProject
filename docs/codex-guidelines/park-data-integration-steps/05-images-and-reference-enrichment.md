# Étape 5 — Images et enrichissement des références

Objectif : ajouter les images fiables et enrichir les fondateurs, exploitants ou constructeurs sans créer de doublons ni d’images non importables.

## Lire avant de commencer

- `park-data-integration-orchestrator.md`
- `park-graph-upsert-enums.md`
- `04-rich-descriptions-localization.md` pour les biographies et descriptions de références

## Export requis

Utiliser l’export actualisé après les items et descriptions concernés. Les `ownerKey` doivent correspondre aux clés déjà présentes ou aux références créées dans le même JSON.

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

Une image externe doit être :

- une URL stable quand c’est possible ;
- téléchargeable ;
- fidèle au parc ou à l’item ;
- créditable ;
- sans watermark non autorisé, sauf logo officiel ;
- issue d’une source fiable ou librement exploitable selon le contexte du projet.

Refuser ou éviter :

- page HTML qui ne renvoie pas directement une image au téléchargement ;
- preview qui ne renvoie pas les octets de l’image finale ;
- miniature trop petite quand une image de meilleure qualité est disponible ;
- image générique ;
- image dont l’élément représenté est douteux.
- image avec watermark, sauf logo officiel.

Ne pas utiliser de lien CDN interne du site comme source externe pour réimporter une image déjà stockée. Utiliser l’ID d’image existant dans ce cas.

## Propriétaires d’images

### Règle absolue : répéter le triplet dans chaque objet

Chaque objet de `images[]` doit contenir explicitement :

- `ownerType` ;
- `ownerId` ;
- `ownerKey`.

Le triplet doit être répété dans chaque objet image. Il n’existe aucune notion d’héritage depuis l’image précédente, le parc cible, `identity`, `metadata`, la sélection admin, un article ou une autre section du document.

Utiliser les formats suivants :

- parc : `ownerType: "Park"`, `ownerId` égal à l’ID exact du parc exporté, `ownerKey: "park"` ;
- parkItem : `ownerType: "ParkItem"`, `ownerId` égal à l’ID exact du parkItem exporté, `ownerKey` égal au même ID ;
- constructeur : `ownerType: "AttractionManufacturer"`, `ownerId` exact, `ownerKey: "manufacturer:<key>"` ;
- exploitant : `ownerType: "ParkOperator"`, `ownerId` exact, `ownerKey: "operator:<key>"` ;
- fondateur : `ownerType: "ParkFounder"`, `ownerId` exact, `ownerKey: "founder:<key>"` ;
- attraction autonome : `ownerType: "StandaloneAttraction"`, `ownerId` exact, `ownerKey` autonome accepté par le flux.

Ne jamais livrer une image avec seulement `ownerId`, seulement `ownerKey`, ou `ownerType` accompagné d’un seul des deux autres champs.

### Enregistrement obligatoire du propriétaire dans le JSON courant

Le processeur construit ses dictionnaires de résolution avant d’importer les images. Avec son comportement actuel :

- une image de parkItem exige une entrée `items[]` minimale dans le même JSON, même si `ownerId` est déjà fourni ;
- cette entrée minimale contient obligatoirement `id`, `key` et `name`, recopiés de l’export actualisé ;
- une image de fondateur, d’exploitant ou de constructeur résolue par `ownerKey` exige la référence minimale correspondante dans `references` ;
- une image d’un propriétaire créé dans un lot précédent exige un nouvel export avant le lot d’images ;
- aucun UUID ou ID interne ne doit être inventé pour éviter cette réexportation.

Si le propriétaire ne peut pas être résolu, ne pas inclure l’image.

Ne jamais utiliser un UUID, un ID interne ou une valeur devinée comme `ownerKey` si l’export ne prouve pas que cette valeur est acceptée. Pour un parkItem, `ownerId` et `ownerKey` doivent tous les deux reprendre exactement l’ID exporté, tandis que `items[].key` reprend la clé exportée. En cas de doute, ne pas inclure l’image et signaler le blocage.

Avant de livrer un JSON avec des images, faire un contrôle croisé simple :

- lister chaque `sourceUrl` ;
- indiquer son `ownerType` attendu ;
- indiquer son `ownerId` ;
- indiquer son `ownerKey` ;
- vérifier que les trois champs sont présents dans le même objet image ;
- vérifier que `ownerId` correspond exactement à une entité de l’export actualisé ;
- vérifier que la clé est enregistrée par une section `items[]`, `references` ou par le contexte parc avant `images[]` ;
- retirer toute image dont le propriétaire ne peut pas être prouvé.

Ne jamais utiliser le nom de fichier, le dossier de galerie, l’URL, une légende approximative ou un slug deviné comme `ownerKey`. Une image d’une galerie source doit être rattachée à `park` si elle représente vraiment le parc dans son ensemble, ou à un `ParkItem` seulement si l’item est déjà présent et que le lien est certain.

Une alerte Preview du type `Remote image ignored: owner could not be resolved` indique une erreur de livrable. Ne pas demander à l’utilisateur d’appliquer quand même : corriger les `ownerKey`, créer la référence ou l’item manquant dans le même JSON si c’est fiable, ou retirer les images concernées et fournir un nouveau fichier téléchargeable.

## Métadonnées image

Chaque image doit avoir obligatoirement :

- `key` ;
- `sourceUrl` ;
- `ownerType` ;
- `ownerId` ;
- `ownerKey` ;
- `category` ;
- `isPublished` ;
- `withWatermark` ;
- `setAsCurrent` si elle doit devenir logo ou image principale ;
- `description` interne courte ;
- `altTexts`, `captions`, `credits` dans les 8 langues quand l’image est publique.

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

Avant de décider que l’étape 5 est inutile, auditer l’export actualisé :

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
- `items` lorsque le lot contient une image de parkItem existant
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
      "ownerId": "id-du-parc",
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

## Propriétaires d’images — règle stricte

Pour les images distantes, le propriétaire doit être résolu avant livraison.

Pour une image rattachée à un parkItem déjà présent dans l’export :

```json
{
  "items": [
    {
      "id": "id-du-parkItem",
      "key": "key-exportee-du-parkItem",
      "name": "Nom exact exporté du parkItem"
    }
  ],
  "images": [
    {
      "key": "park-item-main-image",
      "sourceUrl": "https://example.org/photo.jpg",
      "ownerType": "ParkItem",
      "ownerId": "id-du-parkItem",
      "ownerKey": "id-du-parkItem",
      "category": "ParkItem"
    }
  ]
}
```

Le triplet présent dans l’objet image ne remplace pas la déclaration minimale `items[]`. Les deux sont obligatoires avec le processeur actuel.

Triplet de l’image seule, rappel :

```json
{
  "ownerType": "ParkItem",
  "ownerId": "id-du-parkItem",
  "ownerKey": "id-du-parkItem"
}
```

Ne pas utiliser seulement `ownerKey` ou seulement `ownerId`. Le processeur ne doit pas avoir à deviner le propriétaire depuis une URL, un nom de fichier, une légende, un titre ou un nom affiché.

Pour une image de parc :

```json
{
  "ownerType": "Park",
  "ownerId": "id-du-parc",
  "ownerKey": "park"
}
```

Pour une image constructeur :

```json
{
  "ownerType": "AttractionManufacturer",
  "ownerId": "id-du-constructeur",
  "ownerKey": "manufacturer:key-exportee"
}
```

La référence constructeur minimale doit être incluse dans le même JSON lorsque la résolution utilise `ownerKey`.

Contrôle bloquant :

- aucune image distante ne doit produire `Remote image ignored: owner could not be resolved` ;
- aucune image ne doit avoir un `ownerKey` basé sur une URL, un chemin CDN, un nom de fichier ou une valeur reconstruite ;
- chaque image doit posséder le triplet complet dans son propre objet ;
- si le JSON ajoute une image de parkItem, utiliser `ownerId` explicite **et** ajouter le parkItem correspondant dans `items[]` minimal ;
- le nombre d’objets image doit être égal au nombre de triplets complets contrôlés dans le récap de livraison.

## Contrôles avant livraison

- Toutes les URLs images sont techniquement importables selon les règles ci-dessus.
- Tous les propriétaires sont résolus et chaque objet image répète `ownerType`, `ownerId` et `ownerKey`.
- Tous les parkItems propriétaires d’images sont redéclarés dans `items[]` avec `id`, `key` et `name`.
- Aucune URL image ne peut produire `Remote image ignored: owner could not be resolved`.
- Les constructeurs liés aux items ont une biographie ou une limite de source documentée.
- Les fondateurs liés au parc ont une biographie ou une limite de source documentée.
- Les exploitants liés au parc ont une description et des informations utiles ou une limite de source documentée.
- Les crédits sont lisibles pour un visiteur.
- Les logos ne sont pas confondus avec des photos.
- Les images historiques ne prétendent pas montrer une date ou un état qu’elles ne montrent pas.
- Les biographies ne créent pas de doublons de références.
- Toutes les valeurs enum utilisées sont listées dans `park-graph-upsert-enums.md`.

## Après Apply

Demander l’export actualisé pour récupérer les IDs d’images avant de les référencer dans l’histoire.

À la fin de la réponse, ajouter `Pertinence de la prochaine étape` pour l’étape 6 — Horaires et événements nommés. Si aucun calendrier fiable n’existe ou si le parc est fermé sans horaires utiles, indiquer `probablement inutile` ou `à décider` avec la raison. Si l’étape 6 est `probablement inutile`, appliquer la règle de proche en proche de l’orchestrateur jusqu’à la prochaine étape officielle `utile` ou `à décider`, puis attendre la décision utilisateur.
