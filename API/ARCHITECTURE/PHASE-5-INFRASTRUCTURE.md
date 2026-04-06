# Phase 5 - Mongo documents + adapters Infrastructure

Cette phase ajoute dans `AmusementPark.Infrastructure` :

- des **documents Mongo dédiés** par agrégat / projection ;
- des **mappers centralisés** Core/Application ↔ Mongo ;
- les **repositories Mongo** branchés sur les ports Application ;
- la **configuration Mongo** déplacée dans Infrastructure.

## Objectif atteint

- Core et Application restent indépendants de Mongo.
- Les attributs BSON et les noms de collections sont confinés à Infrastructure.
- La DI Infrastructure sait désormais fournir les adapters Mongo nécessaires à la suite de la migration.

## Limite volontaire à ce stade

Cette phase **ne migre pas encore les contrôleurs HTTP legacy** vers la nouvelle WebAPI propre.
Cette bascule est réservée aux phases suivantes.
