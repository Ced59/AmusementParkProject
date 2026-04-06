# Phase 2 - Charte d'architecture

Cette phase verrouille les règles qui encadrent la refonte Clean Architecture avant le premier vrai déplacement de code métier.

## Objectif

Empêcher les régressions architecturales pendant toute la migration progressive du back.

## Règles de dépendances

- `AmusementPark.Core` ne référence aucun autre projet métier.
- `AmusementPark.Application` ne référence que `AmusementPark.Core`.
- `AmusementPark.Infrastructure` ne référence que `AmusementPark.Application` et `AmusementPark.Core`.
- `AmusementPark.WebAPI` ne référence que `AmusementPark.Application` et `AmusementPark.Infrastructure`.
- Les anciens projets legacy peuvent continuer à coexister pendant la migration, mais ils ne doivent pas être réintroduits dans les nouveaux projets `AmusementPark.*`.

## Règles technologiques

### Core

Interdits dans `AmusementPark.Core` :

- MongoDB
- ASP.NET Core
- MinIO
- MailKit
- Google Auth
- ImageSharp
- Swagger

Le Core reste strictement métier.

### Application

Interdits dans `AmusementPark.Application` :

- MongoDB
- ASP.NET Core
- DTOs HTTP
- MinIO
- MailKit
- Google Auth
- ImageSharp
- Swagger

Application expose uniquement des cas d'usage, des résultats, des conventions de feature slices et des ports.

### Infrastructure

`AmusementPark.Infrastructure` contient :

- MongoDB et les documents BSON
- le mapping Domaine <-> Document
- MinIO
- JWT concret
- email SMTP
- providers externes
- image processing
- seeds et index

### WebAPI

`AmusementPark.WebAPI` contient :

- contrôleurs
- DTOs HTTP
- mapping HTTP <-> Application
- auth, policies, Swagger
- composition root

Un contrôleur ne doit pas parler directement à Mongo, à `IMongoCollection`, aux query handlers legacy ou aux entités persistées.

## Règles de migration

- aucune route existante ne change pendant la refonte
- aucun payload existant ne change pendant la refonte
- aucun schéma Mongo existant ne change tant que les adapters infrastructure ne sont pas prêts
- chaque tranche verticale doit rester iso-fonctionnelle

## Conventions de features

La migration s'organise par slices :

1. Users
2. Parks
3. ParkZones
4. ParkItems
5. Images
6. Search
7. CaptainCoaster

Chaque feature doit contenir ses commandes, requêtes, handlers, résultats, validators et ports spécifiques.
