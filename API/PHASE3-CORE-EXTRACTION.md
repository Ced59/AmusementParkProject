# Phase 3 - Extraction du Core pur

Cette itération matérialise la phase 3 du plan de migration Clean Architecture.

## Objectif atteint dans cette phase

- création d'un **Core pur** sans dépendance Mongo, AspNetCore, MinIO, MailKit ou ImageSharp ;
- extraction des **bases de domaine** : `EntityBase`, `AuditableEntity`, `GeoPoint`, `GeolocatedEntityBase`, `LocalizedText` ;
- création des **agrégats métier purs** : `Country`, `Park`, `ParkZone`, `ParkItem`, `ParkFounder`, `ParkOperator`, `AttractionManufacturer`, `Image`, `ImageTag`, `User` ;
- création des **objets métier et enums** associés à ces agrégats ;
- maintien du **legacy Mongo** dans les anciens projets tant que les mappings Infrastructure ne sont pas prêts.

## Important

Cette phase ne branche pas encore les nouveaux agrégats Core sur les flux runtime existants. Cela viendra en phase 4 puis en phase 5 avec les ports applicatifs et les documents Mongo/adapters.

## Règle de compatibilité

Aucune route HTTP existante n'est modifiée dans cette phase. Le but est uniquement de sortir le domaine du couplage Mongo, conformément au plan d'audit.
