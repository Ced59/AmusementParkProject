# Roadmap 04 — « Quel parc pour nous ? » et comparaison explicable

> Code programme : `FIT`
>
> Dépendances : `RANK-G`, qualité des données publiques, instrumentation transverse et, pour la sauvegarde des projets, briques `PASS`/`WATCH`.
>
> Périmètre : moteur déterministe Web. Aucune recommandation opaque générée par IA, aucune géolocalisation obligatoire et aucune promesse de compatibilité lorsque les données manquent.

## 0. Avenant technique FOUNDATION

Les profils, recherches sauvegardées et résultats utilisent des identifiants chaîne opaques, éventuellement typés dans le Core. Les premiers calculs sont synchrones, déterministes et bornés sur le portefeuille de parcs éligibles. Aucun snapshot par combinaison libre de critères n’est créé.

Une projection ou un job de pré-calcul n’est ajouté qu’après mesure d’un p95 insuffisant malgré les indexes et la réduction du portefeuille. Les données inconnues restent `Unknown` pendant tout cache ou calcul différé ; une projection ne peut pas transformer une absence en compatibilité.

## 1. Vision produit

La recherche actuelle permet de retrouver une entité connue. Le nouveau moteur doit répondre à une question de décision :

> « Parmi les parcs correctement documentés, lesquels correspondent le mieux à notre groupe, à notre date, à notre trajet et à nos préférences — et pourquoi ? »

La valeur ne vient pas d’un score magique. Elle vient de la capacité à expliquer :

- ce que tous les membres peuvent faire ensemble ;
- ce qui nécessite de séparer le groupe ;
- les principales incompatibilités ;
- les préférences satisfaites ;
- le trajet et l’ouverture ;
- les données manquantes ou anciennes ;
- les sources officielles des restrictions sensibles.

## 2. Objectifs

- Construire un moteur de filtres durs et de préférences souples.
- Gérer un groupe composé de plusieurs profils privés.
- Distinguer « compatible », « incompatible », « inconnu » et « non applicable ».
- Calculer un résultat explicable et reproductible.
- Limiter le premier lancement à des parcs dont la complétude est démontrée.
- Offrir une comparaison côte à côte.
- Sauvegarder une recherche ou ajouter un parc à un projet sans imposer un compte avant le premier résultat.
- Documenter la version des règles et des données.
- Mesurer les abandons liés aux données inconnues.

## 3. Non-objectifs

- garantir qu’un parc laissera accéder une personne à une attraction ;
- remplacer les règles officielles ou le personnel du parc ;
- traiter un handicap comme un simple score ;
- stocker des données de santé détaillées ;
- optimiser un itinéraire de journée ;
- prédire des temps d’attente ;
- proposer une réservation ;
- favoriser un partenaire commercial ;
- recommander un parc en échange d’un paiement ;
- utiliser une IA pour combler les données absentes.

## 4. Prérequis de données

Un parc ne peut pas entrer dans le moteur seulement parce qu’il est visible. Il doit franchir une gate de complétude.

### 4.1 Données minimales par parc

- coordonnées fiables ;
- calendrier d’ouverture pour la date demandée ou statut « non publié » ;
- liste publique des éléments couverts ;
- catégories et types cohérents ;
- restrictions de taille lorsque pertinentes ;
- règles accompagné/seul lorsqu’elles existent ;
- tranche d’âge uniquement si la source la fournit ;
- offre intérieure/extérieure ;
- informations d’accessibilité sourcées lorsque proposées ;
- date de dernière vérification ;
- sources ;
- statut de complétude ;
- langues de contenu prises en charge.

### 4.2 États de qualité

- `NotAssessed` ;
- `Insufficient` ;
- `EligibleForDiscoveryOnly` ;
- `EligibleForFitComparison` ;
- `TemporarilyStale` ;
- `Suspended`.

Seuls les parcs `EligibleForFitComparison` apparaissent comme recommandations. Les autres peuvent être listés séparément avec la raison de leur absence.

### 4.3 Provenance des restrictions

Chaque règle sensible possède :

- source ;
- URL ou référence interne ;
- date de collecte ;
- date de vérification ;
- portée ;
- texte officiel résumé sans déformer ;
- langue source ;
- confiance ;
- éventuelle date d’effet ;
- statut `Official`, `OperatorProvided`, `VerifiedSecondary`, `CommunityUnverified`.

Pour l’accès à une attraction, la première version du moteur n’utilise que `Official` et `OperatorProvided`, sauf affichage explicitement non décisionnel.

## 5. Profils de groupe

## 5.1 Minimisation

Un profil n’a pas besoin d’un nom réel. Champs proposés :

- alias local, par exemple « enfant 1 » ;
- taille en centimètres ;
- âge ou tranche d’âge facultative ;
- capacité à être accompagné, non déduite de l’âge ;
- niveau de sensations accepté ;
- préférences de catégories ;
- exclusions choisies ;
- besoins d’accessibilité sous forme de capacités fonctionnelles limitées et facultatives ;
- priorité dans la décision.

Interdit dans la première version :

- diagnostic médical ;
- carte d’invalidité ;
- données biométriques ;
- localisation continue ;
- date de naissance exacte ;
- profil public d’un mineur.

## 5.2 Stockage

Avant compte :

- état de formulaire en mémoire/session ;
- stockage local facultatif après explication ;
- aucune synchronisation serveur implicite.

Avec compte :

- profils privés ;
- noms d’alias ;
- chiffrement en transit ;
- export/suppression ;
- durée de rétention contrôlable ;
- partage dans un voyage uniquement avec consentement.

## 5.3 Modèle

```csharp
public sealed class GroupProfile
{
    public Guid Id { get; }
    public Guid OwnerUserId { get; }
    public string Alias { get; private set; }
    public int? HeightCentimeters { get; private set; }
    public AgeBand? AgeBand { get; private set; }
    public ThrillTolerance? ThrillTolerance { get; private set; }
    public IReadOnlySet<ParkItemCategory> PreferredCategories { get; }
    public IReadOnlySet<ParkItemCategory> ExcludedCategories { get; }
    public AccessibilityPreferences Accessibility { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public int Version { get; private set; }
}
```

Les préférences d’accessibilité sont modélisées comme besoins choisis, jamais comme verdict automatique d’autorisation.

## 6. Moteur de compatibilité des attractions

### 6.1 Résultat par personne et élément

```csharp
public sealed record AttractionCompatibility(
    CompatibilityState State,
    IReadOnlyList<CompatibilityReason> Reasons,
    DataConfidence Confidence,
    DateTime? LastVerifiedAtUtc,
    IReadOnlyList<SourceReference> Sources);
```

`CompatibilityState` :

- `CompatibleAlone` ;
- `CompatibleWithCompanion` ;
- `Incompatible` ;
- `Unknown` ;
- `NotApplicable`.

### 6.2 Règles de taille

Le modèle doit pouvoir représenter :

- taille minimale absolue ;
- taille minimale avec accompagnateur ;
- taille minimale seul ;
- taille maximale ;
- règles combinées ;
- restrictions par véhicule ou siège ;
- règle temporaire ou saisonnière ;
- absence de donnée.

Ne pas réduire toutes les règles à un seul `MinimumHeight`.

### 6.3 Règles de groupe

Pour chaque élément :

- `EveryoneTogether` si tous sont compatibles dans une même configuration ;
- `PossibleWithSplit` si des sous-groupes sont nécessaires ;
- `Partial` si certains ne peuvent pas participer ;
- `Unknown` si au moins une donnée critique manque ;
- `None` si personne n’est compatible.

Le moteur ne suppose pas qu’un adulte peut accompagner plusieurs enfants simultanément sans information sur le véhicule.

### 6.4 Accessibilité

Première version prudente :

- afficher les informations officielles structurées ;
- permettre un filtre « informations d’accessibilité disponibles » ;
- ne pas conclure « accessible » sur un seul critère ;
- renvoyer vers le guide officiel ;
- afficher la date de vérification ;
- permettre le signalement d’une donnée obsolète ;
- éviter toute formulation médicale ou discriminante.

## 7. Critères de parc

### 7.1 Filtres durs

Exemples :

- rayon/durée maximale ;
- pays/région ;
- ouvert à la date ;
- minimum d’expériences compatibles ;
- présence obligatoire d’une catégorie ;
- budget maximal seulement si les tarifs sont suffisamment fiables ;
- disponibilité d’informations d’accessibilité ;
- langue ;
- exclusion d’un type de parc.

Un filtre dur non vérifiable ne produit pas un faux rejet. Le résultat passe en `Unknown` et l’utilisateur choisit s’il veut exclure les inconnus.

### 7.2 Préférences souples

- sensations ;
- attractions familiales ;
- animaux ;
- spectacles ;
- thématisation ;
- expériences couvertes ;
- attractions aquatiques ;
- restauration ;
- intérêt historique ;
- offre pour le groupe entier ;
- distance ;
- budget indicatif.

### 7.3 Disponibilité à une date

Distinguer :

- parc ouvert confirmé ;
- fermé confirmé ;
- calendrier non encore publié ;
- calendrier incomplet ;
- fermeture exceptionnelle ;
- horaire inconnu.

Ne jamais transformer « calendrier non publié » en « fermé » ou « ouvert ».

### 7.4 Trajet

Première version :

- distance géodésique ou trajet via fournisseur choisi ;
- origine saisie explicitement ;
- pas de conservation de l’adresse exacte sans consentement ;
- résultat daté ;
- aucune précision minute par minute si le service ne la fournit pas ;
- cache et quotas ;
- fallback distance à vol d’oiseau clairement libellé.

## 8. Score explicable

## 8.1 Séparation filtres/score

1. évaluer les filtres durs ;
2. conserver les parcs compatibles et inconnus selon préférence ;
3. calculer des sous-scores normalisés ;
4. produire une explication ;
5. afficher données manquantes et confiance.

## 8.2 Sous-scores initiaux

- `GroupCompatibility` ;
- `PreferenceCoverage` ;
- `TravelConvenience` ;
- `DateAvailability` ;
- `IndoorResilience` ;
- `BudgetFit` si données ;
- `DataConfidence` n’est pas une préférence : il borne ou pénalise le résultat et reste visible.

### 8.2.1 Compatibilité de groupe

Indicateurs :

- nombre d’éléments compatibles pour tous ;
- nombre avec séparation ;
- nombre par personne ;
- part inconnue ;
- catégories compatibles ;
- minimum individuel afin de ne pas masquer un membre très mal servi par une moyenne.

### 8.2.2 Couverture des préférences

Chaque préférence possède :

- poids choisi par l’utilisateur ;
- nombre d’éléments pertinents ;
- qualité de donnée ;
- saturation : 40 attractions d’un même type ne doivent pas nécessairement compter 40 fois plus que 10 ;
- explication.

## 8.3 Formule versionnée

Exemple initial à valider :

```text
FitScore =
  0.45 × GroupCompatibility
+ 0.25 × PreferenceCoverage
+ 0.15 × TravelConvenience
+ 0.10 × IndoorResilience
+ 0.05 × BudgetFit
```

`DateAvailability` agit comme filtre ou plafond. `DataConfidence` plafonne le score lorsque trop de critères sont inconnus.

Les poids ne sont pas cachés. Une version `park-fit-2026-01` fige :

- sous-scores ;
- normalisations ;
- poids ;
- règles d’inconnu ;
- seuils de complétude ;
- libellés.

### 8.3.1 Pas de faux pourcentage

Un score `84/100` peut être interprété comme une probabilité. Libellé recommandé :

> « Correspondance élevée selon 8 critères renseignés »

Si un nombre est affiché :

- expliquer qu’il s’agit d’un score comparatif ;
- afficher les composantes ;
- ne pas dire « 84 % de chances d’aimer » ;
- masquer le score global lorsque plus d’un seuil de données critiques manque.

## 9. Explication d’un résultat

Chaque parc retourne :

- état d’éligibilité ;
- score comparatif facultatif ;
- niveau de confiance ;
- principaux points forts ;
- incompatibilités ;
- inconnues ;
- couverture pour chaque membre ;
- distance ;
- ouverture ;
- fraîcheur ;
- sources ;
- date de calcul ;
- version de règle.

Exemple :

> **Très bonne correspondance**
>
> - 31 expériences sont compatibles avec tout le groupe selon les tailles renseignées.
> - 8 autres nécessitent un accompagnateur ou une séparation.
> - Le parc couvre fortement les préférences « familial » et « dark ride ».
> - Le trajet estimé est de 1 h 18.
> - Les restrictions de 6 éléments n’ont pas été vérifiées récemment : elles ne sont pas comptées comme compatibles.

## 10. Comparaison côte à côte

Maximum initial : 4 parcs.

Lignes :

- compatibilité globale ;
- compatibilité par membre ;
- éléments communs au groupe ;
- catégories préférées ;
- inconnues ;
- trajet ;
- ouverture ;
- horaires ;
- budget si fiable ;
- intérieur/extérieur ;
- statut de complétude ;
- date de vérification ;
- liens officiels.

Fonctions :

- surligner une différence sans couleur seule ;
- masquer les lignes identiques ;
- changer les poids ;
- ajouter à `à visiter` ;
- créer un projet de voyage ;
- partager uniquement la comparaison, sans profils personnels détaillés, après aperçu.

## 11. Modèle de domaine

### 11.1 `ParkFitRequest`

```csharp
public sealed record ParkFitRequest(
    SearchOrigin? Origin,
    LocalDate? VisitDate,
    TravelConstraint? Travel,
    IReadOnlyList<GroupMemberCriteria> Members,
    ParkPreferenceSet Preferences,
    UnknownDataPolicy UnknownDataPolicy,
    ParkFitMethodologyVersion MethodologyVersion);
```

### 11.2 `ParkFitResult`

```csharp
public sealed record ParkFitResult(
    Guid ParkId,
    ParkFitEligibility Eligibility,
    decimal? ComparativeScore,
    DataConfidence Confidence,
    GroupCompatibilitySummary Group,
    IReadOnlyList<FitFactor> PositiveFactors,
    IReadOnlyList<FitFactor> LimitingFactors,
    IReadOnlyList<MissingDataFact> MissingData,
    ParkFitMethodologyVersion MethodologyVersion,
    DateTime CalculatedAtUtc);
```

### 11.3 Résolution des données

Ports :

- `IParkFitCandidateReader` ;
- `IAttractionRestrictionReader` ;
- `IParkOpeningCalendarReader` ;
- `ITravelEstimateProvider` ;
- `IParkDataQualityReader` ;
- `IParkFitMethodologyProvider` ;
- `IParkFitExplanationBuilder` côté Application, avec codes traduits côté front.

Le Core calcule des résultats structurés, pas des phrases localisées.

## 12. API

### 12.1 Recherche anonyme

```text
POST /api/public/park-fit/search
POST /api/public/park-fit/compare
GET  /api/public/park-fit/methodology/current
GET  /api/public/park-fit/parks/{parkId}/data-quality
```

Payload borné :

- nombre maximal de membres ;
- critères connus ;
- pas de texte libre ;
- rate limit ;
- aucune persistance anonyme par défaut.

### 12.2 Sauvegarde authentifiée

```text
POST   /api/me/group-profiles
GET    /api/me/group-profiles
PATCH  /api/me/group-profiles/{id}
DELETE /api/me/group-profiles/{id}
POST   /api/me/park-fit-searches
GET    /api/me/park-fit-searches/{id}
DELETE /api/me/park-fit-searches/{id}
```

### 12.3 Réponse de qualité

La réponse contient toujours :

- méthode ;
- date ;
- couverture ;
- sources critiques ;
- `unknownCount` ;
- raisons d’inéligibilité ;
- aucun champ laissant croire qu’une règle inconnue est satisfaite.

## 13. Persistance

### 13.1 Profils

Collection `user-group-profiles` :

- index `{ OwnerUserId, UpdatedAtUtc }` ;
- alias unique seulement au sein du propriétaire si nécessaire ;
- version optimiste ;
- chiffrement applicatif à évaluer pour données les plus sensibles ;
- aucune projection publique.

### 13.2 Recherches sauvegardées

Collection `saved-park-fit-searches` :

- critères structurés ;
- méthode ;
- date ;
- résultats optionnels avec expiration ;
- source revision ;
- pas de duplication permanente des données de restriction ;
- recalcul explicite lorsque les données changent.

### 13.3 Qualité des parcs

Collection/snapshot `park-fit-data-quality` :

- parc ;
- score de complétude par dimension ;
- statut ;
- date ;
- sources ;
- éléments inconnus ;
- prochaine revue ;
- révision.

Le score de complétude admin ne doit pas être affiché comme certitude absolue au public ; publier plutôt des dimensions et un statut.

## 14. Administration et préparation des données

Écran :

- portefeuille de parcs candidats ;
- couverture restrictions ;
- éléments sans source ;
- données anciennes ;
- incohérences min/max ;
- règles ambiguës ;
- calendrier ;
- simulation de profils de référence ;
- statut d’éligibilité ;
- date de prochaine vérification ;
- suspension immédiate d’un parc.

### 14.1 Jeux de profils de référence

Créer des fixtures produit, pas de vraies personnes :

- adulte sensations ;
- famille 2 adultes + 2 tailles ;
- jeune enfant ;
- groupe évitant sensations ;
- profil cherchant intérieur ;
- profil avec besoins d’accessibilité génériques.

Chaque mise à jour de données ou méthode recalcule ces fixtures afin de détecter les résultats aberrants.

## 15. Interface Angular

```text
features/public/park-fit/
  pages/park-fit-start-page/
  pages/park-fit-criteria-page/
  pages/park-fit-results-page/
  pages/park-compare-page/
  components/group-member-form/
  components/preference-weight-form/
  components/fit-result-card/
  components/compatibility-matrix/
  components/data-confidence-panel/
  components/source-list/
  state/park-fit.facade.ts
  data-access/
  models/
```

UX :

- formulaire progressif, mais résumé modifiable ;
- pas d’obligation de compte ;
- résultat partiel si un critère n’est pas renseigné ;
- profils réutilisables seulement après connexion ;
- raisons visibles avant score ;
- inconnues non cachées dans un accordéon secondaire ;
- CTA vers comparaison, wishlist ou voyage ;
- retour sans perdre les critères ;
- URL partageable uniquement via snapshot contrôlé.

## 16. SEO

Créer des pages éditoriales indexables indépendantes des profils :

- méthodologie ;
- guides de restrictions ;
- pages de comparaison statiques validées éditorialement ;
- pages « parcs familiaux dans une région » seulement si critères transparents et données suffisantes.

Les résultats personnalisés :

- `noindex` ;
- pas de critères sensibles dans l’URL ;
- pas de profil dans le HTML SSR public ;
- partage via publication dédiée et minimisée.

## 17. Probité et indépendance commerciale

- aucun partenaire ne modifie le score ;
- affiliation éventuelle affichée séparément après le résultat ;
- ordre de recommandation indépendant ;
- méthode publique ;
- données manquantes visibles ;
- correction accessible ;
- historique de version ;
- aucune formule « meilleur parc garanti » ;
- aucune urgence basée sur un faux stock ou prix ;
- aucune recommandation médicale.

## 18. Tests obligatoires

### Core

- chaque combinaison min/max/accompagné ;
- taille exacte au seuil ;
- donnée inconnue ;
- max inférieur au min détecté ;
- profils multiples ;
- groupe séparable ;
- catégorie préférée/exclue ;
- saturation des sous-scores ;
- poids ;
- score plafonné par confiance ;
- invariance à l’ordre des membres.

### Application

- parc inéligible ;
- calendrier inconnu ;
- source périmée ;
- distance fallback ;
- profils privés ;
- méthode versionnée ;
- sauvegarde/recalcul ;
- suspension admin.

### Infrastructure

- cache ;
- quotas trajet ;
- indexes ;
- snapshots qualité ;
- source indisponible ;
- données volumineuses ;
- absence de N+1.

### Angular

- formulaire sans compte ;
- profil à plusieurs membres ;
- compatibilité/incompatibilité/inconnu ;
- comparaison 2 à 4 ;
- accessibilité ;
- responsive ;
- huit langues ;
- sources et dates ;
- sauvegarde après connexion.

### End-to-end

- enfant exactement sous un seuil ;
- règle avec accompagnateur ;
- parc calendrier non publié ;
- résultat avec données manquantes ;
- modification d’une taille et recalcul ;
- ajout à wishlist ;
- aucune donnée du profil dans analytics/URL/HTML public.

## 19. Déploiement progressif

### Pilote 1

- 3 à 5 parcs très documentés ;
- critères taille, catégories, date et distance ;
- pas d’accessibilité avancée ni budget ;
- tests qualitatifs avec familles/passionnés.

### Pilote 2

- 10 à 20 parcs ;
- profils sauvegardés ;
- comparaison ;
- indoor/outdoor ;
- sources et signalements.

### Extension

- nouveaux pays ;
- accessibilité structurée ;
- budget ;
- voyages ;
- pages éditoriales SEO.

Chaque parc est activé individuellement par feature flag/data gate. La visibilité générale du parc n’implique pas son éligibilité au moteur.

## 20. Observabilité

- recherches démarrées/terminées ;
- nombre de parcs éligibles ;
- fréquence des inconnues par champ ;
- filtres causant zéro résultat ;
- ouverture des explications ;
- ajout à wishlist/projet ;
- correction signalée ;
- source périmée ;
- latence ;
- appels trajet et cache ;
- abandon avant résultat.

Ne pas journaliser tailles et besoins avec un identifiant analytics externe stable si cela n’est pas indispensable.

## 21. Découpage recommandé en PR

| PR | Contenu | Critère |
|---|---|---|
| `FIT-01` | ADR inconnus, profils et méthode | Sémantique validée |
| `FIT-02` | Modèle de restrictions enrichi + sources | Aucun écrasement en minHeight unique |
| `FIT-03` | Audit de complétude et admin | Parcs pilotes identifiés |
| `FIT-04` | Core compatibilité individuelle | Fixtures seuils complètes |
| `FIT-05` | Compatibilité groupe | Split/inconnu testés |
| `FIT-06` | Méthode et sous-scores | Version publique |
| `FIT-07` | API recherche anonyme | Payload minimal et borné |
| `FIT-08` | Formulaire Web | Premier résultat sans compte |
| `FIT-09` | Résultats et explications | Facteurs et inconnues visibles |
| `FIT-10` | Comparaison côte à côte | 2–4 parcs accessibles |
| `FIT-11` | Profils privés sauvegardés | Export/suppression |
| `FIT-12` | Distance/calendrier | Fallbacks honnêtes |
| `FIT-13` | Sources, signalements et suspension | Exploitation possible |
| `FIT-14` | Pilote instrumenté | Gate qualitative |
| `FIT-15` | Extension de portefeuille | Activation parc par parc |

## 22. Gate finale `FIT-G`

- aucun parc n’est recommandé sans franchir la gate de données ;
- compatible, incompatible et inconnu sont distincts ;
- chaque résultat explique ses facteurs ;
- chaque restriction critique possède une source et une date ;
- les profils restent privés et minimisés ;
- aucune donnée de santé détaillée n’est stockée ;
- le score n’est pas présenté comme probabilité ;
- les inconnues peuvent réduire ou suspendre le résultat ;
- le produit ne remplace pas la confirmation officielle ;
- l’ordre ne dépend d’aucun partenariat ;
- un premier résultat utile est accessible sans compte ;
- les tests terrain confirment que les utilisateurs comprennent pourquoi un parc ressort ;
- le nombre de corrections et les données manquantes restent opérables.
