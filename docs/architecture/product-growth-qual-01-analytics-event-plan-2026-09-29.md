# QUAL-01 — Décision analytics et catalogue d’événements produit

> Date : 29 septembre 2026  
> Statut : accepté  
> Portée : programme `RANK`, `PASS`, `SHARE`, `FIT`, `WATCH`, `TRIP`, `HIST`, `LIVE`  
> Décision suivante : `QUAL-02`, infrastructure commune de feature flags et kill switches

## 1. Décision

Le programme adopte trois canaux volontairement séparés :

1. **analytics produit consentis** : événements peu nombreux, typés et sans
   identifiant métier, envoyés depuis le navigateur vers Matomo uniquement après
   acceptation des cookies facultatifs ;
2. **indicateurs métier internes** : agrégats calculés côté serveur à partir de
   l’état métier ou d’événements first-party déjà nécessaires au fonctionnement ;
3. **métriques techniques** : compteurs, durées et erreurs nécessaires à
   l’exploitation, sans contenu utilisateur ni finalité marketing.

Ces canaux ne sont pas interchangeables. Une métrique technique ne devient pas un
outil de profilage, un événement Matomo ne devient pas une preuve métier et une
absence de consentement ne doit jamais dégrader une fonctionnalité.

Le projet conserve Matomo comme destination des événements produit consentis. Il
n’ajoute ni data warehouse, ni broker, ni nouvel outil marketing. Microsoft
Clarity reste un outil facultatif distinct : il ne porte aucun événement canonique
du présent catalogue et ne peut pas servir à calculer une gate produit.

## 2. Pourquoi cette décision

Le dépôt contient déjà plusieurs formes de mesure adaptées à leur contexte :

- événements Matomo typés pour le Passeport et le partage ;
- observations first-party agrégées pour FIT, WATCH, PASS et TRIP ;
- compteurs `System.Diagnostics.Metrics` pour les jobs et le live ;
- journaux d’audit métier nécessaires à la correction et au support.

Les remplacer par une collecte universelle augmenterait la sensibilité, la charge
du VPS et le risque de divergence. La décision fixe donc un vocabulaire commun et
des frontières, puis prévoit une convergence incrémentale dans `QUAL-05` sans
dupliquer les données existantes.

## 3. Principes non négociables

- Pas de collecte de tous les clics.
- Pas d’identifiant `UserId`, `VisitId`, `ParkId`, `ParkItemId`, `TripId`,
  `ShareId`, token ou URL privée dans un événement produit.
- Pas de nom, e-mail, alias, commentaire, texte libre, recherche libre ou contenu
  de notification.
- Pas de note exacte, date exacte, position précise, taille, âge ou besoin
  d’accessibilité dans un outil analytics tiers.
- Pas de liste de parcs, d’attractions, de participants ou d’éléments sélectionnés.
- Les volumes et durées utilisent des classes fermées documentées, jamais une
  valeur arbitraire lorsque la précision n’est pas nécessaire.
- Le refus ou le retrait du consentement arrête les outils facultatifs et supprime
  leurs cookies connus ; le parcours métier reste identique.
- Les pages administrateur, les invitations opaques et les surfaces privées à
  contenu sensible ne doivent pas alimenter une relecture de session.
- Les audits de sécurité et de concurrence restent séparés des analytics produit.
- Aucun indicateur isolé ne justifie une généralisation, une relance ou une
  notification plus intrusive.

## 4. Enveloppe canonique

Le modèle logique commun est le suivant, même lorsqu’un adaptateur externe encode
ensuite les champs selon son propre protocole :

```json
{
  "event": "visit_completed",
  "schemaVersion": 1,
  "properties": {
    "entryCountBucket": "5-9",
    "hasTemporalRating": true,
    "datePrecision": "Day",
    "source": "park-page"
  }
}
```

Chaque définition du catalogue possède obligatoirement :

- un nom anglais en `snake_case`, stable et orienté résultat ;
- une version entière de schéma ;
- un propriétaire fonctionnel ;
- une finalité explicite ;
- un instant d’émission non ambigu ;
- une liste fermée de propriétés autorisées ;
- une classification de sensibilité ;
- une rétention ;
- des tests de contrat avant activation ;
- une date et un plan de retrait en cas de remplacement.

Une propriété non déclarée est interdite par défaut. Une évolution additive garde
la version ; une modification de sens, de type ou de valeurs possibles crée une
nouvelle version.

## 5. Classification et rétention

| Canal | Contenu admis | Identité | Rétention maximale |
|---|---|---|---:|
| Matomo produit | résultat d’action et classes fermées | aucune identité applicative transmise | 180 jours bruts |
| Agrégat produit interne | compteurs journaliers et cohortes minimales | pseudonymisation seulement si le calcul l’exige | 25 mois agrégés |
| Métrique technique | compteur, durée, taille, code d’issue | aucune donnée utilisateur | 30 jours détaillés, 13 mois agrégés |
| Audit métier | preuve nécessaire à la correction/sécurité | selon le domaine et ses droits d’accès | politique du domaine, jamais répliquée dans Matomo |

La fenêtre de 25 mois permet une comparaison de deux saisons sans conserver les
événements tiers bruts. Toute exception doit être documentée dans la matrice
`QUAL-04`, avec finalité, propriétaire et procédure de purge.

## 6. Sources et responsabilités

```text
Action dans l’interface
  └─ événement produit autorisé et consentement présent
       └─ port analytics Angular
            └─ adaptateur Matomo

État métier durable / audit déjà nécessaire
  └─ calcul first-party agrégé
       └─ diagnostic administrateur borné

Job, cache, fournisseur ou endpoint
  └─ métrique technique sans contenu utilisateur
       └─ observabilité et alerting
```

- **Core** : définit les invariants métier et les vocabulaires first-party qui
  font partie du domaine ; il ne dépend d’aucun fournisseur analytics.
- **Application** : décide qu’un résultat métier a réellement eu lieu et expose
  les ports nécessaires aux agrégats internes.
- **Infrastructure** : persiste les agrégats autorisés et publie les métriques
  techniques.
- **WebAPI** : valide des commandes bornées et n’accepte jamais un nom
  d’événement libre fourni par un client.
- **Angular** : porte les événements d’interaction consentis derrière des ports
  typés ; aucun composant ne construit directement une requête Matomo.

## 7. Catalogue produit canonique

Les propriétés listées sont les seules admises. `status` précise si le nom existe
déjà dans le dépôt ou s’il est réservé pour la convergence `QUAL-05`.

### 7.1 RANK — comprendre la confiance des classements

| Événement | Émission | Propriétés autorisées | Statut |
|---|---|---|---|
| `ranking_methodology_opened` | méthode effectivement ouverte | `surface` | réservé |
| `ranking_evidence_details_opened` | détail de preuve ouvert | `evidenceLevel` | réservé |
| `provisional_rating_state_seen` | état provisoire rendu visible | `evidenceLevel`, `surface` | réservé |
| `ranking_data_issue_reported` | signalement accepté par l’API | `issueKind` | réservé |

Les valeurs de note, le scope classé et la cible consultée sont interdits.

### 7.2 PASS — vérifier que le Passeport apporte une valeur récurrente

| Événement | Émission | Propriétés autorisées | Statut |
|---|---|---|---|
| `passport_opened` | accueil du Passeport chargé | `source` | actif |
| `visit_creation_started` | formulaire réellement ouvert | `source`, `datePrecision` | actif |
| `visit_created` | création confirmée par le stockage | `source`, `datePrecision` | actif |
| `visit_completed` | transition vers terminée confirmée | `source` | actif |
| `visit_reopened` | réouverture confirmée | `source` | actif |
| `second_visit_recorded` | seconde visite locale reconnue | `source` | actif |
| `ride_occurrence_added` | ajout confirmé | `source`, `countBucket` | actif |
| `temporal_rating_added` | note temporelle confirmée | `source`, `targetType` | actif |
| `passport_statistics_opened` | statistiques chargées | `source`, `scope` | actif |
| `passport_export_requested` | export déclenché | `source`, `format` | actif |
| `passport_deletion_started` | parcours de suppression ouvert | `source` | actif |
| `passport_deletion_completed` | suppression confirmée | `source` | actif |

`source`, `scope`, `format`, `targetType`, `datePrecision` et `countBucket` sont
des unions fermées déjà définies dans le modèle Angular. La north star « seconde
visite » est calculée first-party à partir des visites durables ; Matomo ne doit
pas recevoir d’identifiant permettant de reconstruire une cohorte individuelle.

### 7.3 SHARE — vérifier qu’un partage reste volontaire et contrôlable

| Événement | Émission | Propriétés autorisées | Statut |
|---|---|---|---|
| `share_activation_started` | éditeur de partage ouvert | `recapType` | actif |
| `share_preview_created` | aperçu confirmé par l’API | `recapType` | actif |
| `share_published` | publication confirmée | `recapType` | actif |
| `share_revoked` | révocation confirmée | `recapType` | actif |
| `share_rotated` | rotation confirmée | `recapType` | actif |
| `share_opened` | page publique rendue | `recapType` | actif |
| `share_cta_passport_started` | CTA vers le Passeport activé | `recapType` | actif |
| `share_render_failed` | rendu public impossible | `recapType`, `failureKind` | actif à compléter en v2 |

L’identifiant opaque, les personnes comparées, la date de visite, les champs
publiés et le contenu social sont interdits.

### 7.4 FIT — savoir si l’aide au choix aboutit sans masquer les inconnues

| Événement | Émission | Propriétés autorisées | Statut |
|---|---|---|---|
| `park_fit_search_started` | demande validée | aucune | actif first-party à renommer |
| `park_fit_search_completed` | résultats calculés | `resultBand`, `unknownLevel`, `durationBand`, `methodVersion` | actif first-party à renommer |
| `park_fit_search_failed` | échec classé | `failureKind`, `durationBand` | actif first-party à renommer |
| `park_fit_explanation_opened` | explication ouverte | `unknownLevel`, `methodVersion` | actif first-party à renommer |
| `park_fit_comparison_opened` | comparaison ouverte | `comparisonSize`, `methodVersion` | actif first-party à renommer |

Les contraintes saisies, la localisation, les parcs et les problèmes de qualité
détaillés restent first-party et ne sont pas envoyés à Matomo.

### 7.5 WATCH — vérifier la confiance sans optimiser le volume d’alertes

| Événement | Émission | Propriétés autorisées | Statut |
|---|---|---|---|
| `notification_center_opened` | centre chargé | aucune | actif first-party |
| `notification_source_opened` | source publique ouverte | `notificationKind` | actif first-party |
| `misleading_alert_reported` | signalement accepté | `notificationKind` | actif first-party |
| `watch_subscription_removed` | suppression confirmée | `subscriptionKind` | actif serveur |
| `duplicate_delivery_prevented` | déduplication exécutée | `deliveryChannel` | métrique technique |

Le nombre d’alertes et le taux de clic e-mail ne constituent pas seuls un signal
de succès. Aucune cible surveillée n’est exportée vers un tiers.

### 7.6 TRIP — mesurer la préparation utile, pas les relations sociales

| Événement | Émission | Propriétés autorisées | Statut |
|---|---|---|---|
| `trip_created` | plan créé | `mode`, `dayCountBucket` | réservé |
| `trip_candidate_added` | candidat confirmé | `source` | réservé |
| `trip_invitation_accepted` | invitation acceptée | `role` | réservé |
| `trip_preferences_recorded` | batch confirmé | `countBucket` | réservé |
| `trip_conflict_opened` | synthèse de désaccord ouverte | `conflictCountBucket` | réservé |
| `trip_decision_recorded` | décision confirmée | `decisionKind` | réservé |
| `trip_export_requested` | export demandé | `format` | réservé |
| `trip_passport_transition_confirmed` | visite créée après confirmation individuelle | `visitCountBucket` | réservé |

Les participants, invitations, destinations, dates, horaires et préférences
exactes sont interdits. Les métriques pilote actuelles restent des agrégats
first-party dérivés des documents et audits existants.

### 7.7 HIST — vérifier la compréhension de la couverture historique

| Événement | Émission | Propriétés autorisées | Statut |
|---|---|---|---|
| `history_timeline_opened` | frise rendue | `coverageLevel` | réservé |
| `history_year_opened` | snapshot rendu | `coverageLevel`, `yearKind` | réservé |
| `history_comparison_opened` | comparaison rendue | `coverageLevel` | réservé |
| `history_source_opened` | source publique ouverte | `sourceKind` | réservé |
| `history_data_issue_reported` | signalement accepté | `issueKind` | réservé |
| `history_passport_context_opened` | contexte historique personnel rendu | `verificationState` | réservé |

L’année exacte, le parc, l’élément, la visite et l’URL de source ne sont pas des
propriétés analytics. La fréquence d’ouverture n’est jamais une preuve historique.

### 7.8 LIVE — mesurer la compréhension, jamais la localisation du visiteur

| Événement | Émission | Propriétés autorisées | Statut |
|---|---|---|---|
| `live_status_seen` | état live rendu | `freshnessState`, `operatingState` | réservé |
| `live_refresh_requested` | actualisation manuelle | `outcome` | réservé |
| `live_forecast_seen` | prévision éligible rendue | `confidenceBand`, `methodVersion` | réservé |
| `live_forecast_method_opened` | explication ouverte | `methodVersion` | réservé |
| `live_alert_created` | alerte temporaire confirmée | `thresholdBucket` | réservé |
| `live_alert_removed` | suppression confirmée | aucune | réservé |

Le parc, l’attraction, l’attente exacte, le seuil exact, l’heure locale et toute
position sont interdits dans Matomo. Les appels fournisseur, retards, quotas,
fraîcheur, quarantaines et circuits ouverts restent des métriques techniques.

## 8. Propriétés communes autorisées

Les propriétés communes sont optionnelles et fermées :

| Propriété | Valeurs admises |
|---|---|
| `surface` | `methodology`, `ranking-card`, `park-page`, `item-page`, `passport`, `profile` |
| `source` | valeurs propres au domaine, sans identifiant |
| `failureKind` | `validation`, `rate-limited`, `technical`, `unavailable` |
| `countBucket` | `one`, `two-to-five`, `six-plus` ou classes documentées par le domaine |
| `durationBand` | `<500ms`, `500-1499ms`, `1500-2999ms`, `>=3000ms` |
| `methodVersion` | version publique courte de la méthode, jamais un hash de données |

Un adaptateur doit rejeter à la compilation ou à la validation toute propriété
libre. Les valeurs inconnues ne sont pas concaténées dans un label.

## 9. État actuel et trajectoire de convergence

| Élément existant | Décision |
|---|---|
| Ports Matomo PASS et SHARE | conservés, puis raccordés au catalogue commun dans `QUAL-05` |
| Consentement cookies facultatifs | conservé ; refus et retrait restent sans effet métier |
| Observations FIT first-party | conservées ; noms convertis au catalogue à la frontière HTTP |
| Agrégats PASS/WATCH/TRIP | conservés ; pas de duplication vers Matomo |
| Métriques des jobs et du live | conservées comme observabilité technique |
| Audits métier | conservés dans leurs domaines, exclus de l’analytics tiers |
| Clarity | hors catalogue ; audit des routes privées et masquage dans `QUAL-04`/`QUAL-08` |

`QUAL-05` introduira les helpers typés communs, la validation des propriétés et la
version de schéma. La migration remplacera les adaptateurs locaux directement ;
elle ne laissera pas deux moteurs actifs.

## 10. Gates et conditions d’arrêt

Une famille d’événements ne peut être activée que si :

- la question métier à laquelle elle répond est écrite ;
- l’action de succès et l’échec sont distingués ;
- toutes les propriétés sont fermées et testées ;
- la rétention et la purge sont configurables ;
- le parcours sans consentement est testé ;
- les routes privées et SSR ne fuient aucun événement ;
- le volume et la charge du VPS sont estimés ;
- un propriétaire sait supprimer ou retirer l’événement.

La collecte est suspendue si une propriété interdite apparaît, si une finalité
n’est plus justifiée, si la purge échoue ou si le coût d’exploitation dépasse le
budget. La suspension ne bloque jamais le produit principal.

## 11. Vérification de `QUAL-01`

- inventaire de PASS, SHARE, FIT, WATCH, TRIP et des métriques techniques effectué ;
- séparation analytics/agrégats/observabilité décidée ;
- Matomo retenu sans nouvel outil ;
- consentement et fonctionnement sans consentement explicités ;
- catalogue des huit familles nommé et borné ;
- identifiants et contenus interdits listés ;
- versions, propriétés, rétention et retrait définis ;
- écarts existants affectés à `QUAL-04`, `QUAL-05` ou `QUAL-08` ;
- aucune donnée ni collection MongoDB modifiée par ce jalon documentaire.
