# SHARE-03 — Persistance autoritative et liens opaques

Date : 2026-09-05

Version : 5.2.2

## Résultat métier

Le moteur commun de partage possède désormais un stockage MongoDB dédié. Cette
tranche ne rend encore aucune donnée accessible sur le Web : elle permet aux futurs
cas d'usage de créer, relire et remplacer une publication personnelle avec les mêmes
règles pour une visite, une année, un passeport, un classement ou une comparaison.

Une révocation remplace atomiquement l'état, la visibilité, la version et le jeton.
Dès que l'écriture est confirmée, l'ancien lien ne correspond donc plus à aucun
document résolvable. Une publication ne peut pas être écrasée par une modification
concurrente devenue obsolète.

## Deux versions aux responsabilités distinctes

`PublicationVersion` identifie le contenu rendu public et servira au versionnement
des rendus et caches. `Version` est la clôture de concurrence de la ligne MongoDB et
avance à chaque vraie mutation, y compris lorsqu'un brouillon change avant sa
première publication.

```text
lecture MongoDB : Version 4
       │
       ├── mutation métier → Version 5
       │
       └── remplacement si _id + propriétaire + Version 4 correspondent
                                  │
                                  ├── oui : document complet remplacé
                                  └── non : conflit explicite, aucun écrasement
```

Cette séparation évite qu'un brouillon, dont `PublicationVersion` reste à zéro,
perde silencieusement un choix de confidentialité modifié depuis un autre appareil.

## Jetons publics

`ShareToken` est un type Core distinct des identifiants internes. Il accepte
uniquement l'encodage canonique Base64 URL sans remplissage de 32 octets, soit 256
bits d'entropie et exactement 43 caractères. Infrastructure génère ces octets avec
un générateur cryptographiquement sûr.

- aucune dérivation depuis un utilisateur, une visite, une date ou un email ;
- aucun espace, caractère Base64 classique ou encodage non canonique accepté ;
- unicité protégée par un index MongoDB partiel ;
- collision rarissime remontée explicitement afin que le futur cas d'usage puisse
  générer une nouvelle valeur ;
- aucune méthode de liste publique : une résolution exige le jeton complet exact.

## Schéma MongoDB de cette tranche

```text
share-publications
├── _id                       identifiant interne opaque
├── ownerUserId               propriétaire, accès privé seulement
├── type                      intention de publication
├── sourceScopeKey            périmètre privé, jamais exposé par une route publique
├── shareToken?               absent avant publication et après révocation
├── status                    Draft | Published | NeedsReview | Revoked
├── visibility                Private | Unlisted | Public
├── contentPolicy
│   ├── schemaVersion
│   ├── datePrecision
│   └── includedFields[]      liste blanche uniquement
├── sourceVersion
├── publicationVersion
├── version                   clôture de concurrence MongoDB
├── publishedAtUtc?
├── revokedAtUtc?
├── createdAt
└── updatedAt
```

Les commentaires privés, notes privées textuelles, accompagnants, emails,
coordonnées précises et positions sont absents du document par construction.

## Index et coût d'exploitation

Trois index bornés sont créés au démarrage :

1. `shareToken`, unique et partiel lorsque le champ existe ;
2. `ownerUserId + type + updatedAt`, pour les futures listes privées ;
3. `sourceScopeKey + ownerUserId`, pour retrouver le partage d'un périmètre.

Il n'existe aucun index de découverte par statut/visibilité et aucun TTL susceptible
de réactiver ou supprimer implicitement une publication. Le nombre réduit d'index
limite aussi le coût d'écriture sur le VPS.

## Architecture

```text
Core
  SharePublication + ShareToken
          ▲
Application
  ISharePublicationRepository + IShareTokenFactory
          ▲
Infrastructure
  SharePublicationRepository ── MongoDB
  CryptographicShareTokenFactory ── CSPRNG système
```

MongoDB reste intégralement en Infrastructure. Application ne connaît que les ports
et le Core conserve les invariants. Chaque classe, interface et enum ajouté occupe
son propre fichier.

## Preuves automatisées

Les tests ciblés couvrent :

- la longueur, l'alphabet, la canonicité et le refus des jetons invalides ;
- 256 générations cryptographiques distinctes et reparsables ;
- le round-trip MongoDB des quatre états du cycle de vie ;
- l'absence physique des catégories privées interdites dans le BSON ;
- les filtres exacts de résolution et de propriété ;
- les trois index, l'unicité partielle et l'absence d'index de liste publique ;
- la révocation par remplacement atomique borné au propriétaire et à la version ;
- les conflits de concurrence et la classification des collisions de jeton ;
- l'enregistrement des ports dans l'injection de dépendances.

## Suite

`SHARE-04` construira l'aperçu et les DTO publics depuis la seule liste blanche.
`SHARE-04A` migrera ensuite le partage de classement existant vers cette collection
et retirera son ancien moteur dans la même livraison : aucune double écriture ni
adaptateur permanent ne sera activé.
