# Roadmap 00A — Livraison, migrations et validation des fondations techniques

> Code programme : `FOUNDATION-DELIVERY`
>
> Dépendance : [`00-technical-foundations-and-architecture-decisions-roadmap.md`](00-technical-foundations-and-architecture-decisions-roadmap.md).
>
> Périmètre : ordre d’implémentation, PR, migrations, tests, observabilité, rollbacks et exploitation des décisions `FOUNDATION-ADR-01` à `FOUNDATION-ADR-10`.
>
> Principe : chaque tranche doit produire une capacité utilisable ou une garantie vérifiable. Aucun framework générique ne doit être construit sans consommateur immédiat.

## 1. Objectif

Cette roadmap convertit les décisions d’architecture en une séquence livrable. Elle répond aux questions suivantes :

- que faut-il implémenter avant les seuils de classement ?
- que faut-il implémenter avant la première visite persistée ?
- quelles migrations sont réellement nécessaires ?
- comment éviter une infrastructure générique disproportionnée ?
- quels tests doivent bloquer le merge ?
- quelles métriques doivent être observées sur le VPS ?
- comment revenir en arrière sans perdre les données utilisateur ?

La séquence évite deux écueils opposés :

1. coder le Passeport directement avec des conventions improvisées ;
2. construire plusieurs mois d’infrastructure sans aucune fonction produit utilisable.

# 2. Carte des dépendances

```text
FOUNDATION-01  Inventaire et ADR effectifs
    ├── FOUNDATION-02  RatingValue
    │       └── RANK-02+ et PASS-09+
    ├── FOUNDATION-03  IDs typés compatibles
    │       └── PASS, SHARE, WATCH, TRIP
    ├── FOUNDATION-04  Temps local / VisitDate
    │       └── PASS-02+
    ├── FOUNDATION-05  Jobs durables minimaux
    │       ├── RANK snapshots
    │       ├── PASS exports/recalculs
    │       └── WATCH digests
    ├── FOUNDATION-06  Registre des scopes
    │       └── RANK snapshots
    └── FOUNDATION-07  Conventions Mongo assessments/ordre
            └── PASS occurrences et notes temporelles

RANK evidence peut commencer après FOUNDATION-01/02.
PASS persistence attend FOUNDATION-03/04/07.
Les jobs critiques attendent FOUNDATION-05.
```

# 3. Phase `FOUNDATION-0` — Baseline et décisions exécutables

## 3.1 Objectifs

- confirmer les représentations et comportements actuels ;
- mesurer avant d’ajouter des projections ;
- figer les ADR sous forme testable ;
- identifier les anomalies de données historiques ;
- éviter de découvrir les contraintes Mongo en production.

## 3.2 Inventaire des identifiants

Produire un rapport interne :

- types d’identifiants utilisés dans Core, Application, Infrastructure, WebAPI et Angular ;
- formats réellement présents dans Mongo ;
- longueur minimale/maximale ;
- présence d’identifiants non UUID ;
- comparaison sensible ou insensible à la casse ;
- routes et DTO exposant des IDs ;
- indexes contenant des IDs ;
- scripts d’import/export supposant un format.

### Validation

- aucune proposition de migration `Guid` sans anomalie démontrée ;
- la limite de longueur retenue couvre toutes les valeurs existantes ;
- les wrappers typés acceptent les références historiques valides ;
- les identifiants restent opaques pour Angular.

## 3.3 Inventaire des notes

Requête/diagnostic sur `user-ratings` :

- nombre total ;
- valeurs distinctes ;
- valeurs hors `[0.5, 5]` ;
- valeurs ne correspondant pas à un demi-point ;
- doublons selon `(UserId, TargetType, TargetId)` ;
- documents sans utilisateur ou cible ;
- divergences entre agrégats et notes sources ;
- distribution par cible ;
- valeurs flottantes proches d’un demi-point sans être exactes.

### Politique d’anomalie

- aucune correction silencieuse ;
- rapport avec identifiants minimisés ;
- décision explicite par classe d’anomalie ;
- backup avant correction ;
- script idempotent ;
- recalcul des agrégats après correction ;
- audit de la migration.

## 3.4 Baseline MongoDB

Confirmer :

- standalone ou replica set ;
- version Mongo ;
- taille des collections ;
- indexes ;
- latence des opérations `FindOneAndUpdate` ;
- comportement de l’initializer d’indexes ;
- capacité des bulk writes ;
- stratégie de backup/restauration ;
- quotas disque ;
- mémoire et CPU disponibles.

La roadmap continue de supporter Mongo standalone tant qu’une migration vers replica set n’est pas décidée séparément.

## 3.5 Baseline classements

Mesurer par scope candidat :

- nombre de cibles ;
- nombre éligible selon seuils proposés ;
- taille du résultat ;
- temps de lecture des agrégats ;
- temps de tri ;
- CPU ;
- taille estimée d’un snapshot ;
- fréquence actuelle de mutation ;
- nombre de scopes réellement consultés.

## 3.6 Baseline Passeport simulée

Fixtures volumétriques :

- 10 visites, 20 rides chacune ;
- 100 visites, 100 rides chacune ;
- une visite de 500 occurrences ;
- plusieurs visites le même jour ;
- dates partielles ;
- notes sur chaque occurrence ;
- attractions fermées et renommées.

Objectif : valider les indexes et requêtes avant données réelles.

# 4. Phase `FOUNDATION-1` — Primitives de domaine

## 4.1 `FOUNDATION-01` — ADR et tests de compatibilité des identifiants

### Contenu

- ajouter `IdentifierRules` dans Core ;
- ajouter `VisitId` et `RideOccurrenceId` ;
- conserver documents et DTO en `string` ;
- ajouter mappers explicites ;
- documenter la non-migration des entités existantes ;
- ajouter tests de sérialisation.

### Critères de sortie

- aucune modification de contrat public ;
- aucune migration Mongo ;
- nouveaux IDs générés en chaîne opaque ;
- un identifiant historique non UUID valide passe le mapping ;
- valeurs vides rejetées dans Core ;
- aucune normalisation de casse destructive.

### Rollback

Supprimer les wrappers du nouveau code avant persistance. Aucun document n’a changé.

## 4.2 `FOUNDATION-02` — `RatingValue`

### Contenu

- implémenter `RatingValue` ;
- tests exhaustifs des dix valeurs ;
- adapter `RatingScoreCalculator` avec surcharges compatibles ;
- convertir les handlers de nouvelles fonctions ;
- ajouter mapper depuis `double` historique ;
- diagnostic des valeurs invalides.

### Exemples d’API interne

```csharp
public static RatingValue ParseUserInput(decimal value);
public static long SumHalfSteps(IEnumerable<RatingValue> values);
public static double CalculateAverageFromHalfSteps(long sumHalfSteps, long count);
public static double CalculateBayesianScoreFromHalfSteps(long sumHalfSteps, long count);
```

### Compatibilité mathématique

Pour une somme historique `ratingSum` :

```text
sumHalfSteps = round(ratingSum × 2)
```

Cette conversion n’est autorisée qu’après validation que chaque source appartient à l’échelle. Elle n’est pas utilisée pour réparer une valeur invalide.

### Critères de sortie

- résultats historiques identiques sur fixtures valides ;
- aucune comparaison de note par epsilon ;
- aucune note temporelle en `double` ou `decimal` dans le Core ;
- `RatingValue?` représente l’absence ;
- OpenAPI conserve un nombre lisible.

## 4.3 `FOUNDATION-03` — Option de backfill `ValueHalfSteps`

Cette PR n’est exécutée que si la mesure le justifie.

### Étapes

1. ajouter `ValueHalfSteps` nullable au document ;
2. lecture préfère le nouveau champ ;
3. écriture duale ;
4. job de backfill borné ;
5. rapport d’anomalies ;
6. validation indépendante ;
7. recalcul agrégats si nécessaire ;
8. activer lecture stricte ;
9. conserver fallback pendant une version ;
10. suppression ultérieure du champ `Value` dans une PR distincte facultative.

### Kill switch

`ratings:valueHalfSteps:readEnabled`.

### Rollback

Désactiver la lecture nouvelle ; conserver le champ additionnel sans impact.

# 5. Phase `FOUNDATION-2` — Temps local et modèle de visite

## 5.1 `FOUNDATION-04` — `VisitDate` et conventions temporelles

### Contenu

- `VisitDatePrecision` ;
- `VisitDate` ;
- `LocalServiceDayConvention` ;
- validation année/mois/jour ;
- règles de date future ;
- formatage localisé ;
- comparaison et tri documentés ;
- aucun `DateTime` synthétique comme source de vérité.

### Tri des dates partielles

Le tri doit être stable mais ne doit pas simuler une précision. Proposition :

```text
Year DESC,
Month connu avant inconnu selon vue,
Day connu avant inconnu selon vue,
CreatedAtUtc DESC,
Id ASC
```

Dans les graphiques, une date partielle est placée dans une catégorie distincte ou une période, pas sur un jour inventé.

### Critères de sortie

- années bissextiles ;
- mois invalides ;
- jour invalide ;
- précision incohérente ;
- dates avant 1900 si le domaine les autorise ;
- année future ;
- affichage dans huit langues ;
- export conservant la précision.

## 5.2 `FOUNDATION-05` — Fuseaux et heures locales

### Contenu

- service de validation IANA ;
- résolution du fuseau du parc ;
- stockage sur `Visit` ;
- `OccurrenceMoment` ;
- gestion DST ;
- interdiction d’heure sans jour/fuseau ;
- tests Europe/Paris, America/New_York et zones sans DST.

### Cas DST

- heure inexistante au passage à l’heure d’été : validation demande correction ;
- heure ambiguë au passage à l’heure d’hiver : conserver heure locale et offset choisi si une conversion UTC est nécessaire ;
- ancienne visite sans offset : ne pas inventer.

### Critères de sortie

- aucun décalage de date lors d’un export ;
- l’ordre ne dépend pas de l’heure ;
- le changement de fuseau est audité ;
- le fuseau proposé depuis le parc reste corrigeable.

# 6. Phase `FOUNDATION-3` — Persistance Mongo du Passeport

## 6.1 `FOUNDATION-06` — Conventions de documents

### `VisitDocument`

Champs minimaux :

```text
_id / id
schemaVersion
userId
parkId
date.year
date.month?
date.day?
date.precision
date.isApproximate
timeZoneId?
serviceDayConvention
status
title?
privateNote?
parkAssessment?
version
statisticsRevision
createdAtUtc
updatedAtUtc
completedAtUtc?
deletedAtUtc?
```

### `RideOccurrenceDocument`

```text
_id / id
schemaVersion
visitId
userId
parkId
parkItemId
sortPosition
moment.localTime?
moment.isApproximate
status
source
privateNote?
assessment?
version
createdAtUtc
updatedAtUtc
deletedAtUtc?
```

### Règles

- `schemaVersion` explicite ;
- noms BSON stables ;
- champs privés absents plutôt que chaînes vides lorsque pertinent ;
- aucune liste d’occurrences embarquée dans la visite ;
- assessment actif embarqué ;
- audit séparé ;
- tombstone seulement si nécessaire au workflow de suppression/reprise.

## 6.2 Indexes `Visit`

Indexes initiaux :

```text
(UserId, Date.Year DESC, Date.Month DESC, Date.Day DESC)
(UserId, ParkId, Date.Year DESC)
(UserId, Status, UpdatedAtUtc DESC)
(UserId, DeletedAtUtc)
```

Pas d’unicité parc/date.

Évaluer un index partiel sur les documents non supprimés. Chaque index est justifié par une query documentée.

## 6.3 Indexes `RideOccurrence`

```text
(VisitId, SortPosition ASC, Id ASC)
(UserId, ParkItemId, VisitId)
(UserId, ParkId, VisitId)
(VisitId, Status)
(UserId, DeletedAtUtc)
```

Évaluer :

```text
(UserId, ParkItemId, Assessment.UpdatedAtUtc DESC)
```

seulement pour la timeline notée.

## 6.4 Validation des indexes

Pour chaque query :

- `explain` sur fixture ;
- documents examinés ;
- index utilisé ;
- absence de scan global ;
- taille index ;
- impact écriture ;
- test d’initializer idempotent.

## 6.5 `FOUNDATION-07` — Assessments embarqués

### Commandes

- `UpsertVisitParkAssessmentCommand` met à jour `VisitDocument.ParkAssessment` ;
- `DeleteVisitParkAssessmentCommand` unset le sous-document ;
- `UpsertRideAssessmentCommand` met à jour `RideOccurrenceDocument.Assessment` ;
- `DeleteRideAssessmentCommand` unset le sous-document.

### Filtre atomique

Exemple :

```text
Id = visitId
AND UserId = currentUserId
AND Version = expectedVersion
AND DeletedAtUtc absent
```

Update :

```text
set assessment
inc version
inc statisticsRevision
set updatedAtUtc
```

### Audit

L’événement d’audit transporte :

- parent ;
- révision ;
- ancienne/nouvelle valeur en demi-points ;
- indicateur de commentaire modifié ;
- origine ;
- corrélation.

Il ne transporte pas nécessairement le texte privé.

### Critères de sortie

- aucun assessment orphelin possible ;
- conflit de version visible ;
- suppression atomique ;
- statistiques marquées obsolètes ;
- note communautaire inchangée ;
- export inclut l’assessment actif.

## 6.6 `FOUNDATION-08` — Ordonnancement des rides

### API métier

- `AppendRideOccurrence` ;
- `PrependRideOccurrence` facultatif ;
- `InsertRideOccurrenceBefore` ;
- `MoveRideOccurrenceBefore` ;
- `MoveRideOccurrenceAfter` ;
- `NormalizeRideOccurrencePositions` interne.

### Algorithme

Pas = 1024.

- append sans lecture globale si la visite conserve `LastSortPosition` atomique ;
- sinon lecture max indexée ;
- insertion par moyenne entière ;
- normalisation lorsque gap <= 1 ;
- bulk write limité à la visite ;
- version optimiste ;
- tri secondaire par ID.

### Optimisation facultative `LastSortPosition`

`Visit` peut conserver `LastRideSortPosition` si cela évite une lecture max à chaque append. Mise à jour atomique nécessaire ; si l’invariant devient complexe sur Mongo standalone, préférer la lecture indexée initiale.

### Tests

- append 1000 rides ;
- insertions répétées au même endroit ;
- normalisation ;
- conflit ;
- double clic ;
- suppression ;
- réinsertion ;
- valeurs proches des bornes `long` ;
- ordre déterministe après migration.

# 7. Phase `FOUNDATION-4` — Jobs durables

## 7.1 `FOUNDATION-09` — Collection et repository de jobs

### Indexes

- unique `(Kind, NaturalKey)` pour jobs coalescibles actifs, via filtre partiel adapté ;
- unique `(Kind, IdempotencyKey)` pour jobs exacts ;
- `(Status, NotBeforeUtc, Priority, CreatedAtUtc)` ;
- `(LeaseExpiresAtUtc)` ;
- TTL uniquement pour succès anciens après politique validée.

### Repository

Capacités :

- enqueue exact ;
- upsert/coalesce par révision ;
- lease next ;
- renew lease ;
- complete ;
- retry ;
- dead-letter ;
- cancel ;
- release expired ;
- list diagnostics.

Aucune méthode générique permettant de modifier arbitrairement le payload après exécution partielle.

## 7.2 `FOUNDATION-10` — Worker .NET borné

### Boucle

1. attendre cancellation ;
2. réclamer un job ;
3. si aucun job, délai progressif ;
4. résoudre handler par `Kind` ;
5. démarrer timeout ;
6. renouveler lease si nécessaire ;
7. exécuter idempotemment ;
8. marquer succès/retry/dead-letter ;
9. publier métriques ;
10. continuer.

### Concurrence

Configuration :

```text
HeavyWorkerCount = 1
LightWorkerCount = 1 ou 2
MaxExportConcurrency = 1
MaxRankingRebuildConcurrency = 1
MaxEmailConcurrency = valeur bornée
```

Les kinds sont classés lourd/léger. `LIVE` possède plus tard son propre budget.

### Arrêt propre

- ne plus réclamer de nouveau job ;
- signaler cancellation au handler ;
- laisser expirer le lease si interruption brutale ;
- ne pas marquer succès après cancellation non confirmée ;
- conserver la possibilité de reprise.

## 7.3 `FOUNDATION-11` — Réconciliateurs

### `RatingScopeReconciler`

Compare :

- révision source du scope ;
- révision du snapshot courant ;
- job actif ;
- âge du décalage.

Crée ou coalesce le job manquant.

### `PassportStatisticsReconciler`

Compare :

- `statisticsRevision` des sources ;
- révision du cache/snapshot privé éventuel ;
- job actif.

### `AuditReconciler`

Seulement si un marqueur d’audit attendu est conservé dans la source. Ne parcourt pas aveuglément tout l’historique à chaque cycle.

### Fréquence

- bornée ;
- pagination par curseur ;
- budget CPU ;
- pause si VPS sous pression ;
- métrique de retard.

## 7.4 Résilience

Tests :

- process tué après lease ;
- process tué après effet mais avant succès ;
- même job rejoué ;
- payload version inconnue ;
- job supersédé ;
- erreur permanente ;
- file volumineuse ;
- lease renouvelé ;
- horloge décalée dans une tolérance documentée.

# 8. Phase `FOUNDATION-5` — Publication des classements

## 8.1 `FOUNDATION-12` — Registre des scopes

Le registre doit être du code/config versionné, pas une collection modifiable librement en production lors de la première version.

Exemple :

```csharp
public static class RankingScopes
{
    public static RankingScopeDefinition GlobalParks { get; }
    public static IReadOnlyList<RankingScopeDefinition> PublicItemCategories { get; }
}
```

### Validation d’une clé

- parse strict ;
- correspondance à une définition connue ;
- aucune injection de filtre ;
- scope public/privé ;
- méthode supportée ;
- seuil minimum.

## 8.2 `FOUNDATION-13` — Snapshot header/chunks/pointer

### Collections

- `rating-ranking-snapshot-headers` ;
- `rating-ranking-snapshot-chunks` ;
- `rating-ranking-publication-pointers`.

### Indexes headers

```text
unique (ScopeKey, MethodologyVersion, SourceRevision)
(ScopeKey, Status, GeneratedAtUtc DESC)
```

### Indexes chunks

```text
unique (SnapshotId, ChunkIndex)
(SnapshotId, FirstRank, LastRank)
```

### Pointer

```text
unique (ScopeKey)
```

Le pointer contient version optimiste et précédent snapshot pour rollback.

## 8.3 Construction idempotente

Natural key du job :

```text
ratings.rebuild-scope:{scopeKey}
```

Payload :

```json
{
  "scopeKey": "parks:global",
  "requestedSourceRevision": 1234,
  "methodologyVersion": "ratings-2026-01"
}
```

Le handler :

- abandonne si un snapshot courant couvre déjà la révision ;
- peut abandonner un calcul si une révision très récente le rend inutile avant publication ;
- ne remplace jamais un pointer par un snapshot incomplet ;
- vérifie le nombre de chunks ;
- vérifie checksums ;
- nettoie les builds abandonnés par job séparé.

## 8.4 Pagination publique

Le lecteur :

1. résout le pointer ;
2. charge header ;
3. calcule chunks nécessaires ;
4. charge seulement ces chunks ;
5. mappe les noms/images depuis une projection légère ou des références déjà présentes ;
6. renvoie méthodologie et génération ;
7. applique cache public versionné par snapshot ID.

Aucun join N+1 par entrée.

## 8.5 Rollback

- repointer vers le snapshot précédent validé ;
- invalider cache ;
- ne pas recalculer dans la requête ;
- audit admin ;
- si aucun snapshot sain : afficher classement indisponible ou données sans rang.

# 9. Phase `FOUNDATION-6` — Validation intégrée aux roadmaps

## 9.1 Gate avant `RANK`

- `RatingValue` disponible ;
- inventaire des données terminé ;
- seuils testés ;
- scopes canoniques listés ;
- pas d’obligation de snapshot pour masquer les rangs faibles ;
- stratégie de job choisie avant reconstruction asynchrone.

## 9.2 Gate avant `PASS`

- IDs et mappers ;
- `VisitDate` ;
- fuseaux ;
- documents et indexes ;
- assessment embarqué ;
- `SortPosition` ;
- idempotence ;
- propriété/autorisation ;
- export minimal défini ;
- suppression définie.

## 9.3 Gate avant `SHARE`

- `SourceVersion` disponible ;
- invalidation directe lors d’une révocation ;
- job d’invalidation idempotent ;
- cache key avec publication version ;
- IDs opaques ;
- aucune donnée privée copiée automatiquement.

## 9.4 Gate avant `WATCH`

- worker durable stable ;
- outbox/révision source documentée ;
- déduplication ;
- templates versionnés ;
- limites d’e-mail ;
- centre Web fonctionnel avant digests.

# 10. Découpage détaillé en PR

| PR | Contenu | Dépendance | Critère de sortie |
|---|---|---|---|
| `FOUNDATION-01` | ADR effectifs + inventaire IDs/notes/Mongo | aucune | Rapport et décisions validés |
| `FOUNDATION-02` | `RatingValue` + calculateurs compatibles | 01 | Fixtures historiques identiques |
| `FOUNDATION-03` | IDs typés nouveaux agrégats | 01 | Contrats toujours en chaîne |
| `FOUNDATION-04` | `VisitDate` et jour de service | 01 | Dates partielles exactes |
| `FOUNDATION-05` | Fuseau/heure locale | 04 | DST et exports testés |
| `FOUNDATION-06` | Documents/indexes Visit/Ride | 03,04 | Initializer et explain validés |
| `FOUNDATION-07` | Assessments embarqués | 02,06 | Écriture atomique et audit |
| `FOUNDATION-08` | SortPosition et normalisation | 06 | Ordre stable sous concurrence |
| `FOUNDATION-09` | Repository jobs Mongo | 01 | Lease/idempotence testés |
| `FOUNDATION-10` | Worker borné + handlers | 09 | Reprise après crash |
| `FOUNDATION-11` | Réconciliateurs | 10 | Jobs manquants réparés |
| `FOUNDATION-12` | Registre scopes canoniques | RANK evidence | Aucun filtre arbitraire persisté |
| `FOUNDATION-13` | Snapshots bornés | 10,12 | Publication atomique |
| `FOUNDATION-14` | Dashboards/runbooks | 10,13 | Exploitation possible |
| `FOUNDATION-15` | Clôture docs/flags temporaires | gates | Dette temporaire inventoriée |

Les PR peuvent être regroupées lorsque le diff reste lisible. Elles ne doivent pas mélanger un grand refactoring existant avec ces fondations.

# 11. Stratégie de migration détaillée

## 11.1 Aucun backfill d’identifiants

- nouveaux documents : nouveaux IDs chaîne ;
- anciennes entités : inchangées ;
- wrappers : mapping runtime ;
- exports : chaînes ;
- aucun changement de route.

## 11.2 Notes globales

- diagnostic ;
- value object ;
- lecture compatible ;
- éventuel champ additionnel ;
- backfill par batch ;
- rapport ;
- aucune note inventée.

## 11.3 Passeport

Nouvelle persistance, donc pas de backfill automatique depuis `UserRating`.

- collection créée ;
- indexes ;
- feature flag ;
- bêta fermée ;
- export/suppression ;
- aucune visite synthétique.

## 11.4 Changement futur d’assessment embarqué vers séparé

Seulement si besoin mesuré :

1. créer collection cible ;
2. dual write ;
3. backfill depuis parents ;
4. comparer counts/checksums ;
5. lire séparé avec fallback embedded ;
6. activer ;
7. conserver embedded pendant délai ;
8. unset dans PR dédiée ;
9. rollback possible tant que dualité conservée.

# 12. Tests transverses obligatoires

## 12.1 Core

- IDs ;
- `RatingValue` ;
- dates ;
- heures ;
- états ;
- ordre ;
- seuils ;
- aucune dépendance Infrastructure.

## 12.2 Application

- propriété ;
- idempotence ;
- conflits ;
- révisions ;
- jobs demandés ;
- absence de modification communautaire ;
- codes d’erreur stables.

## 12.3 Infrastructure

- Mongo standalone réel ou environnement équivalent ;
- indexes ;
- find-and-update conditionnel ;
- leases ;
- bulk write ;
- crash/replay ;
- documents volumineux ;
- snapshots incomplets ;
- pointer atomique.

## 12.4 WebAPI

- chaînes d’IDs ;
- validation de note ;
- `409`, `422`, Problem Details ;
- idempotency header ;
- pagination ;
- auth ;
- aucune fuite cross-user ;
- OpenAPI diff additif.

## 12.5 Angular

- IDs opaques ;
- saisie de demi-points ;
- dates partielles ;
- heures conditionnelles ;
- conflit/reprise ;
- ajout groupé sans doublon ;
- ordre accessible sans drag obligatoire ;
- huit langues ;
- responsive.

## 12.6 E2E de référence

1. créer une visite précise ;
2. créer une visite à l’année ;
3. ajouter trois occurrences par batch ;
4. rejouer exactement la requête ;
5. vérifier trois occurrences, pas six ;
6. noter chaque ride ;
7. déplacer le deuxième ;
8. forcer une normalisation ;
9. modifier une note avec bonne version ;
10. provoquer un conflit ;
11. vérifier la note globale inchangée ;
12. exporter ;
13. supprimer ;
14. vérifier purge/recalcul/jobs ;
15. vérifier qu’aucune donnée privée n’est SSR/cache public.

# 13. Performance et budgets initiaux

Les budgets sont des hypothèses à mesurer, non des promesses marketing.

## 13.1 API synchrone

- création visite p95 cible < 300 ms hors réseau ;
- append occurrence p95 < 300 ms ;
- batch 100 occurrences < 2 s ou job si le budget n’est pas tenu ;
- liste 50 visites < 500 ms ;
- visite 500 occurrences < 800 ms paginée/projetée ;
- aucun scan global.

## 13.2 Jobs

- backlog normal proche de zéro hors pics ;
- âge du plus vieux job mesuré ;
- un job lourd à la fois ;
- timeouts par kind ;
- payload < limite définie ;
- aucun job contenant une liste massive d’entités si une référence suffit.

## 13.3 Snapshots

- chunks bornés ;
- taille totale mesurée ;
- reconstruction annulable ;
- publication atomique ;
- lecture d’une page sans charger tout le classement ;
- cache par snapshot ID.

# 14. Observabilité

## 14.1 Métriques fondation

- `rating_value_mapping_anomaly_total` ;
- `identifier_validation_failure_total` ;
- `visit_write_conflict_total` ;
- `ride_idempotent_replay_total` ;
- `ride_position_normalization_total` ;
- `background_jobs_pending` ;
- `background_job_oldest_age_seconds` ;
- `background_job_duration_seconds` ;
- `background_job_dead_letter_total` ;
- `background_job_lease_expired_total` ;
- `ranking_snapshot_build_duration_seconds` ;
- `ranking_snapshot_build_aborted_total` ;
- `ranking_scope_revision_lag` ;
- `passport_statistics_revision_lag`.

## 14.2 Logs

Chaque log structuré utile contient :

- kind/opération ;
- corrélation ;
- durée ;
- résultat ;
- code d’erreur ;
- révision ;
- scope ou type de cible ;
- aucun commentaire privé ;
- aucun token ;
- aucun payload complet.

## 14.3 Alertes

Alertes initiales :

- dead-letter > 0 sur job critique ;
- backlog âgé ;
- snapshot courant absent ;
- pointer incohérent ;
- hausse de conflits ;
- normalisations très fréquentes ;
- anomalies de mapping de notes ;
- CPU élevé pendant reconstruction.

# 15. Runbooks

## 15.1 Job bloqué

1. identifier kind/natural key ;
2. vérifier lease ;
3. vérifier handler/version payload ;
4. vérifier effet déjà appliqué ;
5. ne pas dupliquer ;
6. libérer/rejouer idempotemment ;
7. documenter cause ;
8. corriger reconciler si nécessaire.

## 15.2 Snapshot incomplet

1. ne pas activer ;
2. conserver pointer courant ;
3. inspecter header/chunks ;
4. marquer failed ;
5. supprimer build après diagnostic ;
6. rejouer avec même révision ou plus récente ;
7. surveiller CPU.

## 15.3 Divergence assessment/statistiques

1. état parent fait foi ;
2. comparer `statisticsRevision` ;
3. recalcul ciblé ;
4. ne pas modifier note globale ;
5. audit ;
6. corriger le job/reconciler.

## 15.4 Ordre dupliqué

1. trier par position puis ID ;
2. passer en preview ;
3. renormaliser la visite ;
4. vérifier version ;
5. journaliser ;
6. aucune suppression d’occurrence.

# 16. Sécurité et confidentialité

- propriété vérifiée en Application ;
- IDs opaques mais jamais considérés comme autorisation ;
- rate limit sur batch, export et jobs admin ;
- payload borné ;
- commentaires privés sanitizés à l’affichage ;
- logs minimisés ;
- données privées absentes des caches partagés ;
- exports authentifiés et expirables ;
- suppression graphe testée ;
- admin jobs protégés et audités ;
- aucune route permettant de lire les jobs d’un autre utilisateur ;
- `NaturalKey` ne contient pas d’e-mail ni de secret ;
- lease owner technique, pas donnée personnelle.

# 17. Conditions d’arrêt ou de simplification

Simplifier ou arrêter une infrastructure si :

- le calcul à la demande tient largement les budgets ;
- aucun consommateur immédiat du job générique ;
- la complexité de snapshot dépasse la valeur ;
- une simple invalidation synchrone suffit ;
- le mode standalone rend une garantie impossible et aucune réparation proportionnée n’existe ;
- la charge du VPS est disproportionnée ;
- les utilisateurs ne réutilisent pas le Passeport.

Exemples :

- ne pas livrer `FOUNDATION-13` avant besoin de stabilité des rangs ;
- ne pas ajouter snapshot de statistiques personnelles si les agrégations restent rapides ;
- ne pas ajouter un worker léger supplémentaire sans backlog ;
- ne pas introduire replica set uniquement pour imiter une architecture théorique.

# 18. Gate finale `FOUNDATION-DELIVERY-G`

- [ ] ADR référencés par les roadmaps fonctionnelles ;
- [ ] `RatingValue` exact ;
- [ ] IDs compatibles ;
- [ ] dates partielles sans date inventée ;
- [ ] fuseaux testés ;
- [ ] assessments actifs atomiques ;
- [ ] ordre stable ;
- [ ] idempotence sur batch ;
- [ ] jobs à lease et retry borné ;
- [ ] réconciliateurs ;
- [ ] scopes canoniques ;
- [ ] snapshots non obligatoires pour les premiers seuils ;
- [ ] publication atomique si snapshots activés ;
- [ ] indexes justifiés ;
- [ ] tests cross-user ;
- [ ] export/suppression ;
- [ ] métriques ;
- [ ] runbooks ;
- [ ] rollback sûr ;
- [ ] dette temporaire et flags avec date de retrait.

# 19. Résultat attendu

Cette livraison doit permettre d’implémenter le programme sans ambiguïté sur les choix les plus risqués :

- pas de migration d’identifiants inutile ;
- pas de précision flottante incohérente ;
- pas de note orpheline ;
- pas de faux jour UTC ;
- pas de réordonnancement fragile ;
- pas d’explosion des snapshots ;
- pas de broker surdimensionné ;
- pas de garantie transactionnelle fictive ;
- pas de calcul lourd dans une requête publique ;
- pas de données utilisateur perdues lors d’un rollback.