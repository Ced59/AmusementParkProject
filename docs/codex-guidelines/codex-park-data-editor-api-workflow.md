# Workflow API autonome réservé à Codex

Version : **2026-08-06**

Rôle : **PARK_DATA_EDITOR**

Client : `tools/codex/park-data-editor.ps1`

Ce document complète le parcours officiel des étapes 0 à 8 lorsque **Codex** exécute lui-même les exports, Preview, Apply, contrôles et uploads par API. Il ne remplace aucune règle éditoriale ou métier des fichiers `park-data-integration-steps/`.

## Contrat de la commande `Complète le parc <nom>`

Cette commande courte autorise Codex à :

- rechercher le parc et ses doublons ;
- exécuter toutes les étapes 0 à 8 applicables, en autant de lots bornés que nécessaire ;
- rechercher les données actuelles, historiques et récentes ;
- exécuter Preview/Apply, importer les images, réexporter et corriger jusqu’à un audit final propre ;
- fournir un tableau de couverture chiffré et un état `prêt pour publication`.

Elle n’autorise pas Codex à publier le parc, ses nouveaux contenus ou ses images, supprimer, masquer un parc déjà public, nettoyer une entité legacy ambiguë, gérer des utilisateurs ni appeler une route d’administration extérieure aux surfaces autorisées. La publication exige une nouvelle instruction explicite après l’audit de l’étape 8.

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
| Rechercher un parc, y compris masqué ou fermé | `GET park-data-editor/parks` |
| Exporter le graphe courant | `GET admin/park-graph-upserts/parks/{id}/export` |
| Prévisualiser/appliquer un lot borné | `POST admin/park-graph-upserts/preview` puis `apply` |
| Lire l’historique d’intégration | `GET admin/park-graph-upserts/history` |
| Contrôler la complétude | `GET park-data-editor/parks/{id}/data-completeness` |
| Téléverser/rattacher/documenter une image de parc | `park-data-editor/images/*` |
| Révoquer le jeton courant | `DELETE park-data-editor/tokens/current` |

Le rôle n’ouvre pas la gestion des utilisateurs, l’audit, la sécurité, les réseaux sociaux, le SEO, les sources de données, la suppression d’images ou les autres fonctions d’administration.

## Boucle obligatoire pour chaque étape officielle

Les noms, l’ordre et le contenu des étapes restent ceux de l’orchestrateur existant.

1. Lire l’orchestrateur, le fichier exact de l’étape et, si nécessaire, le fichier des enums.
2. À l’étape 0, rechercher d’abord les doublons par l’API technique. Exporter le parc s’il existe.
3. Avant toute étape suivante, exporter à nouveau le graphe après l’Apply précédent. Cet export devient l’unique état de référence.
4. Rechercher et sourcer seulement les données de l’étape courante. Produire un JSON borné et conserver le compteur traité/total ainsi que le registre des lacunes.
5. Exécuter `Preview`. Examiner toutes les erreurs, tous les warnings, les entités résolues et chaque changement de champ.
6. Ne jamais exécuter `Apply` si `canApply` est faux, si une erreur existe ou si un warning bloquant subsiste. Par défaut, le client bloque même les warnings non bloquants; `-AllowWarnings` exige une décision explicite après lecture.
7. Le client écrit un reçu contenant le hash SHA-256 du JSON, l’API visée et l’heure du Preview. `Apply` refuse un reçu âgé de plus de 30 minutes ou un JSON modifié depuis le Preview.
8. Après `Apply`, contrôler `isApplied`, les erreurs et les compteurs, puis réexporter immédiatement avant le prochain lot ou la prochaine étape.
9. Terminer tous les lots applicables avant de poursuivre. Une étape objectivement non applicable peut être consignée puis traversée sans pause ; une étape applicable ne peut jamais être sautée pour accélérer le parcours.
10. Ne jamais publier un parc, masquer une donnée publique ou supprimer un contenu au-delà du lot annoncé. Pour un parc existant déjà visible, préserver sa visibilité pendant l’enrichissement.
11. Après chaque étape, rapprocher les compteurs de l’export avec les objectifs de couverture établis à l’étape 0. Une réussite `Apply` ne prouve pas la complétude éditoriale.

Exemple :

```powershell
.\tools\codex\park-data-editor.ps1 -Action Preview -JsonPath .\work\park-step-03.json
.\tools\codex\park-data-editor.ps1 -Action Apply `
  -JsonPath .\work\park-step-03.json `
  -ReceiptPath .\work\park-step-03.preview-receipt.json
```

## Étape 5 : parcours Codex obligatoire des images

Pour les nouvelles images de parc, de parkItem ou d’attraction autonome, Codex utilise `ImportPhoto` et les surfaces `park-data-editor/images/*`. Les images distantes intégrées au JSON restent le mécanisme de ChatGPT ; elles ne justifient pas un retour de Codex vers l’administration.

### Inventaire de couverture avant import

Construire depuis l’export une ligne par propriétaire avec : identifiant, type, statut, image courante, nombre d’images, image recherchée et résultat. Le registre doit couvrir au minimum :

- logo officiel actuel ;
- image principale du parc ;
- chaque attraction actuelle ;
- chaque attraction annoncée ou en construction ;
- chaque attraction définitivement fermée ;
- chaque jalon et article historique existant ou créé à l’étape 7.

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
6. Réexporter immédiatement le parc et vérifier propriétaire, catégorie, métadonnées, publication et hiérarchie courante.

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

Les catégories de compte, commentaire, vidéo, exploitant, constructeur et fondateur sont refusées. Le jeton ne peut pas supprimer une image : si le rattachement ou les métadonnées échouent après l’upload, le client signale l’identifiant de l’orpheline pour contrôle et nettoyage par un administrateur.

### Audit après images

Après réexport, annoncer : logo courant, image principale du parc, attractions avec image/total pour chaque statut, attractions fermées avec image/total, jalons avec image/total, articles avec image/total et liste exacte des exceptions. Vérifier aussi que les images n’ont pas été dupliquées et que les imports historiques n’ont pas remplacé une image courante appropriée.

Un warning de doublon d’image distante peut être non bloquant uniquement si l’export prouve que la source est déjà liée au bon propriétaire et qu’aucune modification n’était attendue. Tous les autres warnings doivent être compris et corrigés avant de poursuivre.

## Audit final

À l’étape 8, Codex doit au minimum : réexporter, exécuter le contrôle de complétude, vérifier les compteurs attendus, les propriétaires d’images, le logo courant, les sources et crédits, les warnings résiduels, la visibilité conservée et l’historique d’Apply. Il fournit le tableau quantitatif exigé par l’étape 8 et ne conclut pas sur le seul score numérique.

Si une réponse d’export volumineuse échoue ou arrive tronquée, ne jamais réutiliser silencieusement un ancien export. Réessayer par la surface technique autorisée, réduire les lectures auxiliaires quand elles sont paginées et considérer Preview puis le nouvel export comme les preuves de l’état appliqué. Ne pas basculer vers l’administration ou la base de données.

Après une instruction explicite de publication, Codex suit l’ordre de l’étape 8 : publier de façon ciblée les images et contenus dépendants prêts, puis les articles, contrôler les parkItems, et enfin valider et rendre visible le nouveau parc en dernier. La publication des images réutilise leurs IDs exportés et la surface de métadonnées autorisée ou un JSON upsert borné conforme au contrat exporté ; elle ne réimporte jamais les fichiers. Codex vérifie ensuite les pages publiques anonymes, le logo, les articles, la complétude et l’idempotence d’un dernier Preview. Une annonce sociale indisponible est rapportée séparément et n’autorise aucun appel à une route admin interdite.

Le propriétaire peut rapprocher chaque appel avec `park-data-editor.request` dans le journal d’audit et filtrer par compte, email, trace ID ou identifiant de jeton.
