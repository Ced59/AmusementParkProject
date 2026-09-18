# TRIP-10 — Journal d’activité et concurrence explicite

## 1. Résultat métier

`TRIP-10` permet à chaque membre actif d’un voyage privé de comprendre les
changements importants du groupe : nature de l’action, auteur encore visible,
date et volume concerné. Le journal est accessible depuis la page du voyage et
présente les événements les plus récents en premier.

Le jalon ne crée pas un second moteur de concurrence. Il complète les garanties
déjà livrées par les versions optimistes, les leases enfant, les epochs de
suppression, les clés d’idempotence et les positions stables. Les écrans
continuent d’afficher les conflits et de recharger l’état serveur ; le journal
explique ensuite les mutations effectivement enregistrées.

Le journal couvre :

- création et renommage du voyage ;
- modification des dates ;
- ajout, mise à jour, déplacement et retrait d’un parc candidat ;
- ajout, mise à jour et retrait d’une journée ;
- création, révocation, acceptation et refus d’une invitation ;
- changement de rôle, transfert de l’organisation et départ d’un membre ;
- modification d’un lot de préférences, sans son contenu ;
- modification d’une décision collective.

L’historique commence au déploiement de `5.3.70`. Les actions antérieures ne sont
pas inventées ni reconstituées à partir de dates techniques incomplètes.

## 2. Ce que le journal est — et n’est pas

Le journal est une preuve narrative append-only. Il répond à « qui a fait quel
type de changement et quand ? ». L’état actuel reste porté par les collections
métier ; il n’est jamais recalculé en rejouant l’audit.

| Conservé dans MongoDB | Renvoyé à l’interface | Jamais conservé dans l’audit |
|---|---|---|
| voyage, membre interne éventuel, rôle au moment de l’action | libellé public de l’acteur s’il est encore membre actif | identifiant de compte, adresse e-mail ou jeton d’invitation |
| type, séquence, clé d’opération, quantité et date UTC | type, auteur lisible, quantité et date | préférence individuelle, raison privée ou note libre |
| identifiants nécessaires à l’idempotence | aucun identifiant technique | ancien ou nouveau titre, commentaire collectif |

Un ancien membre, un invité ayant refusé ou un compte qui n’est plus membre est
affiché sous le libellé localisé « Personne non affichée ». L’API transmet pour
cela un marqueur neutre et ne résout jamais son profil, afin de ne pas transformer
le journal en annuaire de personnes ayant quitté le voyage.

## 3. Architecture

```mermaid
classDiagram
    class TripActivityEvent {
      +string Id
      +TripPlanId TripPlanId
      +TripMemberId? ActorMemberId
      +TripEffectiveRole? ActorRole
      +TripActivityKind Kind
      +string OperationKey
      +long Sequence
      +int AffectedCount
      +DateTime OccurredAtUtc
    }
    class TripActivityRecorder {
      +RecordAsync(trip, actor, kind, operation, count)
    }
    class ITripAuditWriter {
      <<interface>>
      +AppendAsync(write)
    }
    class ITripAuditReader {
      <<interface>>
      +ListAsync(tripId, cursor, limit)
    }
    class TripAuditRepository
    class TripActivityService {
      +GetAsync(userId, tripId, cursor)
    }
    class TripActivityController
    class TripActivityFacade
    class TripActivityDataPort {
      <<interface>>
    }
    class TripActivityApiService

    TripActivityRecorder --> ITripAuditWriter
    TripAuditRepository ..|> ITripAuditWriter
    TripAuditRepository ..|> ITripAuditReader
    TripAuditRepository --> TripActivityEvent
    TripActivityService --> ITripAuditReader
    TripActivityController --> TripActivityService : query handler
    TripActivityFacade --> TripActivityDataPort
    TripActivityApiService ..|> TripActivityDataPort
```

- **Core** valide l’événement immuable, ses bornes et son rôle d’acteur.
- **Application** enregistre après une mutation métier réussie, contrôle l’accès
  à la lecture et minimise les identités visibles.
- **Infrastructure** alloue une séquence par voyage et garantit l’idempotence.
- **WebAPI** expose un contrat privé non mis en cache et sans identifiant.
- **Angular** respecte `service API -> port -> façade -> composant` ; la façade
  remplace la page récente et ajoute les pages anciennes sans doublon.

Chaque classe demeure dans son propre fichier.

## 4. Schéma MongoDB

```mermaid
erDiagram
    TRIP_PLANS ||--o{ TRIP_AUDIT_EVENTS : "auditSequence / tripPlanId"
    TRIP_PLANS {
      string _id
      long auditSequence
      string deletionState
      long version
      long childMutationEpoch
    }
    TRIP_AUDIT_EVENTS {
      string _id
      string tripPlanId
      string actorMemberId_nullable
      string actorRole_nullable
      string kind
      string operationKey
      long sequence
      int affectedCount
      date createdAt
      date updatedAt
    }
```

Indexes de `trip-audit-events` :

```javascript
{ tripPlanId: 1, operationKey: 1 } // unique : même commande, même preuve
{ tripPlanId: 1, sequence: -1 }    // unique : ordre stable du journal
{ tripPlanId: 1, createdAt: -1 }   // diagnostic chronologique
```

L’allocation incrémente atomiquement `trip-plans.auditSequence` uniquement si le
voyage n’est pas en suppression. Une répétition de la même opération retrouve
l’événement existant. Une tentative après le début d’une suppression est refusée.
Après l’insertion, le repository revérifie la barrière de suppression et retire
l’événement si la fermeture a gagné la course. Si la fermeture commence après
cette vérification, la purge voit déjà l’événement et le retire normalement.
La purge du voyage supprime la collection enfant avec les candidats, jours,
invitations, préférences et décisions.

Les anciens documents `trip-plans` n’ont pas besoin de migration de données :
l’absence de `auditSequence` vaut zéro, puis le premier `$inc` produit la séquence
1. La collection et ses indexes sont créés par l’initialiseur MongoDB au
démarrage. Aucune commande manuelle en production n’est requise.

## 5. Séquence d’écriture

```mermaid
sequenceDiagram
    actor M as Membre
    participant UI as Écran du voyage
    participant A as Service Application
    participant B as Collection métier
    participant R as TripActivityRecorder
    participant P as trip-plans
    participant J as trip-audit-events

    M->>UI: confirme une modification
    UI->>A: commande + version/clé attendue
    A->>B: écriture protégée par version ou lease
    alt conflit ou refus
      B-->>A: aucune mutation
      A-->>UI: conflit explicite + état serveur
    else mutation réussie
      B-->>A: état persisté
      A->>R: type + acteur + opération + quantité
      R->>P: $inc auditSequence si suppression absente
      P-->>R: prochaine séquence
      R->>J: insert append-only
      alt clé déjà enregistrée
        J-->>R: événement existant
      else nouvelle preuve
        J-->>R: événement créé
      end
      A-->>UI: résultat métier
    end
```

Les écritures d’audit utilisent `CancellationToken.None` après la réussite métier
afin qu’une fermeture du navigateur ne coupe pas la preuve. Les clés stables des
opérations idempotentes empêchent un double événement lors d’une répétition.

## 6. Séquence de lecture et confidentialité

```mermaid
sequenceDiagram
    actor M as Membre actif
    participant UI as Journal Angular
    participant API as GET activité
    participant S as TripActivityService
    participant T as trip-plans
    participant J as trip-audit-events
    participant U as utilisateurs

    M->>UI: ouvre « Voir l’historique »
    UI->>API: GET /me/trips/{id}/activity
    API->>S: userId authentifié + tripId
    S->>T: GetAccessibleAsync
    alt voyage inaccessible
      S-->>API: 404 neutre
    else membre actif
      S->>J: 31 événements avant le curseur
      S->>S: garde 30 événements et calcule le curseur
      S->>U: noms des seuls acteurs encore membres actifs
      S-->>API: titres lisibles, aucun identifiant
      API-->>UI: no-store
      UI-->>M: timeline traduite
    end
```

La pagination est bornée à 30 événements et utilise la séquence décroissante,
pas une page numérique fragile sous écriture concurrente. `nextBeforeSequence`
est un curseur d’ordre, pas un identifiant MongoDB.

## 7. Concurrence réutilisée

Le jalon s’appuie sur les mécanismes déjà déployés :

1. `TripPlan.Version` protège les mutations de la racine ;
2. `TripChildMutationLease` porte opération, epoch et génération pour candidats,
   jours, préférences et décisions ;
3. les clés client protègent création du voyage, candidats et invitations ;
4. `SortPosition` évite de remplacer toute une liste pour un déplacement ;
5. la suppression ferme admissions et nouvelles leases avant la purge ;
6. les façades affichent les conflits et rechargent le serveur.

Aucun websocket ni SignalR n’est ajouté : une actualisation manuelle est
disponible et évite un coût permanent sur le VPS. Une notification de voyage
opt-in exige un contrat dédié lors de `TRIP-13` ; le domaine `WATCH` actuel suit
des changements factuels de parcs et ne doit pas être détourné pour des actions
privées de membres.

## 8. Expérience responsive et accessibilité

La timeline est conçue dès 320 pixels :

- `min-width: 0`, `max-width: 100%` et `overflow-wrap: anywhere` sur les zones
  contenant les pseudonymes et textes traduits ;
- colonnes `3rem minmax(0, 1fr)`, réduites sur les téléphones très étroits ;
- marge basse intégrant navigation fixe et safe area ;
- boutons d’actualisation et de pagination avec état occupé ;
- dates rendues avec la langue active et balise `time` ;
- erreurs initiales et erreurs de pagination distinguées ;
- aucune commande désactivée sans utilité ni identifiant technique visible.

Le contrôle Chromium partagé mesure 320, 360, 390, 768 et 1280 pixels et vérifie
le débordement horizontal de la page, des cartes, contrôles et liens.

## 9. Preuves automatisées

- **Core** : acteur invité sans membre, UTC, séquence et quantité bornées.
- **Application** : accès obligatoire, page bornée, curseur, nom des seuls membres
  actifs et anonymisation d’un ancien participant.
- **Infrastructure** : unicité opération/séquence et ordre MongoDB.
- **WebAPI** : enum métier sérialisé en texte et absence de propriété `*Id`.
- **Angular** : URL encodée, `transferCache: false`, pagination sans doublon,
  actualisation, route authentifiée, façade/port et une classe par fichier.
- **Responsive** : assertions CSS et mesures dans un vrai Chromium.
- **i18n** : huit langues issues des fragments source et fichiers générés vérifiés.

## 10. Limites assumées

- le journal ne fabrique pas l’historique antérieur à `5.3.70` ;
- il explique une mutation, mais l’état métier courant reste la source de vérité ;
- il ne diffuse pas les événements en temps réel ;
- les exports seront traités séparément dans `TRIP-11`, avec des libellés métier
  et sans identifiants techniques isolés.
