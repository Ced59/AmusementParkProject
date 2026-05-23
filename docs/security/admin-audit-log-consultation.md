# M18.8 bis — Consultation admin du journal d'audit

## Objectif

Le journal d'audit M18.8 n'est plus uniquement consultable dans MongoDB. Une consultation lecture seule est disponible dans le panel d'administration afin de retrouver rapidement une action sensible sans accès direct à la base.

## Endpoint

```http
GET /admin/audit-logs
```

Protection :

- authentification obligatoire ;
- compte activé et non bloqué ;
- rôle `ADMIN` uniquement ;
- lecture seule, aucun attribut `[AdminAudit]` sur cet endpoint afin d'éviter de journaliser la simple consultation du journal.

## Architecture

Le découpage respecte les couches existantes :

- `WebAPI` : contrôleur `AdminAuditLogsController`, DTO HTTP, mapping HTTP ;
- `Application` : query `GetAdminAuditLogsQuery`, handler, résultat exposable, port `IAdminAuditLogReader` ;
- `Infrastructure` : implémentation Mongo `AdminAuditLogReader` ;
- `Front` : service data-access, facade de state, composant admin dédié.

Le document Mongo `AdminAuditLogDocument` n'est jamais exposé directement au contrôleur ou au front.

## Filtres disponibles

- `fromUtc` / `toUtc` ;
- `actorUserId` ;
- `actorEmail` ;
- `action` ;
- `entityType` ;
- `entityId` ;
- `traceId` ;
- pagination `page` / `size`.

## Interface admin

Route front :

```text
/:lang/admin/audit-logs
```

La page affiche :

- date UTC ;
- acteur ;
- action ;
- entité ;
- méthode / chemin / IP ;
- statut HTTP ;
- traceId ;
- métadonnées filtrées.

## Limite volontaire

Ce M18.8 bis reste volontairement minimal : pas de dashboard analytique, pas d'export, pas d'agrégation. Ces évolutions relèveront d'un palier M24/ops si besoin.
