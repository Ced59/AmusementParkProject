# TRIP-13 — Pilote collaboratif et suivi privé

Date : 25 septembre 2026
Statut : implémenté

## 1. Enjeu métier

Un voyage partagé peut évoluer sans que chaque membre consulte constamment le
programme. Le suivi TRIP-13 permet donc à chaque participant de choisir, pour
chaque voyage, s’il souhaite voir les changements importants qu’il n’a pas
lui-même réalisés.

Le comportement est volontairement simple :

- le suivi est désactivé par défaut ;
- son activation est individuelle et privée ;
- les événements antérieurs à l’activation ne sont pas présentés comme nouveaux ;
- seuls les changements collaboratifs importants sont comptés ;
- les propres actions du membre ne gonflent pas son compteur ;
- le journal existant donne le détail sans recopier les données ;
- une actualisation manuelle suffit : aucun websocket, push ou polling permanent ;
- quitter un voyage ou supprimer celui-ci efface les abonnements concernés.

```mermaid
flowchart LR
    A[Voyage privé] --> B{Suivi activé ?}
    B -- non --> C[Aucun compteur]
    B -- oui --> D[Journal append-only]
    D --> E{Action importante d’un autre membre ?}
    E -- non --> F[Compteur inchangé]
    E -- oui --> G[Compteur de nouveautés]
    G --> H[Voir le journal]
    H --> I[Tout marquer comme vu]
```

## 2. Ce qui constitue une nouveauté

Sont suivis : titre, dates, parcs candidats, programme journalier, invitations,
participants et rôles, préférences, transfert de propriété et décisions
collectives.

Ne sont pas suivis :

- la création initiale du voyage, antérieure à tout abonnement utile ;
- l’export, qui est une lecture privée sans modification du plan ;
- les actions réalisées par le membre lui-même.

La politique est définie dans le Core par `TripNotificationPolicy`. Elle n’est
donc ni dupliquée dans MongoDB, ni décidée par le contrôleur ou Angular.

## 3. Architecture

### Core

`TripNotificationSubscription` porte les invariants : abonnement individuel,
curseur monotone, activation sans historique rétroactif et version optimiste.
`TripNotificationPolicy` centralise les types d’activité significatifs et borne
le compteur visible à 99, avec un indicateur `99+`.

### Application

`TripNotificationService` :

1. vérifie l’accès actif au voyage ;
2. charge l’abonnement du membre ;
3. demande au journal les événements importants après son curseur ;
4. exclut l’identité membre courante au niveau de la requête ;
5. applique une écriture optimiste lors de l’activation, désactivation ou lecture.

Les handlers restent minces. Le départ, la suppression immédiate et son
réconciliateur révoquent les abonnements via le port applicatif dédié.

Le handler de pilotage lit un instantané agrégé : voyages actifs, voyages avec
au moins deux membres actifs, plans ayant des préférences ou décisions,
invitations expirées encore retenues, suivis activés, volume d’audit, marqueurs
d’audit en attente et répartition des types d’activité. Son contrat ne contient
ni identifiant membre, ni titre, ni note.

### Infrastructure

MongoDB conserve uniquement un curseur par couple voyage/membre. Aucun document
de notification n’est dupliqué pour chaque événement : le journal TRIP-10 reste
la preuve canonique. Ce choix réduit les écritures, la rétention et la charge du
VPS.

Le tableau de bord utilise des comptages MongoDB, deux lectures distinctes de
clés de voyage et des agrégations par groupe. Il ne charge aucun document métier
complet et ne déclenche aucune requête par voyage ou par membre.

La lecture est bornée à 100 événements (`99 + preuve qu’il en reste`) et utilise
les index du journal sur `tripPlanId` et `sequence`. L’unicité de l’abonnement
est protégée par un index MongoDB, pas par une vérification en mémoire.

### WebAPI

Les trois opérations privées sont :

- `GET /me/trips/{tripPlanId}/notifications` ;
- `PUT /me/trips/{tripPlanId}/notifications` ;
- `POST /me/trips/{tripPlanId}/notifications/read`.

Elles exigent un compte activé, refusent un voyage inaccessible et répondent
avec `no-store`. La version courante est renvoyée lors d’un conflit optimiste.

L’instantané agrégé est exposé séparément par
`GET /admin/trip-pilot/metrics`, strictement réservé au rôle administrateur,
limité en débit et également `no-store`.

### Angular

Le panneau suit la chaîne `API service → data port → facade → composant`. Il se
trouve en haut du voyage, explique clairement l’opt-in, montre le compteur,
ouvre le journal et permet de tout marquer comme vu. Il n’expose aucun
identifiant technique.

La page d’administration « Pilote des voyages » est lazy-loaded et présente les
indicateurs agrégés, l’état du rattrapage d’audit et une répartition graphique
légère des activités. Elle ne rejoint jamais les comptes ou les textes privés.

## 4. Schéma MongoDB

```javascript
trip-notification-subscriptions {
  _id: string,                 // opaque, jamais exposé
  tripPlanId: string,          // clé du voyage privé
  userId: string,              // propriétaire de l’opt-in
  isEnabled: boolean,
  seenThroughSequence: long,   // dernière séquence globale reconnue
  version: long,               // concurrence optimiste
  createdAt: date,
  updatedAt: date
}
```

Index :

```text
UNIQUE (tripPlanId ASC, userId ASC)  uq_trip_notification_plan_user
       (userId ASC, isEnabled ASC)   ix_trip_notification_user_enabled
```

Le déploiement crée automatiquement la collection et ses index. Aucune
migration manuelle de données n’est nécessaire : l’absence de document signifie
simplement « suivi non activé ».

## 5. Diagramme de classes

```mermaid
classDiagram
    class TripNotificationsController
    class GetTripNotificationStateQueryHandler
    class SetTripNotificationsCommandHandler
    class MarkTripNotificationsReadCommandHandler
    class TripNotificationService
    class TripNotificationSubscription {
      +IsEnabled bool
      +SeenThroughSequence long
      +Version long
      +SetEnabled(enabled, sequence, now)
      +MarkSeenThrough(sequence, now)
    }
    class TripNotificationPolicy {
      +ImportantActivityKinds
      +MaximumUnreadCount
    }
    class ITripNotificationSubscriptionRepository
    class ITripAuditReader
    class TripNotificationSubscriptionRepository
    class TripAuditRepository
    class GetTripPilotMetricsQueryHandler
    class ITripPilotMetricsRepository
    class TripPilotMetricsRepository
    class AdminTripPilotController
    class TripNotificationPanelComponent
    class TripNotificationFacade
    class TripNotificationDataPort
    class TripNotificationApiService

    TripNotificationsController --> GetTripNotificationStateQueryHandler
    TripNotificationsController --> SetTripNotificationsCommandHandler
    TripNotificationsController --> MarkTripNotificationsReadCommandHandler
    GetTripNotificationStateQueryHandler --> TripNotificationService
    SetTripNotificationsCommandHandler --> TripNotificationService
    MarkTripNotificationsReadCommandHandler --> TripNotificationService
    TripNotificationService --> TripNotificationSubscription
    TripNotificationService --> TripNotificationPolicy
    TripNotificationService --> ITripNotificationSubscriptionRepository
    TripNotificationService --> ITripAuditReader
    ITripNotificationSubscriptionRepository <|.. TripNotificationSubscriptionRepository
    ITripAuditReader <|.. TripAuditRepository
    AdminTripPilotController --> GetTripPilotMetricsQueryHandler
    GetTripPilotMetricsQueryHandler --> ITripPilotMetricsRepository
    ITripPilotMetricsRepository <|.. TripPilotMetricsRepository
    TripNotificationPanelComponent --> TripNotificationFacade
    TripNotificationFacade --> TripNotificationDataPort
    TripNotificationDataPort <|.. TripNotificationApiService
```

## 6. Séquence d’activation et de consultation

```mermaid
sequenceDiagram
    actor M as Membre
    participant UI as Panneau Angular
    participant API as API privée
    participant APP as TripNotificationService
    participant AUDIT as Journal TRIP-10
    participant SUB as Abonnements MongoDB

    M->>UI: Active le suivi
    UI->>API: PUT enabled=true, expectedVersion=0
    API->>APP: SetEnabledAsync
    APP->>AUDIT: Dernière séquence matérialisée
    AUDIT-->>APP: 42
    APP->>SUB: Création (seenThroughSequence=42)
    SUB-->>APP: Succès unique
    APP-->>UI: Activé, 0 nouveauté

    Note over AUDIT: Un autre membre modifie le programme

    M->>UI: Actualise le panneau
    UI->>API: GET état
    API->>APP: GetAsync
    APP->>SUB: Charge le curseur 42
    APP->>AUDIT: Importants après 42, hors membre courant, limite 100
    AUDIT-->>APP: 3 événements
    APP-->>UI: 3 nouveautés
    M->>UI: Tout marquer comme vu
    UI->>API: POST read, expectedVersion
    APP->>AUDIT: Dernière séquence
    APP->>SUB: Replace si version inchangée
    APP-->>UI: 0 nouveauté, nouvelle version
```

## 7. Concurrence et reprise

Deux onglets ne peuvent pas écraser silencieusement la préférence : chaque
écriture cible l’identifiant, le voyage, le membre et la version attendue. Une
version différente produit un conflit `trip.notification.changed-concurrently`.
La façade recharge alors l’état serveur.

Après une première création, le service relit l’appartenance. Si le membre a
quitté le voyage ou si celui-ci a été supprimé pendant la course, il compense
immédiatement l’insertion avant de répondre. Si le départ intervient après
cette relecture, le nettoyage obligatoire du départ supprime l’abonnement.

Un événement matérialisé après un « tout marquer comme vu » reçoit une séquence
supérieure et reste donc visible : la course ne peut pas faire perdre une
nouveauté. À l’inverse, une action personnelle est filtrée mais sa séquence peut
être franchie sans risque, puisque le curseur est global au journal du voyage.

## 8. Confidentialité, sécurité et performance

- opt-in explicite, jamais activé automatiquement ;
- aucune adresse, jeton d’invitation, note ou préférence détaillée dans la réponse ;
- contrôle d’appartenance avant chaque lecture ou mutation ;
- réponse `no-store` ;
- compteur borné et pas de N+1 ;
- aucune boucle de polling ni connexion permanente ;
- suppression ciblée lors du départ, purge complète avant suppression du voyage ;
- domaine `WATCH` public inchangé : aucun mélange entre faits publics et actions privées.

## 9. Responsive et accessibilité

Le panneau et le tableau de bord admin sont contenus dès 320 pixels :
`min-width: 0`, rupture des textes longs, grilles `minmax(0, 1fr)` et actions
empilées sous 36 rem. Le compteur est annoncé dans une zone `aria-live`, les
erreurs utilisent `role="alert"` et tous les boutons conservent un libellé
textuel. Les huit langues sont fournies. La fixture Chromium réelle couvre les
deux nouveaux écrans à 320, 360, 390, 768 et 1280 pixels.

## 10. Observabilité et gate pilote

Le journal append-only constitue la preuve produit : créations, ajouts de parc,
invitations, acceptations/refus, préférences, décisions, participants et volume
d’audit sont reconstituables sans donnée publique ni visite réelle. Les conflits
optimistes restent identifiables par leurs codes applicatifs stables.

Le tableau de bord admin rend directement visibles les indicateurs actuellement
prouvables, dont les invitations expirées présentes durant leur période de
rétention, ainsi que les marqueurs d’audit qui n’ont pas encore été
matérialisés. Les métriques non persistées historiquement, telles qu’une latence
d’interface précise ou un conflit seulement vu côté client, ne sont pas
inventées : leur instrumentation nécessitera un contrat dédié si elles deviennent
un critère de décision réel.

La condition communautaire de cohorte réelle a été explicitement retirée du
chemin bloquant par décision produit. La gate TRIP-13 atteste donc les capacités
techniques et métier testables ; elle ne fabrique pas une preuve d’adoption.

## 11. Preuves

- 7 tests Core : activation, réactivation, curseur monotone et politique ;
- 6 tests Application dédiés : opt-in, absence de rétroactivité, compteur,
  concurrence, compensation de départ et métriques agrégées sans contenu privé ;
- 15 tests Application du périmètre : notifications, départ, purge et pilotage ;
- 16 tests Infrastructure du périmètre : index, filtre privé, agrégations et
  résolution du nettoyage en tâche de fond ;
- 1 test WebAPI du contrat agrégé ;
- 17 tests Angular ciblés : façades, navigation admin et contrats responsive ;
- build WebAPI Release réussi ;
- build Angular production/SSR réussi ;
- architecture façade/ports et règle une classe par fichier réussies ;
- i18n générée et validée pour les huit langues.
