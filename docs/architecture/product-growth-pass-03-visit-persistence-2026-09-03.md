# PASS-03 — Persistance propriétaire des visites

Date : 2026-09-03

Roadmap : `docs/roadmaps/product-growth/02-visit-passport-and-ride-log-roadmap.md`

## Résultat

La collection MongoDB `user-visits` persiste l'agrégat `Visit` défini par PASS-02. Le Core reste indépendant de MongoDB : Application expose uniquement la capacité `IUserVisitRepository`, tandis qu'Infrastructure possède le document, le mapping, les filtres, les index et l'implémentation concrète.

Aucun contrôleur, DTO HTTP ou écran n'est introduit dans cette tranche.

## Schéma actif

```text
user-visits
├── _id: string                         identité VisitId, index Mongo unique natif
├── userId: string                      propriétaire privé
├── parkId: string
├── date
│   ├── year: int
│   ├── month: int?                     absent pour une précision Year
│   ├── day: int?                       absent pour Year ou Month
│   ├── precision: Year | Month | Day
│   └── isApproximate: bool
├── timeZoneId: string?
├── serviceDayConvention: string
├── status: Draft | Completed | Archived
├── privacy: Private
├── title: string?
├── privateNote: string?
├── version: long
├── createdAt: UTC datetime
├── updatedAt: UTC datetime
└── completedAtUtc: UTC datetime?
```

Les enums sont stockés sous forme de chaînes pour rendre les documents inspectables et éviter de lier leur sens à un ordinal. Le mapper reconstruit l'agrégat via ses fabriques Core : un document incohérent n'est donc pas accepté silencieusement.

Le futur `parkAssessment` actif sera embarqué dans ce document en PASS-09. L'ajouter avant l'existence de son invariant métier créerait un contrat persistant prématuré.

## Propriété et concurrence

```text
lecture      = _id + userId
mise à jour = _id + userId + version attendue
suppression = _id + userId + version attendue
```

- aucune lecture par identifiant ne contourne le propriétaire ;
- aucune mise à jour ne fait d'upsert ;
- l'agrégat de remplacement doit être exactement à `version attendue + 1` ;
- une version obsolète, un identifiant absent et un identifiant appartenant à un autre utilisateur produisent tous un échec borné, sans écrasement ;
- la création utilise `InsertOne`, donc l'index `_id` natif interdit tout remplacement implicite ;
- les listes sont limitées à 100 documents au maximum dans cette première capacité interne.

## Ordre et index

L'ordre descendant `year`, `month`, `day` place les dates exactes avant la date mensuelle correspondante, puis les mois connus avant une date limitée à l'année. `updatedAt` puis `_id` stabilisent l'ordre sans inventer de date.

MongoDB fournit l'index unique `_id`. Trois index métier sont ajoutés :

```text
{ userId: 1, date.year: -1, date.month: -1, date.day: -1 }
{ userId: 1, parkId: 1, date.year: -1 }
{ userId: 1, status: 1, updatedAt: -1 }
```

Aucun index unique `(userId, parkId, date)` n'existe : plusieurs visites du même parc le même jour restent légitimes.

## Preuves

Les tests couvrent :

- round-trip complet d'une visite terminée avec données privées ;
- date partielle approximative sans jour inventé ;
- noms BSON et enums sous forme de chaînes ;
- rejet d'un document persistant contraire aux invariants Core ;
- présence systématique du propriétaire dans les filtres ;
- verrou optimiste par version ;
- ordre temporel et définition exacte des index ;
- création sans upsert, conflit de mise à jour et suppression propriétaire ;
- borne des listes et enregistrement DI scoped.

## Retour arrière

La tranche peut être retirée en supprimant l'enregistrement DI et l'initialisation de la collection. Aucune donnée publique, note communautaire ou route existante n'est modifiée. Si des documents ont déjà été créés par une tranche ultérieure, leur suppression nécessite alors une migration explicite plutôt qu'un effacement automatique.
