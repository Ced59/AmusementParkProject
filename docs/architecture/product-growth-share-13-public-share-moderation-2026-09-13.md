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
    +ShareModerationReportId ModerationSuspensionReportId
    +bool IsModerationSuspended
    +bool IsResolvable
    +SuspendByModeration()
    +RestoreAfterModeration()
  }
  class ProfileComparison {
    +ShareModerationReportId ModerationSuspensionReportId
    +bool IsModerationSuspended
    +bool IsPubliclyResolvable
    +SuspendByModeration()
    +RestoreAfterModeration()
  }
  class ShareModerationService {
    +SubmitAsync()
    +ReviewAsync()
  }
  class ShareModerationDecisionScheduler
  class ShareModerationDecisionExecutor
  class ShareModerationPublicationTargetExecutor
  class ShareModerationComparisonTargetExecutor
  class ShareModerationDecisionJobHandler
  class IDurableBackgroundJobRepository
  class IShareModerationReportRepository
  class ISharePublicationRepository
  class IProfileComparisonRepository
  class SharePublicationCacheInvalidationScheduler

  ShareModerationService --> ShareModerationReport
  ShareModerationService --> IShareModerationReportRepository
  ShareModerationService --> ShareModerationDecisionScheduler
  ShareModerationService --> ShareModerationDecisionExecutor
  ShareModerationDecisionScheduler --> IDurableBackgroundJobRepository
  ShareModerationDecisionJobHandler --> ShareModerationDecisionExecutor
  ShareModerationDecisionExecutor --> ShareModerationPublicationTargetExecutor
  ShareModerationDecisionExecutor --> ShareModerationComparisonTargetExecutor
  ShareModerationPublicationTargetExecutor --> ISharePublicationRepository
  ShareModerationPublicationTargetExecutor --> SharePublicationCacheInvalidationScheduler
  ShareModerationComparisonTargetExecutor --> IProfileComparisonRepository
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
  participant J as Tâche durable de décision
  participant E as ShareModerationDecisionExecutor
  participant T as Publication ou comparaison
  participant R as Rapport
  participant C as Invalidation des caches

  A->>UI: Suspendre le partage
  UI->>API: décision Suspend + note facultative
  API->>S: Review(command, adminId)
  S->>R: recharge Pending avec version
  S->>J: enregistre la décision avant toute mutation
  S->>E: première tentative synchrone
  E->>T: SuspendByModeration(reportId, now)
  T-->>E: IsResolvable = false + reportId actif
  E->>C: purge les caches publics si publication
  E->>R: MarkPublicationSuspended(adminId)
  R-->>API: écriture optimiste réussie
  API-->>UI: 204 puis rechargement de la file
  opt panne ou conflit entre les deux écritures
    API-->>UI: 204 car la décision durable est acceptée et auditée
    J->>E: rejoue la même décision idempotente
    E->>T: complète ou compense l'état manquant
    E->>R: complète l'état métier manquant
  end
```

Le rétablissement effectue la transition inverse uniquement depuis le rapport qui
a posé la suspension. Un ancien rapport ne peut donc pas lever une suspension plus
récente. Le jeton, le snapshot et `PublicationVersion` sont conservés : le contenu
revient à l'identique sans créer un second système de partage. MongoDB fonctionne
en instance simple en production ; la tâche durable, créée avant toute mutation,
assure la compensation et la convergence sans supposer des transactions
multi-documents indisponibles.
Deux suspensions visant la même cible sont elles aussi sérialisées par l'état
métier : tant qu'un rapport plus ancien est encore attaché à la cible, la décision
suivante reste rejouable au lieu d'être abandonnée. Dès que le rétablissement
antérieur termine, le worker applique la suspension suivante et empêche une
réouverture publique entre deux décisions pourtant acceptées.
Une fois la tâche durable enregistrée, un conflit ou une indisponibilité pendant
la première tentative reste une décision acceptée : l'API répond avec succès afin
que l'action administrative et son auteur soient bien inscrits dans l'audit HTTP,
puis le worker converge vers cette décision. Cela inclut le cas où un autre
signalement suspend déjà temporairement la même cible : la nouvelle décision reste
explicitement acceptée au lieu de renvoyer un conflit tout en laissant sa tâche
exécutable. Une cible ou un rapport définitivement absent reste en revanche refusé.
Chaque rejeu programme aussi l'invalidation du cache public, y compris lorsque la
cible porte déjà la décision attendue : une coupure entre l'écriture MongoDB et la
création de cette invalidation ne peut donc pas laisser durablement une ancienne
page publique en cache. Le worker écrit enfin une trace d'achèvement idempotente,
indépendante du cycle de vie de la requête HTTP, afin de conserver la preuve de la
mutation même si le navigateur admin s'est déconnecté après sa mise en file.
Si une dépendance reste indisponible pendant toute la fenêtre de rejeu, la tâche
crée une continuation durable avant de terminer : la convergence n'est donc pas
abandonnée après un nombre fixe de tentatives.

## Schéma MongoDB

```mermaid
erDiagram
  SHARE_PUBLICATIONS {
    string _id PK
    string shareToken UK
    string type
    string status
    string moderationSuspensionReportId FK
    long publicationVersion
    long version
  }
  PROFILE_COMPARISONS {
    string _id PK
    string shareToken UK
    string status
    string moderationSuspensionReportId FK
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
  SHARE_PUBLICATIONS o|--o{ SHARE_MODERATION_REPORTS : "suspension active et historique"
  PROFILE_COMPARISONS o|--o{ SHARE_MODERATION_REPORTS : "suspension active et historique"
```

Index :

- `status + submittedAtUtc desc + _id desc` pour la file stable ;
- `targetType + targetRecordId + submittedAtUtc desc` pour l'historique ;
- les recherches publiques existantes exigent
  `moderationSuspensionReportId = null`.

Au démarrage, `MongoDatabaseInitializer` ajoute à `null` la référence de suspension
absente sur les publications et comparaisons existantes avant de créer les index.
Il s'agit d'une migration de l'autorité existante, pas d'un adaptateur ou d'une
double lecture.

## Contrats et interface

- `POST passport/shared/reports` : anonyme, `202`, limité par IP ;
- `GET admin/share-moderation/reports` : admin, paginé et filtrable ;
- `PUT admin/share-moderation/reports/{reportId}` : admin, audité, décisions
  `Dismiss`, `Suspend`, `Restore`.

Le composant public est replié par défaut. La page admin utilise des cartes qui
s'adaptent à 320 px, des champs à largeur bornée, du retour à la ligne pour les
textes et des actions empilées sur les écrans étroits. Le module admin reste chargé
en lazy loading et n'alourdit pas le bundle public initial.
Dans l'espace propriétaire, une publication suspendue reste identifiée comme une
publication existante : un encart explique que seul son accès public est coupé,
masque les actions inapplicables et conserve la révocation. Le propriétaire ne peut
donc ni confondre la suspension avec une mise en privé, ni être poussé vers une
nouvelle publication qui serait refusée par la règle de modération.

## Preuves automatisées

- transitions, conservation des versions et séparation public/privé dans le Core ;
- rejet des balises et liens dangereux ;
- orchestration de la résolution, de la suspension et de l'invalidation ;
- liaison de la suspension à son rapport exact, rejeu après conflit et compensation
  d'une décision concurrente ;
- maintien en rejeu d'une suspension concurrente jusqu'au rétablissement du rapport
  précédent, pour les publications comme pour les comparaisons ;
- reprogrammation de l'invalidation après une panne intermédiaire et audit
  d'achèvement idempotent par le worker ;
- acquittement auditable d'une décision durable et refus explicite de republier
  un partage encore suspendu ;
- aller-retour Mongo, index de file et absence de jeton dans les rapports ;
- rejet d'une pagination dont le décalage dépasserait la limite MongoDB ;
- mappings HTTP sans références internes ;
- façades Angular, prévention des doubles envois et contrats responsive ;
- validation des huit catalogues de traduction et de l'architecture des ports.
