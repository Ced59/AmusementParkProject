# WATCH-05 — Diff factuel, outbox durable et déduplication

## Résultat métier

Une modification validée peut désormais devenir un fait suivi sans dépendre de la
disponibilité immédiate du worker. Le système compare des valeurs structurées,
ignore les sauvegardes sans changement réel et conserve l'intention et la révision
monotone dans le même document que la source avant de programmer le traitement. Une
même clé métier et une même révision ne peuvent créer
qu'un seul événement logique.

Le premier producteur réellement raccordé est le calendrier d'ouverture d'un parc.
L'édition administrative et l'import `PARK_DATA_EDITOR` utilisent la même capture.
Après la sauvegarde, même si la requête cliente est annulée, et uniquement lorsqu'une
URL officielle, une date de vérification et au moins une règle sont présentes, une
empreinte métier stable compare les horaires précédents aux nouveaux. Les identifiants
de règles, les dates techniques, les notes internes et les simples retouches
éditoriales ne provoquent pas de faux changement. Une date de vérification future ou
non UTC est refusée avant la persistance, aussi bien par l'édition administrative que
par l'import.

Ce jalon ne notifie encore personne. Il prépare des brouillons factuels fiables pour
la vérification administrative de `WATCH-06`, puis pour le centre Web de `WATCH-07`.

## Architecture retenue

```mermaid
flowchart LR
    Mutation[Mutation métier validée et commitée]
    Calendar[Calendrier officiel d'ouverture]
    Capture[FactualChangeCaptureService]
    Diff[FactualChangeDiff]
    SourceMarker[(marqueur durable dans le calendrier)]
    Outbox[(factual-change-outbox)]
    Jobs[(durableBackgroundJobs)]
    Worker[Worker durable FOUNDATION]
    Events[(factual-change-events)]
    Reconciler[Reconciler borné]

    Calendar --> Mutation
    Mutation -->|avant, après, source, révision| Capture
    Capture --> Diff
    Diff -->|aucun changement| Stop[Aucun événement]
    Diff -->|changement réel, écriture atomique| SourceMarker
    SourceMarker -->|copie idempotente| Outbox
    Outbox -->|après persistance| Jobs
    Jobs --> Worker
    Worker -->|brouillon unique| Events
    Worker -->|acquittement optimiste| Outbox
    Reconciler -->|entrées non acquittées| Outbox
    Reconciler -->|job exact manquant| Jobs
```

Les responsabilités restent séparées :

- le Core calcule le diff canonique et conserve les invariants du fait ;
- l'Application orchestre la capture après commit, la planification et la
  matérialisation ;
- l'Infrastructure persiste les deux collections Mongo, initialise leurs index et
  exécute le reconciler ;
- la WebAPI et le frontend ne connaissent pas encore ces primitives internes.

## Modèle MongoDB

```mermaid
erDiagram
    FACTUAL_CHANGE_OUTBOX {
        string id PK
        string eventId
        string deduplicationKey
        long sourceRevision
        string type
        int definitionVersion
        object target
        object previousValue
        object newValue
        object source
        string confidence
        datetime occurredAtUtc
        datetime createdAt
        datetime materializedAtUtc
        datetime terminalAtUtc
        string terminalErrorCode
        long version
    }

    PARK_OPENING_HOURS {
        string parkId PK
        long factualRevision
        array pendingFactualChanges
    }

    FACTUAL_CHANGE_EVENT {
        string id PK
        string deduplicationKey
        long revision
        string type
        int definitionVersion
        object target
        object previousValue
        object newValue
        object source
        string confidence
        string status
        datetime createdAt
        datetime updatedAt
        long version
    }

    DURABLE_BACKGROUND_JOB {
        string id PK
        string kind
        string idempotencyKey
        object payload
        string status
        int attemptCount
    }

    PARK_OPENING_HOURS ||--o{ FACTUAL_CHANGE_OUTBOX : reprend
    FACTUAL_CHANGE_OUTBOX ||--o| DURABLE_BACKGROUND_JOB : programme
    FACTUAL_CHANGE_OUTBOX ||--o| FACTUAL_CHANGE_EVENT : matérialise
```

Index critiques :

- unicité outbox `(deduplicationKey, sourceRevision)` ;
- révision monotone et intentions en attente embarquées dans le document calendrier ;
- index partiel `(updatedAt, parkId)` limité aux calendriers qui portent au moins
  une intention en attente ;
- lecture bornée des entrées avec `materializedAtUtc` et `terminalAtUtc` nuls grâce
  à un index composé commençant par ces champs ;
- unicité événement `(deduplicationKey, revision)` ;
- lecture des événements par état et par cible ;
- unicité du job exact grâce à une clé SHA-256 stable et bornée.

L'initialiseur crée les nouvelles collections et leurs index au déploiement. Il
n'existe aucune donnée historique équivalente à migrer et aucun ancien mécanisme ne
coexiste avec celui-ci.

## Séquence nominale et reprise

```mermaid
sequenceDiagram
    participant Producer as Producteur métier
    participant Capture as Capture applicative
    participant Source as Calendrier et marqueur Mongo
    participant Outbox as Outbox Mongo
    participant Queue as Worker durable
    participant Handler as Materialisation
    participant Event as Événements Mongo
    participant Repair as Reconciler

    Producer->>Capture: CaptureAfterCommit(avant, après, source, révision)
    Capture->>Capture: diff canonique
    alt aucune différence
        Capture-->>Producer: NoChange
    else différence réelle
        Producer->>Source: calendrier + intention + révision monotone (une écriture)
        Source-->>Capture: intention persistée
        Capture->>Outbox: insertion unique clé + révision
        Outbox-->>Capture: Created ou AlreadyRecorded
        Capture->>Queue: enqueue exact
        Queue-->>Capture: job durable
        Capture-->>Producer: Scheduled
        Queue->>Handler: traitement au moins une fois
        Handler->>Outbox: lire la révision source
        Handler->>Event: créer le brouillon unique
        Handler->>Outbox: marquer matérialisé avec version optimiste
    end

    opt panne après l'insertion outbox
        Repair->>Outbox: lire la prochaine page de 100 entrées en attente
        Repair->>Queue: recréer le job exact manquant
    end

    opt panne avant l'insertion outbox
        Repair->>Source: lire les intentions factuelles en attente
        Repair->>Outbox: recopier l'intention avec le même identifiant
        Repair->>Source: retirer le marqueur après persistance
    end

    opt job exact définitivement terminé sans acquittement
        Repair->>Outbox: acquitter l'échec terminal et son code
    end
```

Le calendrier et son intention factuelle sont remplacés atomiquement dans un seul
document, conformément à la contrainte MongoDB autonome. Une panne avant la copie
vers la collection d'outbox laisse donc une source de reprise durable. Plusieurs
intentions peuvent rester embarquées et leur révision est strictement croissante :
une nouvelle édition ne remplace pas une intention précédente non acquittée.
Chaque remplacement utilise en plus une révision d'écriture comparée atomiquement.
En cas de concurrence, le dépôt relit le calendrier, recalcule le véritable diff
avant/après, attribue la révision factuelle suivante et retente au plus cinq fois.

Une panne de planification n'annule donc pas le fait déjà enregistré. Une panne
entre la création de l'événement et l'acquittement est rejouée : l'index unique
retrouve le même événement, puis l'acquittement reprend. Un contenu différent sous
la même clé et la même révision est un conflit explicite et part en dead-letter ; il
n'est jamais écrasé silencieusement.

Un job exact déjà en dead-letter, annulé, supplanté ou terminé sans acquittement ne
reste pas éternellement dans le scan. L'outbox reçoit atomiquement une date et un
code terminaux. Cette anomalie reste donc visible pour l'exploitation et ne bloque
pas les événements suivants.

## Performance et exploitation

- le scan de réparation est limité à 100 entrées par minute ;
- le scan des sources limite réellement chaque lot à 100 marqueurs, même lorsqu'un
  seul calendrier en contient davantage ; son curseur
  `(updatedAt, parkId, recordedAtUtc, entryId)` reprend à l'intérieur du même document,
  puis reboucle en fin de liste : ni un parc très actif ni des marqueurs conflictuels
  anciens ne peuvent affamer les faits suivants ;
- le dépliage et la limite sont exécutés dans MongoDB : le réseau et la mémoire du
  processus ne reçoivent jamais tout le tableau d'un calendrier très actif ;
- un curseur `(createdAt, id)` avance entre les pages pleines puis reboucle en fin
  de liste : des jobs terminaux anciens ne peuvent donc pas affamer les faits plus
  récents ;
- les lectures utilisent le préfixe d'un index composé et un prédicat Mongo `null`
  compatible avec les documents anciens où le champ est absent ;
- le traitement est léger et limité à deux exécutions concurrentes ;
- les tentatives sont bornées à cinq avec backoff jusqu'à dix minutes ;
- les conflits irréparables utilisent la dead-letter administrative déjà fournie
  par FOUNDATION ;
- aucun broker externe, polling agressif ou charge frontend n'est ajouté.

## Preuves automatisées

Les tests couvrent :

- l'équivalence canonique et les créations/suppressions de valeur ;
- l'absence totale d'écriture quand le diff est vide ;
- la conservation de l'outbox quand la mise en file échoue ;
- la conservation d'un marqueur dans le calendrier quand l'insertion outbox échoue ;
- le compare-and-swap qui préserve les marqueurs et réattribue une révision sous
  écritures concurrentes ;
- l'avancement du curseur source malgré un marqueur conflictuel ;
- la reprise à l'intérieur d'un même calendrier lorsque son nombre de marqueurs
  dépasse la taille maximale d'un lot ;
- le pipeline Mongo qui déplie les marqueurs avant la limite et l'index partiel qui
  évite de parcourir les calendriers sans reprise ;
- la clé de job déterministe et bornée même avec une clé métier maximale ;
- la poursuite du reconciler lorsqu'une entrée échoue ;
- l'acquittement terminal d'un job exact définitivement échoué ;
- la matérialisation d'un seul brouillon et le rejeu sans doublon ;
- la dead-letter en cas de collision sémantique ;
- le round-trip Mongo des valeurs, sources et versions ;
- la présence des index d'unicité et de reprise ;
- l'empreinte stable d'un calendrier malgré l'ordre des données ;
- le refus d'un faux événement de publication pour un calendrier initial vide et
  la capture explicite de la suppression d'un calendrier existant ;
- l'inclusion de la priorité des règles dans l'empreinte canonique ;
- la conservation de l'ordre déterminant entre deux règles de même priorité et de
  même date de début ;
- le rejet applicatif d'une date de vérification future ;
- le raccordement post-commit du calendrier officiel avec source et confiance,
  depuis l'édition comme depuis l'import, même après annulation de la requête.
