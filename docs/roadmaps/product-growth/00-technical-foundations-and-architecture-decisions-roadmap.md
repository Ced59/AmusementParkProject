# Roadmap 00 — Fondations techniques et décisions d’architecture

> Code programme : `FOUNDATION`
>
> Priorité : bloquante avant les premières PR persistantes de `RANK` et `PASS`.
>
> Base auditée : `master` au commit `8742d6e657ef6c1c64f6e360e29fe2aa2ae6b019`, le 28 août 2026.
>
> Périmètre : conventions de domaine, persistance MongoDB, idempotence, concurrence, ordonnancement, calculs de notes, publications de classements et travaux différés nécessaires au programme produit Web.
>
> Statut : décisions recommandées à appliquer aux roadmaps `RANK`, `PASS`, `SHARE`, `WATCH`, `TRIP`, `HIST`, `LIVE` et `QUAL`.

## 1. Rôle de cette roadmap

Les roadmaps produit définissent déjà les capacités, les invariants métier, les contrats envisagés, les gates de qualité et le découpage fonctionnel. Plusieurs exemples techniques y étaient toutefois volontairement illustratifs : identifiants en `Guid`, notes en `decimal`, évaluations stockées dans des collections séparées, séquences de rides, snapshots sans périmètre canonique définitivement borné et référence générique à une outbox.

Le dépôt réel utilise aujourd’hui :

- des identifiants fonctionnels `string` dans `EntityBase`, `UserRating`, les documents Mongo et les contrats existants ;
- des notes persistées en `double` avec une échelle de `0,5` à `5`, par pas de `0,5` ;
- MongoDB en mode autonome dans le chemin de production actuellement supporté ;
- des synchronisations par versions monotones pour éviter qu’un recalcul ancien écrase un résultat récent ;
- une API .NET et un front Angular SSR déployés sur un VPS aux ressources volontairement modestes.

Cette roadmap transforme les pistes illustratives en décisions compatibles avec ce socle. Elle poursuit cinq objectifs :

1. éviter une migration transversale inutile des identifiants ;
2. garantir une représentation exacte et unique des notes ;
3. réduire les écritures multi-documents lorsque MongoDB autonome ne peut pas offrir de transaction ;
4. borner les projections, snapshots et travaux différés ;
5. conserver une progression incrémentale, observable et réversible.

## 2. Règle de précédence documentaire

En cas de divergence entre un exemple de code antérieur et la présente roadmap :

- les invariants métier de la roadmap fonctionnelle restent applicables ;
- les décisions de représentation, persistance, concurrence et livraison de ce document prévalent ;
- une nouvelle décision contraire nécessite un ADR explicite, une analyse d’impact et une mise à jour des roadmaps concernées ;
- aucun développeur ne doit déduire une migration globale uniquement parce qu’un exemple antérieur utilisait `Guid`, `decimal` ou une collection séparée.

## 3. Registre synthétique des décisions

| ADR | Sujet | Décision retenue | Conséquence principale |
|---|---|---|---|
| `FOUNDATION-ADR-01` | Identifiants | Stockage et contrats compatibles `string`; value objects typés autour d’une chaîne normalisée | Aucune migration générale des IDs |
| `FOUNDATION-ADR-02` | Valeur de note | `RatingValue` exact fondé sur 1 à 10 demi-points | Plus de divergence `double`/`decimal` dans le domaine |
| `FOUNDATION-ADR-03` | Sémantique d’une visite | Une visite est une session d’un utilisateur dans un parc, rattachée à un jour de service local lorsqu’il est connu | Statistiques et heures cohérentes sans date inventée |
| `FOUNDATION-ADR-04` | Notes temporelles actives | Note de visite embarquée dans `Visit`; note de ride embarquée dans `RideOccurrence`; audit séparé | Écriture atomique sur MongoDB autonome |
| `FOUNDATION-ADR-05` | Ordre des rides | `SortPosition` entier 64 bits espacé, renormalisé ponctuellement | Pas de décimaux ni de LexoRank prématuré |
| `FOUNDATION-ADR-06` | Classements publiés | Snapshots uniquement pour des scopes canoniques, activés par pointeur atomique | Pas d’explosion combinatoire des filtres |
| `FOUNDATION-ADR-07` | Jobs et réactions | Worker .NET borné, jobs Mongo à lease, révisions source et réparateur | Pas de broker externe ni de fausse atomicité |
| `FOUNDATION-ADR-08` | Cohérence et audit | État courant dans le document source; audit append-only minimisé; réconciliation explicite | Lecture simple, correction et support possibles |
| `FOUNDATION-ADR-09` | Matérialisation | Calcul à la demande d’abord; projection seulement après baseline et budget | Protection du VPS et réduction du code mort |
| `FOUNDATION-ADR-10` | Rollout | Expand/contract, flags temporaires, kill switches et gates mesurées | Déploiements réversibles |

# 4. `FOUNDATION-ADR-01` — Identifiants compatibles et typés

## 4.1 Contexte

Le dépôt actuel expose et persiste des identifiants sous forme de chaînes. Un nouvel agrégat utilisant directement `Guid` introduirait plusieurs risques :

- conversion permanente entre anciens et nouveaux modèles ;
- contrats HTTP incohérents ;
- serializers et mappers spécifiques inutiles ;
- impossibilité de référencer proprement des entités historiques ou externes dont l’identifiant n’est pas un UUID ;
- migration globale sans valeur utilisateur ;
- risque de casser des liens, caches, exports, documents Mongo et tests existants.

Le besoin réel n’est pas de changer la représentation persistée. Il est d’éviter de confondre par erreur un `ParkId`, un `VisitId`, un `UserId` et un `ParkItemId` dans le domaine et les cas d’usage.

## 4.2 Décision

Les nouvelles capacités respectent les règles suivantes :

1. les documents Mongo conservent des identifiants `string` ;
2. les contrats API publics conservent des identifiants JSON de type chaîne ;
3. les routes continuent de transporter des chaînes ;
4. les nouveaux types métier peuvent utiliser des value objects fortement typés autour d’une chaîne ;
5. aucune conversion automatique d’une chaîne existante en `Guid` n’est exigée ;
6. la génération d’un nouvel identifiant interne peut continuer à utiliser un UUID aléatoire sérialisé en chaîne ;
7. le format généré est normalisé, mais le parseur n’exige pas que toutes les références historiques respectent ce format.

## 4.3 Types proposés

```csharp
public readonly record struct VisitId
{
    public string Value { get; }

    private VisitId(string value)
    {
        this.Value = value;
    }

    public static VisitId New()
    {
        return new VisitId(Guid.NewGuid().ToString("N"));
    }

    public static VisitId Parse(string value)
    {
        return new VisitId(IdentifierRules.NormalizeRequired(value, nameof(value)));
    }

    public override string ToString()
    {
        return this.Value;
    }
}
```

Types initiaux :

- `VisitId` ;
- `RideOccurrenceId` ;
- `SharePublicationId` ;
- `TripPlanId` ;
- `BackgroundJobId` ;
- éventuellement wrappers de références existantes uniquement lorsqu’ils réduisent réellement les erreurs.

Les types `ParkId`, `ParkItemId` et `UserId` ne sont pas imposés à tout le code dans une migration massive. Ils peuvent être introduits progressivement dans les nouveaux use cases, avec mappers explicites aux frontières.

## 4.4 Normalisation

Une règle centrale `IdentifierRules` :

- refuse `null`, vide et whitespace pour les identifiants requis ;
- applique `Trim()` ;
- fixe une longueur maximale ;
- n’applique pas de conversion de casse aux identifiants existants sans preuve que leur comparaison est insensible à la casse ;
- interdit les caractères de contrôle ;
- ne transforme jamais silencieusement un identifiant invalide en nouvel identifiant ;
- produit un code d’erreur métier stable.

## 4.5 Sérialisation et persistance

Deux approches acceptables :

### Approche A — value object dans le Core, chaîne dans les documents

```csharp
public sealed class VisitDocument
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string ParkId { get; set; } = string.Empty;
}
```

Le mapper fait :

```csharp
VisitId visitId = VisitId.Parse(document.Id);
```

C’est l’approche recommandée pour les premières PR.

### Approche B — serializer Mongo dédié

À étudier seulement si les value objects sont largement utilisés. Le serializer doit continuer à écrire une chaîne et disposer de tests de compatibilité sur les documents historiques.

## 4.6 Contrats API

Les DTO conservent :

```csharp
public sealed record VisitDto(
    string Id,
    string ParkId,
    ...);
```

Le mapping Application/WebAPI convertit depuis les types métier. Le front Angular ne dépend pas d’un format UUID et considère les identifiants comme des chaînes opaques.

## 4.7 Tests obligatoires

- chaîne normale ;
- UUID avec et sans tirets ;
- identifiant historique non UUID ;
- trim ;
- vide ;
- caractères de contrôle ;
- longueur maximale ;
- sérialisation JSON ;
- mapping Mongo aller-retour ;
- comparaison sensible à la casse conforme au comportement existant ;
- impossibilité de passer un `VisitId` à une méthode exigeant un `RideOccurrenceId` dans le nouveau code typé.

## 4.8 Non-objectifs

- réécrire `EntityBase` ;
- migrer tous les documents existants ;
- imposer des IDs numériques ;
- exposer le format interne comme une garantie publique ;
- dériver un identifiant depuis une donnée personnelle.

# 5. `FOUNDATION-ADR-02` — `RatingValue` exact et commun

## 5.1 Problème

L’échelle autorise exactement dix valeurs, de `0,5` à `5,0`, par pas de `0,5`. Une représentation métier en `double` nécessite une tolérance flottante. Une nouvelle représentation en `decimal` pour les notes temporelles créerait deux vérités techniques pour la même notion.

Le domaine doit exprimer l’ensemble fini réellement autorisé, pas seulement un nombre à virgule validé après coup.

## 5.2 Décision

Créer un value object `RatingValue` fondé sur un nombre entier de demi-points :

```csharp
public readonly record struct RatingValue
{
    public const byte MinimumHalfSteps = 1;
    public const byte MaximumHalfSteps = 10;

    public byte HalfSteps { get; }

    public decimal DecimalValue => this.HalfSteps / 2m;

    public double DoubleValue => this.HalfSteps / 2d;

    private RatingValue(byte halfSteps)
    {
        this.HalfSteps = halfSteps;
    }

    public static RatingValue FromHalfSteps(byte halfSteps)
    {
        if (halfSteps < MinimumHalfSteps || halfSteps > MaximumHalfSteps)
        {
            throw new DomainValidationException("rating.invalid-value");
        }

        return new RatingValue(halfSteps);
    }

    public static RatingValue FromDecimal(decimal value)
    {
        decimal doubled = value * 2m;
        if (doubled != decimal.Truncate(doubled))
        {
            throw new DomainValidationException("rating.invalid-step");
        }

        return FromHalfSteps(checked((byte)doubled));
    }
}
```

## 5.3 Source de vérité

- le domaine stocke `HalfSteps` ;
- les calculs de somme peuvent additionner des entiers ;
- les moyennes, médianes et scores bayésiens convertissent au moment du calcul ;
- l’arrondi n’intervient qu’à l’affichage ;
- `0` ne représente jamais « non noté » ; l’absence reste `null`.

## 5.4 Compatibilité avec `UserRating`

Le changement suit expand/contract :

### Étape R1 — Ajouter le type domaine

- `RatingValue` et tests ;
- surcharge ou adaptation de `RatingScoreCalculator` ;
- aucune modification de document.

### Étape R2 — Mapper le champ `double` historique

- lecture de `Value` ;
- validation stricte ;
- conversion en demi-points ;
- diagnostic des valeurs hors grille ;
- aucune correction silencieuse.

### Étape R3 — Écriture duale facultative

Ajouter `ValueHalfSteps` au document existant seulement si cela simplifie la migration :

- nouvelle écriture renseigne `Value` et `ValueHalfSteps` ;
- lecture préfère `ValueHalfSteps`, puis fallback `Value` ;
- backfill idempotent ;
- rapport des anomalies.

### Étape R4 — Contract ultérieur

La suppression de `Value` n’est envisagée que dans une PR dédiée après mesure, compatibilité et sauvegarde. Elle n’est pas un prérequis du Passeport.

## 5.5 Nouvelles notes temporelles

Les documents `Visit` et `RideOccurrence` persistent directement :

```text
assessment.valueHalfSteps: 1..10
```

Les DTO peuvent continuer à exposer :

```json
{ "value": 4.5 }
```

Le stockage exact reste un détail interne.

## 5.6 Calculs statistiques

- somme exacte en demi-points ;
- moyenne calculée en `double` ou `decimal` selon l’algorithme, mais jamais utilisée comme nouvelle note valide sans conversion explicite ;
- médiane sur les unités entières ;
- min/max sur `RatingValue` ;
- dispersion documentée ;
- score bayésien calculé à partir d’une somme exacte ;
- aucune comparaison de note par epsilon ;
- epsilon réservé aux scores dérivés de classement.

## 5.7 Tests obligatoires

- dix valeurs valides ;
- `0`, `0,25`, `5,5`, valeurs négatives ;
- conversion depuis tous les doubles historiques valides ;
- rejet ou diagnostic de `4,499999` ;
- JSON dans les huit locales sans modifier la valeur ;
- Mongo aller-retour ;
- moyenne et médiane de fixtures indépendantes ;
- calcul bayésien identique au comportement historique pour les données valides.

# 6. `FOUNDATION-ADR-03` — Sémantique d’une visite et temps local

## 6.1 Définition

Une `Visit` représente une session déclarée par un utilisateur dans un seul parc.

Lorsqu’un jour précis est connu, elle est rattachée à un **jour de service local** du parc. Ce jour correspond normalement à la date locale du début de la visite. Si un parc ferme après minuit, les rides effectués après minuit peuvent rester rattachés au jour de service choisi au lieu de créer automatiquement une nouvelle visite.

## 6.2 Cas couverts

- deux parcs le même jour : deux visites ;
- deux jours consécutifs dans le même parc : deux visites ;
- sortie puis retour dans le même parc le même jour : une visite par défaut, mais l’utilisateur peut en créer deux ;
- deux visites du même parc le même jour : autorisées ;
- ancienne visite dont seule l’année est connue : une visite avec précision `Year`, pas une période entière ;
- ancienne visite dont seul le mois est connu : précision `Month` ;
- visite future planifiée : relève d’une intention ou d’un `TripPlan`, pas d’une visite accomplie, sauf statut explicite de brouillon prévu ;
- visite commencée avant minuit et terminée après : une seule visite si l’utilisateur conserve le même jour de service.

Aucun index unique `(UserId, ParkId, Date)` n’est créé.

## 6.3 Modèle recommandé

```csharp
public sealed record VisitDate(
    int Year,
    int? Month,
    int? Day,
    VisitDatePrecision Precision,
    bool IsApproximate);

public sealed class Visit
{
    public VisitId Id { get; }
    public string UserId { get; }
    public string ParkId { get; }
    public VisitDate Date { get; private set; }
    public string? TimeZoneId { get; private set; }
    public LocalServiceDayConvention ServiceDayConvention { get; private set; }
    ...
}
```

Le fuseau appartient à la visite, pas à `VisitDate`.

## 6.4 Fuseau horaire

- utiliser un identifiant IANA, par exemple `Europe/Paris` ;
- le fuseau est facultatif pour une visite sans heure ;
- il devient requis avant d’enregistrer une heure locale précise ;
- le fuseau par défaut peut être proposé depuis le parc, mais reste visible et corrigeable ;
- ne jamais convertir une date partielle en minuit UTC ;
- conserver l’heure locale saisie et les informations permettant une conversion lorsque celle-ci est nécessaire ;
- gérer les heures ambiguës ou inexistantes lors des changements DST ;
- une heure approximative reste marquée comme telle.

## 6.5 `OccurrenceMoment`

```csharp
public sealed record OccurrenceMoment(
    TimeOnly? LocalTime,
    bool IsApproximate);
```

L’ordre principal vient de `SortPosition`. L’heure est une information supplémentaire et ne doit pas être fabriquée pour trier.

## 6.6 Jour de service

`LocalServiceDayConvention` valeurs initiales :

- `VisitStartLocalDate` ;
- `UserSelectedServiceDate`.

Une future intégration d’horaires peut proposer automatiquement le jour de service, sans modifier silencieusement les visites existantes.

## 6.7 Invariants

- `Day` exige mois et jour ;
- `Month` interdit le jour ;
- `Year` interdit mois et jour ;
- une heure exige précision `Day` et fuseau ;
- les rides d’une visite partagent le parc et le jour de service ;
- un changement de date déclenche une nouvelle vérification de cohérence historique ;
- les dates futures sont interdites pour une visite `Completed`, sauf tolérance technique de fuseau bornée ;
- un brouillon rétrospectif peut être incomplet sans inventer de date.

# 7. `FOUNDATION-ADR-04` — Évaluations actives embarquées

## 7.1 Contexte

La roadmap `PASS` proposait initialement quatre collections : visites, occurrences, notes de visite et notes de ride. Cette normalisation est viable, mais elle oblige à maintenir des relations un-à-un et des suppressions coordonnées sur un MongoDB autonome.

Or :

- une visite possède au maximum une note de parc active ;
- une occurrence possède au maximum une note active ;
- la note active suit le cycle de vie de son parent ;
- la lecture affiche presque toujours parent et note ensemble ;
- l’audit des corrections peut être séparé de l’état courant.

## 7.2 Décision

La source de vérité active est embarquée :

```csharp
public sealed record TemporalAssessment(
    RatingValue Value,
    string? PrivateComment,
    int Revision,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
```

Dans `VisitDocument` :

```text
parkAssessment: {
  valueHalfSteps,
  privateComment,
  revision,
  createdAtUtc,
  updatedAtUtc
}
```

Dans `RideOccurrenceDocument` :

```text
assessment: {
  valueHalfSteps,
  privateComment,
  revision,
  createdAtUtc,
  updatedAtUtc
}
```

## 7.3 Avantages recherchés

- une seule écriture atomique pour créer ou modifier l’état courant ;
- aucune note orpheline ;
- suppression locale ;
- contrôle propriétaire sur le même document ;
- lecture plus simple ;
- moins d’indexes ;
- pas de transaction multi-document ;
- concurrence optimiste cohérente avec le parent.

## 7.4 Audit

Les corrections sensibles restent dans un journal append-only :

- création ;
- modification ;
- suppression ;
- ancienne valeur minimisée ;
- nouvelle valeur minimisée ;
- révision ;
- corrélation ;
- origine ;
- date UTC.

Le commentaire privé complet n’est recopié dans l’audit que si la finalité et la rétention le justifient. Par défaut, l’audit conserve un indicateur de changement ou une valeur protégée/minimisée.

## 7.5 Écritures

Une modification d’assessment :

1. vérifie propriétaire ;
2. vérifie version du parent ;
3. valide `RatingValue` ;
4. applique l’update atomique du document ;
5. incrémente version du parent et révision de l’assessment ;
6. marque la révision statistique ;
7. enregistre ou planifie l’audit idempotent ;
8. retourne l’état courant.

Si l’audit différé échoue, l’état métier reste valide et un réparateur détecte l’écart à partir de la révision source.

## 7.6 Requêtes analytiques

MongoDB peut filtrer et agréger des sous-documents embarqués. Indexes candidats :

```text
user-visits:           (UserId, ParkId, Date.Year)
user-ride-occurrences: (UserId, ParkItemId, VisitId)
user-ride-occurrences: (UserId, ParkItemId, Assessment.UpdatedAtUtc)
```

Ne pas indexer `Assessment.ValueHalfSteps` avant qu’une requête réelle le nécessite.

## 7.7 Condition de séparation future

Une collection distincte ne sera introduite que si au moins un besoin mesuré l’exige :

- plusieurs assessments actifs par parent ;
- rétention différente de l’état parent ;
- fréquence d’écriture créant une contention significative ;
- requêtes analytiques globales impossibles à tenir dans les budgets ;
- réplication indépendante ;
- obligation juridique spécifique.

La séparation suivra expand/contract et ne changera pas les contrats publics.

# 8. `FOUNDATION-ADR-05` — Ordonnancement stable des occurrences

## 8.1 Décision

Chaque `RideOccurrence` possède :

```csharp
public long SortPosition { get; private set; }
```

La position n’est pas un numéro visible. Le numéro affiché est dérivé après tri.

## 8.2 Valeurs initiales

Pas recommandé : `1024`.

Exemple :

```text
1024, 2048, 3072, 4096
```

## 8.3 Opérations

### Ajouter à la fin

```text
newPosition = currentMax + 1024
```

### Ajouter au début

```text
newPosition = currentMin - 1024
```

### Insérer entre A et B

Si `B - A > 1` :

```text
newPosition = A + ((B - A) / 2)
```

### Plus d’espace disponible

- acquérir la version optimiste de la visite ;
- relire les occurrences dans l’ordre stable ;
- réattribuer `1024, 2048, ...` par bulk write borné ;
- réappliquer l’insertion ;
- rendre l’opération idempotente ;
- produire une métrique de renormalisation.

## 8.4 Tri déterministe

```text
SortPosition ASC,
CreatedAtUtc ASC,
Id ASC
```

Les deux derniers critères ne donnent pas un sens métier supplémentaire ; ils garantissent seulement un ordre stable en cas d’anomalie.

## 8.5 Concurrence

- le déplacement accepte `ExpectedVisitVersion` ;
- un conflit retourne `409` et l’ordre courant ;
- aucune stratégie last-write-wins ;
- un batch de réordonnancement utilise une clé idempotente ;
- une seconde demande identique retourne le résultat initial ;
- le front permet de recharger puis rejouer le choix.

## 8.6 Non-objectifs

- rang lexical distribué ;
- CRDT ;
- ordre temps réel multi-utilisateur ;
- décimaux arbitraires ;
- réécriture à chaque insertion ;
- dépendance SignalR.

# 9. `FOUNDATION-ADR-06` — Scopes canoniques des classements

## 9.1 Risque à éviter

Un snapshot par combinaison de pays, région, catégorie, type, statut, langue, période et filtre utilisateur provoquerait :

- multiplication des documents ;
- recalculs en cascade ;
- invalidations coûteuses ;
- cache difficile à raisonner ;
- CPU disproportionné sur le VPS ;
- publication de classements minuscules sans valeur.

## 9.2 Décision

Seuls des scopes canoniques et explicitement publiés peuvent disposer d’un snapshot durable.

Scopes initiaux :

1. `parks:global` ;
2. `park-items:category:{category}` pour les catégories publiques principales ;
3. `park-items:type:{type}` uniquement lorsqu’une route publique dédiée existe et franchit la gate de volume ;
4. `parks:country:{countryId}` uniquement après mesure d’au moins trois entrées éligibles et utilité éditoriale ;
5. aucun scope par langue : les scores sont indépendants de la traduction ;
6. aucun scope par utilisateur pour les classements communautaires ;
7. les classements personnels restent calculés depuis les préférences courantes de leur propriétaire.

## 9.3 Registre de scopes

```csharp
public sealed record RankingScopeDefinition(
    string Key,
    RankingTargetFamily TargetFamily,
    RankingFilterDefinition Filter,
    bool IsPublic,
    int MinimumEligibleEntries,
    int PageSize,
    RankingPublicationMode PublicationMode);
```

Le registre est versionné et testé. Une chaîne issue directement d’une query HTTP ne devient jamais une clé de snapshot.

## 9.4 Filtres non canoniques

Trois stratégies, dans cet ordre :

1. filtrer un snapshot canonique déjà chargé si la sémantique reste correcte ;
2. calculer à la demande depuis les agrégats avec pagination et cache court ;
3. refuser de présenter un rang et afficher seulement des résultats triés/filtrés sans prétention de classement.

Un filtre rare n’est promu en scope durable qu’après métriques et revue.

## 9.5 Livraison incrémentale

Les seuils d’éligibilité ne dépendent pas du moteur de snapshot.

### Phase RANK-A

- calculer `RankingEvidence` depuis les agrégats actuels ;
- masquer les rangs insuffisants ;
- publier la méthode ;
- conserver le provider actuel avec garde-fous.

### Phase RANK-B

- mesurer latence, stabilité et coût ;
- introduire les snapshots uniquement pour les scopes canoniques ;
- comparer l’ordre et les volumes ;
- activer par flag.

## 9.6 Modèle de snapshot borné

Recommandation : en-tête + chunks.

### `RankingSnapshotHeader`

- `Id` chaîne ;
- scope canonique ;
- version de méthodologie ;
- révision source ;
- statut `Building`, `Validated`, `Current`, `Superseded`, `Failed` ;
- nombre total d’entrées ;
- nombre d’entrées éligibles ;
- nombre de chunks ;
- checksum ;
- dates ;
- erreur minimisée.

### `RankingSnapshotChunk`

- `SnapshotId` ;
- `ChunkIndex` ;
- tableau borné, par exemple 250 à 500 entrées ;
- checksum du chunk.

### `RankingPublicationPointer`

- un document par scope ;
- `CurrentSnapshotId` ;
- version ;
- changement atomique ;
- ancien pointeur conservé pour rollback.

## 9.7 Construction

1. réserver `SourceRevision` ;
2. créer header `Building` ;
3. calculer depuis une lecture cohérente au niveau applicatif ;
4. écrire chunks idempotents ;
5. vérifier comptes/checksums ;
6. passer `Validated` ;
7. comparer avec snapshot courant ;
8. activer le pointeur atomiquement ;
9. passer ancien snapshot `Superseded` ;
10. invalider les caches du scope ;
11. conserver un nombre borné de versions.

Un snapshot incomplet n’est jamais rendu public.

## 9.8 Déclenchement

Les mutations ne reconstruisent pas tout le classement dans la requête utilisateur.

- une mutation augmente une révision source ;
- un job coalescé par scope demande une reconstruction ;
- plusieurs notes rapprochées produisent une seule reconstruction utile ;
- délai de stabilisation court configurable ;
- reconstruction manuelle admin auditée ;
- réparateur des scopes en retard.

## 9.9 Budgets

Valeurs de départ à mesurer :

- aucun scope au-delà de 5000 candidats sans pagination/chunking explicite ;
- un seul job de reconstruction lourd simultané sur le VPS ;
- temps et CPU enregistrés ;
- abandon contrôlé si révision devenue obsolète ;
- limite de rétention de snapshots ;
- aucune recomputation déclenchée par lecture publique.

# 10. `FOUNDATION-ADR-07` — Jobs durables sur MongoDB autonome

## 10.1 Réalité technique

MongoDB autonome ne fournit pas de transaction atomique entre une mutation métier et l’insertion d’un document d’outbox dans une autre collection. Écrire « utiliser une outbox » sans traiter ce point créerait une garantie fictive.

Le système adopte donc :

- des écritures métier atomiques au niveau d’un document ;
- des jobs idempotents ;
- des révisions monotones dans les sources ;
- un réparateur périodique ;
- un worker .NET borné ;
- aucune dépendance à RabbitMQ, Kafka ou Redis pour le premier socle.

## 10.2 `DurableBackgroundJob`

```csharp
public sealed class DurableBackgroundJobDocument
{
    public string Id { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string NaturalKey { get; set; } = string.Empty;
    public int PayloadVersion { get; set; }
    public BsonDocument Payload { get; set; } = new();
    public string Status { get; set; } = string.Empty;
    public int AttemptCount { get; set; }
    public DateTime NotBeforeUtc { get; set; }
    public string? LeaseOwner { get; set; }
    public DateTime? LeaseExpiresAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public string? LastErrorCode { get; set; }
    public string? CorrelationId { get; set; }
}
```

## 10.3 États

- `Pending` ;
- `Leased` ;
- `Succeeded` ;
- `RetryScheduled` ;
- `DeadLetter` ;
- `Cancelled` ;
- `Superseded` pour un job coalescé remplacé par une révision plus récente.

## 10.4 Claim par lease

Le worker utilise un `FindOneAndUpdate` atomique :

- statut exécutable ;
- `NotBeforeUtc <= now` ;
- lease absente ou expirée ;
- ordre par priorité puis ancienneté ;
- pose `LeaseOwner` et `LeaseExpiresAtUtc` ;
- incrémente l’attempt seulement au démarrage réel.

Un process mort libère implicitement le job à expiration du lease.

## 10.5 Idempotence

Deux familles :

### Jobs exacts

Clé unique `(Kind, IdempotencyKey)`.

Exemples :

- générer un export précis ;
- envoyer un digest précis ;
- purger une visite précise.

### Jobs coalescibles

Clé naturelle `(Kind, NaturalKey)` avec `RequestedRevision` maximale.

Exemples :

- reconstruire `parks:global` ;
- recalculer les statistiques d’un utilisateur ;
- invalider un partage après modification source.

Une nouvelle demande met à jour la révision demandée au lieu d’empiler cent jobs.

## 10.6 Couplage avec la source

Lorsque la réaction ne peut pas être insérée atomiquement avec la source :

1. la source porte `ProcessingRevision` ou une révision métier monotone ;
2. la mutation réussit ;
3. l’application tente d’enregistrer le job ;
4. si l’étape 3 échoue, la réponse métier n’est pas annulée par une fausse transaction ;
5. un reconciler compare révision source et dernière révision traitée ;
6. il recrée le job manquant ;
7. le handler reste idempotent.

Pour les chemins critiques, un marqueur `PendingReactionRevision` peut être écrit dans le même document que la mutation.

## 10.7 Retry

- erreurs transitoires : backoff exponentiel borné + jitter ;
- erreurs de validation définitives : `DeadLetter` immédiat ;
- nombre maximal d’essais par kind ;
- message d’erreur minimisé ;
- pas de payload utilisateur sensible dans les logs ;
- possibilité admin de rejouer après correction ;
- même handler idempotent lors du replay.

## 10.8 Concurrence et ressources

Configuration VPS initiale :

- un worker lourd global ;
- un ou deux workers légers ;
- un seul snapshot de classement simultané ;
- exports limités ;
- batch bornés ;
- cancellation à l’arrêt ;
- aucune boucle de polling agressive ;
- intervalle adaptatif lorsque la file est vide ;
- métriques de backlog et âge du plus vieux job.

## 10.9 Types de jobs prévus

- `ratings.rebuild-scope` ;
- `passport.recalculate-user-stats` ;
- `passport.generate-export` ;
- `passport.purge-deleted-visit` ;
- `share.invalidate-publication` ;
- `watch.build-digest` ;
- `watch.deliver-email` ;
- `trip.refresh-source-facts` ;
- `history.build-snapshot` ;
- `live.ingest-source` seulement dans la phase LIVE.

Chaque kind possède :

- schéma payload versionné ;
- taille maximale ;
- politique de retry ;
- timeout ;
- concurrence ;
- métriques ;
- runbook ;
- politique de rétention.

## 10.10 Rétention

- `Succeeded` supprimés ou compactés après une durée courte ;
- `DeadLetter` conservés assez longtemps pour diagnostic ;
- aucune TTL sur l’audit métier nécessaire ;
- TTL autorisé sur les jobs purement techniques après export des métriques ;
- nettoyage lui-même idempotent et borné.

# 11. `FOUNDATION-ADR-08` — Cohérence, audit et réparation

## 11.1 État courant

L’état courant utile au produit réside dans les documents métier :

- préférence globale dans `UserRating` ;
- note de visite dans `Visit` ;
- note de ride dans `RideOccurrence` ;
- visibilité dans la publication ;
- rôle dans le participant de voyage.

Les lectures publiques ou privées ne reconstruisent pas l’état depuis l’audit.

## 11.2 Audit append-only

L’audit répond à :

- qui a modifié ;
- quel objet ;
- quelle révision ;
- quand ;
- par quelle origine ;
- avec quelle corrélation ;
- quelle nature de changement.

Il ne devient pas un Event Store complet.

## 11.3 Réparateurs

Réparateurs initiaux :

- visites sans statistiques à jour ;
- rides dont `VisitId`, `UserId` ou `ParkId` divergent ;
- jobs expirés en lease ;
- snapshots `Building` trop anciens ;
- pointeur vers snapshot absent ;
- publications dont `SourceVersion` a changé ;
- audits attendus manquants si un marqueur source existe.

Chaque réparateur :

- mode preview ;
- pagination ;
- limite ;
- idempotence ;
- journal ;
- métriques ;
- pas de correction destructrice silencieuse.

# 12. `FOUNDATION-ADR-09` — Matérialiser seulement après mesure

## 12.1 Principe

Le programme ne crée pas immédiatement :

- snapshot pour chaque statistique utilisateur ;
- cache permanent pour chaque filtre ;
- projection par catégorie, année, constructeur et pays pour tous les comptes ;
- pipeline analytique séparé ;
- data warehouse.

## 12.2 Progression

1. indexes adaptés ;
2. requêtes ciblées et paginées ;
3. baseline p50/p95, CPU, mémoire et documents examinés ;
4. cache privé court si utile ;
5. projection ciblée uniquement pour les lectures réellement lentes ou fréquentes ;
6. révision source et invalidation explicites ;
7. suppression de la projection si elle n’apporte pas de gain mesuré.

## 12.3 Critères de matérialisation

Une projection est justifiée si plusieurs critères sont réunis :

- p95 dépasse le budget après indexes ;
- calcul répété fréquemment ;
- coût CPU visible ;
- résultat stable entre mutations ;
- invalidation compréhensible ;
- stockage supportable ;
- valeur produit observée.

# 13. `FOUNDATION-ADR-10` — Livraison et réversibilité

## 13.1 Expand/contract

Pour chaque schéma :

1. ajouter les champs/collections/indexes ;
2. déployer un code capable de lire ancien et nouveau ;
3. commencer l’écriture nouvelle ;
4. backfill borné si nécessaire ;
5. vérifier intégrité et métriques ;
6. activer la lecture nouvelle par flag ;
7. conserver le fallback ;
8. retirer le fallback dans une PR distincte ;
9. supprimer l’ancien champ seulement après sauvegarde et délai.

## 13.2 Flags

Flags initiaux :

- `ratings:evidence` ;
- `ratings:canonicalSnapshots` ;
- `foundation:durableJobs` ;
- `passport:visits` ;
- `passport:rideOccurrences` ;
- `passport:temporalRatings` ;
- `passport:statistics`.

Chaque flag a une date de retrait et ne remplace pas une règle métier permanente.

## 13.3 Fallbacks sûrs

- classements : afficher moins, jamais réafficher un rang faible ;
- Passeport : conserver les données et désactiver les nouvelles écritures si nécessaire ;
- statistiques : revenir au calcul à la demande ;
- partage : suspendre la résolution plutôt qu’exposer une donnée ;
- jobs : arrêter un kind sans bloquer toute l’API ;
- audit : ne jamais supprimer l’état métier parce que l’audit est temporairement indisponible.

# 14. Impacts obligatoires sur les roadmaps fonctionnelles

## 14.1 `RANK`

- `RatingValue` devient le type commun ;
- l’éligibilité peut être livrée avant les snapshots ;
- les snapshots sont limités aux scopes canoniques ;
- reconstruction asynchrone coalescée ;
- pointeur atomique ;
- aucun filtre HTTP arbitraire matérialisé.

## 14.2 `PASS`

- exemples d’identifiants interprétés comme value objects chaîne ;
- une visite = session dans un parc, jour de service local lorsqu’il est connu ;
- fuseau sur `Visit` ;
- assessment actif embarqué ;
- audit séparé ;
- `SortPosition` long ;
- statistiques à la demande avant snapshot ;
- jobs/reconciler pour exports, purge et recalculs différés.

## 14.3 `SHARE`

- IDs opaques en chaîne ;
- snapshot hybride possède une version source ;
- invalidation par job coalescé ;
- révocation reste une écriture atomique directe, jamais dépendante d’un job.

## 14.4 `WATCH`

- événement factuel écrit après mutation ;
- distribution différée par jobs durables ;
- déduplication par clé naturelle ;
- réparateur des notifications manquantes ;
- pas de broker externe initial.

## 14.5 `TRIP`

- IDs chaîne typés ;
- ordres manuels sur positions espacées ;
- version optimiste ;
- rafraîchissement des faits différé ;
- aucune collaboration temps réel obligatoire.

## 14.6 `HIST`

- identifiants compatibles avec les entités existantes ;
- date partielle partagée avec des primitives cohérentes sans confondre visite et fait historique ;
- snapshots historiques calculés à la demande puis matérialisés seulement après mesure.

## 14.7 `LIVE`

- scheduler et jobs réutilisent le socle borné ;
- ingestion live ne doit pas saturer les jobs produit ;
- concurrence, quotas et kill switch propres ;
- historique uniquement selon droits et budget.

## 14.8 `QUAL`

- les ADR FOUNDATION sont une gate de conception ;
- CI vérifie tests de compatibilité, indexes, migrations et payload versions ;
- dashboards incluent backlog jobs, leases expirés, révisions en retard et renormalisations.

# 15. Gate `FOUNDATION-G`

Aucune PR de persistance `PASS` et aucune activation de snapshots `RANK` n’est généralisée tant que :

- [ ] le format d’identifiant est compatible avec les documents existants ;
- [ ] `RatingValue` possède ses tests de référence ;
- [ ] la notion de visite et de jour de service est figée ;
- [ ] les assessments actifs sont embarqués ou une exception contraire est documentée ;
- [ ] l’ordre des rides utilise une stratégie unique ;
- [ ] le registre des scopes canoniques est défini ;
- [ ] le worker durable et son modèle de lease sont validés avant les jobs critiques ;
- [ ] les limites de concurrence VPS sont configurées ;
- [ ] les chemins de réparation sont décrits ;
- [ ] les migrations suivent expand/contract ;
- [ ] les rollbacks n’exposent aucune donnée ni aucun classement faible ;
- [ ] les contrats restent additifs ;
- [ ] les nouvelles données privées sont exportables et supprimables.

# 16. Décisions explicitement rejetées à ce stade

- migration globale `string` vers `Guid` ;
- mélange durable `double`/`decimal` sans type domaine ;
- collection séparée pour chaque assessment un-à-un dès la V1 ;
- transaction multi-document supposée sur MongoDB autonome ;
- Event Sourcing complet ;
- broker de messages externe ;
- snapshot par filtre arbitraire ;
- LexoRank ou CRDT pour quelques centaines de rides ;
- matérialisation de toutes les statistiques ;
- conversion d’une date partielle en date exacte ;
- job lourd exécuté dans une requête publique ;
- retry infini ;
- audit contenant inutilement les commentaires privés complets.

# 17. Résultat attendu

À l’issue de cette fondation :

- le code peut ajouter `RANK` et `PASS` sans contredire les conventions actuelles ;
- les notes restent exactes et comparables ;
- les visites ont une sémantique temporelle exploitable ;
- les écritures principales restent atomiques sur MongoDB autonome ;
- les travaux différés sont durables mais proportionnés ;
- les classements ne créent pas une explosion de snapshots ;
- le VPS reste protégé par des limites explicites ;
- chaque évolution dispose d’un chemin de test, réparation et rollback.