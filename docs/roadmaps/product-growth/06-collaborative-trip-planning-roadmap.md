# Roadmap 06 — Voyages et journées de parc collaboratifs

> Code programme : `TRIP`
>
> Dépendances : collections `WATCH`, moteur `FIT`, données d’ouverture fiables et politiques de partage `SHARE`.
>
> Périmètre : préparation Web avant la visite. Aucun chat généraliste, aucune position partagée, aucune réservation intégrée et aucune optimisation live de journée dans cette roadmap.

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
    public Guid Id { get; }
    public Guid OwnerUserId { get; }
    public string Title { get; private set; }
    public TripDateRange DateRange { get; private set; }
    public TripPlanStatus Status { get; private set; }
    public TripPlanPrivacy Privacy { get; private set; }
    public int Version { get; private set; }
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
- `Viewer` : lecture seule.

### 5.2 Permissions fines

| Action | Owner | Editor | Participant | Viewer |
|---|---:|---:|---:|---:|
| Renommer | Oui | Oui | Non | Non |
| Inviter/révoquer | Oui | Selon option | Non | Non |
| Ajouter un parc candidat | Oui | Oui | Option | Non |
| Modifier programme décidé | Oui | Oui | Non | Non |
| Voter | Oui | Oui | Oui | Non |
| Modifier ses contraintes | Oui | Oui | Oui | Non |
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

Les profils de groupe `FIT` ne sont pas copiés en totalité. Le propriétaire choisit les contraintes partagées avec ce voyage.

## 6. Invitations

## 6.1 `TripInvitation`

- token opaque ;
- voyage ;
- rôle proposé ;
- initiateur ;
- destinataire facultatif ;
- expiration ;
- nombre maximal d’utilisations, `1` par défaut ;
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
- `trip-participants` ;
- `trip-invitations` ;
- `trip-park-candidates` ;
- `trip-day-plans` ;
- `trip-item-preferences` ;
- `trip-audit-events`.

Indexes :

- `{ OwnerUserId, UpdatedAtUtc }` ;
- `{ ParticipantUserId, Status, UpdatedAtUtc }` ;
- token invitation unique + TTL ;
- unique `(TripId, UserId)` participant ;
- unique `(TripId, UserId, ParkItemId)` préférence ;
- unique `(TripId, Date)` jour si un seul plan par date ;
- `{ TripId, Sequence }` ;
- audit par voyage/date.

Évaluer l’embarquement de petits sous-documents versus collections séparées. Les préférences potentiellement nombreuses restent séparées.

## 12. Ports et cas d’usage

Ports :

```text
ITripPlanRepository
ITripParticipantRepository
ITripInvitationRepository
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
| `TRIP-02` | Core/persistance voyage individuel | CRUD fiable |
| `TRIP-03` | Candidats et jours | Programme cohérent |
| `TRIP-04` | UI individuelle + wishlist | Valeur sans invitation |
| `TRIP-05` | Invitations opaques | Preview minimisé |
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
