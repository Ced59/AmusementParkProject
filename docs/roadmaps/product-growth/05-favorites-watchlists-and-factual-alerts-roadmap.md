# Roadmap 05 — Favoris, projets de visite, surveillance et alertes factuelles

> Code programme : `WATCH`
>
> Dépendances : qualité/provenance des données, préférences utilisateur, instrumentation transverse. Les notifications push et l’application mobile sont hors périmètre ; commencer par le centre Web et, si validé, l’e-mail opt-in.
>
> Principe : une alerte décrit un changement vérifiable, sa source, sa date et ses limites. Elle ne crée ni urgence artificielle ni contenu sensationnaliste.

## 1. Vision produit

Un simple cœur « favori » ne suffit pas à créer une raison de revenir. Le produit distingue quatre intentions :

- **préféré** : j’aime particulièrement ce parc ou cet élément ;
- **à visiter / à faire** : je souhaite le découvrir ;
- **projet** : je l’envisage dans une période ou un voyage concret ;
- **surveillé** : je souhaite être informé de certains changements factuels.

L’utilisateur contrôle les types d’événements, le canal et la fréquence. Le système contrôle la provenance, la déduplication, la fraîcheur et les limites.

## 2. Objectifs

- Introduire des collections personnelles sémantiquement distinctes.
- Permettre l’ajout depuis les fiches, classements, comparaisons et Passeport.
- Construire un catalogue d’événements factuels versionné.
- Créer un centre de notifications Web privé.
- Proposer des résumés e-mail opt-in plutôt que des alertes incessantes.
- Éviter les doublons lors de corrections successives d’une même donnée.
- Afficher source, date, état de confirmation et différence avant/après.
- Permettre désabonnement global et granulaire.
- Auditer l’origine d’une alerte.
- Ne jamais influencer classements ou recommandations selon un partenariat.

## 3. Non-objectifs

- push mobile ;
- alerte de présence géographique ;
- marketing automatisé non sollicité ;
- alerte « dernières places » sans intégration officielle ;
- scraping agressif ;
- relai automatique de rumeurs ;
- recommandation payante ;
- fil d’actualité social ;
- chat ;
- séries de connexion ;
- notifications destinées uniquement à augmenter artificiellement la fréquence d’usage.

## 4. Modèle des intentions

## 4.1 `UserCollectionEntry`

```csharp
public sealed class UserCollectionEntry
{
    public Guid Id { get; }
    public Guid UserId { get; }
    public CollectionTargetType TargetType { get; }
    public Guid TargetId { get; }
    public UserCollectionKind Kind { get; }
    public string? PrivateNote { get; private set; }
    public int? Priority { get; private set; }
    public DateRangePreference? PreferredPeriod { get; private set; }
    public DateTime CreatedAtUtc { get; }
    public DateTime UpdatedAtUtc { get; private set; }
}
```

`UserCollectionKind` :

- `Favorite` ;
- `WantToVisit` pour un parc ;
- `WantToExperience` pour un élément ;
- `Planned` lorsqu’associé à un projet ;
- `Watched` n’est pas nécessairement une entrée séparée si une `WatchSubscription` existe ; l’interface peut les présenter ensemble.

### 4.1.1 Invariants

- unicité `(UserId, TargetType, TargetId, Kind)` ;
- un même parc peut être préféré et à revisiter ;
- priorité bornée ;
- note privée ;
- cible fermée conservée avec statut ;
- suppression d’une entrée ne supprime ni visite ni note ;
- ajout depuis un résultat anonyme peut être conservé localement puis réclamé après inscription, ultérieurement.

## 4.2 `WatchSubscription`

```csharp
public sealed class WatchSubscription
{
    public Guid Id { get; }
    public Guid UserId { get; }
    public WatchTargetType TargetType { get; }
    public Guid TargetId { get; }
    public IReadOnlySet<FactualEventType> EventTypes { get; private set; }
    public NotificationFrequency Frequency { get; private set; }
    public IReadOnlySet<NotificationChannel> Channels { get; private set; }
    public bool IsPaused { get; private set; }
    public DateTime CreatedAtUtc { get; }
    public DateTime UpdatedAtUtc { get; private set; }
}
```

Un abonnement sans canal reste utile pour le centre Web : les événements attendent d’être consultés.

## 5. Catalogue d’événements factuels

### 5.1 Types initiaux

#### Parc

- `OpeningCalendarPublished` ;
- `OpeningCalendarChanged` ;
- `SeasonOpeningConfirmed` ;
- `SeasonClosingConfirmed` ;
- `ParkTemporaryClosureConfirmed` ;
- `ParkPermanentClosureConfirmed` ;
- `ParkReopeningConfirmed` ;
- `ParkNameChanged` ;
- `OperatorChanged` ;
- `TicketPricePublishedOrChanged` seulement avec source et comparaison fiable ;
- `MajorDataCompletionImproved` pour le produit, fréquence limitée.

#### Élément

- `AttractionAnnouncedOfficially` ;
- `OpeningDateConfirmed` ;
- `OpeningDateChanged` ;
- `OpenedConfirmed` ;
- `TemporarilyClosedConfirmed` ;
- `ReopenedConfirmed` ;
- `PermanentClosureConfirmed` ;
- `Renamed` ;
- `MajorRestrictionChanged` ;
- `LocationOrCategoryCorrected` si impact utilisateur réel.

#### Éditorial

- `HistoryPublished` ;
- `MajorHistoryUpdate` ;
- `VerifiedSourceAdded` ;
- `CorrectionAfterUserReport` uniquement au déclarant ou abonnés ayant choisi les corrections.

### 5.2 États de confirmation

- `Draft` : jamais envoyé ;
- `Verified` : source et changement contrôlés ;
- `Published` : visible et distribuable ;
- `Corrected` : remplacé par une nouvelle version ;
- `Retracted` : information retirée ;
- `Expired` : événement plus utile pour l’alerte mais conservé dans l’audit.

Une information `CommunityUnverified` peut apparaître dans un espace de contribution futur, mais n’alimente pas les alertes factuelles de cette roadmap.

## 5.3 `FactualChangeEvent`

```csharp
public sealed class FactualChangeEvent
{
    public Guid Id { get; }
    public FactualEventType Type { get; }
    public ChangeTarget Target { get; }
    public FactValue? PreviousValue { get; }
    public FactValue? NewValue { get; }
    public SourceReference Source { get; }
    public DataConfidence Confidence { get; }
    public DateTime OccurredAtUtc { get; }
    public DateTime VerifiedAtUtc { get; }
    public DateTime PublishedAtUtc { get; }
    public string DeduplicationKey { get; }
    public int Revision { get; }
    public FactualChangeStatus Status { get; }
}
```

Les valeurs structurées évitent de comparer deux chaînes localisées. Le texte public est construit à partir de codes et contenus éditoriaux validés.

## 6. Provenance et probité

Chaque événement distribué expose :

- source ;
- type de source ;
- date de publication originale ;
- date de vérification ;
- fait confirmé ;
- éléments encore inconnus ;
- correction éventuelle ;
- lien vers la fiche ou l’article complet.

### 6.1 Formulations

Acceptable :

> « Le parc a publié son calendrier 2027 le 14 octobre. Les horaires de trois dates restent non renseignés. »

Interdit :

> « Incroyable : le calendrier vient de tomber, réservez avant qu’il ne soit trop tard ! »

Le domaine transporte un fait, pas un titre sensationnaliste. Les templates éditoriaux sont revus et testés.

### 6.2 Affiliations

Si un lien billet affilié existe :

- placé séparément après l’information ;
- marqué clairement ;
- n’influence ni la création ni la priorité de l’alerte ;
- absence d’affiliation sans effet sur la couverture ;
- journalisation de la règle de séparation.

## 7. Détection des changements

### 7.1 Sources internes

Événements produits après modification validée :

- calendrier ;
- statut ;
- date d’ouverture/fermeture ;
- nom ;
- exploitant ;
- restriction ;
- publication historique.

Les événements sont émis **après commit**, via outbox si nécessaire.

### 7.2 Import/data source

Lors d’un import :

1. calculer le diff structuré ;
2. classer l’impact ;
3. ne pas publier automatiquement les champs sensibles ;
4. présenter en revue admin ;
5. associer les sources ;
6. approuver ;
7. créer un événement idempotent ;
8. distribuer selon fréquence.

### 7.3 Corrections répétées

Un même fait modifié trois fois en quelques minutes ne produit pas trois e-mails.

- fenêtre de stabilisation configurable ;
- clé de déduplication ;
- révision ;
- événement final ;
- correction visible dans le centre si une version a déjà été distribuée ;
- notification de correction seulement si l’erreur précédente pouvait influencer une décision.

## 8. Routage des abonnements

### 8.1 Résolution de cible

Un abonnement sur un parc peut inclure :

- événements du parc ;
- événements de tous les éléments ;
- seulement nouveautés majeures ;
- catégories choisies.

Ne pas abonner automatiquement à des milliers d’événements. L’écran affiche la portée estimée.

### 8.2 Fréquences

- `Immediate` : réservé à quelques événements explicitement choisis ;
- `DailyDigest` ;
- `WeeklyDigest` recommandé par défaut pour e-mail ;
- `WebOnly` ;
- `Paused`.

Pas de réactivation silencieuse après pause.

### 8.3 Priorité

La priorité de distribution est fondée sur le type factuel et la préférence, pas sur le potentiel de clic.

Exemples :

- fermeture définitive d’une attraction surveillée : importante ;
- correction d’une couleur de fiche : aucune alerte ;
- publication d’une histoire : digest ;
- ouverture confirmée d’une nouveauté : selon choix.

## 9. Modèle de notification

### 9.1 `UserNotification`

- `Id` ;
- `UserId` ;
- `FactualChangeEventId` ;
- `SubscriptionId` ;
- type ;
- statut `Pending`, `Delivered`, `Read`, `Dismissed`, `Failed`, `Suppressed` ;
- canal ;
- date ;
- template version ;
- langue au moment de la génération ;
- idempotency key ;
- erreur technique minimisée.

### 9.2 Centre Web

Fonctions :

- non lus ;
- filtre par parc/type ;
- source ;
- lire/marquer lu ;
- masquer ;
- accéder à la préférence ;
- se désabonner ;
- voir les corrections ;
- aucune pagination infinie non accessible ;
- rétention affichée.

### 9.3 E-mail

Conditions :

- adresse vérifiée ;
- consentement ;
- fréquence ;
- désabonnement en un clic ;
- lien vers gestion détaillée ;
- texte et HTML accessibles ;
- aucune donnée privée inutile ;
- pas de pixel de suivi tiers par défaut ;
- limitation des envois ;
- gestion bounce/complaint si fournisseur.

## 10. API

### Collections

```text
PUT    /api/me/collections/{kind}/{targetType}/{targetId}
DELETE /api/me/collections/{kind}/{targetType}/{targetId}
GET    /api/me/collections
PATCH  /api/me/collections/{entryId}
```

### Abonnements

```text
POST   /api/me/watch-subscriptions
GET    /api/me/watch-subscriptions
PATCH  /api/me/watch-subscriptions/{id}
DELETE /api/me/watch-subscriptions/{id}
POST   /api/me/watch-subscriptions/{id}/pause
POST   /api/me/watch-subscriptions/{id}/resume
```

### Notifications

```text
GET  /api/me/notifications
POST /api/me/notifications/{id}/read
POST /api/me/notifications/read-all
POST /api/me/notifications/{id}/dismiss
GET  /api/me/notification-preferences
PUT  /api/me/notification-preferences
```

### Administration

```text
GET  /api/admin/factual-events
POST /api/admin/factual-events/{id}/verify
POST /api/admin/factual-events/{id}/publish
POST /api/admin/factual-events/{id}/retract
GET  /api/admin/notification-deliveries
POST /api/admin/notification-digests/preview
```

## 11. Persistance et indexes

Collections :

- `user-collection-entries` ;
- `watch-subscriptions` ;
- `factual-change-events` ;
- `user-notifications` ;
- `notification-delivery-attempts` ;
- `notification-preferences`.

Indexes :

- unique collection `(UserId, TargetType, TargetId, Kind)` ;
- unique subscription `(UserId, TargetType, TargetId)` si un abonnement regroupe les types ;
- événement unique `DeduplicationKey + Revision` ;
- notification unique `(UserId, EventId, Channel)` ;
- `{ UserId, ReadAtUtc, CreatedAtUtc }` ;
- `{ Status, NextAttemptAtUtc }` pour delivery ;
- TTL sur tentatives/logs selon rétention ;
- aucune TTL sur préférences actives.

## 12. Outbox et distribution

Pipeline :

```text
modification validée
→ outbox factuelle
→ construction/déduplication de l’événement
→ revue éventuelle
→ publication
→ résolution des abonnements
→ création idempotente des notifications
→ regroupement digest
→ livraison
→ statut/audit
```

Garanties :

- at-least-once au transport ;
- exactement une notification logique grâce à l’idempotence ;
- retries bornés ;
- dead-letter inspectable ;
- kill switch par canal/type ;
- aucune perte silencieuse ;
- aucun blocage de la mutation métier par l’e-mail.

## 13. Interface Angular

```text
features/profile/collections/
features/profile/watchlist/
features/profile/notifications/
features/profile/notification-preferences/
shared/components/collection-action/
shared/components/watch-action/
```

Depuis une fiche :

- bouton avec état actuel ;
- menu explicite `préféré`, `à faire`, `surveiller` ;
- pas quatre icônes ambiguës ;
- confirmation du périmètre de surveillance ;
- accès rapide à la gestion.

Centre :

- résumé par intention ;
- dates ;
- filtres ;
- sources ;
- état des parcs fermés ;
- déplacement vers un voyage ;
- export.

## 14. Confidentialité et conformité

- listes privées par défaut ;
- futur partage séparé via `SHARE` ;
- préférences incluses dans export/suppression ;
- e-mail soumis au consentement applicable ;
- preuve du consentement et version du texte ;
- désinscription immédiate ;
- rétention courte des logs de livraison ;
- aucune surveillance implicite à partir d’une simple consultation ;
- suppression de compte annule abonnements et livraisons ;
- commentaires privés exclus des e-mails.

## 15. Tests obligatoires

### Domaine/Application

- unicité des intentions ;
- cible fermée ;
- événement vérifié/non vérifié ;
- diff ;
- déduplication ;
- correction/rétractation ;
- portée parc/éléments ;
- fréquence ;
- pause ;
- consentement e-mail ;
- priorité indépendante du partenariat.

### Infrastructure

- outbox ;
- retry ;
- double traitement ;
- digest ;
- TTL ;
- provider e-mail en panne ;
- désabonnement concurrent ;
- index volumique.

### API/Angular

- collections depuis toutes les fiches ;
- notification privée ;
- source visible ;
- correction ;
- filtres ;
- accessibilité ;
- huit langues ;
- noindex ;
- unsubscription ;
- aucune fuite cross-user.

### End-to-end

1. surveiller une attraction pour ouverture confirmée ;
2. créer deux corrections de date avant stabilisation ;
3. publier une seule alerte ;
4. vérifier source et différence ;
5. désabonner avant digest ;
6. vérifier aucun e-mail ;
7. rétracter un événement déjà livré ;
8. afficher la correction dans le centre.

## 16. Observabilité

- abonnements actifs par type ;
- événements vérifiés/publiés/rétractés ;
- notifications dédupliquées ;
- latence événement → centre ;
- digests générés ;
- bounces/complaints ;
- désabonnements ;
- ouvertures du centre ;
- clics vers la source sans pixel intrusif ;
- signalements d’alerte trompeuse ;
- charge outbox et files.

Une hausse du taux de clic n’est pas une justification pour rendre les formulations plus alarmistes.

## 17. Déploiement

### Étape 1

- collections `Favorite` et `WantToVisit/Experience` ;
- aucun e-mail ;
- centre privé simple.

### Étape 2

- `WatchSubscription` ;
- événements internes manuels vérifiés ;
- notifications Web.

### Étape 3

- outbox automatique sur quelques faits ;
- déduplication ;
- administration.

### Étape 4

- digest e-mail opt-in ;
- monitoring fournisseur ;
- désabonnement.

### Étape 5

- extension des types uniquement après mesure de qualité.

## 18. Découpage recommandé en PR

| PR | Contenu | Critère |
|---|---|---|
| `WATCH-01` | Domaine collections | Intentions distinctes |
| `WATCH-02` | API/UI favoris et wishlist | Usage privé fiable |
| `WATCH-03` | Domaine abonnements/préférences | Portée explicite |
| `WATCH-04` | Catalogue d’événements et provenance | Types versionnés |
| `WATCH-05` | Diff/outbox/déduplication | Un fait logique, une alerte |
| `WATCH-06` | Administration de vérification | Rien de non vérifié distribué |
| `WATCH-07` | Notifications Web | Centre accessible |
| `WATCH-08` | Corrections/rétractations | Historique honnête |
| `WATCH-09` | Digests | Groupement déterministe |
| `WATCH-10` | E-mail opt-in | Consentement et unsubscription |
| `WATCH-11` | Export/suppression | Cycle complet |
| `WATCH-12` | Pilote et métriques | Gate franchie |

## 19. Gate finale `WATCH-G`

- collections et surveillances sont sémantiquement distinctes ;
- aucune alerte ne part sans source et vérification ;
- les inconnues et corrections sont visibles ;
- les doublons sont supprimés par clé logique ;
- l’e-mail est opt-in, désabonnable et non requis ;
- la priorité ne dépend pas du potentiel commercial ;
- les listes restent privées ;
- aucune notification n’est créée pour une simple consultation ;
- une rétractation déjà distribuée produit une correction adaptée ;
- la charge et le volume sont bornés ;
- l’utilisateur peut tout exporter et supprimer ;
- les formulations restent factuelles même si une formulation sensationnelle obtiendrait plus de clics.
