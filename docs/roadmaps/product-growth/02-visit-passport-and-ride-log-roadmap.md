# Roadmap 02 — Passeport de visites, Ride Log et notes temporelles

> Code programme : `PASS`
>
> Dépendance bloquante : gate `RANK-G` de la roadmap 01.
>
> Périmètre : Web responsive uniquement. Les contrats sont conçus pour rester réutilisables par un futur client mobile, mais aucun fonctionnement hors ligne, GPS, push ou application native n’est engagé ici.
>
> Décision centrale : une note de visite ou de ride est une **observation personnelle temporelle**. Elle n’ajoute jamais automatiquement un vote supplémentaire au classement communautaire.

## 0. Avenant technique FOUNDATION

Les décisions suivantes sont figées avant les premières PR persistantes de cette roadmap. Elles prévalent sur les exemples illustratifs plus bas lorsqu’ils utilisent directement `Guid`, placent le fuseau dans `VisitDate` ou proposent des collections séparées pour les assessments actifs.

### 0.1 Identifiants

- les documents Mongo et DTO continuent d’utiliser des chaînes opaques ;
- le Core peut utiliser `VisitId`, `RideOccurrenceId` et autres value objects autour d’une chaîne normalisée ;
- aucune migration générale des IDs existants vers `Guid` ;
- Angular ne déduit jamais le format d’un identifiant.

### 0.2 Note exacte

- `RatingValue` représente de 1 à 10 demi-points ;
- les nouvelles notes temporelles persistent `valueHalfSteps` ;
- l’API peut continuer d’exposer `0.5`, `1.0`, ..., `5.0` ;
- la note globale historique en `double` est convertie par mapper validé ;
- aucune coexistence durable de règles `double` et `decimal` dans le domaine.

### 0.3 Définition d’une visite

Une visite est une session déclarée par un utilisateur dans un seul parc. Lorsqu’un jour est connu, elle est rattachée à un jour de service local. Deux parcs le même jour sont deux visites ; deux jours consécutifs dans le même parc sont deux visites ; plusieurs visites du même parc le même jour restent autorisées.

Le fuseau IANA appartient à `Visit`, pas à `VisitDate`. Une heure de ride exige une date précise au jour et un fuseau. Une ancienne visite connue seulement à l’année ou au mois ne reçoit jamais une heure ou une date UTC inventée.

### 0.4 Assessments actifs embarqués

Première version :

```text
Visit.parkAssessment
RideOccurrence.assessment
```

Les sous-documents contiennent valeur, commentaire privé facultatif, révision et timestamps. La création, modification ou suppression de l’assessment est atomique avec la version du parent sur MongoDB autonome.

L’audit append-only reste séparé. Les anciennes propositions `user-visit-park-assessments` et `user-ride-assessments` doivent être lues comme une option de séparation future conditionnée par une mesure, pas comme le stockage V1.

### 0.5 Ordre des rides

`RideOccurrence` utilise `SortPosition: long` avec un pas initial de 1024. L’affichage calcule le numéro après tri. Une insertion utilise l’espace entre deux positions ; lorsque cet espace est épuisé, les occurrences de la seule visite concernée sont renormalisées par bulk write borné et version optimiste.

### 0.6 Travaux différés

- création et modification courantes restent synchrones et atomiques ;
- exports, purges, statistiques matérialisées éventuelles et réparations utilisent les jobs Mongo à lease ;
- la source porte une révision monotone ;
- un reconciler recrée un job manquant ;
- aucun broker externe n’est introduit pour le premier socle ;
- les statistiques sont calculées à la demande avant toute matérialisation.

### 0.7 Gate préalable

`PASS-02` ne commence qu’après validation des décisions IDs, `RatingValue`, `VisitDate`, fuseau, documents, assessments embarqués, ordre et idempotence décrites dans les roadmaps FOUNDATION.

## 1. Vision produit

Le Passeport doit permettre à une personne de reconstruire et poursuivre son histoire de visite :

- parcs visités ;
- dates de visite, même lorsqu’elles sont partielles ou approximatives ;
- éléments faits, manqués, fermés ou volontairement ignorés ;
- nombre exact ou approximatif de rides ;
- note du parc à chaque visite ;
- note de chaque occurrence d’une attraction lorsque la personne souhaite ce niveau de précision ;
- évolution de son appréciation dans le temps ;
- préférence globale actuelle, distincte de l’historique ;
- statistiques personnelles vérifiables ;
- export et suppression complets ;
- partage seulement dans une roadmap ultérieure.

La promesse fonctionnelle est :

> « Je peux retrouver ce que j’ai réellement vécu, voir comment mon opinion évolue et corriger mon historique sans fausser la voix de la communauté. »

## 2. Objectifs

- Créer un agrégat `Visit` robuste et versionné.
- Enregistrer zéro, une ou plusieurs occurrences pour chaque élément visité.
- Permettre une note de parc par visite.
- Permettre une note facultative par occurrence de ride.
- Permettre un mode simplifié où plusieurs rides sont saisis sans note individuelle.
- Conserver les cibles fermées, renommées ou devenues invisibles dans l’historique du propriétaire.
- Gérer dates complètes, dates partielles et dates approximatives.
- Produire des statistiques par cible, visite, année, parc, catégorie et période.
- Ne calculer une tendance que lorsque le volume la rend raisonnablement interprétable.
- Offrir une saisie rétrospective rapide et une saisie détaillée.
- Garantir idempotence, concurrence, audit, export, effacement et reprise après erreur.
- Préparer le rattachement futur de brouillons anonymes sans en faire un prérequis de la première version.

## 3. Non-objectifs

La première livraison ne couvre pas :

- localisation en temps réel ;
- détection automatique d’entrée dans un parc ;
- chronométrage automatique d’une file ;
- temps d’attente live ;
- publication automatique d’une visite ;
- fil social ;
- albums photo ;
- badges ou séries quotidiennes ;
- classement des utilisateurs par nombre de rides ;
- import direct depuis un service tiers sans contrat et consentement ;
- recommandation générative ;
- calcul d’une note communautaire à partir de toutes les occurrences.

## 4. Séparation des modèles de note

### 4.1 Préférence globale courante

L’entité existante `UserRating` reste la réponse à :

> « Globalement, aujourd’hui, quelle note veux-tu donner à ce parc ou cet élément ? »

Invariants :

- une seule note courante par `(UserId, TargetType, TargetId)` ;
- modification explicite ;
- alimente au maximum une fois l’agrégat communautaire ;
- peut être publique via le classement personnel si le propriétaire l’autorise ;
- peut exister sans aucune visite enregistrée.

### 4.2 Note de parc pour une visite

Répond à :

> « Quelle note donnes-tu à ce parc pour cette visite précise ? »

Invariants :

- zéro ou une note active par visite ;
- liée au parc de la visite ;
- historisée lorsqu’elle est corrigée ;
- privée par défaut ;
- utilisée dans les statistiques personnelles temporelles ;
- n’alimente pas directement l’agrégat communautaire.

### 4.3 Note d’une occurrence de ride

Répond à :

> « Quelle note donnes-tu à cette expérience précise, ce jour-là et à ce tour-là ? »

Invariants :

- zéro ou une note active par occurrence ;
- l’occurrence appartient à la visite ;
- l’élément appartient au parc de la visite à la date enregistrée, ou une exception historique est explicitement reconnue ;
- une correction conserve un audit ;
- privée par défaut ;
- n’alimente pas directement l’agrégat communautaire.

### 4.4 Proposition d’actualisation de la préférence globale

Après une visite, le système peut comparer :

- la note globale courante ;
- la dernière note de visite ou de ride ;
- la moyenne personnelle récente ;
- la médiane historique.

Il peut afficher une suggestion non intrusive :

> « Ta note globale est 4,5. Tes trois dernières expériences donnent une moyenne de 3,8. Souhaites-tu revoir ta note globale ? »

Règles :

- aucune modification automatique ;
- aucune suggestion avec moins de deux nouvelles observations depuis la dernière modification globale ;
- aucune suggestion répétée à chaque page ;
- fréquence plafonnée ;
- possibilité de désactiver ;
- raison affichée ;
- événement analytics sans stocker les valeurs exactes si cela n’est pas nécessaire.

## 5. Modèle de domaine

## 5.1 Agrégat `Visit`

Proposition :

```csharp
public sealed class Visit
{
    public Guid Id { get; }
    public Guid UserId { get; }
    public Guid ParkId { get; }
    public VisitDate Date { get; private set; }
    public VisitStatus Status { get; private set; }
    public string? Title { get; private set; }
    public string? PrivateNote { get; private set; }
    public VisitPrivacy Privacy { get; private set; }
    public int Version { get; private set; }
    public DateTime CreatedAtUtc { get; }
    public DateTime UpdatedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
}
```

L’agrégat porte les invariants de la visite ; les occurrences peuvent être persistées séparément pour éviter un document Mongo sans borne.

### 5.1.1 `VisitStatus`

- `Draft` : visite en cours de saisie ou incomplète ;
- `Completed` : l’utilisateur considère la saisie terminée ;
- `Archived` : conservée mais retirée des vues courantes ;
- `Deleted` n’est pas un statut métier public : utiliser suppression logique temporaire ou tombstone uniquement pour les besoins de synchronisation/audit, puis purge.

Transitions :

```text
Draft -> Completed
Completed -> Draft       correction volontaire
Draft -> Archived
Completed -> Archived
Archived -> Draft/Completed
Any -> deletion workflow
```

Chaque transition vérifie le propriétaire et la version optimiste.

### 5.1.2 `VisitPrivacy`

Première version :

- `Private` uniquement dans cette roadmap.

Préparer sans activer :

- `Unlisted` ;
- `Public`.

Ne pas ajouter un booléen `IsPublic` partout : les niveaux futurs nécessitent un type explicite.

## 5.2 Valeur `VisitDate`

Les utilisateurs ne connaissent pas toujours la date exacte d’une ancienne visite. Le domaine doit préserver l’incertitude au lieu d’inventer le 1er janvier.

```csharp
public sealed record VisitDate(
    int Year,
    int? Month,
    int? Day,
    VisitDatePrecision Precision,
    bool IsApproximate,
    string? TimeZoneId);
```

`VisitDatePrecision` :

- `Year` ;
- `Month` ;
- `Day`.

Règles :

- `Day` exige mois et jour valides ;
- `Month` exige mois, interdit jour ;
- `Year` interdit mois et jour ;
- `IsApproximate` signale « vers cette date » ;
- le fuseau n’est requis que pour des heures ;
- aucune conversion en `DateTime` minuit UTC comme source de vérité ;
- tri : dates précises d’abord dans leur période, puis dates partielles avec une convention documentée ;
- affichage localisé respectant la précision.

## 5.3 `VisitParkAssessment`

```csharp
public sealed class VisitParkAssessment
{
    public Guid Id { get; }
    public Guid VisitId { get; }
    public Guid UserId { get; }
    public Guid ParkId { get; }
    public decimal Value { get; private set; }
    public string? PrivateComment { get; private set; }
    public DateTime CreatedAtUtc { get; }
    public DateTime UpdatedAtUtc { get; private set; }
    public int Revision { get; private set; }
}
```

Index unique sur `VisitId`. Le `ParkId` est redondant volontairement pour contrôle, requêtes et intégrité ; il doit correspondre à celui de la visite.

## 5.4 `RideOccurrence`

Le modèle explicite une occurrence réelle, car une note par ride exige de distinguer les tours.

```csharp
public sealed class RideOccurrence
{
    public Guid Id { get; }
    public Guid VisitId { get; }
    public Guid UserId { get; }
    public Guid ParkId { get; }
    public Guid ParkItemId { get; }
    public int Sequence { get; private set; }
    public OccurrenceMoment Moment { get; private set; }
    public RideOccurrenceStatus Status { get; private set; }
    public RideLogSource Source { get; }
    public string? PrivateNote { get; private set; }
    public DateTime CreatedAtUtc { get; }
    public DateTime UpdatedAtUtc { get; private set; }
    public int Version { get; private set; }
}
```

### 5.4.1 `RideOccurrenceStatus`

- `Completed` : expérience réellement faite ;
- `Attempted` : tentative interrompue ou évacuation, facultatif après validation métier ;
- `MissedClosed` : élément voulu mais fermé ;
- `MissedUnavailable` : indisponibilité non qualifiée ;
- `SkippedByChoice` : volontairement non fait ;
- `Planned` est réservé à une future liste de journée et ne doit pas être mélangé à l’historique accompli.

Seul `Completed` compte dans le nombre de rides. Les autres statuts peuvent alimenter les statistiques « manqué/fermé » mais jamais un compteur de rides.

### 5.4.2 `OccurrenceMoment`

Une occurrence ancienne peut n’avoir aucune heure connue.

```csharp
public sealed record OccurrenceMoment(
    TimeOnly? LocalTime,
    int? ManualOrder,
    bool IsApproximate);
```

Règles :

- heure facultative ;
- `ManualOrder` permet de reconstruire l’ordre sans fausse heure ;
- au moins l’ordre ou la séquence technique est disponible ;
- l’heure locale n’est convertie en UTC que si le fuseau de la visite est connu ;
- ne pas afficher une heure fabriquée.

### 5.4.3 Saisie groupée de plusieurs rides

Le raccourci « j’ai fait cette attraction 5 fois » présente deux options :

1. créer cinq occurrences non notées ;
2. créer cinq occurrences et appliquer une même note **uniquement après confirmation explicite**.

Ne pas persister seulement `Count = 5` si l’utilisateur veut des statistiques par ride. Un compteur agrégé perd l’identité de chaque occurrence, empêche une correction ciblée et rend ambiguë une note unique.

Pour les imports volumineux, l’API peut accepter `count`, mais le cas d’usage crée les occurrences de manière idempotente avec une limite maximale par commande.

## 5.5 `RideAssessment`

```csharp
public sealed class RideAssessment
{
    public Guid Id { get; }
    public Guid RideOccurrenceId { get; }
    public Guid VisitId { get; }
    public Guid UserId { get; }
    public Guid ParkItemId { get; }
    public decimal Value { get; private set; }
    public string? PrivateComment { get; private set; }
    public DateTime CreatedAtUtc { get; }
    public DateTime UpdatedAtUtc { get; private set; }
    public int Revision { get; private set; }
}
```

Index unique sur `RideOccurrenceId`.

## 5.6 Audit des corrections

Les objets métier exposent leur état courant ; un journal append-only conserve les corrections sensibles :

```text
VisitCreated
VisitDateChanged
VisitCompleted
VisitReopened
VisitArchived
VisitDeleted
ParkAssessmentCreated
ParkAssessmentChanged
ParkAssessmentDeleted
RideOccurrenceAdded
RideOccurrenceChanged
RideOccurrenceDeleted
RideAssessmentCreated
RideAssessmentChanged
RideAssessmentDeleted
```

Le journal contient : identifiant d’action, utilisateur, entité, ancienne/nouvelle valeur minimisée, date UTC, raison facultative, corrélation et origine. Il n’est pas exposé publiquement.

Une architecture Event Sourcing complète n’est pas nécessaire. Un audit transactionnel après succès de persistance suffit si sa cohérence est garantie.

## 6. Invariants métier détaillés

### 6.1 Propriété

- seul le propriétaire peut lire ou modifier tant que la visite est privée ;
- un administrateur technique ne dispose pas d’une route publique générique permettant de parcourir les journaux ;
- toute consultation de support exceptionnelle est tracée et encadrée ;
- les identifiants utilisateur ne viennent jamais du payload public.

### 6.2 Cohérence parc/élément

À la création d’une occurrence :

- le parc existe ;
- l’élément existe ou est résolu comme élément historique connu ;
- l’élément est ou a été rattaché au parc ;
- la date de visite est compatible avec les dates d’ouverture/fermeture lorsque celles-ci sont connues ;
- si la compatibilité historique est inconnue, l’utilisateur est averti mais peut conserver l’entrée avec `HistoricalConsistency = Unverified` ;
- une incohérence certaine nécessite une confirmation renforcée ou bloque selon le cas.

### 6.3 Cycle de vie des cibles

Une attraction fermée, renommée ou masquée après une visite :

- reste visible dans le Passeport du propriétaire ;
- conserve le nom pertinent actuel et, si disponible, le nom à la date de visite ;
- affiche son statut ;
- ne produit pas de 404 dans le journal privé ;
- utilise un snapshot minimal de présentation seulement si l’entité source peut être supprimée juridiquement ;
- ne réécrit pas l’historique lors d’un changement de catégorie.

### 6.4 Suppression d’une cible par l’administration

Avant suppression physique d’un parc ou élément référencé :

- vérifier les dépendances de visites ;
- préférer un état retiré/invisible ;
- si suppression obligatoire, conserver un `HistoricalTargetReference` minimal et non éditorial ;
- journaliser la transformation ;
- ne jamais supprimer silencieusement les occurrences utilisateur.

### 6.5 Valeurs de note

Réutiliser la même échelle que `RatingScoreCalculator` :

- minimum `0.5` ;
- maximum `5.0` ;
- pas `0.5` ;
- validation Core unique ;
- affichage localisé ;
- aucun zéro servant à représenter « non noté ».

### 6.6 Idempotence

Toutes les commandes de création rapide ou groupée acceptent un `Idempotency-Key` ou `ClientOperationId` :

- unique par utilisateur et type d’opération ;
- résultat rejouable pendant une durée définie ;
- même clé + même payload retourne le résultat initial ;
- même clé + payload différent retourne un conflit ;
- évite les doubles rides après double clic ou retry réseau.

### 6.7 Concurrence

- `Version` incrémentée sur `Visit` et `RideOccurrence` ;
- `If-Match` ou version dans la commande ;
- conflit `409` avec état courant minimal ;
- pas de last-write-wins silencieux ;
- opérations groupées transactionnelles lorsque possible ;
- sinon résultat détaillé par item et reprise idempotente.

## 7. Parcours utilisateur Web

## 7.1 Première entrée dans le Passeport

Depuis :

- le profil ;
- une fiche parc ;
- une fiche élément ;
- le classement personnel ;
- un futur récapitulatif partagé.

Écran d’introduction :

- bénéfice concret ;
- confidentialité par défaut ;
- distinction note globale / note par visite ;
- export et suppression ;
- possibilité de commencer par une seule visite.

## 7.2 Créer une visite depuis une fiche parc

Flux court :

1. bouton « Ajouter une visite » ;
2. date exacte, mois, année ou « approximative » ;
3. choix `aujourd’hui` uniquement si réellement souhaité, jamais prérempli pour une visite passée ;
4. création du brouillon ;
5. redirection vers la saisie des éléments ;
6. note du parc facultative ;
7. terminer plus tard possible.

Critère : création en moins de trois actions après ouverture du formulaire pour une visite datée du jour.

## 7.3 Saisie rétrospective rapide

Pour un ancien parc :

- liste par zone/catégorie ;
- recherche ;
- sélection multiple ;
- statuts `fait`, `fermé`, `manqué`, `ignoré` ;
- nombre de rides facultatif ;
- une note globale par élément facultative pour la visite ;
- possibilité de détailler ensuite les occurrences individuellement.

### Décision sur la « note par élément pour la visite »

Pour éviter d’obliger à noter cinq occurrences identiques une par une, introduire une vue de saisie qui peut :

- appliquer une note à toutes les occurrences créées lors de la même action ;
- ou créer une **appréciation de visite par élément** distincte des rides.

La deuxième option ajoute un troisième concept de note et risque de confondre l’utilisateur. **Décision initiale recommandée :** ne pas créer d’entité supplémentaire. L’interface propose d’appliquer une même note aux occurrences sélectionnées, puis permet de les différencier.

## 7.4 Saisie détaillée

Dans le détail d’une visite :

- timeline ordonnée ;
- ajout d’une occurrence ;
- heure facultative ;
- note facultative ;
- commentaire privé ;
- duplication « refaire cette attraction » ;
- déplacement dans l’ordre ;
- correction ou suppression ;
- compteur par élément ;
- filtre noté/non noté ;
- indication claire des données approximatives.

## 7.5 Modifier une visite terminée

- l’utilisateur ouvre explicitement la correction ;
- la visite passe de `Completed` à `Draft` ou reste `Completed` avec mode édition selon arbitrage UX ;
- les statistiques concernées sont marquées à recalculer ;
- la correction n’est pas présentée comme une nouvelle visite ;
- le journal conserve l’opération ;
- les partages futurs sont invalidés si leurs données changent.

## 7.6 Supprimer

Deux niveaux :

- supprimer une occurrence ;
- supprimer toute la visite.

Avant suppression de visite :

- annoncer le nombre d’occurrences et de notes supprimées ;
- proposer export ;
- confirmation explicite ;
- tombstone courte pour reprise/synchronisation future ;
- purge après délai documenté ;
- recalcul ciblé ;
- révocation des futurs partages.

## 8. Brouillons anonymes et conversion de compte

Ce volet est facultatif après le socle authentifié, mais doit être anticipé.

### 8.1 Stockage local

- IndexedDB, pas `localStorage` pour un journal volumineux ;
- schéma versionné ;
- aucune donnée envoyée avant consentement et création de compte ;
- avertissement sur la dépendance à l’appareil/navigateur ;
- export local ;
- purge manuelle.

### 8.2 Rattachement après inscription

- afficher le nombre de visites et rides détectés ;
- consentement explicite ;
- import idempotent avec `ClientOperationId` ;
- résolution des doublons par date/parc sans fusion automatique destructive ;
- rapport final ;
- ne purger le brouillon local qu’après accusé de réception serveur vérifié.

### 8.3 Conflits

Si une visite similaire existe :

- garder séparé ;
- fusionner après aperçu ;
- ignorer l’import ;
- comparer occurrences et notes ;
- aucune fusion basée uniquement sur le même jour et le même parc.

## 9. Persistance MongoDB

## 9.1 Collections proposées

- `user-visits`, avec `parkAssessment` actif embarqué ;
- `user-ride-occurrences`, avec `assessment` actif embarqué ;
- `user-visit-audit-events` ou intégration à l’audit existant ;
- `durable-background-jobs` pour exports, purges et recalculs réellement différés ;
- `user-passport-stat-snapshots` seulement après besoin mesuré.

Séparer les occurrences évite qu’une visite très riche dépasse une taille de document raisonnable et facilite les requêtes temporelles par cible. Les assessments actifs un-à-un restent embarqués dans leur parent afin d’éviter les écritures multi-documents et les orphelins sur MongoDB autonome. Une séparation future suit expand/contract et exige un besoin mesuré.

<!-- FOUNDATION: embedded-active-assessments -->

## 9.2 Indexes `user-visits`

- unique `{ UserId: 1, Id: 1 }` ou `_id` + contrôle propriétaire ;
- `{ UserId: 1, "Date.Year": -1, "Date.Month": -1, "Date.Day": -1 }` ;
- `{ UserId: 1, ParkId: 1, "Date.Year": -1 }` ;
- `{ UserId: 1, Status: 1, UpdatedAtUtc: -1 }` ;
- index partiel pour brouillons récents si besoin ;
- pas d’unicité `(UserId, ParkId, Date)` : plusieurs visites le même jour sont possibles.

## 9.3 Indexes `user-ride-occurrences`

- unique `{ UserId: 1, Id: 1 }` ;
- unique `{ VisitId: 1, Sequence: 1 }` avec stratégie de réordonnancement ;
- `{ UserId: 1, ParkItemId: 1, VisitId: 1 }` ;
- `{ UserId: 1, ParkItemId: 1, CreatedAtUtc: -1 }` ;
- `{ VisitId: 1, Status: 1 }` ;
- `{ UserId: 1, ParkId: 1, VisitId: 1 }`.

Pour éviter de réécrire toutes les séquences lors d’une insertion, utiliser des positions espacées ou un ordre décimal contrôlé, puis normalisation ponctuelle. Ne pas exposer ce détail dans le domaine public.

## 9.4 Indexes des évaluations

`user-visit-park-assessments` :

- unique `{ VisitId: 1 }` ;
- `{ UserId: 1, ParkId: 1, VisitId: 1 }` ;
- `{ UserId: 1, ParkId: 1, UpdatedAtUtc: -1 }`.

`user-ride-assessments` :

- unique `{ RideOccurrenceId: 1 }` ;
- `{ UserId: 1, ParkItemId: 1, VisitId: 1 }` ;
- `{ UserId: 1, ParkItemId: 1, UpdatedAtUtc: -1 }`.

## 9.5 Transactions et cohérence

Si MongoDB fonctionne en replica set et supporte les transactions :

- création occurrence + assessment éventuel + audit dans une transaction courte ;
- suppression visite par lots bornés, pas transaction géante ;
- publication d’événement après commit.

Sinon :

- outbox ou marqueurs d’état ;
- opérations idempotentes ;
- réparateur de cohérence ;
- diagnostics admin ;
- aucune promesse atomique non tenue.

La roadmap d’implémentation doit confirmer le mode Mongo de production avant de choisir.

## 10. Ports Application

Interfaces orientées capacités :

```csharp
IUserVisitRepository
IRideOccurrenceRepository
IVisitAssessmentRepository
IRideAssessmentRepository
IVisitTargetResolver
IVisitStatisticsReader
IVisitExportWriter
IVisitAuditWriter
IIdempotencyStore
IPassportClock
```

Éviter :

- `IGenericRepository<T>` ;
- accès Mongo depuis les handlers ;
- service `PassportService` omnipotent ;
- DTO WebAPI dans le Core ;
- calcul statistique dans les composants Angular.

## 11. Cas d’usage Application

### 11.1 Visites

- `CreateVisitCommand` ;
- `UpdateVisitMetadataCommand` ;
- `CompleteVisitCommand` ;
- `ReopenVisitCommand` ;
- `ArchiveVisitCommand` ;
- `DeleteVisitCommand` ;
- `GetVisitQuery` ;
- `ListUserVisitsQuery` ;
- `GetParkVisitHistoryQuery`.

### 11.2 Occurrences

- `AddRideOccurrenceCommand` ;
- `AddRideOccurrencesBatchCommand` ;
- `UpdateRideOccurrenceCommand` ;
- `ReorderRideOccurrenceCommand` ;
- `DeleteRideOccurrenceCommand` ;
- `ListVisitOccurrencesQuery` ;
- `GetTargetOccurrenceHistoryQuery`.

### 11.3 Évaluations temporelles

- `UpsertVisitParkAssessmentCommand` ;
- `DeleteVisitParkAssessmentCommand` ;
- `UpsertRideAssessmentCommand` ;
- `DeleteRideAssessmentCommand` ;
- `GetTargetAssessmentTimelineQuery` ;
- `GetGlobalRatingUpdateSuggestionQuery`.

### 11.4 Passeport

- `GetPassportOverviewQuery` ;
- `GetPassportParkSummaryQuery` ;
- `GetPassportTargetSummaryQuery` ;
- `ExportPassportCommand/Query` ;
- `DeletePassportDataCommand` ;
- `ImportPassportDraftCommand` ultérieur.

## 12. API Web proposée

Préfixe possible : `/api/me/passport` pour rendre la propriété explicite.

### 12.1 Visites

```text
POST   /api/me/passport/visits
GET    /api/me/passport/visits
GET    /api/me/passport/visits/{visitId}
PATCH  /api/me/passport/visits/{visitId}
POST   /api/me/passport/visits/{visitId}/complete
POST   /api/me/passport/visits/{visitId}/reopen
POST   /api/me/passport/visits/{visitId}/archive
DELETE /api/me/passport/visits/{visitId}
```

Filtres : parc, année, statut, présence de notes, pagination cursorisée.

### 12.2 Occurrences

```text
POST   /api/me/passport/visits/{visitId}/occurrences
POST   /api/me/passport/visits/{visitId}/occurrences:batch
GET    /api/me/passport/visits/{visitId}/occurrences
PATCH  /api/me/passport/visits/{visitId}/occurrences/{occurrenceId}
DELETE /api/me/passport/visits/{visitId}/occurrences/{occurrenceId}
POST   /api/me/passport/visits/{visitId}/occurrences:reorder
```

### 12.3 Notes temporelles

```text
PUT    /api/me/passport/visits/{visitId}/assessment
DELETE /api/me/passport/visits/{visitId}/assessment
PUT    /api/me/passport/occurrences/{occurrenceId}/assessment
DELETE /api/me/passport/occurrences/{occurrenceId}/assessment
```

### 12.4 Statistiques

```text
GET /api/me/passport/summary
GET /api/me/passport/parks/{parkId}/stats
GET /api/me/passport/items/{itemId}/stats
GET /api/me/passport/items/{itemId}/timeline
GET /api/me/passport/years/{year}/summary
GET /api/me/passport/rating-update-suggestions
```

### 12.5 Export et suppression

```text
POST /api/me/passport/exports
GET  /api/me/passport/exports/{exportId}
POST /api/me/passport/deletion-preview
DELETE /api/me/passport
```

Un export volumineux peut être généré par job avec durée de conservation courte et téléchargement authentifié. Ne jamais envoyer un lien permanent contenant un secret dans les logs.

## 13. Contrats essentiels

### 13.1 Création de visite

```json
{
  "parkId": "uuid",
  "date": {
    "year": 2026,
    "month": 8,
    "day": 27,
    "precision": "Day",
    "isApproximate": false,
    "timeZoneId": "Europe/Paris"
  },
  "title": null,
  "clientOperationId": "uuid"
}
```

### 13.2 Batch de rides

```json
{
  "operations": [
    {
      "parkItemId": "uuid",
      "status": "Completed",
      "count": 3,
      "sharedAssessmentValue": 4.5,
      "localTime": null,
      "isTimeApproximate": true,
      "clientOperationId": "uuid"
    }
  ]
}
```

Réponse :

- occurrences créées ;
- opérations déjà appliquées ;
- erreurs par entrée ;
- nouvelle version de visite ;
- statistiques marquées obsolètes ou recalculées.

### 13.3 Timeline d’une cible

Chaque point contient :

- visite ;
- date avec précision ;
- occurrence ;
- note ;
- ordre ;
- statut de la cible à cette date si connu ;
- indicateur d’approximation ;
- aucune donnée d’un autre utilisateur.

## 14. Statistiques personnelles

## 14.1 Principes

- calculer à partir des observations actives, pas de l’audit ;
- exclure les occurrences non `Completed` des compteurs de rides ;
- distinguer `rideCount`, `ratedRideCount` et `visitCount` ;
- afficher le dénominateur ;
- arrondir pour présentation seulement ;
- documenter le traitement des dates partielles ;
- ne pas conclure à une tendance avec trop peu de points ;
- ne pas comparer automatiquement des catégories incompatibles.

## 14.2 Statistiques par élément

Minimum :

- nombre total de rides ;
- nombre de visites distinctes ;
- nombre de rides notés ;
- taux de couverture des notes ;
- première expérience ;
- dernière expérience ;
- moyenne arithmétique ;
- médiane ;
- minimum ;
- maximum ;
- écart-type population ou échantillon — choix documenté ;
- note globale actuelle ;
- différence entre note globale et moyenne historique ;
- moyenne par visite ;
- moyenne par année ;
- série chronologique.

### 14.2.1 Tendance

Première version prudente :

- aucune tendance sous `3` évaluations réparties sur au moins `2` visites ;
- afficher seulement `stable`, `en hausse`, `en baisse` si la différence entre la moyenne des premières et dernières fenêtres dépasse un seuil, par exemple `0.5` ;
- toujours afficher les points bruts ;
- ne pas inférer une causalité ;
- libellé « ton appréciation récente est plus élevée » plutôt que « l’attraction s’est améliorée ».

Une régression linéaire ou un intervalle de confiance peut être étudié plus tard, mais ne doit pas être introduit pour donner une apparence scientifique avec quatre observations.

## 14.3 Statistiques par parc

- nombre de visites ;
- années visitées ;
- note de parc par visite ;
- moyenne et médiane des visites notées ;
- rides totaux dans le parc ;
- éléments distincts faits ;
- éléments refaits ;
- éléments marqués fermés/manqués ;
- couverture par catégorie ;
- évolution de la note de parc ;
- top personnel actuel fondé sur `UserRating`, clairement séparé du top historique moyen.

## 14.4 Statistiques transverses

Après validation du socle :

- par année ;
- pays/région ;
- catégorie et type ;
- constructeur ;
- parc ;
- nouveautés pour l’utilisateur ;
- éléments disparus visités ;
- distribution des notes ;
- nombre de visites avec date approximative.

Toute agrégation dépend des données de référence au moment de la consultation. Pour éviter que l’historique change silencieusement lors d’une recatégorisation, documenter si la statistique utilise la catégorie actuelle ou historique. Choix recommandé : catégorie historique lorsque disponible, sinon catégorie actuelle avec indicateur.

## 14.5 Calcul synchrone ou matérialisé

### Première version

- agrégations Mongo ciblées par utilisateur et cible ;
- pagination ;
- cache privé court ;
- mesure de latence.

### Matérialisation ultérieure si nécessaire

`UserPassportStatSnapshot` :

- scope ;
- user ;
- target ;
- source revision ;
- generated at ;
- statistiques ;
- version de formule.

Recalcul ciblé après événement. Ne pas matérialiser prématurément des centaines de combinaisons inutilisées.

## 15. Interface Angular

Structure proposée :

```text
features/profile/passport/
  pages/
    passport-overview-page/
    visits-page/
    visit-detail-page/
    park-history-page/
    target-history-page/
    passport-settings-page/
  components/
    visit-date-editor/
    visit-item-selector/
    ride-occurrence-editor/
    temporal-rating-editor/
    rating-timeline/
    passport-stat-card/
    passport-table/
  state/
    passport-overview.facade.ts
    visit-editor.facade.ts
    target-history.facade.ts
  data-access/
  models/
  utils/
```

Règles :

- composants de présentation sans logique statistique ;
- façades responsables de l’orchestration ;
- formulaires réactifs typés ;
- URL stable pour chaque visite privée ;
- guards d’authentification ;
- aucune donnée privée dans TransferState public ou HTML SSR cacheable ;
- écrans privés rendus en CSR/noindex selon la politique existante ;
- erreurs avec reprise locale du formulaire ;
- loaders ciblés, pas écran bloqué pour une modification mineure.

## 16. Accessibilité et ergonomie

- ajout de ride possible au clavier ;
- boutons d’incrément avec libellés explicites ;
- aucune étoile seule sans valeur textuelle ;
- timeline lisible comme liste ;
- graphiques complétés par tableaux ;
- contraste et focus visibles ;
- confirmation de suppression accessible ;
- date partielle utilisable sans date picker obligatoire ;
- gros volumes virtualisés sans casser lecteur d’écran ;
- aucun swipe obligatoire ;
- textes dans les huit langues ;
- unités et dates via services de localisation existants.

## 17. Sécurité et confidentialité

- toutes les routes sous authentification ;
- contrôle propriétaire en Application, pas seulement dans le contrôleur ;
- éviter les identifiants séquentiels prédictibles ;
- rate limits sur batch/import/export ;
- limite de taille des notes privées ;
- sanitisation à l’affichage, même pour contenu privé ;
- pas d’indexation ;
- pas de cache partagé ;
- logs sans commentaire privé ni note exacte ;
- export chiffré en transit et lien court ;
- suppression complète vérifiable ;
- données de visite intégrées aux réponses RGPD ;
- consentement séparé pour futurs partages.

## 18. Export

Formats initiaux :

- JSON canonique et versionné ;
- CSV séparés `visits`, `ride-occurrences`, `visit-assessments`, `ride-assessments` ;
- métadonnées de schéma ;
- timestamps UTC et valeurs locales/précisions ;
- identifiants de cible, noms de confort et statuts ;
- pas de dépendance à l’interface pour relire les données.

Le JSON doit permettre un futur réimport, mais l’import complet nécessite une roadmap de mapping et de déduplication avant activation.

## 19. Migration depuis les notes actuelles

### Interdit

- créer une visite à la date `CreatedAtUtc` de `UserRating` ;
- considérer la date de création du compte comme date d’expérience ;
- dupliquer la note courante dans chaque visite ;
- modifier l’agrégat communautaire.

### Autorisé

Dans le Passeport :

- afficher « note globale existante » ;
- proposer « ajouter une visite correspondant à cette expérience » ;
- laisser l’utilisateur choisir la date et le parc ;
- proposer de recopier la note globale dans la nouvelle observation après confirmation ;
- conserver les deux concepts clairement étiquetés.

## 20. Gestion des imports tiers futurs

Préparer un format d’adaptateur, sans implémenter de scraping :

- source déclarée ;
- fichier fourni par l’utilisateur ;
- droits et conditions vérifiés ;
- mapping explicite des parcs/éléments ;
- score de confiance ;
- aperçu avant import ;
- journal ;
- idempotence ;
- possibilité d’annuler le lot importé ;
- aucune association automatique ambiguë par simple nom.

## 21. Observabilité

### Technique

- latence CRUD ;
- erreurs de concurrence ;
- doublons idempotents ;
- taille des batchs ;
- temps d’agrégation ;
- documents orphelins détectés ;
- exports échoués ;
- volume par collection et index.

### Produit

Événements minimaux :

- `passport_opened` ;
- `visit_created` ;
- `visit_completed` ;
- `occurrence_added` avec catégorie mais sans identifiant personnel si non nécessaire ;
- `temporal_rating_added` sans valeur exacte ;
- `target_timeline_opened` ;
- `second_visit_recorded` ;
- `passport_export_requested` ;
- `visit_deleted` ;
- `global_rating_update_suggestion_accepted/dismissed`.

Ne pas collecter l’intégralité du journal dans un outil analytics tiers.

## 22. Tests obligatoires

### Core

- toutes précisions de date ;
- années bissextiles ;
- date future autorisée ou interdite selon statut — décision explicite ;
- transitions d’état ;
- cohérence parc ;
- valeurs de note ;
- plusieurs visites même jour ;
- plusieurs occurrences identiques ;
- statuts non accomplis ;
- ordre sans heure ;
- corrections et versions.

### Application

- propriétaire/non-propriétaire ;
- création idempotente ;
- batch partiel ;
- conflit de version ;
- cible fermée ;
- cible historiquement incohérente ;
- suppression et recalcul ;
- note globale inchangée ;
- suggestion d’actualisation ;
- export.

### Infrastructure

- indexes ;
- documents volumineux ;
- transactions ou réparation ;
- reprise de batch ;
- audit ;
- suppression en cascade bornée ;
- agrégations de référence ;
- concurrence.

### WebAPI

- contrats OpenAPI ;
- auth ;
- `404`, `409`, `422`, Problem Details ;
- idempotency ;
- pagination ;
- limites de payload ;
- aucune fuite cross-user.

### Angular

- création rapide ;
- date partielle ;
- sélection multiple ;
- ajout de 1 et N rides ;
- application d’une note partagée ;
- correction ;
- navigation avant sauvegarde ;
- reprise après erreur ;
- responsive ;
- accessibilité ;
- huit langues ;
- graphiques et tableaux cohérents.

### End-to-end

Scénarios de référence :

1. créer une visite, ajouter cinq attractions, terminer ;
2. ajouter trois rides du même élément avec trois notes différentes ;
3. vérifier moyenne/médiane ;
4. modifier la deuxième note ;
5. vérifier recalcul ciblé ;
6. supprimer une occurrence ;
7. vérifier que la note communautaire globale et le classement n’ont pas changé ;
8. exporter ;
9. supprimer la visite ;
10. vérifier purge, statistiques et futurs partages.

## 23. Performance et budgets

Valeurs initiales à mesurer et fixer :

- liste de 50 visites sous 500 ms API hors réseau ;
- ouverture d’une visite de 500 occurrences sous 800 ms au percentile choisi ;
- ajout unitaire sous 300 ms hors réseau ;
- batch de 100 occurrences borné et asynchrone si nécessaire ;
- timeline paginée ;
- aucune reconstruction globale des classements communautaires ;
- cache privé contrôlé ;
- aucune requête N+1 de noms/images ;
- projection légère pour les listes ;
- images chargées uniquement dans les vues qui en ont besoin.

Ces nombres sont des cibles de cadrage à ajuster après baseline, pas des garanties arbitraires.

## 24. Stratégie de feature flags

- `passport:visits` ;
- `passport:rideOccurrences` ;
- `passport:temporalRatings` ;
- `passport:statistics` ;
- `passport:anonymousDrafts` ;
- `passport:globalRatingSuggestions`.

Chaque flag : propriétaire, valeur par défaut, date de retrait, comportement de repli, métriques et kill switch.

## 25. Découpage recommandé en PR

| PR | Tranche | Critère de sortie |
|---|---|---|
| `PASS-01` | ADR note courante vs observations + modèle de date | Invariants validés avant code persistant |
| `PASS-02` | Core `Visit`, statuts, date, confidentialité | Tests purs complets |
| `PASS-03` | Repository visites + indexes + tests Mongo | CRUD propriétaire fiable |
| `PASS-04` | API création/liste/détail de visite | Contrat OpenAPI additif |
| `PASS-05` | UI création rapide depuis parc et profil | Visite privée créée simplement |
| `PASS-06` | Core/persistance `RideOccurrence` | N occurrences fiables et idempotentes |
| `PASS-07` | API batch + réordonnancement | Double clic/retry sans doublon |
| `PASS-08` | UI sélection multiple et timeline | Saisie rétrospective utilisable |
| `PASS-09` | `VisitParkAssessment` | Une note de parc par visite |
| `PASS-10` | `RideAssessment` | Une note facultative par occurrence |
| `PASS-11` | Audit et corrections | Aucune modification silencieuse |
| `PASS-12` | Statistiques élément de base | Fixtures indépendantes concordantes |
| `PASS-13` | Statistiques parc/année | Dénominateurs explicites |
| `PASS-14` | Timeline et tableaux accessibles | Pas de dépendance au graphique |
| `PASS-15` | Suggestions de note globale | Jamais d’update automatique |
| `PASS-16` | Export JSON/CSV | Données complètes et versionnées |
| `PASS-17` | Suppression, purge et RGPD | Aucune donnée orpheline |
| `PASS-18` | Brouillons IndexedDB, si validés | Import explicite et idempotent |
| `PASS-19` | Beta cohort et instrumentation | Gate `PASS-G` mesurable |
| `PASS-20` | Nettoyage flags et documentation | Chemins stabilisés |

## 26. Beta et validation qualitative

Cohorte initiale : petit groupe de passionnés capables de reconstruire plusieurs visites.

Protocoles :

- une visite récente détaillée ;
- une visite ancienne avec année seulement ;
- plusieurs rides d’une attraction ;
- correction d’une erreur ;
- attraction fermée ;
- export ;
- compréhension de la distinction note globale/note par ride.

Questions :

- la saisie apporte-t-elle plus de valeur qu’un tableur ?
- le niveau de détail est-il choisi ou imposé ?
- les utilisateurs comprennent-ils quelle note compte pour la communauté ?
- reviennent-ils enregistrer une deuxième visite ?
- les statistiques sont-elles crédibles et lisibles ?
- quelles données refusent-ils de renseigner ?

## 27. Gate finale `PASS-G`

La roadmap de partage ne commence que lorsque :

- le modèle sépare physiquement et conceptuellement préférence globale et observations ;
- chaque visite et occurrence appartient uniquement à son utilisateur ;
- une personne peut noter chaque visite de parc et chaque occurrence d’élément ;
- plusieurs rides ne créent aucun vote communautaire supplémentaire ;
- les dates partielles restent partielles ;
- les cibles fermées ou renommées ne détruisent pas l’historique ;
- les opérations sont idempotentes et les conflits visibles ;
- les statistiques de référence sont reproductibles ;
- une tendance n’est pas affichée sous le seuil ;
- export et suppression sont complets ;
- le Web est utilisable sur mobile sans constituer une application mobile ;
- au moins quelques testeurs enregistrent une seconde visite sans assistance ;
- les coûts de requête et de stockage restent compatibles avec le VPS ;
- aucun libellé ne fait croire que la moyenne personnelle temporelle est une vérité communautaire.
