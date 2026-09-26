# Roadmap 07 — Explorateur historique « le parc à travers le temps »

> Code programme : `HIST`
>
> Dépendances : qualité des historiques, dates et relations existantes ; règles de publication ; SSR/SEO ; Passeport pour la contextualisation personnelle.
>
> Principe : l’interface rend visibles les faits, les périodes, les sources et les incertitudes. Elle ne déduit pas une continuité, un remplacement ou une localisation historique lorsqu’ils ne sont pas documentés.

## 0. Avenant technique FOUNDATION

Les références historiques conservent les identifiants chaîne des entités actuelles et peuvent utiliser des wrappers typés sans migration. `HistoricalDate` et `VisitDate` partagent les principes de précision et d’incertitude, mais restent deux types distincts : une visite est une occurrence personnelle, un fait historique peut être une période, une borne ouverte ou une qualification `Before/After/Circa`.

Les snapshots historiques sont d’abord calculés à la demande avec cache borné. Ils ne deviennent des projections durables que pour des dates ou pages réellement consultées, avec méthodologie et révision source. Les jobs de génération utilisent le worker FOUNDATION ; aucune année arbitraire ne crée automatiquement un document persistant ou une URL indexable.

## 1. Vision produit

Amusement Parks Fun possède un actif difficile à reproduire : des histoires contextualisées, des dates, des changements de noms, des exploitants, des constructeurs, des ouvertures, fermetures et relations entre entités.

L’explorateur doit transformer ces données en usages publics :

- frise d’un parc ;
- vue du parc à une année ou une date ;
- attractions ouvertes pendant une période ;
- éléments disparus ;
- changements de noms et statuts ;
- lignées « remplacé par », seulement lorsqu’elles sont sourcées ;
- évolution des zones ;
- anniversaires ;
- comparaison entre deux dates ;
- contextualisation d’une ancienne visite personnelle.

La promesse :

> « Comprendre ce qui existait réellement à une époque donnée, ce qui a changé et ce qui reste incertain. »

## 2. Objectifs

- Définir un modèle temporel commun aux faits historiques.
- Représenter dates exactes, partielles, intervalles et incertitudes.
- Distinguer fait, interprétation éditoriale et relation déduite.
- Exposer les sources et la date de vérification.
- Générer une vue temporelle sans recopier manuellement chaque état annuel.
- Préserver l’histoire lorsque les entités actuelles changent.
- Offrir des pages SSR indexables et partageables.
- Relier une visite personnelle aux éléments compatibles avec sa date.
- Permettre la correction et l’audit admin.
- Mesurer la couverture afin de ne pas présenter une reconstitution partielle comme exhaustive.

## 3. Non-objectifs

- cartographie historique exacte sans données géométriques ;
- reconstruction 3D ;
- génération d’histoire par IA ;
- déduction automatique d’un remplacement parce que deux attractions partagent une zone ;
- affirmation d’une date précise à partir d’une année seulement ;
- collecte de photos sans droits ;
- réécriture de contenus éditoriaux existants ;
- publication de brouillons admin ;
- fusion d’entités homonymes par heuristique seule.

## 4. Vocabulaire temporel

## 4.1 `HistoricalDate`

```csharp
public sealed record HistoricalDate(
    int Year,
    int? Month,
    int? Day,
    DatePrecision Precision,
    bool IsApproximate,
    DateQualifier? Qualifier);
```

`DateQualifier` :

- `Early` ;
- `Mid` ;
- `Late` ;
- `Before` ;
- `After` ;
- `Circa`.

Une date « 1998 » ne devient jamais `1998-01-01`. Les requêtes temporelles utilisent une plage possible et conservent l’incertitude.

## 4.2 Intervalles

```csharp
public sealed record HistoricalPeriod(
    HistoricalDate? Start,
    HistoricalDate? End,
    PeriodBoundaryConfidence StartConfidence,
    PeriodBoundaryConfidence EndConfidence);
```

- début ouvert ;
- fin ouverte ;
- dates partielles ;
- événement ponctuel ;
- chevauchement possible ;
- statut d’incertitude.

## 4.3 États d’un fait

- `Verified` ;
- `Probable` ;
- `Disputed` ;
- `Unverified` ;
- `Retracted`.

Le public peut voir `Probable` ou `Disputed` avec explication. `Unverified` reste admin par défaut.

## 5. Faits historiques

### 5.1 Types initiaux

#### Parc

- ouverture ;
- fermeture ;
- réouverture ;
- changement de nom ;
- changement d’exploitant/propriétaire ;
- extension/réduction ;
- création, renommage ou suppression de zone ;
- événement majeur ;
- changement de positionnement documenté.

#### Élément

- annonce ;
- construction ;
- ouverture ;
- fermeture temporaire notable ;
- réouverture ;
- fermeture définitive ;
- démantèlement ;
- relocalisation ;
- changement de nom ;
- changement de thème ;
- changement de constructeur/exploitant si pertinent ;
- modification technique majeure ;
- remplacement confirmé ;
- déplacement entre zones.

### 5.2 `HistoricalFact`

```csharp
public sealed class HistoricalFact
{
    public Guid Id { get; }
    public HistoricalSubject Subject { get; }
    public HistoricalFactType Type { get; }
    public HistoricalPeriod Period { get; }
    public HistoricalFactState State { get; }
    public IReadOnlyList<HistoricalSourceReference> Sources { get; }
    public string? StructuredValue { get; }
    public Guid? NarrativeContentId { get; }
    public DateTime VerifiedAtUtc { get; }
    public int Revision { get; }
}
```

Les textes multilingues restent dans le modèle éditorial adapté ; le fait structuré porte les dates et relations.

## 6. Relations temporelles

## 6.1 Types

- `RenamedTo` ;
- `ReplacedBy` ;
- `MovedTo` ;
- `RethemedAs` ;
- `SuccessorOf` ;
- `SamePhysicalAssetAs` ;
- `SharesLocationWith` ;
- `OperatedByDuring` ;
- `LocatedInZoneDuring`.

### 6.1.1 Distinctions obligatoires

`ReplacedBy` ne signifie pas nécessairement :

- même emplacement ;
- même matériel ;
- succession immédiate ;
- causalité.

`SamePhysicalAssetAs` ne signifie pas seulement même nom ou même constructeur.

Chaque relation possède :

- période ;
- confiance ;
- sources ;
- note éditoriale ;
- direction ;
- statut de validation.

## 6.2 Interdiction des déductions silencieuses

Une règle technique peut proposer des candidats admin :

- dates proches ;
- même zone ;
- nom similaire ;
- même identifiant externe.

Mais la relation n’est jamais publiée sans validation humaine et source. L’interface admin affiche « suggestion », pas « fait ».

## 7. Calcul d’état à une date

## 7.1 `ParkHistoricalSnapshot`

Résultat calculé, non nécessairement persisté :

```csharp
public sealed record ParkHistoricalSnapshot(
    Guid ParkId,
    HistoricalInstant RequestedInstant,
    IReadOnlyList<HistoricalParkItemState> Items,
    IReadOnlyList<HistoricalZoneState> Zones,
    HistoricalParkIdentity Identity,
    HistoricalCoverage Coverage,
    IReadOnlyList<HistoricalAmbiguity> Ambiguities,
    DateTime GeneratedAtUtc,
    string MethodologyVersion);
```

### 7.1.1 Inclusion d’un élément

Un élément est :

- `KnownOpen` ;
- `KnownClosed` ;
- `PossiblyOpen` ;
- `Unknown`.

Il est affiché dans la vue principale si `KnownOpen`. Les `PossiblyOpen` apparaissent séparément avec raison. Un élément sans dates n’est pas arbitrairement considéré ouvert.

### 7.1.2 Identité à la date

Utiliser :

- nom historique ;
- zone historique ;
- catégorie historique ;
- exploitant ;
- statut ;
- image datée si droits et contexte connus.

En absence de valeur historique, afficher la valeur actuelle seulement avec libellé « information actuelle », ou masquer selon le champ.

## 7.2 Couverture

`HistoricalCoverage` indique :

- nombre d’éléments avec périodes fiables ;
- nombre avec périodes partielles ;
- nombre sans dates ;
- couverture des zones ;
- couverture des noms ;
- dernière revue ;
- statut `Partial`, `Substantial`, `HighConfidence`.

Phrase publique :

> « Cette vue reconstitue 24 éléments documentés. L’état de 6 autres éléments pour 1998 reste inconnu. »

## 8. Expérience publique

## 8.1 Frise du parc

- événements par année ;
- filtres parc/zone/catégorie ;
- ouverture/fermeture/renommage ;
- médias contextualisés ;
- sources ;
- états probables ou contestés ;
- aucun carrousel purement décoratif ;
- mode liste accessible ;
- liens profonds vers événements.

## 8.2 Sélecteur de date

- année rapide ;
- date complète si couverture ;
- années suggérées selon événements ;
- URL stable `/park/.../history/1998` ;
- canonical ;
- pas de millions d’URLs annuelles dans le sitemap ;
- indexation seulement pour snapshots éditorialement pertinents ;
- changement client fluide avec données calculées/cachées.

## 8.3 Vue « à cette époque »

Sections :

- identité du parc ;
- éléments connus ouverts ;
- ouvertures/fermetures proches ;
- zones ;
- éléments incertains ;
- couverture ;
- sources ;
- comparaison avec aujourd’hui.

## 8.4 Comparaison entre deux dates

- présents aux deux dates ;
- ouverts entre les dates ;
- fermés ;
- renommés ;
- déplacés ;
- statut incertain ;
- changement net par catégorie ;
- aucune phrase causale automatique.

## 8.5 Lignées

Une vue graphe ou liste pour :

- même attraction renommée ;
- matériel déplacé ;
- remplacement confirmé ;
- succession d’exploitants.

Toujours afficher la nature exacte de la relation et sa source.

## 9. Intégration au Passeport

Pour une visite datée :

- proposer les éléments `KnownOpen` ;
- montrer séparément `PossiblyOpen` ;
- permettre recherche dans tout l’historique ;
- avertir sur incohérence certaine ;
- ne pas bloquer une mémoire personnelle lorsque la base est incomplète ;
- permettre signalement « cet élément existait » ;
- stocker `HistoricalConsistency` sur l’occurrence ;
- revalider après amélioration des données sans supprimer l’entrée.

### 9.1 Statistiques historiques personnelles

- attractions disparues visitées ;
- visites avant/après transformation ;
- noms au moment de la visite ;
- parc dans plusieurs époques ;
- catégories historiques ;
- année de première visite comparée à l’évolution du parc.

Ces statistiques restent privées tant que non publiées via `SHARE`.

## 10. Données éditoriales et sources

### 10.1 Source reference

- titre ;
- éditeur/auteur ;
- URL/référence ;
- date de publication ;
- date d’accès ;
- langue ;
- type ;
- archive éventuelle conforme ;
- portée ;
- note admin ;
- droits média séparés.

### 10.2 Contradictions

Lorsque deux sources fiables divergent :

- conserver les deux ;
- état `Disputed` ;
- expliquer ;
- ne pas choisir arbitrairement une date précise ;
- présenter la fourchette ;
- résolution future auditée.

### 10.3 Médias

- contexte/date ;
- droits ;
- crédit ;
- alt text ;
- ne pas illustrer 1980 avec une photo actuelle sans mention ;
- fallback graphique ;
- retrait possible sans supprimer le fait.

## 11. Architecture Application

Ports :

```text
IHistoricalFactRepository
IHistoricalRelationRepository
IHistoricalSourceRepository
IParkHistoricalSnapshotBuilder
IHistoricalCoverageReader
IHistoricalTargetResolver
IHistoricalAuditReader
IHistoricalSnapshotCache
```

Les ports d'écriture des faits, sources et relations reçoivent obligatoirement
la transition de revue correspondante. Révision et audit sont persistés dans le
même document immuable afin qu'aucun état éditorial ne puisse exister sans sa
trace, y compris sans transaction MongoDB multi-collections.

Cas d’usage :

- `GetParkTimelineQuery` ;
- `GetParkHistoricalSnapshotQuery` ;
- `CompareParkHistoricalSnapshotsQuery` ;
- `GetHistoricalLineageQuery` ;
- `GetHistoricalCoverageQuery` ;
- `CreateHistoricalFactCommand` ;
- `ReviewHistoricalFactCommand` ;
- `RetractHistoricalFactCommand` ;
- `CreateHistoricalRelationCommand` ;
- `PreviewHistoricalSnapshotImpactQuery` ;
- `RebuildHistoricalSnapshotCacheCommand`.

Le builder reste déterministe, versionné et testable sans HTTP/Mongo.

## 12. API

Public :

```text
GET /api/public/parks/{parkId}/history/timeline
GET /api/public/parks/{parkId}/history/snapshot?year=1998&month=...
GET /api/public/parks/{parkId}/history/compare?fromYear=1998&toYear=2026
GET /api/public/history/subjects/{type}/{id}/lineage
GET /api/public/parks/{parkId}/history/coverage
GET /api/public/history/methodology/current
```

Admin :

```text
POST  /api/admin/history/facts
PATCH /api/admin/history/facts/{id}
POST  /api/admin/history/facts/{id}/review
POST  /api/admin/history/facts/{id}/retract
POST  /api/admin/history/relations
GET   /api/admin/history/parks/{parkId}/diagnostics
POST  /api/admin/history/parks/{parkId}/rebuild
```

Payload de snapshot borné et projections légères. Pas d’embarquement de tous les récits dans chaque réponse.

## 13. Persistance et indexes

Collections proposées ou adaptation de l’existant :

- `historical-facts` ;
- `historical-relations` ;
- `historical-sources` ;
- `historical-snapshot-cache` facultatif.

Chaque révision de fait, source ou relation embarque son événement de revue
immuable. Le journal est donc consultable par ressource sans collection
d'audit dissociée ni risque de double écriture partielle.

Indexes :

- `{ Subject.Type, Subject.Id, Period.Start.Year }` ;
- `{ Subject.Type, Subject.Id, Type }` ;
- `{ Relation.SourceId, Relation.Type }` ;
- `{ Relation.TargetId, Relation.Type }` ;
- `{ State, VerifiedAtUtc }` ;
- cache unique `(ParkId, InstantKey, MethodologyVersion, SourceRevision)` ;
- pas de TTL sur les faits ;
- cache expiré/invalidation selon révision.

## 14. Administration

### 14.1 Éditeur de fait

- sujet ;
- type ;
- dates/précision ;
- état ;
- sources ;
- récit lié ;
- aperçu public ;
- conflits ;
- impact sur snapshots et visites ;
- validation avant publication.

### 14.2 Diagnostics

- dates manquantes ;
- intervalles impossibles ;
- ouverture après fermeture ;
- relations cycliques incompatibles ;
- noms qui se chevauchent ;
- zones inexistantes ;
- éléments sans source ;
- visites personnelles potentiellement incohérentes comptées anonymement pour diagnostic, sans exposer les utilisateurs ;
- couverture par décennie.

### 14.3 Workflow

```text
Draft
→ SourcesAttached
→ EditorialReview
→ StructuredValidation
→ Published
→ Corrected/Retracted
```

Un administrateur peut cumuler les rôles au début, mais les étapes restent visibles.

## 15. Interface Angular et SSR

```text
features/public/history/
  pages/park-timeline-page/
  pages/park-historical-snapshot-page/
  pages/park-history-comparison-page/
  pages/historical-lineage-page/
  components/historical-date-selector/
  components/history-event-card/
  components/historical-coverage-panel/
  components/historical-item-list/
  components/history-comparison-table/
  state/park-history.facade.ts
```

- SSR du snapshot initial ;
- changement de date côté client ;
- URL synchronisée ;
- HTML initial utile ;
- meta unique ;
- JSON-LD prudent ;
- breadcrumb ;
- Open Graph ;
- listes accessibles ;
- graphiques non obligatoires ;
- huit langues, avec fallback clairement marqué pour contenu non traduit.

## 16. SEO

- page timeline principale indexable ;
- snapshots d’années clés seulement dans sitemap ;
- critères : événement majeur, contenu éditorial suffisant, couverture minimale ;
- pages de filtres arbitraires `noindex` ;
- canonical vers date/année normalisée ;
- pas de génération de toutes les dates possibles ;
- liens internes depuis histoires, parcs, éléments et Passeport public ;
- métadonnées qui n’affirment pas « liste complète » lorsque couverture partielle.

## 17. Cache et performance

- snapshots déterministes ;
- cache par parc/date/méthode/révision ;
- invalidation ciblée sur fait/relation ;
- pré-calcul des années clés ;
- projection d’IDs puis chargement batch des présentations ;
- pas de N+1 images/histoires ;
- limite de profondeur des lignées ;
- détection de cycles ;
- pagination timeline ;
- budgets SSR ;
- monitoring CPU afin de ne pas reproduire un fan-out coûteux.

## 18. Tests obligatoires

### Core

- dates partielles ;
- circa/before/after ;
- intervalles ouverts ;
- chevauchement ;
- état à la frontière ;
- renommage ;
- réouverture ;
- probable/inconnu ;
- relation cyclique ;
- comparaison ;
- couverture.

### Application/Infrastructure

- sources ;
- contradiction ;
- révision ;
- cache invalidation ;
- snapshot idempotent ;
- gros parc ;
- suppression média ;
- cible retirée ;
- audit.

### API/Angular

- date invalide ;
- année sans données ;
- couverture partielle ;
- huit langues ;
- SSR ;
- canonical/noindex ;
- accessibilité ;
- comparaison ;
- source visible.

### End-to-end

1. créer ouverture, renommage et fermeture ;
2. afficher avant/pendant/après ;
3. ajouter une relation `ReplacedBy` sourcée ;
4. comparer deux années ;
5. corriger la date ;
6. invalider le cache ;
7. vérifier une ancienne visite ;
8. confirmer qu’aucune continuité non sourcée n’apparaît.

## 19. Observabilité

- snapshots demandés ;
- années sans couverture ;
- cache hit/miss ;
- durée ;
- faits incomplets ;
- conflits ;
- corrections ;
- signalements ;
- ouverture des sources ;
- passage histoire → Passeport ;
- pages indexées ;
- erreurs SSR.

Ne pas utiliser la fréquence de visite d’une page comme preuve historique.

## 20. Déploiement

### Pilote A

- un parc très documenté ;
- frise ;
- années clés ;
- couverture ;
- sources.

### Pilote B

- comparaison ;
- lignées ;
- intégration Passeport ;
- administration.

### Pilote C

- portefeuille de parcs ;
- SEO ;
- récapitulatifs partageables ;
- diagnostics de couverture.

Chaque parc est activé individuellement. Une histoire narrative existante ne suffit pas si les faits temporels ne sont pas structurés.

## 21. Découpage recommandé en PR

| PR | Contenu | Critère |
|---|---|---|
| [`HIST-01`](../../architecture/product-growth-hist-01-temporal-semantics-2026-09-25.md) | ADR dates, faits, relations et incertitude | Sémantique figée |
| `HIST-02` | Core temporel | Frontières testées — implémenté le 25 septembre 2026 |
| `HIST-03` | Persistance faits/sources | Audit et indexes — implémenté le 26 septembre 2026 |
| `HIST-04` | Migration/adaptation des historiques existants | Aucune perte de contenu — implémenté le 26 septembre 2026 |
| `HIST-05` | Builder snapshot | Résultat déterministe |
| `HIST-06` | Couverture et ambiguïtés | Partiel visible |
| `HIST-07` | API timeline/snapshot | Contrats bornés |
| `HIST-08` | UI frise et année pilote | SSR accessible |
| `HIST-09` | Relations/lignées | Aucune déduction silencieuse |
| `HIST-10` | Comparaison de dates | Diff exact |
| `HIST-11` | Intégration Passeport | Anciennes visites contextualisées |
| `HIST-12` | Admin diagnostics/revue | Exploitation fiable |
| `HIST-13` | SEO/partage | Pages clés seulement |
| `HIST-14` | Extension parcs | Gate par parc |

### Implémentation `HIST-02` — 25 septembre 2026

Le Core possède désormais un vocabulaire temporel indépendant de MongoDB, de
l'API et d'Angular. `HistoricalDate` conserve une précision à l'année, au mois
ou au jour, l'approximation et les qualificatifs `Early`, `Mid`, `Late`,
`Before`, `After` et `Circa`. Son enveloppe civile sert au raisonnement sans
modifier la valeur affichable : `1998` couvre ses douze mois mais ne devient
jamais le 1er janvier.

`HistoricalInstant` représente l'enveloppe demandée par le visiteur et produit
des clés distinctes pour une année, un mois ou un jour. `HistoricalPeriod`
gère les bornes ouvertes, les événements ponctuels, l'inclusion civile, la
confiance `Confirmed`, `Estimated` ou `Disputed`, les ordres certains et les
chevauchements ambigus. Une intersection partielle prouvée reste distincte d'un
simple chevauchement possible. Les erreurs portent des codes métier stables afin
que les couches suivantes puissent les traduire sans recopier les règles.

Les tests du Core couvrent les années bissextiles, les jours invalides, les
qualificatifs exclusifs, les limites du calendrier, les périodes inversées,
les premiers et derniers jours inclus, les enveloppes partielles et les bornes
incertaines. Aucune persistance, API ou interface n'est introduite dans ce
jalon ; elles consommeront ces valeurs dans les PR suivantes.

### Implémentation `HIST-03` — 26 septembre 2026

Le registre canonique peut désormais conserver séparément les faits, leurs
sources et chaque événement de revue. Un fait mémorise son sujet avec son
libellé historique figé, sa période structurée, son état de preuve, son état de
publication, ses sources, sa version de méthodologie et sa chaîne de révisions.
Chaque preuve citée est figée sur sa révision exacte : une correction future de
la source ne réécrit donc jamais rétroactivement un fait déjà publié.
Avant l'insertion, un service de domaine vérifie que ces révisions existent,
sont admissibles et couvrent ensemble l'identité, le libellé historique, le
type, la période et, lorsqu'elle existe, la valeur structurée du fait.
Les combinaisons incohérentes sont refusées dans le Core : un fait vérifié sans
preuve, un contenu incertain sans explication dans les huit langues ou une
cible supprimée rendue publique ne peuvent pas atteindre MongoDB. La famille du
fait doit aussi être compatible avec la nature de son sujet : un démontage ne
peut pas qualifier un parc et une création de zone ne peut pas qualifier une
attraction.

Les écritures sont append-only et idempotentes : rejouer exactement la même
révision ne crée pas de doublon, tandis qu'une autre valeur utilisant la même
identité de révision est signalée comme conflit. Les preuves suivent la même
discipline et conservent leur référence stable, leur date de consultation, leur
accessibilité et la partie du fait qu'elles soutiennent. Leurs révisions figées
peuvent être chargées par lot, sans lecture N+1. Chaque révision et son
événement de revue sont écrits atomiquement dans le même document : le journal
reste permanent, ne peut pas être omis par un appelant et associe chaque action
à la révision concernée sans exposer ces informations au public. Les
modifications de brouillon possèdent leur propre événement et chaque audit doit
succéder chronologiquement à celui de la révision précédente.

MongoDB initialise les collections `historical-facts` et `historical-sources`
avec des index adaptés aux recherches par sujet, période, publication, preuve,
révision et audit embarqué. Aucun TTL ne peut effacer ce
patrimoine. Les ports Application isolent entièrement le Core et les futurs cas
d'usage de MongoDB. Le modèle historique antérieur n'est pas lu en parallèle :
sa conversion et la bascule unique restent le jalon `HIST-04`.

### Implémentation `HIST-04` — 26 septembre 2026

Au démarrage du déploiement, une migration versionnée prend un instantané de
la collection historique antérieure, calcule son empreinte et recopie chaque
document à l'identique dans une sauvegarde dédiée. Les titres, résumés,
articles, images, sources, slugs, identifiants liés et indicateurs de mise en
avant restent ainsi récupérables sans dépendre du nouveau modèle. La collection
opérationnelle est ensuite remplacée en une seule bascule par les récits
canoniques ; l'application ne relit ni ne réécrit l'ancienne collection.

Chaque type d'événement connu possède une correspondance explicite vers un fait
structuré. Les dates gardent leur précision d'origine, l'importance reste
identique et les changements de nom, logo, exploitant, propriétaire, thème ou
localisation conservent leurs anciennes et nouvelles valeurs lorsqu'elles sont
présentes. Une valeur inconnue n'est jamais transformée silencieusement en
« autre ». Les associations historiques vagues sont conservées dans le récit
mais ne deviennent pas des relations sans preuve.

Les sources valides deviennent des références canoniques figées sur leur
première révision. Les informations absentes ou invalides alimentent un rapport
d'anomalies mesurable. Un contenu visible mais encore non prouvé entre dans la
file `LegacyPublishedPendingReview`, avec un avertissement dans les huit
langues, et reste exclu des décisions certaines. Une cible cachée, absente ou
marquée non pertinente est conservée avec la politique `Suppressed` et ne peut
pas être exposée par accident. Les conversions impossibles restent
administrables comme récits bloqués : elles ne sont ni supprimées ni inventées.

La migration possède un bail, un marqueur de réussite, des identités
déterministes et des compteurs avant/après. Une interruption avant la bascule
fait reconstruire uniquement la sortie inachevée au prochain démarrage. La
réussite exige une empreinte source inchangée et l'égalité entre source,
sauvegarde et récits migrés. MongoDB est donc mis à jour automatiquement par le
déploiement, sans manipulation manuelle ni coexistence durable de deux moteurs.

## 22. Gate finale `HIST-G`

- dates partielles et incertitudes sont conservées ;
- chaque fait public possède une source ou un état explicitement non certain ;
- aucune relation de remplacement n’est déduite silencieusement ;
- la vue à une date distingue ouvert, possiblement ouvert et inconnu ;
- la couverture est affichée ;
- les cibles renommées/fermées restent accessibles dans l’histoire ;
- les anciennes visites peuvent être saisies sans inventer une date ;
- les caches sont invalidés après correction ;
- les pages SEO ne prétendent pas être exhaustives sans couverture ;
- les médias sont contextualisés et licenciés ;
- le pilote démontre que l’explorateur apporte une valeur différente d’une simple liste d’événements.
