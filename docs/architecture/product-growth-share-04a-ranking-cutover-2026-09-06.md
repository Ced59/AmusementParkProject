# SHARE-04A — Remplacement du partage de classement personnel

## Résultat métier

Le classement personnel déjà partageable conserve exactement ses routes et son lien
public. Son consentement, sa révocation et sa résolution sont désormais portés par
`SharePublication`, comme le seront les futurs récaps de visite, bilans annuels et
passeports publics. Il n'existe plus deux autorités applicatives concurrentes.

Pour la personne :

- un lien publié avant la migration reste utilisable avec le même jeton ;
- couper le partage invalide immédiatement ce jeton ;
- republier après révocation crée un nouveau jeton, l'ancien restant inutilisable ;
- les écrans et URL Angular ne changent pas dans cette tranche.

## Frontières d'architecture

```text
UserRankingSharesController (routes historiques conservées)
        │
        ├── GetSharePublicationSettingsQuery
        └── SetSharePublicationVisibilityCommand
                         │
                         ▼
                SharePublication (Core)
                         │
                         ▼
          ISharePublicationRepository (port)
                         │
                         ▼
       share-publications (MongoDB, vérité unique)
```

Les handlers de lecture des classements publics utilisent
`ISharePublicationAccessResolver`. Celui-ci exige un jeton Base64 URL canonique de
256 bits, une publication résolvable du bon type et un compte encore actif et non
bloqué. Le contrôleur ne porte aucune règle métier et les repositories MongoDB ne
remontent pas dans la couche Application.

L'ancien agrégat `UserRankingShare`, son repository, son générateur de jeton et ses
handlers ont été supprimés. La classe `LegacyUserRankingShareDocument` n'est qu'une
projection Infrastructure en lecture seule du backup gelé pendant la migration ;
elle n'est injectée dans aucun cas d'usage.

## Migration MongoDB

```text
userRankingShares (ancien)
  _id, userId, isPublic, shareId, publishedAtUtc
                  │
                  │ mapping déterministe et reprenable
                  ▼
share-publications (nouveau)
  _id = SHA-256(namespace + ancien _id)
  ownerUserId
  type = PersonalRanking
  sourceScopeKey = personal-ranking:{owner}
  status / visibility
  shareToken = ancien shareId inchangé si public
  contentPolicy = pseudonyme + avatar + notes globales
  sourceVersion / publicationVersion / version
```

Le migrateur est exécuté avant l'ouverture du trafic par
`MongoDatabaseInitializer`. Il suit les règles suivantes :

1. un lease MongoDB autorise une seule instance à migrer ;
2. l'identifiant central dérive de façon déterministe de l'identifiant interne
   historique, ce qui rend une reprise idempotente sans exposer cet identifiant ;
3. les propriétaires et jetons historiques dupliqués bloquent le démarrage ;
4. un partage public sans jeton canonique ou sans date cohérente bloque le démarrage ;
5. les révisions propriétaire et catalogue sont relues avant et après leur calcul ;
6. chaque document existant est comparé à sa source avant d'être accepté ;
7. le total complet et un échantillon déterministe allant jusqu'à dix publications
   sont revérifiés ;
8. le marqueur de cutover n'est écrit qu'après toutes ces preuves.

Une fois ce marqueur présent, les démarrages suivants ne lisent plus la collection
historique. Elle reste seulement un backup de rollback, sans repository applicatif.

## Déploiement zéro-coupure et rollback

Pendant un déploiement roulant, l'ancienne API peut encore recevoir une mutation.
Le script de déploiement fige donc physiquement les écritures de
`userRankingShares`, avec le compte MongoDB d'administration, avant de démarrer le
candidat. La nouvelle API migre alors une source stable.

```text
backup Mongo
    │
    ▼
gel des écritures legacy
    │
    ▼
candidat API : migration + vérifications + marqueur
    │
    ├── échec avant bascule canonique
    │       └── migration inverse + dégel automatique
    │
    └── candidat sain
            └── services canoniques sur le moteur central
```

Si le déploiement échoue avant la bascule canonique, le rollback projette le dernier
état central de chaque propriétaire dans l'ancien format, supprime les seules
publications `PersonalRanking`, retire le marqueur et dégèle la collection. Il couvre
donc aussi une publication ou une révocation reçue par le candidat pendant la courte
fenêtre de déploiement. Après démarrage des services canoniques, le moteur central
reste autoritaire : revenir à un ancien binaire exige l'exécution explicite de cette
migration inverse, jamais une double écriture permanente.

## Preuves automatisées

- tests Application : création, lecture des réglages, révocation atomique, validation
  du type et calcul stable de la révision ;
- tests d'accès : conservation d'un jeton historique valide, refus d'un identifiant
  technique ou mal formé, contrôle de l'état du compte ;
- tests Infrastructure : mapping public/privé, jeton inchangé, identifiant
  déterministe et blocage d'une donnée historique incohérente ;
- tests WebAPI : contrats HTTP historiques, propriétaire issu du claim et protections
  d'autorisation inchangées ;
- test de déploiement : présence du gel, du rollback inverse et du bypass de
  validation strictement limité à cette restauration.

## Hors périmètre

Cette tranche ne change pas encore l'interface de partage. L'éditeur visuel, le
résumé de confidentialité et la confirmation explicite appartiennent à `SHARE-05`.
Le snapshot public entièrement figé et les pages de récaps arrivent dans les tranches
suivantes.
