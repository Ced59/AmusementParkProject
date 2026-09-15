# WATCH-04 — Catalogue des faits surveillables et provenance

## Résultat métier

Ce jalon définit ce que le produit a le droit d'appeler un changement factuel. Un
favori ou un abonnement ne déclenche donc pas une alerte à partir d'un texte libre :
il attend un fait typé, sourcé, daté et validé.

Les 25 événements prévus par la roadmap sont inscrits dans un catalogue versionné.
Le catalogue précise si chaque événement concerne un parc, un élément de parc ou
les deux. Cette même règle alimente les abonnements et le contrôle des faits afin
d'éviter deux interprétations concurrentes.

Un fait conserve :

- sa cible et, pour un élément, son parc parent ;
- une valeur structurée avant et après le changement ;
- la publication qui sert de preuve et son type ;
- un niveau de confiance ;
- les dates d'occurrence, de vérification et de publication ;
- une clé de déduplication et une révision ;
- son état, sa version optimiste et la référence d'une éventuelle correction.

## Frontières d'architecture

Tout le comportement est placé dans `AmusementPark.Core` : il s'agit d'invariants
métier purs, sans MongoDB, contrôleur, transport HTTP, traduction ni envoi d'e-mail.
Les couches Application et Infrastructure pourront persister et orchestrer ces
objets lors des prochains jalons sans réécrire leurs règles.

Chaque type possède son propre fichier. Les valeurs exposées par les définitions et
les préférences sont gelées : un appelant ne peut pas modifier le catalogue ou un
ensemble après création.

## Modèle de classes

```mermaid
classDiagram
    class FactualEventCatalog {
        +CurrentSchemaVersion int
        +All IReadOnlyDictionary
        +Get(type) FactualEventDefinition
    }
    class FactualEventDefinition {
        +Type FactualEventType
        +Code string
        +SchemaVersion int
        +SupportedTargetTypes IReadOnlySet
        +Supports(targetType) bool
    }
    class FactualChangeEvent {
        +Id FactualChangeEventId
        +Type FactualEventType
        +DefinitionVersion int
        +Target ChangeTarget
        +PreviousValue FactValue
        +NewValue FactValue
        +Source SourceReference
        +Confidence DataConfidence
        +DeduplicationKey string
        +Revision int
        +Status FactualChangeStatus
        +Version long
        +CanBeDistributed bool
        +Verify(atUtc)
        +Publish(atUtc)
        +Correct(successorId, atUtc)
        +Retract(reasonCode, atUtc)
        +Expire(atUtc)
    }
    class ChangeTarget {
        +Type FactualTargetType
        +TargetId string
        +ParentParkId string
    }
    class FactValue {
        +Kind FactValueKind
        +CanonicalValue string
        +UnitCode string
    }
    class SourceReference {
        +Type SourceReferenceType
        +PublisherName string
        +Title string
        +Url string
        +PublishedAtUtc DateTime
    }
    class WatchSubscription

    FactualEventCatalog "1" *-- "25" FactualEventDefinition
    FactualChangeEvent --> FactualEventDefinition : validé par
    FactualChangeEvent *-- ChangeTarget
    FactualChangeEvent *-- FactValue
    FactualChangeEvent *-- SourceReference
    WatchSubscription --> FactualEventCatalog : compatibilité cible/événement
```

## Cycle de vie

```mermaid
stateDiagram-v2
    [*] --> Draft
    Draft --> Verified : preuve contrôlée\nconfiance moyenne ou haute
    Verified --> Published : publication explicite
    Draft --> Expired : devenu inutile
    Verified --> Expired : devenu inutile
    Published --> Expired : devenu inutile
    Published --> Corrected : remplacé par une nouvelle révision
    Published --> Retracted : preuve retirée ou fait invalidé
    Corrected --> [*]
    Retracted --> [*]
    Expired --> [*]
```

Seul `Published` est distribuable. `Draft` et `Verified` ne peuvent jamais alimenter
une alerte. Une correction pointe vers le nouvel événement au lieu de modifier
silencieusement l'ancien ; une rétractation exige un code de raison stable.

## Séquence de validation d'un fait

```mermaid
sequenceDiagram
    participant Producer as Producteur de changement
    participant Catalog as Catalogue versionné
    participant Event as FactualChangeEvent
    participant Reviewer as Validation métier
    participant Delivery as Distribution future

    Producer->>Catalog: Get(type)
    Catalog-->>Producer: version et cibles admises
    Producer->>Event: CreateDraft(cible, avant, après, source, confiance)
    Event-->>Producer: fait non distribuable
    Reviewer->>Event: Verify(date UTC)
    Event-->>Reviewer: état Verified
    Reviewer->>Event: Publish(date UTC)
    Event-->>Delivery: CanBeDistributed = true
```

## Invariants couverts par les tests

- chaque valeur d'énumération possède exactement une définition et un code unique ;
- les événements de parc et d'élément ne peuvent pas changer de périmètre ;
- une valeur est canonique, culture-indépendante et cohérente avec son type ;
- une source utilise une URL HTTP(S) absolue et une date UTC ;
- une absence de changement, une chronologie impossible ou une révision invalide est rejetée ;
- une preuve à faible confiance ne peut pas être vérifiée ;
- les transitions rejouées à l'identique restent idempotentes ;
- une correction référence son successeur et une rétractation conserve sa raison ;
- seul un fait publié reste distribuable.

## Persistance et migration

Ce jalon ne crée encore aucune collection MongoDB ni route publique. Il ne nécessite
donc aucune migration de données en production. `WATCH-05` ajoutera la persistance,
le calcul de différences et l'outbox durable avec déduplication à partir de ce modèle
unique ; aucun second système transitoire n'est prévu.
