# TRIP-12 — Transition privée du voyage vers le Passeport

Date : 25 septembre 2026
Statut : implémenté

## 1. Enjeu métier

Une journée planifiée n’est pas une preuve de visite et une préférence n’est pas
une preuve de tour effectué. Après la date du voyage, chaque participant peut
donc repartir du programme pour créer **son propre brouillon privé** :

- le parc et la date sont préremplis ;
- aucune visite n’est créée avant une confirmation explicite ;
- aucune attraction n’est présélectionnée ;
- les préférences personnelles sont affichées uniquement comme aide-mémoire ;
- les choix et préférences des autres participants ne sont jamais copiés ;
- chaque attraction cochée devient un passage effectué uniquement après la
  confirmation du membre ;
- le voyage reste conservé : la transition ne le supprime pas et ne modifie pas
  son statut, y compris lorsqu’il est déjà archivé.

Le résultat est volontairement un brouillon. Le membre peut ensuite corriger,
noter, compléter ou terminer la visite avec le parcours Passeport existant.

## 2. Parcours utilisateur

```mermaid
flowchart LR
    A[Voyage privé] --> B[Compléter mon Passeport]
    B --> C{Journée passée ?}
    C -- non --> D[Information : disponible après la journée]
    C -- oui --> E[Parc et date préremplis]
    E --> F[Sélection manuelle des attractions réellement faites]
    F --> G[Confirmation explicite]
    G --> H[Brouillon de visite privé]
    H --> I[Éditeur Passeport existant]
```

Si une visite existe déjà pour le même membre, le même parc et la même date,
l’interface propose de l’ouvrir au lieu d’en créer une seconde.

## 3. Frontières d’architecture

### Core

Le domaine Passeport reste propriétaire de la signification d’un passage. La
source `TripTransition` complète `RideLogSource` pour conserver la provenance
de la déclaration sans introduire une règle Voyage dans l’entité `Visit`.

### Application

Deux services applicatifs ciblés orchestrent les deux parcours sans mélanger lecture et écriture :

- `TripPassportTransitionReader` construit la proposition privée ;
- `TripPassportTransitionConfirmer` transforme une sélection explicite en brouillon de visite.

1. vérification de l’accès au voyage ;
2. lecture cohérente de son programme ;
3. calcul de la date courante dans le fuseau de destination ;
4. lecture groupée des visites existantes ;
5. lecture du catalogue public et des seules préférences de l’appelant ;
6. création par les handlers Passeport existants ;
7. ajout idempotent des attractions explicitement sélectionnées.

Les validations, l’audit, les verrous de contenu et les règles de brouillon ne
sont pas contournés : la transition réutilise `CreateVisitCommand` et
`AddRideOccurrencesBatchCommand`.

### Infrastructure

La recherche des visites aux dates du voyage utilise une seule requête MongoDB
bornée par le propriétaire et `dateSortKey`. Aucun parcours complet de
collection ni requête par journée n’est ajouté.

### WebAPI

Le contrôleur authentifié expose une proposition privée et une confirmation
pour une journée précise. Les réponses ont `no-store`. Les identifiants restent
des clés d’action ; les libellés visibles utilisent les noms hydratés.

### Angular

La page lazy-loaded suit le découpage `API service → data port → facade → page`.
Le composant ne contient que l’état de sélection visuel et la navigation vers
le brouillon créé.

## 4. Diagramme de classes

```mermaid
classDiagram
    class TripPassportTransitionsController {
      +GetAsync(tripPlanId)
      +ConfirmAsync(tripPlanId, localDate, request)
    }
    class GetTripPassportTransitionQueryHandler
    class ConfirmTripPassportTransitionCommandHandler
    class TripPassportTransitionReader {
      +GetAsync(userId, tripPlanId)
    }
    class TripPassportTransitionConfirmer {
      +ConfirmAsync(userId, tripPlanId, localDate, parkItemIds)
    }
    class TripProgramResultFactory {
      +BuildSnapshotAsync(tripPlanId)
    }
    class IUserVisitRepository {
      +ListOwnedByExactDatesAsync(userId, dates)
      +ResolveExistingCreationAsync(visit, operationId)
      +GetOwnedAsync(visitId, userId)
    }
    class CreateVisitCommandHandler
    class AddRideOccurrencesBatchCommandHandler
    class TripPassportTransitionApiService
    class TripPassportTransitionDataPort
    class TripPassportTransitionFacade
    class TripPassportTransitionPageComponent

    TripPassportTransitionsController --> GetTripPassportTransitionQueryHandler
    TripPassportTransitionsController --> ConfirmTripPassportTransitionCommandHandler
    GetTripPassportTransitionQueryHandler --> TripPassportTransitionReader
    ConfirmTripPassportTransitionCommandHandler --> TripPassportTransitionConfirmer
    TripPassportTransitionReader --> TripProgramResultFactory
    TripPassportTransitionReader --> IUserVisitRepository
    TripPassportTransitionConfirmer --> TripProgramResultFactory
    TripPassportTransitionConfirmer --> IUserVisitRepository
    TripPassportTransitionConfirmer --> CreateVisitCommandHandler
    TripPassportTransitionConfirmer --> AddRideOccurrencesBatchCommandHandler
    TripPassportTransitionPageComponent --> TripPassportTransitionFacade
    TripPassportTransitionFacade --> TripPassportTransitionDataPort
    TripPassportTransitionDataPort <|.. TripPassportTransitionApiService
```

## 5. Séquence de consultation

```mermaid
sequenceDiagram
    actor M as Membre
    participant UI as Page Angular
    participant API as API transition
    participant APP as Service Application
    participant TRIP as Repositories Voyage
    participant PASS as Repository visites
    participant CAT as Catalogue

    M->>UI: Ouvre « Compléter mon Passeport »
    UI->>API: GET /me/trips/{id}/passport-transition
    API->>APP: GetAsync(userId, tripId)
    APP->>TRIP: Voyage accessible + snapshot cohérent
    APP->>PASS: Visites du membre aux dates du voyage (1 requête)
    APP->>CAT: Attractions visibles + images principales
    APP->>TRIP: Préférences du seul membre appelant
    APP-->>API: Jours passés/futurs + visites existantes
    API-->>UI: Proposition privée no-store
    UI-->>M: Cartes illustrées, aucune case présélectionnée
```

## 6. Séquence de confirmation et reprise sur incident

```mermaid
sequenceDiagram
    actor M as Membre
    participant UI as Page Angular
    participant APP as Service transition
    participant VISIT as Handler création visite
    participant RIDE as Handler ajout passages
    participant DB as MongoDB

    M->>UI: Coche les attractions réellement faites
    UI->>APP: POST confirmation de la journée
    APP->>DB: Recherche visite exacte du membre
    alt visite manuelle déjà existante
      APP-->>UI: Réutiliser la visite existante
    else aucune visite ou création TRIP-12 rejouée
      APP->>VISIT: Création idempotente du brouillon privé
      VISIT->>DB: Insert ou replay par clé déterministe
      APP->>RIDE: Batch idempotent des seules cases cochées
      RIDE->>DB: Insert atomique ou replay du batch
      APP-->>UI: Identifiant de la visite
      UI-->>M: Ouverture de l’éditeur Passeport
    end

    Note over APP,DB: Si la visite a été créée mais que le batch échoue,
    Note over APP,DB: une relance reconnaît la clé de création et reprend le même brouillon.
```

Les clés d’opération sont dérivées par SHA-256 de l’identifiant du voyage, du
membre et de la date. Elles ne contiennent donc pas directement ces valeurs et
restent stables entre deux tentatives.

## 7. Schéma MongoDB utilisé

Aucune collection ni migration supplémentaire n’est nécessaire. La transition
réutilise les documents Passeport et leurs index existants.

```mermaid
erDiagram
    tripPlans ||--o{ tripDayPlans : programme
    tripPlans ||--o{ tripItemPreferences : contient
    userVisits ||--o{ userRideOccurrences : contient

    tripPlans {
      string _id
      string ownerUserId
      string destinationTimeZoneId
      string status
      array members
    }
    tripDayPlans {
      string tripPlanId
      date localDate
      string parkId
    }
    tripItemPreferences {
      string tripPlanId
      string userId
      string parkItemId
      string level
    }
    userVisits {
      string _id
      string userId
      string parkId
      object date
      int dateSortKey
      string status
      string privacy
      string creationOperationKeyHash
      string creationPayloadHash
    }
    userRideOccurrences {
      string _id
      string visitId
      string userId
      string parkItemId
      string status
      string source
      string creationOperationKeyHash
      string creationPayloadHash
    }
```

La proposition lit `tripItemPreferences` avec `tripPlanId + userId`. Elle ne
charge pas le résumé collectif et ne reçoit jamais les préférences d’un autre
participant.

## 8. Invariants et preuves

| Invariant | Preuve d’implémentation |
|---|---|
| aucune création automatique | seule la route POST appelle les handlers de création |
| journée strictement passée | comparaison `localDate < destinationToday` dans le fuseau du voyage |
| visite privée et en brouillon | création par le handler Passeport, dont les valeurs initiales sont `Private` et `Draft` |
| aucune priorité transformée automatiquement | la préférence n’est qu’un badge ; la sélection Angular commence vide |
| aucune donnée d’un autre participant | appel unique à `ListForUserAsync(tripId, currentUserId)` |
| aucune attraction d’un autre parc | validation serveur contre le catalogue visible du parc de la journée |
| pas de doublon de visite | détection membre/parc/date puis clés d’idempotence déterministes |
| reprise après échec partiel | reconnaissance de la création précédente puis replay du batch de passages |
| pas de fuite SSR/cache | route authentifiée et réponse `no-store` |
| plan conservé | aucune mutation ou suppression du voyage pendant la transition |

## 9. Responsive et accessibilité

- largeur bornée par `min-width: 0`, `max-width: 100%` et `overflow-x: clip` ;
- cartes en deux colonnes sur grand écran puis une colonne sous 768 px ;
- image ramenée à 5 rem sur téléphone ;
- actions empilées avant 576 px ;
- variante 320 px dédiée ;
- marge basse tenant compte de la navigation fixe et de la safe area ;
- cartes de sélection rendues comme boutons avec `aria-pressed` ;
- état sélectionné communiqué par le texte, la bordure et une icône ;
- le parcours reste entièrement utilisable sans glisser-déposer.

Le contrôle Chromium vérifie les largeurs 320, 360, 390, 768 et 1280 px, les
dépassements horizontaux et le dégagement de la navigation mobile.

## 10. Validation

- tests Application : proposition passée/future, préférence personnelle,
  sélection explicite, reprise d’un échec partiel ;
- tests Angular : endpoints, filtrage défensif de la facade, erreur de
  confirmation, route authentifiée ;
- contrat responsive statique ;
- contrôle réel Chromium multi-viewport ;
- contrôle i18n sur huit langues ;
- contrôles d’architecture facade/port et une classe par fichier.
