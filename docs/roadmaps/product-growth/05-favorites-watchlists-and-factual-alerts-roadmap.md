# Roadmap 05 — Favoris, projets de visite, surveillance et alertes factuelles

> Code programme : `WATCH`
>
> Dépendances : qualité/provenance des données, préférences utilisateur, instrumentation transverse. Les notifications push et l’application mobile sont hors périmètre ; commencer par le centre Web et, si validé, l’e-mail opt-in.
>
> Principe : une alerte décrit un changement vérifiable, sa source, sa date et ses limites. Elle ne crée ni urgence artificielle ni contenu sensationnaliste.

## 0. Avenant technique FOUNDATION

La distribution des alertes réutilise le worker durable défini par FOUNDATION :

- le changement factuel validé est l’état source ;
- il porte une révision et une clé de déduplication ;
- l’application tente d’enregistrer un job après la mutation ;
- un reconciler recrée toute distribution manquante à partir de la révision source ;
- les jobs digest sont coalescés par utilisateur, canal et période ;
- les envois exacts utilisent une idempotency key ;
- les retries sont bornés et une dead-letter est visible en administration ;
- le centre Web peut être alimenté avant l’e-mail ;
- aucun broker externe n’est nécessaire dans la première version.

Une correction ou rétractation est un nouvel état factuel versionné. Elle ne modifie pas silencieusement le texte d’un e-mail déjà envoyé ; elle crée une notification de correction lorsque l’impact le justifie.

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

> État au 15 septembre 2026 : `WATCH-01` est implémenté dans le Core pour la
> version `5.3.39`. Les quatre intentions, leur compatibilité avec les cibles, les
> détails privés, la période, la priorité, la version optimiste et la conservation
> d'une cible fermée sont couvertes par des invariants et des tests. `WATCH-02` est
> livré en `5.3.43` : persistance Mongo bornée et sans doublons, API privée
> idempotente, actions sur les fiches et bibliothèque responsive avec noms et images
> réels. Favoris et envies n'activent toujours aucune surveillance implicite.
> `WATCH-03` est livré en `5.3.45` : l'abonnement explicite porte sa cible, ses
> événements choisis, sa fréquence, ses canaux externes facultatifs, sa pause et sa
> version optimiste. Il reste distinct de toute entrée de collection. `WATCH-04` est
> livré en `5.3.48` : les 25 types d'événements sont catalogués, versionnés et bornés
> par cible ; chaque fait possède une valeur structurée avant/après, une source, un
> niveau de confiance, une clé de déduplication et un cycle de validation auditable.
> `WATCH-05` est livré en `5.3.49` : un diff structuré ignore les non-changements,
> le calendrier conserve atomiquement chaque intention et sa révision monotone avant
> sa copie idempotente dans l'outbox Mongo, puis le worker matérialise
> exactement un brouillon factuel et un reconciler borné répare les interruptions.
> Les calendriers d'ouverture officiellement sourcés sont le premier flux métier
> raccordé ; un job définitivement terminé sans acquittement est isolé explicitement
> afin de ne jamais bloquer les faits suivants. La reprise est bornée par marqueur,
> y compris à l'intérieur d'un même calendrier très actif, et les dates de
> vérification futures sont rejetées avant toute écriture.
> `WATCH-06` est livré en `5.3.50` : l'administration dispose d'un atelier
> responsive qui présente le changement, sa cible lisible et sa preuve, sans
> exposer les identifiants de cible ni l'empreinte technique des calendriers.
> Pour les calendriers, chaque règle conserve sa période, ses jours, son état
> ouvert ou fermé et ses plages horaires : un déplacement du lundi au mardi reste
> donc visible même si les compteurs et les heures sont identiques. Dans un grand
> calendrier, les règles réellement ajoutées, retirées ou modifiées passent avant
> les règles inchangées dans l'aperçu borné. Une migration
> Mongo unique convertit aussi les événements et outbox historiques ; lorsqu'un
> ancien détail ne peut pas être reconstruit honnêtement, l'atelier le dit au lieu
> d'inventer une preuve ou d'exposer une empreinte technique.
> Le passage `Draft` → `Verified` → `Published` est explicite, audité, protégé
> contre les validations concurrentes et limité en charge. Un fait détecté ou
> vérifié reste techniquement non diffusable ; seule la publication volontaire
> ouvre la distribution aux jalons suivants.
> `WATCH-07` est livré en `5.3.51` : chaque membre peut activer, personnaliser,
> mettre en pause ou supprimer un suivi privé directement depuis la fiche d'un
> parc ou d'une attraction, sans relation implicite avec ses favoris. Seul un fait
> publié alimente le centre Web ; la distribution paginée est idempotente et un
> reconciler la répare après interruption. Le centre, accessible depuis le profil,
> affiche la cible et son image, le changement vérifié, la valeur avant/après, la
> source et sa date de vérification. Il propose les non-lus, les filtres par parc
> et type, la lecture, le masquage, le désabonnement et une pagination finie. La
> rétention de 365 jours est annoncée, les identifiants techniques ne sont jamais
> rendus comme libellés et l'ensemble se replie sans débordement sur mobile.
> `WATCH-08` est livré en `5.3.52` : l'administration peut relier un fait publié à
> une révision plus récente ou le rétracter avec un motif explicite. La cohérence
> de cible, de clé logique et de révision est contrôlée avant toute mutation ; les
> transitions restent auditées et protégées par version. Un traitement durable,
> idempotent et borné par lots de 100 avertit les destinataires initiaux, y compris
> après un désabonnement, sans créer de doublon. Une rétractation remonte
> l'ancienne notification comme non lue, tandis qu'une correction diffuse la
> version de remplacement avec sa nouvelle preuve. Le centre privé distingue
> clairement mise à jour, correction, version remplacée et rétractation dans les
> huit langues, sans masquer l'historique ni dépasser le viewport mobile.

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
    public FactualChangeEventId Id { get; }
    public FactualEventType Type { get; }
    public int DefinitionVersion { get; }
    public ChangeTarget Target { get; }
    public FactValue? PreviousValue { get; }
    public FactValue? NewValue { get; }
    public SourceReference Source { get; }
    public DataConfidence Confidence { get; }
    public DateTime OccurredAtUtc { get; }
    public DateTime? VerifiedAtUtc { get; }
    public DateTime? PublishedAtUtc { get; }
    public string DeduplicationKey { get; }
    public int Revision { get; }
    public FactualChangeStatus Status { get; }
    public long Version { get; }
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
- `notification-digests` ;
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
| `WATCH-01` | Domaine collections — livré en `5.3.39` | Intentions distinctes |
| `WATCH-02` | API/UI favoris et wishlist — livré en `5.3.43` | Usage privé fiable |
| `WATCH-03` | Domaine abonnements/préférences — livré en `5.3.45` | Portée explicite |
| `WATCH-04` | Catalogue d’événements et provenance — livré en `5.3.48` | Types versionnés |
| `WATCH-05` | Diff/outbox/déduplication — livré en `5.3.49` | Un fait logique, une alerte |
| `WATCH-06` | Administration de vérification — livré en `5.3.50` | Rien de non vérifié distribué |
| `WATCH-07` | Notifications Web — livré en `5.3.51` | Centre accessible |
| `WATCH-08` | Corrections/rétractations — livré en `5.3.52` | Historique honnête |
| `WATCH-09` | Digests — livré en `5.3.53` | Groupement déterministe |
| `WATCH-10` | E-mail opt-in — livré en `5.3.54` | Consentement et désinscription |
| `WATCH-11` | Export/suppression — livré en `5.3.55` | Cycle complet |
| `WATCH-12` | Pilote et métriques | Gate franchie |

`WATCH-10` livre concrètement le canal e-mail sans le rendre obligatoire : l’utilisateur active ou coupe les résumés depuis son centre privé, uniquement avec une adresse confirmée et un accord explicite versionné. Un digest ne crée du travail de livraison que si cet accord est actif, puis le worker recontrôle l’accord, le compte, la fréquence, la surveillance et la dernière révision factuelle juste avant l’envoi. Les messages de marque existent en HTML accessible et en texte simple dans les huit langues, ne contiennent ni note privée, ni pixel de suivi, ni identifiant métier exposé, et proposent la gestion détaillée ainsi que la désinscription standard en un clic. Les tentatives sont idempotentes, retentées de façon bornée, conservées trente jours et agrégées dans un diagnostic administratif sans donnée personnelle.

`WATCH-11` rattache concrètement favoris, envies et alertes au cycle de vie déjà utilisé par le passeport. L’export personnel JSON ou CSV inclut les intentions privées, les surveillances, les notifications et leurs preuves factuelles, les digests, les choix d’e-mail et la preuve de consentement, avec des noms lisibles et uniquement des références propres au fichier exporté. Aucun identifiant MongoDB, identifiant de compte ou identifiant technique de cible n’est publié. Le sous-système fournit aussi au futur coordinateur transversal de suppression de compte une opération idempotente qui coupe d’abord le consentement et les abonnements, annule les travaux planifiés, puis purge collections, alertes, digests et traces de livraison. Il n’introduit ni second export ni faux bouton de suppression partielle : le compte global ne pourra être supprimé que lorsque le coordinateur couvrira également visites, notes, partages, sessions et identité.

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
