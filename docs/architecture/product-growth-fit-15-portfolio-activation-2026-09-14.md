# FIT-15 — Activation explicite du portefeuille Park Fit

## 1. Résultat métier

La publication générale d'un parc et sa présence dans Park Fit sont désormais deux
décisions différentes. Un parc peut rester parfaitement visible sur le site sans
participer aux recommandations tant que son audit n'est pas prêt ou que l'équipe ne
l'a pas activé explicitement.

Le portefeuille possède trois états :

| État | Fiche publique | Recherche Park Fit | Action attendue |
|---|---:|---:|---|
| `NotActivated` | inchangée | exclu | compléter puis activer |
| `Active` | inchangée | candidat si la qualité reste suffisante | surveiller |
| `Suspended` | inchangée | exclu temporairement | corriger puis rétablir, ou retirer |

Le volume de visites, le nombre de comptes et la taille d'une cohorte ne participent
pas à cette décision. La gate est factuelle : le parc doit réussir le même audit de
qualité que celui utilisé par le moteur public.

## 2. Règles de transition

```mermaid
stateDiagram-v2
    [*] --> NotActivated: nouveau parc ou absence d'état
    NotActivated --> Active: Activate\nqualité obligatoire
    Active --> Suspended: Suspend\nincident temporaire
    Suspended --> Active: Restore\nqualité obligatoire
    Active --> NotActivated: Deactivate\nretrait du portefeuille
    Suspended --> NotActivated: DeactivateDuringSuspension\nsans réactivation intermédiaire
```

Chaque décision exige une justification sans balisage, un acteur admin, une date UTC
et la révision attendue. Une écriture concurrente est refusée. L'historique est
borné aux 50 décisions les plus récentes, mais chaque fragment conservé reste
validable même après troncature.

L'activation et le rétablissement relisent :

- la visibilité et le cycle de vie du parc ;
- les coordonnées, le type et le contenu public ;
- les attractions visibles et ouvertes ;
- leurs conditions d'accès, sources, dates, portées et cohérence ;
- la couverture actuelle du calendrier d'ouverture.

Seul `EligibleForFitComparison` permet de passer à `Active`. L'interface désactive
l'action dans les autres cas et le serveur répète impérativement le contrôle.

## 3. Architecture

```mermaid
classDiagram
    class ParkFitOperationalStatus {
      +ParkId string
      +State ParkFitRecommendationState
      +Revision long
      +Decisions IReadOnlyCollection
      +Activate(actor, reason, at)
      +Suspend(actor, reason, at)
      +RestoreRecommendations(actor, reason, at)
      +Deactivate(actor, reason, at)
    }
    class ParkFitOperationalDecision {
      +Type ParkFitOperationalDecisionType
      +ActorUserId string
      +Reason string
      +DecidedAtUtc DateTime
      +Revision long
    }
    class ChangeParkFitOperationalStatusCommandHandler
    class ParkFitDataQualityAssessor
    class IParkFitOperationalStatusRepository {
      <<interface>>
      +GetAsync(parkId)
      +GetByParkIdsAsync(parkIds)
      +ReplaceAsync(status, expectedRevision)
    }
    class ParkFitOperationalStatusRepository
    class SearchParksByFitQueryHandler
    class AdminParkFitOperationalControlsComponent
    class AdminParkFitDataQualityFacade
    class AdminParkFitDataQualityStatePort {
      <<interface>>
    }
    class AdminParkFitDataQualityApiService
    class AdminParkFitOperationsController

    ParkFitOperationalStatus "1" *-- "0..50" ParkFitOperationalDecision
    ChangeParkFitOperationalStatusCommandHandler --> ParkFitOperationalStatus
    ChangeParkFitOperationalStatusCommandHandler --> ParkFitDataQualityAssessor
    ChangeParkFitOperationalStatusCommandHandler --> IParkFitOperationalStatusRepository
    IParkFitOperationalStatusRepository <|.. ParkFitOperationalStatusRepository
    SearchParksByFitQueryHandler --> IParkFitOperationalStatusRepository
    AdminParkFitOperationalControlsComponent --> AdminParkFitDataQualityFacade
    AdminParkFitDataQualityFacade --> AdminParkFitDataQualityStatePort
    AdminParkFitDataQualityStatePort <|.. AdminParkFitDataQualityApiService
    AdminParkFitDataQualityApiService --> AdminParkFitOperationsController
    AdminParkFitOperationsController --> ChangeParkFitOperationalStatusCommandHandler
```

Le Core possède le cycle de vie et l'audit de qualité. L'Application collecte les
faits par les ports existants et orchestre la transition. Infrastructure est seule
à connaître MongoDB et la migration physique. Le contrôleur HTTP mappe uniquement
les contrats. Le composant Angular ne connaît ni l'URL ni la persistance et passe
par la façade existante.

## 4. Schéma MongoDB

### 4.1 État canonique

Collection `park-fit-operational-statuses` :

```javascript
{
  _id: "park-public-id",
  state: "Active", // NotActivated | Active | Suspended
  revision: NumberLong(3),
  decisions: [
    {
      type: "Activated",
      actorUserId: "admin-user-id",
      reason: "Données et calendrier vérifiés",
      decidedAtUtc: ISODate("2026-09-14T18:00:00Z"),
      revision: NumberLong(1)
    },
    {
      type: "Suspended",
      actorUserId: "admin-user-id",
      reason: "Source officielle à revérifier",
      decidedAtUtc: ISODate("2026-09-14T18:30:00Z"),
      revision: NumberLong(2)
    },
    {
      type: "Restored",
      actorUserId: "admin-user-id",
      reason: "Source officielle renouvelée",
      decidedAtUtc: ISODate("2026-09-14T19:00:00Z"),
      revision: NumberLong(3)
    }
  ],
  createdAt: ISODate("2026-09-14T18:00:00Z"),
  updatedAt: ISODate("2026-09-14T19:00:00Z")
}
```

L'index `{ state: 1, updatedAt: -1 }` sert au pilotage. `_id` est l'identifiant du
parc et garantit un état unique. Les raisons et acteurs sont réservés à
l'administration ; la recherche publique ne les renvoie pas.

### 4.2 Marqueur de migration

Collection `park-fit-portfolio-migrations` :

```javascript
{
  _id: "fit-15-portfolio-activation-v1",
  candidateParkIds: ["park-1", "park-2"],
  startedAtUtc: ISODate("2026-09-14T19:55:00Z"),
  completedAtUtc: ISODate("2026-09-14T20:00:00Z")
}
```

La liste exacte des identifiants éligibles est matérialisée puis écrite atomiquement
avant toute activation. Elle fige la cohorte historique même si plusieurs instances
démarrent, si une instance redémarre ou si un parc est modifié pendant l'activation.
Le marqueur ne remplace pas l'état canonique et ne participe jamais à une recherche.

## 5. Migration sans second système

Avant FIT-15, l'absence de document signifiait implicitement « actif ». Une simple
modification de lecture aurait donc retiré tous les parcs historiques. Au premier
démarrage de FIT-15, la migration :

1. sélectionne les identifiants des parcs visibles, `Operating` et dotés de
   coordonnées valides, soit l'ancien portefeuille implicite ;
2. crée ou relit cette liste de cohorte unique et persistée ;
3. crée pour chacun un document `Active`, révision `0`, seulement s'il n'existe pas ;
4. ne modifie aucun état `Active` ou `Suspended` déjà piloté ;
5. date l'achèvement uniquement après les lots idempotents ;
6. tolère les collisions d'identifiant si deux instances démarrent ensemble.

Après le marqueur, une absence signifie définitivement `NotActivated`. Il n'existe
donc pas d'adaptateur de compatibilité ni deux interprétations concurrentes. Les
collections et la migration sont exécutées par l'initialiseur de production :
aucune commande ou mise à jour MongoDB manuelle n'est requise.

```mermaid
sequenceDiagram
    participant D as Déploiement
    participant I as MongoDatabaseInitializer
    participant P as parks
    participant S as park-fit-operational-statuses
    participant M as park-fit-portfolio-migrations

    D->>I: démarrer la version FIT-15
    I->>M: lire un éventuel plan persistant
    alt migration non terminée
      opt premier démarrage
        I->>P: matérialiser les identifiants visibles Operating
        I->>M: figer atomiquement la liste exacte
      end
      loop chaque lot
        I->>S: upsert les identifiants figés Active / révision 0
      end
      I->>M: dater l'achèvement
    else migration déjà terminée
      M-->>I: aucune écriture
    end
    I-->>D: application prête
```

## 6. Recherche publique

La recherche charge d'abord les états du portefeuille en un appel groupé. Elle ne
charge attractions et calendriers que pour les identifiants explicitement
`Active`, ce qui réduit les lectures quand le portefeuille est volontairement
restreint.

```mermaid
sequenceDiagram
    actor V as Visiteur
    participant Q as SearchParksByFitQueryHandler
    participant P as Parcs publics
    participant O as États opérationnels
    participant F as Faits FIT
    participant C as Core

    V->>Q: recherche anonyme
    Q->>P: au plus 200 candidats publics
    Q->>O: états par identifiants
    Q->>Q: garder uniquement Active
    Q->>F: attractions + synthèses pour les seuls actifs
    Q->>C: audit puis score explicable
    C-->>Q: résultats éligibles
    Q-->>V: résultats + compteurs agrégés
```

La réponse distingue les candidats suspendus, non activés et rejetés par la qualité.
Elle ne révèle ni justification, ni acteur, ni historique administratif.

## 7. Interface admin et responsive

Dans l'audit « Qualité du comparateur », chaque carte présente l'état et sa
conséquence avant les actions :

- actif : suspendre temporairement ou retirer du portefeuille ;
- suspendu : rétablir si la qualité le permet, ou retirer directement ;
- non activé : activer uniquement lorsque l'audit est prêt.

La justification s'ouvre seulement après le choix de l'action et peut être annulée.
Les actions utilisent une grille fluide, puis une colonne unique sous `30rem`.
Tous les descendants critiques ont `min-width: 0`, `max-width: 100%` et les textes
longs se coupent ; aucun défilement horizontal de page n'est créé.

Les libellés existent en français, anglais, allemand, néerlandais, italien,
espagnol, polonais et portugais.

## 8. Preuves automatisées

- Core : transitions, retrait pendant une suspension, versions et historique
  tronqué ;
- Application : activation éligible, refus d'une qualité insuffisante, exclusion
  d'un état absent, compteurs séparés et lectures groupées ;
- Infrastructure : filtre exact de l'ancien portefeuille, document inséré et noms
  des collections ;
- WebAPI : mapping additif du compteur `notActivatedCandidateCount` et validation
  de l'enum central ;
- Angular : activation verrouillée, contrats responsive, façade/port et huit
  dictionnaires cohérents.

FIT-15 termine l'extension contrôlée du portefeuille. La gate finale FIT-G vérifie
ensuite l'ensemble des invariants déjà automatisables ; les observations terrain
restent un outil d'amélioration et ne bloquent pas l'achèvement technique demandé.
