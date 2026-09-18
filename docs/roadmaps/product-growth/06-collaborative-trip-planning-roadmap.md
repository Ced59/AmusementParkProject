# Roadmap 06 — Voyages et journées de parc collaboratifs

> Code programme : `TRIP`
>
> Dépendances : collections `WATCH`, moteur `FIT`, données d’ouverture fiables et politiques de partage `SHARE`.
>
> Périmètre : préparation Web avant la visite. Aucun chat généraliste, aucune position partagée, aucune réservation intégrée et aucune optimisation live de journée dans cette roadmap.

## 0. Avenant technique FOUNDATION

- `TripPlanId`, invitation et participant restent des chaînes opaques aux frontières ;
- la version optimiste du plan protège chaque opération fine ;
- les listes ordonnées utilisent `SortPosition: long` avec positions espacées et renormalisation locale, pas un index contigu réécrit à chaque déplacement ;
- une invitation acceptée est idempotente et son token est consommé atomiquement ;
- le rafraîchissement des faits officiels et les notifications sont différés par jobs bornés ;
- une indisponibilité du worker n’empêche pas l’édition du plan, mais signale les faits potentiellement anciens ;
- SignalR, CRDT et broker ne sont pas des prérequis ;
- un passage à une collaboration temps réel nécessiterait un ADR séparé et des mesures d’usage.

### État de `TRIP-01` au 17 septembre 2026

L'ADR [`product-growth-trip-01-aggregate-roles-privacy-2026-09-17.md`](../../architecture/product-growth-trip-01-aggregate-roles-privacy-2026-09-17.md)
fige les invariants avant toute persistance : identifiants chaîne typés, propriétaire
unique, membres bornés embarqués dans le plan, rôles vérifiés dans Application,
contraintes partagées en liste blanche, invitations opaques, acceptation idempotente
réparable sur MongoDB autonome et concurrence optimiste par opérations fines.

Le voyage reste privé entre membres. Une publication éventuelle passera par
`SHARE`; une invitation ne publie pas le plan. Les faits officiels, les snapshots
`FIT` et les choix du groupe restent trois catégories distinctes. Aucun voyage,
endpoint, écran ou index n'est créé par ce jalon documentaire. `TRIP-02` peut donc
implémenter le voyage individuel sans attendre une cohorte réelle, tout en
conservant les gates techniques, de confidentialité et responsive.

### État de `TRIP-02` au 17 septembre 2026

Le premier usage individuel est désormais porté par un vrai agrégat métier et
par la collection MongoDB `trip-plans`. Un membre authentifié peut créer, lister,
consulter, modifier et supprimer ses voyages privés au travers de `me/trips`.
La création exige une clé d'idempotence : un retry réseau renvoie le résultat
initial, tandis qu'une réutilisation de la même clé avec un autre contenu est
refusée. Le quota est garanti par un slot propriétaire indexé, y compris sous
concurrence.

Un voyage accepte quatre formes de dates : non définies, période confirmée avec
bornes inclusives (un seul jour lorsque les bornes sont égales), intervalle encore
possible ou liste de dates candidates. Les dates utilisent le format ISO et, dès qu'elles
sont définies, un fuseau IANA valide est obligatoire. Chaque écriture porte une
version optimiste afin qu'un onglet ancien ne puisse pas écraser une modification
plus récente. Le propriétaire est aussi enregistré comme membre actif, sans
dupliquer son autorité dans un second rôle.
Les modifications sont déjà séparées en commandes fines pour renommer le voyage
ou changer ses dates ; aucune route ne remplace silencieusement tout le plan.

La suppression produit un tombstone technique limité à 24 heures : les données
privées sont immédiatement effacées, mais les empreintes minimales de création
restent assez longtemps pour qu'un ancien retry réseau ne puisse pas recréer le
voyage supprimé. MongoDB purge automatiquement ce tombstone à l'expiration. La
commande exige aussi une authentification confirmée depuis moins de dix minutes,
en plus de la version attendue, afin qu'une session ancienne ne suffise pas à
déclencher cette action irréversible.

Les empreintes de création utilisent un trousseau HMAC dédié et versionné, distinct
du JWT. Une rotation conserve explicitement les anciennes versions nécessaires aux
reprises ; le tombstone ne garde aucun identifiant de compte en clair.

Ce jalon reste volontairement sans écran : l'expérience utilisateur individuelle,
conçue responsive dès 320 px, appartient à `TRIP-04`. `TRIP-03` enrichit d'abord
le même agrégat avec les parcs candidats et les journées afin que l'interface ne
soit pas bâtie sur un contrat transitoire.

### État de `TRIP-03` au 17 septembre 2026

Le programme privé sait désormais conserver jusqu'à cent parcs envisagés, sans
dupliquer un même parc dans un voyage. Chaque candidat possède des dates possibles,
une source (`Manual`, `Wishlist` ou `Comparator`), une note collective, un état
explicite (`Proposed`, `Shortlisted`, `Selected` ou `Rejected`) et un ordre stable.
Le déplacement d'une carte remplace atomiquement un unique document d'ordre borné
à cent identifiants. Une interruption ne peut donc jamais laisser la moitié des
candidats dans l'ancien ordre et l'autre moitié dans le nouveau.

Une journée n'est programmable que dans une période de voyage déjà fixée et ne
peut référencer qu'un parc candidat marqué `Selected`. Elle accepte une heure
d'arrivée souhaitée, une note du groupe et au plus cinquante blocs simples
identifiés de façon stable : repas, événement, attraction ou note. Les horaires
officiels ne sont pas recopiés dans ce choix collectif ; ils seront lus comme des
faits séparés dans `TRIP-09`.

Les collections MongoDB `trip-park-candidates` et `trip-day-plans` appliquent
l'unicité `(voyage, parc)`, `(voyage, opération d'ajout)` et `(voyage, date)`.
L'ordre canonique borné des candidats et sa version résident sur `trip-plans`, où
leur mutation valide atomiquement la lease racine. Toute écriture enfant acquiert sur
le voyage racine une lease liée à sa version, à son epoch et à une génération.
Une création passe par une coquille `Reserved`; une mutation conserve la dernière
version lisible et pose un marqueur `PendingMutation`. Les validations d'expiration
utilisent `$$NOW`, donc l'horloge du serveur MongoDB plutôt que celle d'un nœud Web.
Les retries d'ajout de parc rejouent le résultat initial, et un `PUT` de journée
identique est sans effet grâce aux identifiants stables de ses blocs. Une même clé
d'ajout réutilisée avec un autre contenu produit un conflit explicite. Deux appels
simultanés ne partagent jamais une lease. Le rejeu est résolu avant la version
courante ou la visibilité du parc ; après retrait du candidat, une preuve minimale
de 24 heures bloque l'ancien retry sans empêcher un nouvel ajout volontaire.

Une modification des dates du voyage acquiert la même barrière exclusive, vérifie
tous les candidats et toutes les journées, puis avance atomiquement la version et
l'epoch racine. Elle est refusée si elle ferait sortir une date candidate ou une
journée du nouveau calendrier.

La suppression ferme d'abord le voyage aux nouvelles écritures, purge candidats
et journées, puis anonymise le plan. Si le processus tombe entre ces phases, un
réconciliateur reprend automatiquement la purge. Les anciennes données `TRIP-02`
sont complétées par migration additive au démarrage ; aucune seconde implémentation
du modèle ne coexiste.

Les routes authentifiées `me/trips/{tripId}/program`, `parks` et `days/{date}`
exposent les noms de parc résolus en lot. Les identifiants techniques restent des
clés d'action et ne servent jamais de libellé. Un parc devenu indisponible expose
un nom nul et un statut neutre à localiser par le client. La lecture du programme
est rejouée si la séquence de mutations enfants change entre la lecture des
candidats et celle des journées. Ce jalon livre volontairement le
contrat métier et sa persistance ; `TRIP-04` apporte l'expérience visuelle mobile,
la wishlist et le réordonnancement accessible.

La preuve détaillée, le schéma MongoDB et les diagrammes de classes et de séquence
sont consignés dans
[`product-growth-trip-03-candidates-days-2026-09-17.md`](../../architecture/product-growth-trip-03-candidates-days-2026-09-17.md).

### État de `TRIP-04` au 18 septembre 2026

Le voyage individuel est désormais accessible depuis le profil, sans invitation
ni activité communautaire préalable. Un membre peut créer un voyage avec ou sans
dates, retrouver ses voyages privés puis reprendre leur préparation. La page de
détail transforme les dates fixes en journées, importe en lot les parcs `WantToVisit`
ou `Planned` encore disponibles, permet de les qualifier comme idée, présélection,
choix ou écart, puis d'affecter les parcs choisis à chaque journée avec une heure
d'arrivée et une note de groupe.

L'ordre des parcs se modifie par glisser-déposer, mais aucun geste précis n'est
obligatoire : des boutons accessibles permettent aussi de placer une carte en
premier, de la monter, de la descendre ou de la placer en dernier. Les images des
collections aident à reconnaître immédiatement les parcs. Les imports multiples
sont sérialisés et reprennent la version du voyage après chaque ajout ; un conflit
optimiste recharge le dernier état plutôt que d'écraser une modification plus
récente.

Le frontend respecte la chaîne `API -> port injecté -> façade -> composant` et les
écrans restent paresseux derrière l'authentification. Les mises en page bornent
toutes les grilles, cartes, champs et textes à la largeur disponible, conservent
la zone sûre de navigation mobile et sont testées jusqu'aux petits écrans. Les
textes sont localisés dans les huit langues prises en charge. Le fuseau IANA de
la destination est explicite et modifiable pour tout voyage daté ; une date
préférée de collection n'est pas transformée en contrainte tant qu'elle ne peut
pas être retirée depuis cette interface.

La preuve d'architecture, les flux, les cas couverts et les limites du jalon sont
consignés dans
[`product-growth-trip-04-individual-ui-2026-09-18.md`](../../architecture/product-growth-trip-04-individual-ui-2026-09-18.md).

### État de `TRIP-05` au 18 septembre 2026

Le propriétaire peut désormais préparer une invitation depuis son voyage, choisir
un rôle de co-organisateur, participant ou lecteur, fixer une validité comprise
entre une heure et trente jours et, facultativement, réserver le lien à une adresse.
Avant la création, l'interface présente les seules informations visibles par
l'invité : titre, alias public de l'organisateur, rôle, période ramenée au mois,
taille du groupe par tranche et expiration. Les parcs, journées, membres, votes,
contraintes et notes ne quittent jamais le périmètre privé.

Le lien contient 256 bits aléatoires et n'est conservé en base que sous forme de
condensat SHA-256. Une copie chiffrée, liée à l'opération et à l'auteur, permet de
rejouer exactement une création dont la réponse réseau aurait été perdue ; elle
est effacée lors de la révocation. Les adresses ciblées sont normalisées puis
protégées par HMAC versionné. Vingt invitations actives au maximum sont garanties
par des slots uniques, y compris en concurrence, et l'expiration utilise l'heure
du serveur MongoDB.

L'aperçu public est accessible sans compte mais rendu côté client, non indexable,
non archivable et sans cache de transfert SSR afin que le token ne soit pas injecté
dans un HTML partagé. Une invitation inconnue, expirée, utilisée ou révoquée
produit le même résultat indisponible. Le propriétaire retrouve uniquement les
informations métier utiles de ses liens en attente et peut les révoquer sans que
les identifiants internes ou empreintes techniques soient affichés.

`TRIP-05` ne modifie pas encore les membres du voyage : la consommation atomique
du lien, l'acceptation ou le refus et la gestion des participants appartiennent à
`TRIP-06`. Le schéma de persistance, les frontières et les séquences sont détaillés
dans
[`product-growth-trip-05-opaque-invitations-2026-09-18.md`](../../architecture/product-growth-trip-05-opaque-invitations-2026-09-18.md).

## 1. Vision produit

Un groupe doit pouvoir transformer des envies dispersées en programme commun :

- parcs candidats ;
- dates possibles ;
- participants ;
- contraintes choisies ;
- attractions prioritaires ;
- votes et désaccords ;
- ordre provisoire des jours ;
- informations officielles ;
- décisions et modifications visibles ;
- export simple.

La croissance par invitation est acceptable parce que l’invitation est nécessaire à la collaboration. Elle ne doit pas être artificiellement exigée pour utiliser une fonction individuelle.

## 2. Objectifs

- Créer un agrégat de voyage avec rôles et versions.
- Permettre un plan mono-parc, multi-parcs et multi-jours.
- Inviter par lien opaque, e-mail facultatif ou compte existant.
- Donner un aperçu avant acceptation.
- Permettre les priorités `indispensable`, `souhaité`, `facultatif`, `pas pour moi`.
- Distinguer choix du groupe et faits officiels.
- Intégrer les résultats du comparateur sans les figer comme vérité.
- Détecter les incohérences d’ouverture ou de trajet.
- Gérer révocation, expiration, départ d’un participant et suppression.
- Ne stocker que les contraintes utiles au voyage.

## 3. Non-objectifs

- messagerie temps réel ;
- réseau social ;
- paiement partagé ;
- réservation d’hôtel ou de billet ;
- géolocalisation ;
- emploi du temps minute par minute ;
- prédiction de files ;
- gestion juridique d’un groupe de mineurs ;
- collecte de noms réels obligatoire ;
- partage public automatique ;
- notification push.

## 4. Agrégat `TripPlan`

```csharp
public sealed class TripPlan
{
    public TripPlanId Id { get; }
    public string OwnerUserId { get; }
    public string Title { get; private set; }
    public TripDateProposal DateProposal { get; private set; }
    public TripPlanStatus Status { get; private set; }
    public TripPlanAccessScope AccessScope { get; private set; }
    public long Version { get; private set; }
    public DateTime CreatedAtUtc { get; }
    public DateTime UpdatedAtUtc { get; private set; }
}
```

### 4.1 Statuts

- `Draft` ;
- `OpenForVotes` ;
- `Decided` ;
- `Completed` ;
- `Archived` ;
- `Cancelled`.

Transitions explicites et auditables. `Completed` ne crée pas automatiquement des visites dans le Passeport ; il propose à chaque participant de confirmer ses propres visites.

### 4.2 Dates

- date fixe ;
- plage ;
- dates candidates ;
- précision jour requise uniquement lorsque le programme est fixé ;
- fuseau de la destination ;
- aucune déduction depuis l’invitation.

## 5. Participants et permissions

### 5.1 Rôles

- `Owner` : gère le plan, les rôles et la suppression ;
- `Editor` : modifie le programme et les candidats ;
- `Participant` : vote, ajoute ses contraintes et préférences ;
- `Viewer` : lecture du voyage, tout en gardant le contrôle de ses propres données
  partagées.

### 5.2 Permissions fines

| Action | Owner | Editor | Participant | Viewer |
|---|---:|---:|---:|---:|
| Renommer | Oui | Oui | Non | Non |
| Inviter/révoquer | Oui | Selon option | Non | Non |
| Ajouter un parc candidat | Oui | Oui | Option | Non |
| Modifier programme décidé | Oui | Oui | Non | Non |
| Voter | Oui | Oui | Oui | Non |
| Créer, remplacer ou retirer ses contraintes | Oui | Oui | Oui | Oui |
| Voir contraintes détaillées d’autrui | Seulement si partagées | Seulement si partagées | Seulement si partagées | Non |
| Supprimer le voyage | Oui | Non | Non | Non |
| Quitter | Non sans transfert | Oui | Oui | Oui |

### 5.3 Minimisation

Un participant peut être représenté par :

- identifiant de compte ;
- alias dans le voyage ;
- état d’invitation ;
- rôle ;
- préférences partagées ;
- aucune adresse, date de naissance ou donnée médicale.

Les profils de groupe `FIT` ne sont pas copiés en totalité. Chaque membre choisit
seul les contraintes qu'il partage avec ce voyage et peut les remplacer ou les
retirer quel que soit son rôle. Le propriétaire ne peut ni les choisir à sa place,
ni empêcher leur suppression.

## 6. Invitations

## 6.1 `TripInvitation`

- token opaque ;
- voyage ;
- rôle proposé ;
- initiateur ;
- destinataire facultatif ;
- expiration ;
- une seule utilisation ; aucune invitation multi-usage dans la première version ;
- état ;
- politique d’aperçu ;
- date de révocation ;
- audit.

## 6.2 Flux

1. l’invitant choisit rôle et expiration ;
2. aperçu de ce que l’invité verra ;
3. lien créé ;
4. l’invité ouvre une page limitée ;
5. il voit titre, période approximative, initiateur affiché et rôle ;
6. il se connecte ou crée un compte uniquement pour participer durablement ;
7. il accepte ou refuse ;
8. le token est consommé ;
9. le plan affiche le nouveau participant.

Ne pas demander de compte pour consulter l’aperçu. Ne pas exposer les contraintes des autres avant acceptation.

## 6.3 Révocation et sécurité

- token non dérivable ;
- TTL ;
- rate limit ;
- rotation ;
- ancien token invalide ;
- aucune énumération ;
- protection contre réutilisation ;
- invitation à une adresse donnée non transférable si cette option est choisie ;
- journal des acceptations/refus ;
- suppression de l’adresse d’invitation après durée définie.

## 7. Candidats et programme

## 7.1 `TripParkCandidate`

- parc ;
- jours candidats ;
- source de l’ajout : manuel, wishlist, comparateur ;
- explication `FIT` conservée comme snapshot daté ;
- état `Proposed`, `Shortlisted`, `Selected`, `Rejected` ;
- note collective facultative ;
- auteur ;
- version.

Le snapshot de recommandation ne remplace pas un recalcul. L’interface affiche « calculé le… » et permet actualisation.

## 7.2 `TripDayPlan`

- date ;
- parc sélectionné ;
- horaires officiels connus ;
- heure d’arrivée souhaitée facultative ;
- attractions prioritaires ;
- repas/événements sous forme de blocs simples ;
- notes privées au groupe ;
- ordre manuel ;
- aucun calcul de file live.

## 7.3 Priorités d’attractions

Par participant et élément :

- `MustDo` ;
- `WantToDo` ;
- `Optional` ;
- `NotForMe` ;
- `Unknown`.

Ajouter une raison facultative structurée : sensations, taille, déjà fait, indisponible, autre. Pas de justification obligatoire.

## 8. Synthèse des préférences

### 8.1 Résultats

Pour chaque élément :

- nombre de `MustDo` ;
- nombre de `WantToDo` ;
- nombre de `NotForMe` ;
- participants sans réponse ;
- compatibilité connue/inconnue ;
- conflit ;
- statut officiel ;
- source et date.

### 8.2 Pas de vote majoritaire aveugle

Une attraction aimée par trois personnes mais impossible pour la quatrième ne devient pas « recommandée pour tout le groupe ». L’interface distingue :

- priorité collective ;
- contraintes individuelles ;
- possibilité de séparation ;
- décision manuelle.

### 8.3 Décision

Le propriétaire ou éditeur peut marquer :

- retenu ;
- groupe séparé ;
- optionnel ;
- exclu ;
- à revoir.

La raison et l’auteur sont auditables. Le système ne prétend pas résoudre automatiquement les désaccords.

## 9. Cohérence du programme

Règles de validation :

- parc ouvert confirmé, fermé confirmé ou inconnu ;
- deux parcs ne peuvent occuper simultanément un jour entier sans avertissement ;
- trajet entre parcs affiché si connu ;
- dates du voyage ;
- parc candidat encore visible ;
- nouvel horaire officiel après décision ;
- attraction fermée ;
- incompatibilité de profil nouvellement connue ;
- données périmées.

Les avertissements ne modifient pas automatiquement le plan.

## 10. Collaboration et concurrence

- version optimiste du plan ;
- opérations fines plutôt que remplacement complet ;
- lease et epoch communs à toute écriture dans une collection enfant ;
- suppression bloquant les nouvelles leases et attendant les écritures autorisées
  avant de déclarer la purge terminée ;
- journal d’activité ;
- conflits affichés ;
- idempotency keys ;
- ordre via positions stables ;
- aucun temps réel obligatoire : polling léger ou actualisation manuelle initiale ;
- SignalR seulement si besoin observé ;
- notification Web via `WATCH` pour changements importants, opt-in.

## 11. Modèle de données

Collections :

- `trip-plans` ;
- `trip-invitations` ;
- `trip-member-constraints` ;
- `trip-park-candidates` ;
- `trip-day-plans` ;
- `trip-item-preferences` ;
- `trip-audit-events`.

Indexes :

- `{ OwnerUserId, Status, UpdatedAtUtc }` ;
- `{ Members.UserId, Status, UpdatedAtUtc }` ;
- token invitation unique + TTL ;
- unicité d’un `UserId` dans les membres vérifiée par le Core et l’écriture conditionnelle du plan ;
- unique `(TripId, UserId, ParkItemId)` préférence ;
- unique `(TripId, Date)` jour si un seul plan par date ;
- `{ TripId, Sequence }` ;
- audit par voyage/date.

La décision `TRIP-01` embarque les membres bornés dans `trip-plans` afin que le
propriétaire unique, le transfert et les rôles soient atomiques. Les contraintes,
préférences, candidats, jours, invitations et événements potentiellement nombreux
restent séparés.

## 12. Ports et cas d’usage

Ports :

```text
ITripPlanRepository
ITripInvitationRepository
ITripMemberConstraintRepository
ITripPreferenceRepository
ITripOfficialDataReader
ITripFitSnapshotReader
ITripAuditWriter
ITripExportWriter
```

Commandes/requêtes :

- `CreateTripPlanCommand` ;
- `UpdateTripPlanCommand` ;
- `ChangeTripStatusCommand` ;
- `CreateTripInvitationCommand` ;
- `AcceptTripInvitationCommand` ;
- `RevokeTripInvitationCommand` ;
- `ChangeTripParticipantRoleCommand` ;
- `LeaveTripCommand` ;
- `AddTripParkCandidateCommand` ;
- `SelectTripParkCandidateCommand` ;
- `UpsertTripDayPlanCommand` ;
- `SetTripItemPreferenceCommand` ;
- `BulkSetTripItemPreferencesCommand` ;
- `GetTripPlanQuery` ;
- `GetTripDecisionSummaryQuery` ;
- `ValidateTripPlanQuery` ;
- `ExportTripPlanQuery` ;
- `DeleteTripPlanCommand`.

## 13. API

```text
POST   /api/me/trips
GET    /api/me/trips
GET    /api/me/trips/{tripId}
PATCH  /api/me/trips/{tripId}
DELETE /api/me/trips/{tripId}
POST   /api/me/trips/{tripId}/status

POST   /api/me/trips/{tripId}/invitations
GET    /api/public/trip-invitations/{token}/preview
POST   /api/public/trip-invitations/{token}/accept
DELETE /api/me/trips/{tripId}/invitations/{invitationId}
PATCH  /api/me/trips/{tripId}/participants/{participantId}
DELETE /api/me/trips/{tripId}/participants/me

POST   /api/me/trips/{tripId}/parks
PATCH  /api/me/trips/{tripId}/parks/{candidateId}
DELETE /api/me/trips/{tripId}/parks/{candidateId}
PUT    /api/me/trips/{tripId}/days/{date}
DELETE /api/me/trips/{tripId}/days/{date}

PUT    /api/me/trips/{tripId}/preferences/{parkItemId}
POST   /api/me/trips/{tripId}/preferences:batch
GET    /api/me/trips/{tripId}/decision-summary
GET    /api/me/trips/{tripId}/validation
GET    /api/me/trips/{tripId}/export
```

Le contrôleur dérive l’utilisateur du contexte authentifié. L’Application vérifie les permissions.

## 14. Interface Angular

```text
features/profile/trips/
  pages/trip-list-page/
  pages/trip-overview-page/
  pages/trip-candidates-page/
  pages/trip-day-page/
  pages/trip-preferences-page/
  pages/trip-participants-page/
  pages/trip-settings-page/
  components/trip-status-stepper/
  components/participant-role-table/
  components/park-candidate-card/
  components/group-preference-matrix/
  components/trip-validation-panel/
  state/trip.facade.ts
  state/trip-preferences.facade.ts
```

### UX

- création en brouillon ;
- ajout depuis wishlist/comparateur ;
- aperçu d’invitation ;
- matrice accessible ;
- filtres par participant/statut ;
- résumé des désaccords ;
- distinction « fait officiel » / « choix du groupe » ;
- journal d’activité ;
- export imprimable ;
- responsive ;
- aucune fonctionnalité critique dépendante du drag-and-drop.

## 15. Passage au Passeport après le voyage

À la date passée :

- chaque participant reçoit une proposition privée ;
- parc/jour préremplis ;
- aucune visite créée automatiquement ;
- priorités ne deviennent pas des rides accomplis ;
- l’utilisateur confirme les éléments réellement faits ;
- les données des autres participants ne sont pas copiées ;
- le plan peut rester archivé.

## 16. Partage et export

Première version :

- accès participants ;
- export PDF/HTML imprimable ou calendrier après validation technique ;
- export JSON ;
- lien d’invitation, pas lien public général.

Partage public éventuel :

- passe par `SHARE` ;
- snapshot minimisé ;
- aucun profil individuel ;
- aucune contrainte sensible ;
- date exacte optionnelle ;
- révocation.

## 17. Confidentialité

- voyage privé par défaut ;
- participant voit seulement les données nécessaires ;
- chaque membre choisit ses contraintes partagées ;
- pas de nom réel obligatoire ;
- pas de profil mineur public ;
- export par participant de ses données et du plan accessible ;
- suppression d’un participant retire ses préférences après politique annoncée ;
- possibilité d’anonymiser les décisions historiques plutôt que casser le plan ;
- propriétaire ne peut pas empêcher un participant de supprimer ses données personnelles ;
- suppression du voyage révoque invitations et notifications.

## 18. Audit et modération

- qui a invité ;
- qui a accepté ;
- changement de rôle ;
- ajout/retrait de parc ;
- décision de jour ;
- modification de préférence ;
- suppression ;
- export.

Pas de modération publique initiale puisque le voyage est privé. Les champs texte restent bornés et sécurisés.

## 19. Observabilité

Produit :

- plans créés ;
- parc ajouté ;
- invitation créée/acceptée/refusée/expirée ;
- premier vote ;
- plan décidé ;
- second participant actif ;
- passage vers Passeport après voyage ;
- conflits rencontrés ;
- données officielles inconnues.

Technique :

- conflits de version ;
- latence matrice ;
- taille des batchs ;
- invitations abusives ;
- erreurs d’autorisation ;
- volume d’audit ;
- requêtes N+1.

## 20. Tests obligatoires

### Core/Application

- transitions ;
- rôles ;
- owner unique ;
- transfert avant départ du owner ;
- token expiré/révoqué ;
- acceptation double ;
- préférence par personne ;
- incompatibilité ;
- programme incohérent ;
- date/heure ;
- plan passé ;
- suppression participant.

### Infrastructure/API

- indexes ;
- concurrence ;
- idempotence ;
- TTL ;
- permissions croisées ;
- export ;
- audit ;
- aucune fuite dans preview ;
- payload borné.

### Angular/E2E

1. créer un voyage ;
2. importer deux parcs de wishlist ;
3. inviter un participant ;
4. accepter ;
5. chacun vote ;
6. afficher désaccord ;
7. fixer un parc ;
8. modifier un horaire source et afficher avertissement ;
9. révoquer un participant ;
10. après date, confirmer une visite dans le Passeport.

Accessibilité : matrice, focus, ordre, mobile, lecteurs d’écran et huit langues.

## 21. Déploiement

### Pilote A

- voyage individuel ;
- candidats ;
- jours ;
- export ;
- pas d’invitation.

### Pilote B

- invitations ;
- rôles ;
- priorités ;
- audit.

### Pilote C

- intégration `FIT` et `WATCH` ;
- validation d’ouverture ;
- transition vers Passeport.

Pas de chat tant que les testeurs ne démontrent pas qu’un commentaire structuré et les outils existants sont insuffisants.

## 22. Découpage recommandé en PR

| PR | Contenu | Critère |
|---|---|---|
| `TRIP-01` | ADR agrégat, rôles et confidentialité | Invariants validés |
| `TRIP-02` | Core/persistance voyage individuel | CRUD fiable — implémenté le 17 septembre 2026 |
| `TRIP-03` | Candidats et jours | Programme cohérent — implémenté le 17 septembre 2026 |
| `TRIP-04` | UI individuelle + wishlist | Valeur sans invitation — implémenté le 18 septembre 2026 |
| `TRIP-05` | Invitations opaques | Preview minimisé — implémenté le 18 septembre 2026 |
| `TRIP-06` | Participants/rôles | Permissions testées |
| `TRIP-07` | Préférences par élément | Unicité et batch |
| `TRIP-08` | Synthèse/conflits | Pas de majorité aveugle |
| `TRIP-09` | Validation calendrier/trajet | Faits distingués des choix |
| `TRIP-10` | Audit/concurrence | Modifications reconstituables |
| `TRIP-11` | Export | Plan portable |
| `TRIP-12` | Transition Passeport | Confirmation individuelle |
| `TRIP-13` | Pilote collaboratif | Gate franchie |

## 23. Gate finale `TRIP-G`

- un voyage individuel apporte déjà de la valeur ;
- l’invitation n’est pas un prétexte artificiel à la création de compte ;
- rôles et permissions sont appliqués dans l’Application ;
- un participant contrôle et supprime ses données ;
- l’aperçu d’invitation ne fuit aucune contrainte ;
- faits officiels et choix du groupe sont distincts ;
- les désaccords restent visibles ;
- aucun vote majoritaire ne transforme une incompatibilité en compatibilité ;
- les modifications concurrentes ne s’écrasent pas silencieusement ;
- aucune visite n’est créée automatiquement après le voyage ;
- le produit fonctionne sans chat, GPS ou paiement ;
- les premiers groupes arrivent à une décision et réutilisent le plan sur une visite réelle.
