# Workflow API autonome réservé à Codex

Version : **2026-08-13-r5**

Rôle : **PARK_DATA_EDITOR**

Client : `tools/codex/park-data-editor.ps1`

Ce document complète le parcours officiel des étapes 0 à 9 lorsque **Codex** exécute lui-même les exports, Preview, Apply, contrôles et uploads par API. Il ne remplace aucune règle éditoriale ou métier des fichiers `park-data-integration-steps/`.

## Contrat de la commande `Complète le parc <nom>`

Cette commande courte autorise Codex à :

- rechercher le parc et ses doublons ;
- exécuter toutes les étapes 0 à 9 applicables, en autant de lots bornés que nécessaire ;
- rechercher les données actuelles, tarifaires, historiques et récentes ;
- exécuter Preview/Apply, importer les images, contrôler chaque réponse, tenir un état local consolidé et corriger jusqu’à un audit final propre, précédé d’un export complet frais ;
- fournir un tableau de couverture chiffré et un état `prêt pour publication`.

Elle n’autorise pas Codex à publier le parc, ses nouveaux contenus ou ses images, supprimer, masquer un parc déjà public, nettoyer une entité legacy ambiguë, gérer des utilisateurs ni appeler une route d’administration extérieure aux surfaces autorisées. La publication exige une nouvelle instruction explicite après l’audit de l’étape 9.

Codex ne demande pas à l’utilisateur de relancer manuellement chaque étape. Il consigne les étapes non applicables et continue. Il s’arrête seulement lorsqu’une décision matérielle du propriétaire est nécessaire, que l’identité reste ambiguë, que les droits techniques manquent ou qu’un blocage ne peut pas être corrigé dans le périmètre autorisé.

## Isolation obligatoire du workflow ChatGPT

Ce complément est réservé à Codex authentifié avec un jeton technique `PARK_DATA_EDITOR`.

ChatGPT conserve exactement le workflow existant : l’utilisateur lui fournit les exports actualisés, ChatGPT livre les JSON bornés, et les images externes continuent à passer par les champs d’images distantes du Park Graph Upsert et son processeur actuel. ChatGPT ne doit ni utiliser le client technique, ni télécharger localement une image, ni appeler les endpoints `park-data-editor/*`.

Cette isolation ne crée aucune différence de qualité : logo, couverture des attractions, inventaire historique, contextualisation des images, localisation et charte éditoriale restent obligatoires dans les deux parcours.

## Garanties d’architecture et de sécurité

- Le jeton est opaque, aléatoire et stocké uniquement sous forme de hash côté serveur.
- Sa durée est comprise entre 1 et 90 jours, avec au plus 3 jetons actifs par compte.
- Chaque requête recharge le jeton et le compte. Expiration, révocation, blocage du compte ou retrait du rôle coupent donc l’accès immédiatement.
- Le principal issu du jeton ne reçoit que le rôle `PARK_DATA_EDITOR`, jamais `ADMIN` ni `USER`.
- Le jeton est refusé par défaut sur toute route non explicitement ouverte à ce mécanisme.
- Toutes ses requêtes, y compris les lectures, refus et erreurs HTTP, sont écrites dans l’audit avec l’utilisateur, le jeton, l’adresse IP, le User-Agent, le statut et le trace ID.
- Un administrateur peut voir et révoquer un jeton, ou tous les jetons du compte, depuis la fiche utilisateur. Retirer le rôle est un coupe-circuit supplémentaire immédiat.
- Le secret en clair n’est retourné qu’une fois. Le client le conserve hors du dépôt avec le chiffrement lié à l’utilisateur Windows courant.
- Les identifiants du compte technique sont conservés séparément avec le même chiffrement Windows afin de permettre un renouvellement explicite sans navigateur. Ils ne sont jamais placés dans le dépôt, un fichier JSON, une ligne de commande en clair ou une sortie de log.

## Compte de référence et connexion courante

Le compte déjà provisionné pour ce parcours est `admin@amusement-parks.fun`, auquel le propriétaire attribue le droit spécifique `PARK_DATA_EDITOR`. Ne jamais lancer `RegisterAccount` pour cette adresse existante et ne jamais inscrire son mot de passe ou un jeton dans le dépôt.

Au début d’une commande de complétude :

1. Utiliser d’abord le jeton technique déjà chiffré localement et vérifier l’accès avec une lecture autorisée comme `SearchParks`.
2. Si aucun jeton local n’existe, si son expiration naturelle est connue ou si un nouveau jeton a été explicitement demandé, utiliser `CreateToken`. Le client se connecte alors avec les identifiants de `admin@amusement-parks.fun` conservés dans le coffre local Windows, obtient un JWT court, crée un jeton `PARK_DATA_EDITOR`, le chiffre localement et n’en affiche jamais la valeur.
3. Si les identifiants ne sont pas enregistrés localement, demander leur amorçage sécurisé avec `SaveAccountCredential` plutôt que de demander un mot de passe dans la conversation ou de l’ajouter à une commande en clair.
4. Sur un `401` inattendu, une révocation ou un retrait de rôle, s’arrêter et signaler le blocage. Ne pas recréer automatiquement un jeton, car cela pourrait contourner une décision de sécurité du propriétaire.

Le compte peut disposer d’autres droits dans l’application, mais le principal construit à partir du jeton reste limité à `PARK_DATA_EDITOR`. Codex n’utilise donc jamais ses éventuels droits d’administration par un autre canal.

## Amorçage initial ou changement de compte sans navigateur

Cette procédure s’applique uniquement si le coffre local n’a pas encore été préparé, ou si le propriétaire demande explicitement de changer de compte :

1. Enregistrer l’adresse et le mot de passe dans le coffre local Windows, sans les écrire dans le dépôt, la conversation, la ligne de commande ni les logs. Pour `admin@amusement-parks.fun`, utiliser uniquement le mot de passe existant transmis par le canal sécurisé du propriétaire ; générer un mot de passe aléatoire fort seulement pour un compte réellement nouveau :

   ```powershell
   $password | .\tools\codex\park-data-editor.ps1 `
     -Action SaveAccountCredential `
     -AccountEmail 'admin@amusement-parks.fun'
   ```

2. Si le compte n’existe réellement pas, Codex lance `RegisterAccount`, qui crée un compte local ordinaire par `POST /api/users`. Cette étape est interdite pour `admin@amusement-parks.fun`, déjà provisionné.
3. Pour un nouveau compte seulement, le propriétaire confirme l’adresse reçue et assigne lui-même `PARK_DATA_EDITOR` depuis l’administration.
4. Après cette autorisation explicite, Codex lance :

   ```powershell
   .\tools\codex\park-data-editor.ps1 -Action CreateToken
   ```

   Le client appelle `POST /api/auth/login`, puis `POST /api/park-data-editor/tokens` avec le JWT court obtenu. Un jeton technique ne peut jamais créer un autre jeton. Le secret retourné une seule fois est immédiatement chiffré localement et n’est pas affiché.

Aucune session de navigateur n’est nécessaire à Codex. `SaveToken` reste disponible pour importer exceptionnellement un jeton créé par un autre client, toujours par le pipeline et sans l’afficher.

Un jeton expiré n’est jamais réactivé. Après expiration naturelle, Codex peut exécuter explicitement `CreateToken` pour en créer un nouveau. Il ne doit jamais le faire automatiquement en réaction à un `401`.

Une révocation manuelle interrompt le workflow. Codex ne recrée un jeton qu’après une nouvelle instruction explicite du propriétaire. La révocation vise un jeton; pour empêcher techniquement toute nouvelle création, le propriétaire retire le rôle `PARK_DATA_EDITOR` ou bloque le compte avant ou en même temps que la révocation.

Pour couper l’accès depuis Codex :

```powershell
.\tools\codex\park-data-editor.ps1 -Action RevokeCurrent
```

`ClearToken` efface seulement la copie locale et ne remplace pas une révocation serveur.
`ClearAccountCredential` efface les identifiants locaux sans supprimer le compte; il rend tout renouvellement autonome impossible jusqu’à un nouvel amorçage.

## Surfaces API autorisées

| Besoin | Surface |
|---|---|
| Vérifier l’activité globale avant une opération coûteuse | `GET park-data-editor/operations/status` |
| Rechercher un parc, y compris masqué ou fermé | `GET park-data-editor/parks` |
| Exporter le graphe courant | job `POST admin/park-graph-upserts/bulk/export-jobs`, suivi puis téléchargement reprenable |
| Prévisualiser/appliquer un lot borné | `POST admin/park-graph-upserts/preview` puis `apply` |
| Prévisualiser/appliquer une suppression contrôlée explicitement autorisée | mêmes routes via `PreviewDeletion` puis `ApplyDeletion` |
| Lire l’historique d’intégration | `GET admin/park-graph-upserts/history` |
| Contrôler la complétude courante ou projetée pour publication | `GET park-data-editor/parks/{id}/data-completeness` |
| Téléverser/rattacher/documenter une image de parc | `park-data-editor/images/*` |
| Préparer puis publier explicitement un lien Facebook | `GET park-data-editor/social-publications/facebook/draft`, puis `POST park-data-editor/social-publications/facebook` |
| Révoquer le jeton courant | `DELETE park-data-editor/tokens/current` |

Le rôle n’ouvre pas la gestion des utilisateurs, l’audit, la sécurité, les autres opérations de réseaux sociaux, le SEO, les sources de données, une route générale de suppression ou les autres fonctions d’administration. La seule suppression disponible est le lot contrôlé `suppr` d’un parc, limité aux images, parkItems et zones explicitement identifiés et autorisés.

`SearchParks` accepte `-Page` et `-PageSize` pour parcourir séquentiellement l’inventaire complet sans contourner le client officiel. La taille de page est limitée à 50 et chaque page doit être traitée avant d’appeler la suivante.

`Completeness` renvoie le score de l’état réellement publié par défaut. Pour la reprise d’un parc déjà visible sélectionné dans le backlog, `-ProjectForPublication` simule le score obtenu après validation du parc et publication des médias et articles déjà intégrés :

```powershell
.\tools\codex\park-data-editor.ps1 -Action Completeness -ParkId '<park-id>' -ProjectForPublication
```

Cette projection est en lecture seule. Elle ne change aucune visibilité, ne publie aucun contenu et ne rend jamais publiable une fiche `NotRelevant`. Elle ne doit être utilisée comme feu vert qu’après l’audit final sans bloqueur ; le score courant doit être recalculé après la publication effective.

## Coordination globale obligatoire

Plusieurs instances Codex peuvent partager le même serveur sans partager leur état local. Avant tout export, Preview, Apply, import d’image ou autre demande coûteuse, chaque instance doit donc interroger l’état global avec le client officiel :

```powershell
.\tools\codex\park-data-editor.ps1 -Action Status
```

La réponse agrège les requêtes et exports actifs de **tous** les jetons `PARK_DATA_EDITOR`, sans exposer leur identifiant, leur libellé ou un secret. `initiatedByCurrentToken` sert seulement au diagnostic : une opération externe et une opération du jeton courant imposent exactement la même attente.

Les règles suivantes sont non négociables :

1. Une instance Codex n’émet jamais deux appels `PARK_DATA_EDITOR` en parallèle.
2. Si `isBusy` vaut `true` ou si `canStartResourceIntensiveOperation` vaut `false`, Codex n’envoie aucune nouvelle demande coûteuse. Il attend au minimum `recommendedPollIntervalSeconds`, puis relit l’état.
3. Le suivi de l’état global et celui d’un job d’export sont espacés d’au moins cinq secondes. Il est interdit de lancer une boucle rapide, plusieurs pollers ou un second export « de secours ».
4. Tout `429` avec le code `park-data-editor.operation-busy` impose d’honorer `Retry-After`. Les reprises restent séquentielles ; augmenter le nombre d’instances ou changer de jeton pour contourner le refus est interdit.
5. Après une coupure ou une réponse perdue, Codex vérifie d’abord cet état, puis le job d’export ou l’historique d’intégration concerné. Il ne rejoue jamais aveuglément une mutation potentiellement acceptée.

Le serveur rend ces règles contraignantes : au plus deux requêtes techniques ordinaires peuvent être actives, un seul traitement coûteux peut s’exécuter, un seul export peut être en file ou en cours, et les dépassements sont refusés immédiatement sans file d’attente HTTP. L’endpoint d’état est lui-même limité par jeton. Le client officiel effectue l’attente préalable pour les exports, Preview, Apply et uploads, respecte les `429` et sonde les jobs d’export toutes les cinq secondes.

## Publication Facebook explicite

Une complétude ou une publication de données ne déclenche jamais cette opération par elle-même. Codex utilise cette surface uniquement lorsque l’utilisateur demande explicitement une publication Facebook, qu’elle concerne une fiche parc, un parkItem, une vidéo ou une autre page publique reconnue.

1. Résoudre obligatoirement le brouillon depuis l’URL publique. La réponse fournit l’URL normalisée, la cible reconnue, le texte bilingue automatique actuel et une page d’images publiques éligibles :

   ```powershell
   .\tools\codex\park-data-editor.ps1 -Action ResolveFacebookPublication `
     -Url 'https://amusement-parks.fun/fr/park/park-id/slug' `
     -ImagePage 1 -ImagePageSize 6
   ```

   Pour une page parc, la réponse fournit aussi `hasPublishedParkAnnouncement`, `parkAnnouncementId`, `parkAnnouncementStatus` et `parkAnnouncementExternalUrl`. Ces champs permettent au workflow du backlog de vérifier l’annonce idempotente et de relancer exactement son enregistrement échoué sans lire l’administration ni la base de données.

2. Parcourir les pages suivantes si nécessaire. Une image est sélectionnable seulement si son identifiant apparaît dans la réponse de cette même cible. Pour une fiche parc ou ses sous-pages, seules ses images publiques de catégorie `PARK` sont proposées ; pour un parkItem, seules ses images publiques de catégorie `PARK_ITEM` le sont. Une page sans propriétaire d’image conserve simplement son Open Graph automatique.
3. Publier après le contrôle d’activité global effectué par le client :

   ```powershell
   .\tools\codex\park-data-editor.ps1 -Action PublishFacebook `
     -Url 'https://amusement-parks.fun/fr/park/park-id/slug' `
     -ImageId 'id-retourne-par-le-brouillon'
   ```

   Omettre `-Message` conserve le texte automatique du brouillon. Fournir `-Message '...'` applique le texte explicite de l’utilisateur. Omettre `-ImageId` conserve exactement les règles et l’image Open Graph actuelles.
4. Ne jamais deviner un identifiant, reprendre une image d’un autre parc ou appeler directement l’administration. Le serveur revalide au moment de la publication la visibilité, la catégorie et le propriétaire de l’image ; un choix devenu privé ou étranger est refusé.
5. Rapporter séparément le résultat Facebook et celui d’une éventuelle publication de données. Une annonce automatique de première publication d’un parc reste indépendante : ne pas la doubler par une publication manuelle sans instruction explicite.

Lorsqu’une annonce automatique de parc existe avec le statut `Failed`, le workflow du backlog peut relancer exclusivement ce même enregistrement après un nouveau contrôle d’activité global :

```powershell
.\tools\codex\park-data-editor.ps1 -Action RetryFacebookPublication `
  -ParkId 'park-id' `
  -PublicationId 'publication-id-retourne-par-le-serveur'
```

`ParkId` et `PublicationId` doivent correspondre à l’annonce Facebook automatique du parc. La commande refuse une publication manuelle, étrangère ou non rattachée au parc, et le service n’accepte la reprise que depuis le statut `Failed`. Avant tout nouvel envoi, il recherche dans les publications de la Page le message exact autour de l’heure de la tentative : une correspondance unique réconcilie l’enregistrement sans doublon, aucune correspondance autorise seulement alors la relance du même enregistrement, et une recherche impossible ou ambiguë bloque tout envoi. Après l’appel, relancer `ResolveFacebookPublication` et exiger le statut `Published`. Ne jamais rappeler `PublishFacebook` ni fabriquer une publication manuelle pour contourner un échec : si la reprise ne publie ou ne réconcilie pas l’annonce, conserver la ligne du backlog et signaler le blocage.

Pour une page parc, `PublishFacebook` appelé sans `Message` et sans `ImageId` utilise le chemin d’annonce idempotent du parc. Une publication existante portant la clé du parc est renvoyée au lieu d’être recréée. Un message ou une image explicitement personnalisés restent une publication manuelle distincte.

## Export asynchrone et reprenable

`ExportPark` utilise le job d’export bulk pour un seul identifiant, attend son achèvement, télécharge le fichier avec reprise HTTP par plages d’octets, vérifie sa taille et son JSON, puis extrait le document du parc. Le fichier remis à Codex conserve donc le contrat `AmusementParkParkGraphUpsert` d’un parc unique ; l’enveloppe bulk temporaire ne fuit pas dans les lots de travail.

```powershell
.\tools\codex\park-data-editor.ps1 -Action ExportPark `
  -ParkId 'park-id' `
  -Sections ParkBasics,Items,Images `
  -OutputPath .\work\park-items-images.json
```

- Sans `-Sections`, le client exporte toutes les sections.
- Pendant les étapes 0 à 8, aucun appel complet à `ExportPark` n’est obligatoire. L’état local est construit depuis la recherche du parc, les éventuels exports ciblés strictement nécessaires et les réponses réussies des mutations.
- Avant l’étape 9, appeler une fois `ExportPark` sans `-Sections` afin d’obtenir l’état complet frais sur lequel repose l’audit final. La liste complète inclut la section `Pricing`.
- Avant ce jalon, un export avec `-Sections` est réservé à l’identification de l’existant ou à une incohérence précise, une réponse de mutation perdue, un ID indispensable ou une dépendance absente des résultats ; il ne devient jamais une routine de fin de lot.
- Une coupure pendant le téléchargement est reprise dans la même exécution ; le client n’écrit le fichier final qu’après contrôle de la longueur et du document retourné.
- Le client privilégie le `curl` système sous Windows et télécharge les exports volumineux par plages courtes dont chaque longueur est contrôlée avant assemblage. Chaque plage dispose d’un nombre de tentatives borné ; les fichiers `.partial` et `.chunk` sont supprimés après toute sortie normale, réussie ou en erreur. Une coupure de proxy ne doit donc produire ni attente indéfinie ni faux export final.
- Les appels JSON utilisent un client HTTP dédié avec connexions non persistantes et un délai borné de cinq minutes. Seul un `429` qui prouve que le serveur a refusé le traitement avant exécution peut être repris automatiquement en respectant `Retry-After` ; une coupure réseau ou une réponse perdue ne relance jamais automatiquement une requête mutante.
- Un export final absent, tronqué, d’un autre parc ou avec plusieurs parcs est rejeté. Ne jamais continuer avec l’ancien fichier présent au même chemin.
- Ne pas recréer un téléchargeur improvisé, revenir à l’ancienne route synchrone, utiliser l’administration ou lire la base de données pour contourner un échec.
- `Preview`, `SearchParks` et un export peuvent être relancés après une erreur réseau puisqu’ils ne modifient pas le graphe. Ne jamais relancer aveuglément un `Apply` dont la réponse a été interrompue : rejouer d’abord la Preview exacte du lot ou produire un export ciblé, prouver si les changements restent attendus et reprendre seulement à cette frontière.

## Boucle obligatoire pour chaque étape officielle

Les noms, l’ordre et le contenu des étapes restent ceux de l’orchestrateur existant.

1. Lire l’orchestrateur, le fichier exact de l’étape et, si nécessaire, le fichier des enums.
2. À l’étape 0, rechercher d’abord les doublons par l’API technique. Ne demander que les sections indispensables pour identifier l’existant et cadrer le premier lot ; un export complet n’est pas un prérequis.
3. Pour les étapes 1 à 8, maintenir un registre local consolidé des entités, IDs, clés, compteurs et lacunes à partir de la recherche initiale, des éventuels exports ciblés et des réponses réussies de Preview, Apply et d’import d’image. Ne produire un export ciblé que pour lever une ambiguïté, récupérer un identifiant créé ou vérifier une dépendance précise ; l’unique export complet obligatoire n’intervient qu’immédiatement avant l’étape 9.
4. Rechercher et sourcer seulement les données de l’étape courante. Produire un JSON borné et conserver le compteur traité/total ainsi que le registre des lacunes.
5. Exécuter `Preview`. Examiner toutes les erreurs, tous les warnings, les entités résolues et chaque changement de champ.
6. Ne jamais exécuter `Apply` si `canApply` est faux, si une erreur existe ou si un warning bloquant subsiste. Par défaut, le client bloque même les warnings non bloquants; `-AllowWarnings` exige une décision explicite après lecture.
7. Le client écrit un reçu contenant le hash SHA-256 du JSON, l’API visée et l’heure du Preview. `Apply` refuse un reçu âgé de plus de 30 minutes ou un JSON modifié depuis le Preview.
8. Après `Apply`, contrôler `isApplied`, les erreurs, les warnings, les compteurs et `changes`, puis intégrer les IDs, clés et valeurs acceptées au registre local. Ne pas réexporter le parc à ce moment-là ; demander seulement un export ciblé si la réponse est ambiguë, si le lot suivant dépend d’un identifiant créé ou si une vérification précise l’exige.
9. Terminer tous les lots applicables avant de poursuivre. Une étape objectivement non applicable peut être consignée puis traversée sans pause ; une étape applicable ne peut jamais être sautée pour accélérer le parcours.
10. Ne jamais publier un parc, masquer une donnée publique ou supprimer un contenu au-delà du lot annoncé. Pour un parc existant déjà visible, préserver sa visibilité pendant l’enrichissement.
11. Après chaque étape, rapprocher les compteurs consolidés localement avec les objectifs de couverture établis à l’étape 0. Une réussite `Apply` ne prouve pas la complétude éditoriale.
12. Immédiatement avant l’étape 9, effectuer l’unique export complet obligatoire, puis reconstruire les compteurs et l’état de référence depuis ce fichier frais avant de lancer l’audit.

Exemple :

```powershell
.\tools\codex\park-data-editor.ps1 -Action Preview -JsonPath .\work\park-step-03.json
.\tools\codex\park-data-editor.ps1 -Action Apply `
  -JsonPath .\work\park-step-03.json `
  -ReceiptPath .\work\park-step-03.preview-receipt.json
```

## Suppression contrôlée après autorisation explicite

Une commande de complétude ou de publication n’autorise jamais implicitement une suppression. Codex n’utilise ce parcours que lorsque le propriétaire a explicitement demandé de retirer un lot identifié, par exemple des doublons confirmés. Le document ne peut contenir aucune autre mutation et respecte toutes les garanties suivantes :

- `targetParkId` et `identity.parkId`, lorsqu’il est présent, désignent exactement le parc annoncé ;
- `createIfMissing` et `replaceCollections` valent `false`, tandis que `document.mode` vaut `merge` ;
- `suppr` contient exactement un objet avec uniquement `entityType` et `id` ; un nettoyage de plusieurs doublons est une file d’opérations unitaires, strictement séquentielles ;
- les seuls types acceptés sont `Image`, `ParkItem` et `ParkZone` ; aucun identifiant nu, alias implicite ou suppression par absence n’est accepté ;
- les IDs proviennent d’un export frais et chaque dépendance à retirer est listée explicitement ; pour un doublon d’image, vérifier notamment qu’il n’est ni courant ni publié et que la copie conservée est bien identifiée ;
- toute suppression d’un contenu public, courant ou partagé exige une instruction spécifique qui annonce cette conséquence ;
- aucun warning, aucune erreur, aucune cible manquante et aucune mutation supplémentaire ne sont tolérés entre la Preview et l’Apply.

Exemple de document borné :

```json
{
  "targetParkId": "park-id",
  "createIfMissing": false,
  "replaceCollections": false,
  "document": {
    "mode": "merge",
    "identity": {
      "parkId": "park-id"
    },
    "suppr": [
      {
        "entityType": "Image",
        "id": "duplicate-image-id"
      }
    ]
  }
}
```

L’exécution utilise exclusivement le client officiel et un reçu dédié, non interchangeable avec celui d’un Apply ordinaire :

```powershell
.\tools\codex\park-data-editor.ps1 -Action PreviewDeletion `
  -ParkId 'park-id' `
  -JsonPath .\work\park-duplicates.deletion.json
.\tools\codex\park-data-editor.ps1 -Action ApplyDeletion `
  -ParkId 'park-id' `
  -JsonPath .\work\park-duplicates.deletion.json `
  -ReceiptPath .\work\park-duplicates.deletion-preview-receipt.json
```

`PreviewDeletion` exige exactement un changement `Deleted` correspondant à la cible annoncée et refuse tout warning ou changement inattendu. `ApplyDeletion` vérifie à nouveau le parc, le hash, l’âge du reçu et l’identité de la cible. Le serveur résout et valide la totalité de tout document `suppr` avant la première mutation, rejette les cibles répétées et interrompt le traitement au premier échec. Le client contrôlé impose en plus une seule cible par Apply : une panne ne peut donc pas produire la réussite partielle d’un lot multi-cible. Une image n’est considérée supprimée que lorsque toutes ses variantes binaires ont été retirées du stockage avant ses métadonnées. Un échec binaire conserve donc les métadonnées et fait échouer l’opération. Les parkItems et zones sont supprimés par leurs repositories applicatifs ; leurs images ou dépendances éventuelles doivent faire l’objet de leurs propres opérations unitaires préalablement contrôlées.

Après Apply, produire l’export ciblé nécessaire pour prouver l’absence de chaque ID et la conservation des entités attendues. Après une réponse perdue ou ambiguë, vérifier l’état global et l’historique puis réexécuter une Preview : ne jamais relancer directement `ApplyDeletion`.

## Étape 5 : parcours Codex obligatoire des images

Pour les nouvelles images de parc, de parkItem ou d’attraction autonome, Codex utilise `ImportPhoto` et les surfaces `park-data-editor/images/*`. Les images distantes intégrées au JSON restent le mécanisme de ChatGPT ; elles ne justifient pas un retour de Codex vers l’administration.

### Inventaire de couverture avant import

Construire depuis l’export une ligne par propriétaire avec : identifiant, type, statut, image courante, nombre d’images, image recherchée et résultat. Le registre doit couvrir au minimum :

- logo officiel actuel ;
- image principale du parc ;
- chaque attraction actuelle ;
- chaque attraction annoncée ou en construction ;
- chaque attraction définitivement fermée ;
- chaque jalon et article historique existant ou créé à l’étape 8.

### Recherche et validation d’une image

1. Privilégier une source officielle ou presse, puis une archive, un fonds de contributeur ou une source spécialisée fiable.
2. Vérifier la page source, les conditions d’utilisation disponibles, l’auteur, les crédits et l’URL finale du fichier.
3. Inspecter visuellement l’image elle-même avant l’import : sujet exact, époque compatible, cadrage utile, qualité suffisante, absence de watermark ou logo incrusté d’un site tiers.
4. Accepter une photo non officielle lorsqu’elle montre sans ambiguïté la bonne entité, reste créditable et satisfait tous les contrôles. Refuser une image générique, un mauvais item, une miniature inutilisable ou un rendu présenté comme une photographie.
5. Si aucune image acceptable n’est trouvée après recherche dans les familles de sources pertinentes, conserver une exception détaillée au lieu de forcer un visuel trompeur.

### Téléchargement, upload et rattachement

Pour chaque fichier validé :

1. Télécharger l’URL publique HTTP(S) avec `curl`, au maximum 5 redirections, 90 secondes et 10 Mo.
2. Vérifier les octets réellement téléchargés. Le client n’accepte que JPEG, PNG ou WebP et supprime toujours le fichier temporaire.
3. Envoyer le fichier à `POST park-data-editor/images`.
4. Rattacher l’image au `Park`, `ParkItem` ou `StandaloneAttraction` exact grâce à son ID exporté.
5. Enregistrer l’URL source, les crédits, textes alternatifs, légendes, publication et statut courant via un fichier `MetadataJsonPath` complet.
6. Contrôler la réponse de l’import et l’intégrer au registre local : ID, propriétaire, catégorie, métadonnées, publication et statut courant. Ne pas réexporter le parc après l’import ; demander la section `Images` seulement si la réponse est ambiguë ou ne fournit pas l’identifiant indispensable au lot suivant. Le contrôle exhaustif aura lieu sur l’export complet frais précédant l’étape 9.

Le téléchargement local ne contourne jamais le traitement applicatif. L’upload appelle le même `UploadImageCommandHandler`, le même `IImageProcessingPipeline` et le même stockage que l’upload existant : détection, métadonnées, conversion, compression, variantes et contraintes continuent donc à s’appliquer.

Pour une image récupérée, le watermark est **désactivé par défaut**. Il ne doit être activé avec `-WithWatermark $true` que sur indication explicite. La règle existante des logos reste prioritaire et empêche leur watermark.

Ne pas importer une image sans `MetadataJsonPath` : le comportement par défaut crée des listes vides pour les textes alternatifs, légendes et crédits et ne satisfait donc pas le contrat de complétude.

Le paramètre `IsPublished` du client vaut `true` par défaut et écrase la valeur éventuellement présente dans `MetadataJsonPath`. Pendant `Complète le parc <nom>`, Codex doit donc passer explicitement `-IsPublished $false` à **chaque** appel `ImportPhoto`, depuis le premier upload jusqu’à la fin de l’audit. Cette règle s’applique aussi à un parc déjà public : le nouveau média reste privé sans modifier la visibilité des médias existants.

Le fichier de métadonnées contient les 8 langues publiques pour `altTexts`, `captions` et `credits`. Les textes alternatifs et légendes sont naturels, spécifiques et destinés au visiteur. Ils ne mentionnent jamais l’URL, l’import, le format, la résolution, le propriétaire technique, la méthode de vérification, les droits, le score ou l’absence d’une autre photo. Les informations d’auteur, source et licence restent dans `credits`.

### Choix du statut courant

- Logo : `Category LOGO`, `OwnerType PARK`, `WithWatermark $false`, `SetAsCurrent $true` ; vérifier ensuite l’identifiant de logo courant du parc et son rendu public.
- Image principale du parc : `Category PARK`, courante seulement si elle est la meilleure vue représentative et ne remplace pas le logo.
- Image principale d’une attraction : `Category PARK_ITEM`, courante si aucune meilleure image courante n’existe.
- Image historique, chantier, rendu ou illustration d’article : `SetAsCurrent $false`, sauf décision éditoriale explicitement justifiée.

Le paramètre `SetAsCurrent` vaut `true` par défaut dans le client. Codex doit donc toujours le choisir consciemment et fournir `$false` pour une image secondaire.

Exemple avec un fichier de métadonnées localisées :

```powershell
.\tools\codex\park-data-editor.ps1 -Action ImportPhoto `
  -SourceUrl 'https://source.example/photo.webp' `
  -Category PARK_ITEM `
  -OwnerType PARK_ITEM `
  -OwnerId 'park-item-id' `
  -IsPublished $false `
  -MetadataJsonPath .\work\photo-metadata.json
```

Pour corriger les métadonnées d’une image déjà rattachée sans réimporter le fichier ni créer de doublon, utiliser son ID exporté et un document complet :

```powershell
.\tools\codex\park-data-editor.ps1 -Action UpdatePhotoMetadata `
  -ImageId 'image-id' `
  -MetadataJsonPath .\work\photo-metadata.json
```

Le fichier doit reprendre explicitement l’identité exportée `imageId` ou `id`, puis `category`, `ownerType`, `ownerId`, `isCurrent`, `description`, `geoLocation`, `altTexts`, `captions`, `credits`, `tagIds`, `isPublished` et `sourceUrl`. Le client refuse l’appel si l’identité du document ne correspond pas à `-ImageId`, puis retire les champs d’identité avant le PUT. Il exige aussi exactement une valeur non vide dans chacune des huit langues pour les textes alternatifs, légendes et crédits. Construire ce document depuis l’export courant afin de préserver le rattachement, la publication, le statut courant, la source, la géolocalisation et les tags ; ne jamais envoyer un fragment qui effacerait silencieusement les autres métadonnées.

L’export emploie les noms métier tels que `ParkItem`, tandis que le contrat HTTP utilise les enums publics tels que `PARK_ITEM`. Le client convertit explicitement `category` et `ownerType`, y compris l’ancien alias `Attraction`, avant l’envoi. Ne pas modifier manuellement le document exporté pour contourner ce décalage.

Les catégories de compte, commentaire, vidéo, exploitant, constructeur et fondateur sont refusées. Si le rattachement ou les métadonnées échouent après l’upload, le client signale l’identifiant de l’orpheline. Codex doit d’abord l’inspecter et obtenir une autorisation explicite ; il peut ensuite employer uniquement `PreviewDeletion` puis `ApplyDeletion`, avec l’ID et le parc exacts.

### Audit après images

Après les imports, actualiser le registre local : logo courant attendu, image principale du parc, attractions avec image/total pour chaque statut, attractions fermées avec image/total, jalons avec image/total, articles avec image/total et liste exacte des exceptions. Vérifier les réponses pour éviter les doublons et les remplacements d’image courante inappropriés ; l’export complet frais préalable à l’étape 9 confirme ensuite ces résultats et porte le tableau annoncé.

Un warning de doublon d’image distante peut être non bloquant uniquement si l’état de référence prouve que la source est déjà liée au bon propriétaire et qu’aucune modification n’était attendue. Tous les autres warnings doivent être compris et corrigés avant de poursuivre.

## Étape 7 : parcours Codex des tarifs

Codex traite les tarifs exclusivement par un JSON Park Graph Upsert borné et la boucle officielle `Status` → `Preview` → `Apply`. Il ne crée pas de service parallèle, n’appelle pas une route d’administration improvisée et ne modifie pas directement la base de données.

1. Confirmer dans le registre que `park.status` vaut `Operating`. Dans tout autre cas, consigner l’étape comme non applicable sans envoyer de section `pricing`.
2. Rechercher la page tarifaire et la billetterie officielles actuelles, puis construire le bloc canonique `pricing` conformément à `park-data-integration-steps/07-pricing.md`.
3. Exécuter Preview sur un lot qui ne contient que l’identité minimale et `pricing`. Contrôler le parc cible, les modes, montants, périodes, codes, compteurs et chaque champ annoncé comme créé ou modifié.
4. Appliquer avec le reçu exact, contrôler la réponse et reporter dans le registre la devise, les trois compteurs d’offres, les périodes, URLs et `lastVerifiedAtUtc`.
5. Ne pas réexporter après Apply. Si la réponse est ambiguë, un export ciblé est disponible avec `-Sections Pricing`. L’export complet final de l’étape 9 inclut aussi cette section et doit permettre un round-trip sans perte fonctionnelle.

Une grille vide ne constitue pas une suppression. La commande `Complète le parc` n’autorise pas Codex à effacer une grille existante ni à remplacer une donnée actuelle par un prix non vérifié.

## Audit final

Juste avant de commencer l’étape 9, Codex doit effectuer l’unique export complet obligatoire du parcours. À partir de cet état frais, il exécute le contrôle de complétude et vérifie les compteurs attendus, les propriétaires d’images, le logo courant, les tarifs, les sources et crédits, les warnings résiduels, la visibilité conservée et l’historique d’Apply. Il fournit le tableau quantitatif exigé par l’étape 9 et ne conclut pas sur le seul score numérique.

Il compare aussi le corpus public après retrait des titres et noms d’entités : descriptions du parc, zones et items, textes historiques, sous-titres d’articles, descriptions, textes alternatifs et légendes d’images. Tout paragraphe de secours répété, conseil d’itinéraire ou traduction générique impose une reprise ciblée avant la conclusion.

Il effectue également, langue par langue, un balayage par familles de vocabulaire mécanique et de spécifications chiffrées. Les regroupements de rails, voies, véhicules, sièges, structures, rotations, accélérations ou trajectoires sont relus manuellement : un nom physique isolé peut décrire honnêtement la scène, mais une succession opératoire ou une fiche de vitesse, durée, capacité et comptage doit être réécrite et laissée aux champs structurés.

Si l’export complet préalable à l’étape 9 échoue ou arrive tronqué, ne jamais le présenter comme l’état courant ni le remplacer silencieusement par un ancien export ou par le registre consolidé. Réessayer par la surface technique autorisée et obtenir impérativement un nouvel export complet valide. Hors audit final, une Preview exacte du lot peut prouver si une écriture ambiguë reste attendue ; utiliser un export ciblé seulement lorsqu’il apporte une information supplémentaire nécessaire. Ne pas basculer vers l’administration ou la base de données.

Après une instruction explicite de publication, Codex contrôle d’abord l’état global des opérations, obtient un nouvel export complet frais et rejoue sur cet état réel tous les contrôles bloquants de l’étape 9. Toute différence inexpliquée avec l’export audité ou les reçus de correction suspend la publication. Ce contrôle appartient au flux de publication séparément autorisé et ne change pas l’unique export complet obligatoire du parcours de complétion 0 à 9. Codex suit ensuite l’ordre de l’étape 9 : publier de façon ciblée les images et contenus dépendants prêts, puis les articles, contrôler les parkItems, et enfin valider et rendre visible le nouveau parc en dernier. La publication des images réutilise leurs IDs exportés et la surface de métadonnées autorisée ou un JSON upsert borné conforme au contrat exporté ; elle ne réimporte jamais les fichiers. Codex vérifie ensuite les pages publiques anonymes, le logo, les articles, la complétude et l’idempotence d’un dernier Preview. Une annonce sociale indisponible est rapportée séparément et n’autorise aucun appel à une route admin interdite.

Le propriétaire peut rapprocher chaque appel avec `park-data-editor.request` dans le journal d’audit et filtrer par compte, email, trace ID ou identifiant de jeton.
