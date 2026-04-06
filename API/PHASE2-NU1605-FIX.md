# Phase 2 - Correctif de compilation NU1605

## Problème
Le projet `AmusementPark.Infrastructure` référençait explicitement `Microsoft.Extensions.DependencyInjection.Abstractions` en `8.0.0`, alors que `Minio 7.0.0` exige `>= 9.0.4`.

## Correctif appliqué
- `AmusementPark.Infrastructure`: passage de `Microsoft.Extensions.DependencyInjection.Abstractions` à `9.0.4`
- `AmusementPark.Application`: alignement également en `9.0.4` pour cohérence entre les nouveaux projets

## Objectif
Supprimer l'erreur de restauration/build `NU1605` avant de poursuivre la migration métier.
