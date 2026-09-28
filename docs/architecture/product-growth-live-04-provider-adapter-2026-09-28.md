# LIVE-04 — Adaptateur fournisseur pilote

> Date : 28 septembre 2026  
> Version : `5.4.4`  
> Source pilote : ThemeParks.wiki REST v1  
> Portée : traduction interne uniquement, sans polling ni exposition publique

## 1. Résultat métier

Le produit sait désormais comprendre une réponse live du fournisseur sans en
reprendre le vocabulaire ni les ambiguïtés. Il distingue explicitement :

- une attente annoncée à **0 minute** d'une attente **inconnue** ;
- une attraction ouverte, fermée, en panne ou en maintenance ;
- une file classique, single rider, payante, à créneau ou à groupe virtuel ;
- une valeur reconnue d'une nouvelle valeur fournisseur non encore validée.

Une nouveauté fournisseur ne devient jamais implicitement « ouvert ». Elle est
normalisée en `Unknown` et accompagnée d'un diagnostic exploitable par la future
quarantaine. Ce jalon ne rend aucune information visible et ne lance aucun appel
automatique.

## 2. Frontières d'architecture

```text
Core
  LiveOperationalStatus
  ExternalLiveObservation
  LiveQueueObservation
            ↑
Application
  ILiveDataProviderAdapter
  LiveProviderReadRequest / LiveProviderReadResult
            ↑
Infrastructure
  ThemeParksWikiLiveDataAdapter
  DTO JSON + normaliseur fournisseur
```

- **Core** définit les faits métier indépendants de toute API.
- **Application** définit le port de lecture et les issues de transport utiles au
  futur ordonnanceur.
- **Infrastructure** connaît l'URL, le JSON et les valeurs ThemeParks.wiki.
- **WebAPI et Angular** ne reçoivent encore aucun endpoint ni composant live.

Chaque type est placé dans son propre fichier. L'adaptateur ne contourne ni le
mapping humain livré par `LIVE-03`, ni la future validation de `LIVE-07`.

## 3. Traduction des statuts

| Valeur source | État interne |
|---|---|
| `OPERATING` | `Open` |
| `DOWN` | `Down` |
| `CLOSED` | `Closed` |
| `REFURBISHMENT` | `Maintenance` |
| absente ou nouvelle | `Unknown` + diagnostic |

Les autres états internes (`TemporarilyClosed`, `Delayed`, `WeatherClosed`,
`OperatingWithLimitations`, `NotOperatingToday`, `Removed`) sont déjà nommés pour
que d'autres sources puissent les exprimer sans déformer le modèle.

## 4. Files et valeurs conservées

| File source | Type interne | Informations conservées |
|---|---|---|
| `STANDBY` | `Standby` | attente officielle facultative |
| `SINGLE_RIDER` | `SingleRider` | attente officielle facultative |
| `RETURN_TIME` | `ReturnTime` | disponibilité et fenêtre de retour |
| `PAID_RETURN_TIME` | `PaidReturnTime` | disponibilité, fenêtre, montant et devise |
| `BOARDING_GROUP` | `BoardingGroup` | groupes, prochaine attribution et attente estimée |
| `PAID_STANDBY` | `PaidStandby` | attente officielle facultative |

Les minutes sont des entiers compris entre 0 et 1 440. `null` signifie que le
fournisseur ne publie pas de valeur ; `0` reste un zéro explicite. Une valeur
fractionnaire, négative ou hors borne produit un diagnostic et n'est jamais
convertie en zéro. Le contrat fournisseur rend `STANDBY.waitTime` facultatif :
son absence est donc conservée comme attente inconnue, comme une valeur `null`,
sans inventer une erreur de schéma.

## 5. Contrat transport sûr

Le résultat de lecture distingue :

- `Success` ;
- `NotModified` pour un `304` ;
- `RateLimited` avec `Retry-After` pour un `429` ;
- `Unavailable` pour les erreurs réseau, timeouts, `401` et `5xx` ;
- `InvalidPayload` pour un JSON illisible ;
- `ResponseTooLarge` au-delà de 2 Mio.

Le client :

- utilise uniquement `https://api.themeparks.wiki/` ;
- encode l'identifiant externe dans un chemin relatif ;
- refuse les redirections ;
- limite chaque requête à dix secondes ;
- lit le flux avec une borne mémoire même sans `Content-Length` ;
- transmet `If-None-Match` et restitue l'ETag ;
- calcule une empreinte SHA-256 du payload reçu ;
- ne contient aucun retry : le respect des quotas appartient à `LIVE-05`.

## 6. Séquence

```text
futur scheduler       port Application       adaptateur Infrastructure       fournisseur
      |                       |                           |                         |
      | FetchLatest(id, etag) |                           |                         |
      |---------------------->| GET borné + If-None-Match |                         |
      |                       |-------------------------->|                         |
      |                       |                           |------------------------>|
      |                       |                           | réponse / 304 / 429      |
      |                       |                           |<------------------------|
      |                       | normalisation + hash      |                         |
      | résultat typé         |<--------------------------|                         |
      |<----------------------|                           |                         |
```

Le scheduler représenté n'est pas encore implémenté. Cette séparation permet à
`LIVE-05` d'ajouter leases, budgets, backpressure et circuit breaker sans modifier
le contrat fournisseur.

## 7. Preuves automatisées

Les fixtures versionnées couvrent :

- les quatre statuts documentés et une valeur future ;
- les six types de file et leurs champs spécifiques ;
- zéro, absence et valeur invalide ;
- entité non supportée et horodatage illisible ;
- lot vide ;
- ETag/304, 429/Retry-After, 401, 500, timeout ;
- JSON invalide et réponse annoncée trop volumineuse.

Les tests Core vérifient en plus les bornes, l'unicité des files et les fenêtres
temporelles. Les tests ciblés ne démarrent aucun serveur et ne contactent pas le
fournisseur réel.

## 8. Suite autorisée

`LIVE-05` peut brancher un ordonnanceur **interne** sur ce port, avec un seul poll
actif, un intervalle minimal de cinq minutes, un circuit breaker et des budgets
VPS. La collecte restera sans affichage public jusqu'aux jalons de stockage,
quarantaine, API et UI.
