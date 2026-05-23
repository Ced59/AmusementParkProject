# M18.8 — Audit log admin minimal

## Objectif

M18.8 ajoute une trace persistée pour les actions d'administration sensibles. L'objectif n'est pas encore de créer un dashboard complet, mais de garantir qu'une action importante laisse une preuve exploitable en MongoDB.

## Collection Mongo

Les traces sont stockées dans la collection :

```txt
adminAuditLogs
```

Le nom est configurable via :

```json
{
  "MongoDB": {
    "AdminAuditLogsCollectionName": "adminAuditLogs"
  }
}
```

## Contrat applicatif

La couche Application expose uniquement le port :

```txt
Application/Features/AdminAudit/Ports/IAdminAuditLogWriter.cs
```

La persistance est portée par l'Infrastructure :

```txt
Infrastructure/Persistence/Mongo/Repositories/AdminAuditLogWriter.cs
```

Le WebAPI déclenche l'audit via l'attribut explicite :

```txt
WebAPI/Filters/AdminAuditAttribute.cs
```

Ce choix évite un audit automatique trop large qui risquerait de stocker des données inutiles ou sensibles.

## Données persistées

Chaque trace contient notamment :

- `occurredAtUtc` ;
- `action` normalisée, par exemple `park.visibility.update` ;
- `entityType` et `entityId` lorsque disponible ;
- `actorUserId`, `actorEmail`, `actorRoles` ;
- `httpMethod`, `path`, `statusCode` ;
- `ipAddress` après traitement des forwarded headers ;
- `userAgent` ;
- `traceId`, cohérent avec le contrat `ProblemDetails` M18.7 ;
- `metadata` limitée et filtrée.

Les champs sensibles dont le nom contient `password`, `token`, `secret`, `key`, `file`, `content` ou `stream` ne sont pas copiés dans les métadonnées d'audit.

## Actions couvertes

### Données métier publiques administrées

- création / modification de parc ;
- changement de visibilité parc ;
- mise à jour admin en masse des parcs ;
- création / modification / suppression d'élément de parc ;
- mise à jour admin en masse des éléments de parc ;
- création / modification / suppression de zone ;
- création / modification d'exploitant, constructeur, fondateur ;
- changement de statut de revue en masse sur exploitants et constructeurs.

### Images

- upload d'image ;
- liaison d'image à une entité ;
- définition de l'image courante ;
- suppression d'image ;
- mise à jour des métadonnées ;
- mise à jour en masse des métadonnées ;
- création / modification de tag image.

### Utilisateurs et rôles

- attribution de rôle ;
- suppression de rôle ;
- verrouillage utilisateur ;
- déverrouillage utilisateur.

### Sources externes

- mise à jour des paramètres data source ;
- lancement d'un import ;
- application des résultats de comparaison.

## Index Mongo

L'initialisation Mongo crée des index sur :

- `occurredAtUtc` décroissant ;
- `actorUserId + occurredAtUtc` ;
- `action + occurredAtUtc` ;
- `entityType + entityId + occurredAtUtc` ;
- `traceId`.

## Comportement en cas d'échec d'audit

L'écriture d'audit est volontairement best-effort : une panne ponctuelle d'audit est loggée côté serveur mais ne bloque pas l'action admin déjà réussie.

Ce choix évite de rendre l'administration inutilisable à cause d'un incident d'écriture secondaire. Pour un palier ultérieur, il sera possible de durcir ce comportement sur certaines actions ultra-sensibles.

## Limites assumées pour M18.8

- Pas encore de page admin de consultation.
- Pas encore de diff avant/après.
- Pas encore de rétention configurable.
- Pas encore d'export CSV.

Ces éléments relèvent plutôt du palier M24, où la roadmap prévoit d'étendre l'audit log admin.
