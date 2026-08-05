# Workflow API autonome réservé à Codex

Version : **2026-08-05**

Rôle : **PARK_DATA_EDITOR**

Client : `tools/codex/park-data-editor.ps1`

Ce document complète le parcours officiel des étapes 0 à 8 lorsque **Codex** exécute lui-même les exports, Preview, Apply, contrôles et uploads par API. Il ne remplace aucune règle éditoriale ou métier des fichiers `park-data-integration-steps/`.

## Isolation obligatoire du workflow ChatGPT

Ce complément est réservé à Codex authentifié avec un jeton technique `PARK_DATA_EDITOR`.

ChatGPT conserve exactement le workflow existant : l’utilisateur lui fournit les exports actualisés, ChatGPT livre les JSON bornés, et les images externes continuent à passer par les champs d’images distantes du Park Graph Upsert et son processeur actuel. ChatGPT ne doit ni utiliser le client technique, ni télécharger localement une image, ni appeler les endpoints `park-data-editor/*`.

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

## Amorçage sans authentification manuelle dans le navigateur

L’amorçage est unique :

1. Codex génère un mot de passe aléatoire fort et enregistre l’adresse et le mot de passe dans le coffre local Windows, sans les écrire dans le dépôt ni dans les logs :

   ```powershell
   $password | .\tools\codex\park-data-editor.ps1 `
     -Action SaveAccountCredential `
     -AccountEmail 'adresse-technique@example.com'
   ```

2. Codex lance `RegisterAccount`, qui crée un compte local ordinaire par `POST /api/users`.
3. Le propriétaire confirme l’adresse reçue et assigne lui-même `PARK_DATA_EDITOR` depuis l’administration.
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
4. Rechercher et sourcer seulement les données de l’étape courante. Produire un JSON borné et conserver le compteur traité/total.
5. Exécuter `Preview`. Examiner toutes les erreurs, tous les warnings, les entités résolues et chaque changement de champ.
6. Ne jamais exécuter `Apply` si `canApply` est faux, si une erreur existe ou si un warning bloquant subsiste. Par défaut, le client bloque même les warnings non bloquants; `-AllowWarnings` exige une décision explicite après lecture.
7. Le client écrit un reçu contenant le hash SHA-256 du JSON, l’API visée et l’heure du Preview. `Apply` refuse un reçu âgé de plus de 30 minutes ou un JSON modifié depuis le Preview.
8. Après `Apply`, contrôler `isApplied`, les erreurs et les compteurs, puis réexporter immédiatement avant le prochain lot ou la prochaine étape.
9. Ne jamais sauter automatiquement une étape, publier un parc, masquer une donnée ou supprimer un contenu au-delà du lot annoncé.

Exemple :

```powershell
.\tools\codex\park-data-editor.ps1 -Action Preview -JsonPath .\work\park-step-03.json
.\tools\codex\park-data-editor.ps1 -Action Apply `
  -JsonPath .\work\park-step-03.json `
  -ReceiptPath .\work\park-step-03.preview-receipt.json
```

## Étape 5 : téléchargement puis upload local des photos

Codex peut remplacer uniquement **l’acquisition distante** par le parcours suivant :

1. Vérifier la source, les droits d’utilisation, le propriétaire cible et les crédits.
2. Télécharger l’URL publique HTTP(S) avec `curl`, au maximum 5 redirections, 90 secondes et 10 Mo.
3. Vérifier les octets réellement téléchargés. Le client n’accepte que JPEG, PNG ou WebP et supprime toujours le fichier temporaire.
4. Envoyer le fichier à `POST park-data-editor/images`.
5. Rattacher l’image à un `Park`, `ParkItem` ou `StandaloneAttraction` compatible.
6. Enregistrer l’URL source, les crédits, textes alternatifs, légendes, publication et statut courant via les métadonnées.
7. Réexporter le parc et vérifier le résultat.

Le téléchargement local ne contourne jamais le traitement applicatif. L’upload appelle le même `UploadImageCommandHandler`, le même `IImageProcessingPipeline` et le même stockage que l’upload existant : détection, métadonnées, conversion, compression, variantes et contraintes continuent donc à s’appliquer.

Pour une image récupérée, le watermark est **désactivé par défaut**. Il ne doit être activé avec `-WithWatermark $true` que sur indication explicite. La règle existante des logos reste prioritaire et empêche leur watermark.

Exemple avec un fichier de métadonnées localisées :

```powershell
.\tools\codex\park-data-editor.ps1 -Action ImportPhoto `
  -SourceUrl 'https://source.example/photo.webp' `
  -Category PARK_ITEM `
  -OwnerType PARK_ITEM `
  -OwnerId 'park-item-id' `
  -MetadataJsonPath .\work\photo-metadata.json
```

Les catégories de compte, commentaire, vidéo, exploitant, constructeur et fondateur sont refusées. Le jeton ne peut pas supprimer une image : si le rattachement ou les métadonnées échouent après l’upload, le client signale l’identifiant de l’orpheline pour contrôle et nettoyage par un administrateur.

## Audit final

À l’étape 8, Codex doit au minimum : réexporter, exécuter le contrôle de complétude, vérifier les compteurs attendus, les propriétaires d’images, les sources et crédits, les warnings résiduels, la visibilité demandée et l’historique d’Apply. Le propriétaire peut rapprocher chaque appel avec `park-data-editor.request` dans le journal d’audit et filtrer par compte, email, trace ID ou identifiant de jeton.
