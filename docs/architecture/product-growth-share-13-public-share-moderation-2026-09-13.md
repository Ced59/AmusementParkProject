# SHARE-13 — Signalement et modération des partages publics

## Résultat métier

Chaque page publique de visite, d'année, de passeport, de classement personnel et
de comparaison permet de transmettre un signalement structuré. L'administration
dispose d'une file responsive pour classer le signalement, suspendre immédiatement
le partage ou le rétablir. La suspension ne modifie ni les visites, ni les notes,
ni le passeport privé du propriétaire.

## Invariants de confidentialité

- le navigateur transmet le jeton opaque uniquement pour désigner la page visible ;
- la collection des signalements ne persiste pas ce jeton public, mais la référence
  interne de la publication résolue côté serveur ;
- les réponses admin n'exposent ni cette référence interne, ni l'identité technique
  du modérateur, ni le jeton public ;
- le texte libre est limité à 500 caractères, rendu comme texte et refuse les
  balises, schémas exécutables et liens ;
- une page déjà privée, révoquée, expirée ou suspendue ne peut pas recevoir un
  nouveau signalement par son ancien lien ;
- les signalements publics sont limités à trois par heure et par adresse IP ;
- les mutations admin restent authentifiées, autorisées, auditées et sérialisées.

## Architecture de classes

```mermaid
classDiagram
  class ShareModerationReport {
    +ShareModerationReportId Id
    +ShareModerationTargetType TargetType
    +string TargetRecordId
    +ShareModerationReason Reason
    +ShareModerationReportStatus Status
    +Dismiss()
    +MarkPublicationSuspended()
    +MarkPublicationRestored()
  }
  class SharePublication {
    +bool IsModerationSuspended
    +bool IsResolvable
    +SuspendByModeration()
    +RestoreAfterModeration()
  }
  class ProfileComparison {
    +bool IsModerationSuspended
    +bool IsPubliclyResolvable
    +SuspendByModeration()
    +RestoreAfterModeration()
  }
  class ShareModerationService {
    +SubmitAsync()
    +ReviewAsync()
  }
  class IShareModerationReportRepository
  class ISharePublicationRepository
  class IProfileComparisonRepository
  class SharePublicationCacheInvalidationScheduler

  ShareModerationService --> ShareModerationReport
  ShareModerationService --> IShareModerationReportRepository
  ShareModerationService --> ISharePublicationRepository
  ShareModerationService --> IProfileComparisonRepository
  ShareModerationService --> SharePublicationCacheInvalidationScheduler
  ISharePublicationRepository --> SharePublication
  IProfileComparisonRepository --> ProfileComparison
```

Les règles et transitions appartiennent au Core. L'Application résout la cible et
orchestre les écritures. Infrastructure persiste et migre. WebAPI ne contient que
les contrats, l'autorisation, la limitation de débit et le mapping HTTP. Angular
place l'orchestration dans des façades derrière des ports.

## Séquence d'un signalement

```mermaid
sequenceDiagram
  actor V as Visiteur
  participant UI as Page publique Angular
  participant API as POST /passport/shared/reports
  participant S as ShareModerationService
  participant P as Publication/Comparaison
  participant R as Mongo share-moderation-reports

  V->>UI: choisit un motif et confirme
  UI->>API: type + jeton opaque + motif + texte
  API->>S: Submit(command)
  S->>P: résolution publique exacte par jeton
  alt cible publiquement résoluble
    P-->>S: référence interne et type confirmé
    S->>S: validation métier et texte sûr
    S->>R: crée un rapport Pending
    R-->>API: succès
    API-->>UI: 202 Accepted
  else lien inconnu, révoqué ou suspendu
    P-->>S: absent
    API-->>UI: réponse non révélatrice
  end
```

## Séquence d'une suspension admin

```mermaid
sequenceDiagram
  actor A as Administrateur
  participant UI as File de modération
  participant API as PUT /admin/share-moderation/reports/{reportId}
  participant S as ShareModerationService
  participant T as Publication ou comparaison
  participant R as Rapport
  participant C as Invalidation des caches

  A->>UI: Suspendre le partage
  UI->>API: décision Suspend + note facultative
  API->>S: Review(command, adminId)
  S->>R: recharge Pending avec version
  S->>T: SuspendByModeration(now)
  T-->>S: IsResolvable = false
  S->>C: purge les caches publics si publication
  S->>R: MarkPublicationSuspended(adminId)
  R-->>API: écriture optimiste réussie
  API-->>UI: 204 puis rechargement de la file
```

Le rétablissement effectue la transition inverse depuis le même rapport. Le jeton,
le snapshot et `PublicationVersion` sont conservés : le contenu revient à l'identique
sans créer un second système de partage.

## Schéma MongoDB

```mermaid
erDiagram
  SHARE_PUBLICATIONS {
    string _id PK
    string shareToken UK
    string type
    string status
    boolean isModerationSuspended
    long publicationVersion
    long version
  }
  PROFILE_COMPARISONS {
    string _id PK
    string shareToken UK
    string status
    boolean isModerationSuspended
    long version
  }
  SHARE_MODERATION_REPORTS {
    string _id PK
    string targetType
    string targetRecordId
    string reason
    string details
    string status
    datetime submittedAtUtc
    string reviewedByUserId
    datetime reviewedAtUtc
    string decisionNote
    long version
  }
  SHARE_PUBLICATIONS ||--o{ SHARE_MODERATION_REPORTS : "référence interne selon targetType"
  PROFILE_COMPARISONS ||--o{ SHARE_MODERATION_REPORTS : "référence interne selon targetType"
```

Index :

- `status + submittedAtUtc desc + _id desc` pour la file stable ;
- `targetType + targetRecordId + submittedAtUtc desc` pour l'historique ;
- les recherches publiques existantes ajoutent `isModerationSuspended = false`.

Au démarrage, `MongoDatabaseInitializer` met à `false` le champ absent sur les
publications et comparaisons existantes avant de créer les index. Il s'agit d'une
migration de l'autorité existante, pas d'un adaptateur ou d'une double lecture.

## Contrats et interface

- `POST passport/shared/reports` : anonyme, `202`, limité par IP ;
- `GET admin/share-moderation/reports` : admin, paginé et filtrable ;
- `PUT admin/share-moderation/reports/{reportId}` : admin, audité, décisions
  `Dismiss`, `Suspend`, `Restore`.

Le composant public est replié par défaut. La page admin utilise des cartes qui
s'adaptent à 320 px, des champs à largeur bornée, du retour à la ligne pour les
textes et des actions empilées sur les écrans étroits. Le module admin reste chargé
en lazy loading et n'alourdit pas le bundle public initial.

## Preuves automatisées

- transitions, conservation des versions et séparation public/privé dans le Core ;
- rejet des balises et liens dangereux ;
- orchestration de la résolution, de la suspension et de l'invalidation ;
- aller-retour Mongo, index de file et absence de jeton dans les rapports ;
- mappings HTTP sans références internes ;
- façades Angular, prévention des doubles envois et contrats responsive ;
- validation des huit catalogues de traduction et de l'architecture des ports.
