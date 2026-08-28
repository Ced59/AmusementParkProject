# Roadmap 01 — Confiance des classements, seuils et méthodologie publique

> Code programme : `RANK`
>
> Priorité : immédiate et bloquante avant le Passeport/Ride Log.
>
> Base auditée : le domaine possède une `UserRating` courante par utilisateur et cible, un `RatingAggregate`, un score bayésien avec moyenne a priori `3.5` et poids `10`, ainsi qu’un score de parc composé à `70 %` de note directe et `30 %` de notes d’éléments équilibrées par catégorie.
>
> Résultat attendu : le site ne présente plus un rang communautaire comme significatif lorsque le volume, la diversité ou la couverture sont insuffisants.

## 0. Avenant technique FOUNDATION

Cette roadmap dépend désormais de la gate [`FOUNDATION-G`](00-technical-foundations-and-architecture-decisions-roadmap.md#15-gate-foundation-g) pour ses primitives, mais l’affichage honnête des seuils ne doit pas attendre la totalité de l’infrastructure de snapshots.

### Décisions applicables

1. **Valeur exacte.** Les notes sont manipulées dans le Core via `RatingValue`, fondé sur 1 à 10 demi-points. Le champ `double` historique reste lisible par mapping compatible pendant expand/contract.
2. **Éligibilité avant snapshot.** Les premières PR calculent `RankingEvidence` depuis les agrégats actuels et retirent les rangs insuffisants. Un snapshot n’est pas un prérequis pour cesser d’afficher une affirmation trompeuse.
3. **Scopes canoniques uniquement.** Les snapshots durables sont réservés aux classements explicitement publiés : parcs globaux, catégories publiques d’éléments et quelques scopes territoriaux ayant franchi leur gate. Aucune combinaison arbitraire de query string n’est matérialisée.
4. **Publication atomique.** Lorsqu’ils sont introduits, les snapshots utilisent un header, des chunks bornés et un pointeur courant atomique. Un build incomplet ne peut pas devenir public.
5. **Reconstruction coalescée.** Une mutation augmente une révision source et demande un job `ratings.rebuild-scope`. Plusieurs mutations rapprochées mettent à jour la révision demandée du même job au lieu d’empiler des reconstructions.
6. **Budget VPS.** Un seul rebuild lourd est exécuté simultanément. Une révision devenue obsolète peut interrompre proprement un build avant publication.
7. **Rollback éditorial sûr.** En cas d’échec, le site conserve le dernier snapshot validé ou masque le rang. Il ne revient jamais silencieusement à un classement sous-échantillonné.

### Scopes initiaux

```text
parks:global
park-items:category:{category}
park-items:type:{type}        seulement si route et volume validés
parks:country:{countryId}     seulement après gate de volume et utilité
```

La langue, les filtres utilisateur et les combinaisons libres ne créent pas de snapshot. Les classements personnels restent une lecture des préférences courantes du propriétaire.

### Ajustement du découpage RANK

- `RANK-02` à `RANK-04` peuvent livrer preuve, contrats et UI sans snapshot ;
- `RANK-05` devient l’introduction du registre de scopes et du format header/chunks/pointer ;
- `RANK-06` bascule uniquement les scopes canoniques derrière feature flag ;
- les filtres rares restent calculés à la demande, triés sans rang ou dérivés d’un scope canonique lorsque la sémantique le permet.

## 1. Problème à résoudre

Le classement actuel peut attribuer un rang à une cible dès qu’un agrégat non vide existe. Le lissage bayésien réduit l’effet d’une note extrême, mais il ne transforme pas un ou deux contributeurs en échantillon représentatif.

Le risque produit n’est pas seulement statistique. Il est éditorial :

- une position précise peut suggérer une autorité que les données ne possèdent pas ;
- `ratingCount` peut être interprété comme un nombre de personnes alors qu’un classement de parc additionne des notes de parc et d’éléments ;
- un parc riche en éléments notés peut accumuler davantage d’observations qu’un petit parc sans avoir davantage de visiteurs uniques ;
- les utilisateurs ne voient pas clairement le prior bayésien, le poids des catégories, les seuils ou les limites ;
- une future note à chaque ride pourrait accidentellement multiplier le poids des visiteurs les plus actifs.

La correction doit intervenir avant toute fonctionnalité qui augmente le nombre et les types de notes.

## 2. Objectifs

- Définir une politique d’éligibilité centralisée, testable, configurable et versionnée.
- Calculer séparément le nombre d’observations et le nombre de contributeurs uniques.
- Ne publier un rang principal que lorsque le niveau minimal de preuve est atteint.
- Afficher une moyenne ou une tendance faible avec un vocabulaire honnête, sans la confondre avec un classement établi.
- Publier la méthode complète dans les huit langues.
- Transporter la preuve et la version de méthode dans les contrats API.
- Rendre les recomputations auditables et réversibles.
- Préparer l’arrivée des observations de visite sans modifier la règle « une personne, au maximum un vote communautaire courant par cible ».

## 3. Non-objectifs

Cette roadmap ne couvre pas :

- la création du passeport et des visites ;
- les comparaisons par duels entre deux cibles ;
- un nouvel algorithme de recommandation ;
- une validation d’identité forte des votants ;
- une détection anti-fraude probabiliste complexe ;
- une pondération selon l’ancienneté ou la réputation du compte ;
- une note payante ou sponsorisée ;
- une prédiction de popularité future.

## 4. Vocabulaire public et métier

### 4.1 États d’éligibilité

| État | Signification | Rang public | Moyenne publique | Libellé recommandé |
|---|---|---:|---:|---|
| `NoEvidence` | Aucune note courante valide | Non | Non | « Pas encore évalué » |
| `Insufficient` | 1 à 2 contributeurs uniques | Non | Oui, avec volume | « Premiers avis — données insuffisantes pour classer » |
| `Provisional` | 3 à 9 contributeurs uniques | Non dans le classement principal | Oui | « Tendance provisoire » |
| `Eligible` | 10 à 29 contributeurs uniques et critères de couverture atteints | Oui | Oui | « Classement communautaire » |
| `Established` | 30 à 99 contributeurs uniques | Oui | Oui | « Classement établi » |
| `StrongEvidence` | 100 contributeurs uniques ou plus | Oui | Oui | « Forte participation » |
| `Excluded` | Cible non éligible pour raison métier | Non | Selon règle | Raison explicite |

Ces seuils constituent une **politique initiale**, pas une vérité scientifique universelle. Ils sont :

- définis côté domaine/application ;
- configurables par type de classement ;
- versionnés ;
- affichés publiquement ;
- réexaminés lorsque la distribution réelle des votes devient mesurable.

### 4.2 Termes à ne pas confondre

- `UniqueContributorCount` : nombre d’utilisateurs distincts ayant une note courante retenue.
- `RatingObservationCount` : nombre de notes retenues dans le calcul. Pour une cible simple, il coïncide aujourd’hui avec le nombre de contributeurs ; pour un score de parc composé, il peut être supérieur.
- `DirectParkContributorCount` : utilisateurs distincts ayant noté directement le parc.
- `ItemContributorCount` : utilisateurs distincts ayant noté au moins un élément du parc.
- `EligibleItemCount` : éléments franchissant leur propre seuil.
- `EligibleCategoryCount` : catégories disposant d’une couverture suffisante.
- `EvidenceLevel` : état de preuve public.
- `MethodologyVersion` : identifiant immuable de la méthode appliquée.

L’interface ne doit jamais afficher « 128 visiteurs » lorsque la donnée représente 128 notes sur plusieurs éléments.

## 5. Politique initiale proposée

### 5.1 Cibles simples : parc direct et élément

Une cible simple devient éligible au classement principal lorsque :

```text
UniqueContributorCount >= 10
AND TargetCanReceiveVisitorRatings = true
AND AggregateIntegrity = Valid
AND IsExcludedByModeration = false
```

Le score utilisé pour l’ordre reste le score bayésien existant, sous réserve de validation des tests de non-régression.

### 5.2 Classement composé des parcs

Le score d’un parc est actuellement composé de :

- `70 %` de la note directe du parc ;
- `30 %` d’un score d’éléments équilibré entre catégories.

La nouvelle politique distingue l’existence d’un composant et son éligibilité.

#### Composant direct

- utilisable comme signal provisoire dès une note ;
- éligible au classement principal à partir de 10 contributeurs directs uniques ;
- établi à partir de 30 ;
- fort à partir de 100.

#### Composant éléments

Pour éviter qu’une seule attraction très notée représente tout le parc :

- un élément doit être lui-même `Eligible` ;
- une catégorie est couverte lorsqu’elle possède au moins `2` éléments éligibles, sauf catégorie ne contenant objectivement qu’un seul élément public ;
- le parc doit posséder au moins `2` catégories couvertes ou, pour les parcs réellement mono-catégorie, une exception explicite et documentée ;
- au moins `5` éléments éligibles sont requis au total pour déclarer le composant éléments éligible ;
- le nombre d’utilisateurs distincts ayant contribué à ces éléments doit être au moins `10`.

Ces valeurs sont initiales et paramétrables.

#### Éligibilité du parc composé

Version initiale :

```text
Eligible si :
- composant direct éligible ;
- OU composant direct provisoire ET composant éléments éligible ;
- ET au moins 10 contributeurs uniques sur l’union des deux composants.
```

Le score conserve les poids `70/30` uniquement lorsque les deux composants sont éligibles. Lorsqu’un seul composant est éligible :

- le rang peut être calculé sur ce composant seul ;
- la réponse indique `CompositionMode = DirectOnly` ou `ItemsOnly` ;
- l’interface ne présente pas ce score comme directement comparable à une composition complète sans avertissement ;
- une décision produit doit confirmer si ces parcs partagent le même tableau ou apparaissent dans une section séparée.

**Décision recommandée pour la première version :** le classement principal exige le composant direct éligible. Le composant éléments enrichit le score lorsque sa couverture est suffisante. Les parcs sans note directe suffisante restent en tendance provisoire. Cette règle est plus simple à expliquer et évite les comparaisons de composition hétérogène.

### 5.3 Taille minimale du tableau

Même si une cible franchit le seuil, un « top » n’est pas publié si moins de `3` cibles comparables sont éligibles dans le filtre courant.

L’interface affiche alors :

> « Les données de cette catégorie progressent, mais au moins trois lieux doivent atteindre le seuil avant de publier un classement. »

### 5.4 Gestion des égalités

Définir une politique stable :

1. arrondir uniquement pour l’affichage, jamais avant le tri ;
2. considérer deux scores comme ex æquo lorsque leur différence est inférieure à `0.0001` ;
3. attribuer le même rang public aux ex æquo ;
4. utiliser ensuite un ordre d’affichage déterministe — contributeurs uniques décroissants, moyenne décroissante, nom ordinal — sans prétendre départager le rang ;
5. documenter la convention de rang choisie : classement de compétition `1, 2, 2, 4` recommandé.

## 6. Versionnement de méthodologie

Créer une valeur métier, par exemple :

```csharp
public readonly record struct RatingMethodologyVersion(string Value);
```

Première version proposée : `ratings-2026-01`.

Une version fige :

- échelle et pas de note ;
- prior mean et prior weight ;
- poids direct/éléments ;
- équilibrage des catégories ;
- seuils par état ;
- règles de couverture ;
- gestion des égalités ;
- exclusions métier ;
- date d’effet.

### 6.1 Changements compatibles

Peuvent conserver la version :

- correction de texte sans impact mathématique ;
- ajout d’une traduction ;
- correction d’un bug qui restaure exactement le comportement documenté.

### 6.2 Changements nécessitant une nouvelle version

- modification d’un seuil ;
- modification du prior ;
- modification d’un poids ;
- changement du traitement des catégories ;
- ajout d’une pondération temporelle ;
- changement de gestion des égalités ;
- inclusion d’une nouvelle famille de notes dans le score.

### 6.3 Historique public

La page de méthode conserve :

- version ;
- date d’entrée en vigueur ;
- résumé lisible ;
- détail mathématique ;
- raison du changement ;
- effet attendu ;
- lien vers la version précédente ;
- mention lorsqu’une recomputation a modifié les positions.

## 7. Modèle de domaine cible

### 7.1 `RankingEligibilityPolicy`

Responsabilités :

- recevoir le contexte d’une cible ou d’un parc composé ;
- retourner un verdict pur et explicable ;
- ne dépendre ni de MongoDB, ni de HTTP, ni de la langue ;
- exposer les seuils utilisés dans le verdict.

```csharp
public sealed record RankingEligibilityPolicy(
    RatingMethodologyVersion Version,
    int ProvisionalMinUniqueContributors,
    int EligibleMinUniqueContributors,
    int EstablishedMinUniqueContributors,
    int StrongEvidenceMinUniqueContributors,
    int MinimumEligibleEntriesPerRanking,
    int MinimumEligibleItemsForParkItemComponent,
    int MinimumEligibleItemsPerCategory,
    int MinimumEligibleCategories);
```

### 7.2 `RankingEvidence`

```csharp
public sealed record RankingEvidence(
    RankingEvidenceLevel Level,
    bool IsEligibleForMainRanking,
    int UniqueContributorCount,
    int RatingObservationCount,
    int? DirectParkContributorCount,
    int? ItemContributorCount,
    int? EligibleItemCount,
    int? EligibleCategoryCount,
    RatingMethodologyVersion MethodologyVersion,
    RankingIneligibilityReason? IneligibilityReason);
```

### 7.3 Raisons d’inéligibilité

Valeurs initiales :

- `NoRatings` ;
- `TooFewUniqueContributors` ;
- `TooFewComparableEntries` ;
- `InsufficientItemCoverage` ;
- `InsufficientCategoryCoverage` ;
- `TargetUnavailable` ;
- `TargetExcluded` ;
- `AggregateIntegrityFailure` ;
- `UnsupportedComposition`.

Une raison est un code stable traduit côté client, pas une chaîne anglaise persistée.

### 7.4 Extension de `RatingAggregate`

Ne pas mélanger immédiatement toutes les preuves dans l’agrégat existant si elles se calculent différemment. Deux options doivent être évaluées :

#### Option A — Étendre l’agrégat

Ajouter :

- `UniqueContributorCount` ;
- `MethodologyVersion` ;
- `EvidenceLevel` ;
- `EligibilityUpdatedAtUtc`.

Avantage : lecture rapide. Inconvénient : migration et couplage plus fort.

#### Option B — Snapshot séparé

Créer `RatingRankingSnapshot` par cible/méthode avec :

- score ;
- rang ;
- preuve ;
- date de calcul ;
- version ;
- empreinte des sources.

Avantage : audit et recomputation facilités. Inconvénient : nouvelle cohérence à gérer.

**Choix recommandé :** garder `RatingAggregate` comme agrégat de calcul de base et introduire un snapshot de publication versionné. Le rang est une propriété d’un ensemble et d’une méthode, pas seulement d’une cible.

## 8. Persistance et index

### 8.1 Garantir l’unicité actuelle

Vérifier ou ajouter un index unique :

```text
(UserId, TargetType, TargetId)
```

Cet index porte l’invariant « un vote communautaire courant par utilisateur et cible ».

### 8.2 Snapshots de classement

Collection proposée : `rating-ranking-snapshots`.

Champs essentiels :

- `Id` ;
- `RankingScope` ;
- `FilterKey` canonique ;
- `MethodologyVersion` ;
- `GeneratedAtUtc` ;
- `SourceRevision` ;
- `Entries[]` ou documents par entrée selon taille ;
- preuve, score brut, rang, état ;
- `IsCurrent` ;
- checksum.

Indexes :

- unique `(RankingScope, FilterKey, MethodologyVersion, SourceRevision)` ;
- `(RankingScope, FilterKey, IsCurrent)` ;
- `(TargetType, TargetId, MethodologyVersion)` si documents séparés ;
- TTL interdit : l’historique de méthode ne doit pas disparaître automatiquement.

### 8.3 Audit de recomputation

Collection ou audit log :

- déclencheur `rating-change`, `methodology-change`, `admin-rebuild`, `migration` ;
- début, fin, durée ;
- version ;
- nombre de cibles lues ;
- nombre éligible/inéligible ;
- erreurs ;
- snapshot précédent et nouveau ;
- initiateur lorsqu’il est manuel.

## 9. Application et cas d’usage

### 9.1 Cas d’usage nouveaux

- `GetPublicRatingMethodologyQuery` ;
- `GetRatingEvidenceQuery` ;
- `GetRankingEligibilityPolicyQuery` pour l’administration ;
- `RebuildRatingRankingSnapshotsCommand` protégé admin ;
- `PreviewRatingMethodologyImpactQuery` protégé admin ;
- `PublishRatingMethodologyVersionCommand` protégé admin ;
- `GetRatingMethodologyHistoryQuery` ;
- `GetRankingDiagnosticsQuery` protégé admin.

### 9.2 Adaptation des cas existants

`GetRatingSummaryQuery` :

- retourne toujours l’agrégat disponible ;
- ajoute la preuve ;
- ne retourne `Rank` que si l’entrée est éligible dans le scope demandé ;
- ne déclenche pas une recomputation coûteuse par requête publique.

`ListParkRatingRankingsQuery` et `ListParkItemRatingRankingsQuery` :

- chargent un snapshot courant ;
- filtrent les entrées non éligibles du tableau principal ;
- peuvent retourner séparément un compteur de tendances provisoires ;
- signalent si moins de trois entrées sont publiables ;
- incluent `methodologyVersion`, `generatedAtUtc` et un lien logique vers la méthode.

`UpsertUserRatingCommand` et `DeleteUserRatingCommand` :

- conservent l’invariant actuel ;
- invalident la révision source ;
- planifient ou déclenchent une recomputation bornée ;
- ne bloquent pas la réponse utilisateur sur la reconstruction complète de tous les classements ;
- publient un événement interne après persistance réussie.

## 10. Contrats API

### 10.1 Résumé de note

Ajouter sans casser les consommateurs :

```json
{
  "targetType": "ParkItem",
  "targetId": "...",
  "ratingCount": 7,
  "uniqueContributorCount": 7,
  "averageRating": 4.43,
  "bayesianScore": 3.88,
  "rank": null,
  "evidence": {
    "level": "Provisional",
    "isEligibleForMainRanking": false,
    "ineligibilityReason": "TooFewUniqueContributors",
    "nextThreshold": 10
  },
  "methodologyVersion": "ratings-2026-01"
}
```

### 10.2 Entrée de classement

Chaque entrée transporte :

- rang public ;
- score d’ordre ;
- moyenne affichée ;
- contributeurs uniques ;
- observations ;
- niveau de preuve ;
- composition du score de parc ;
- couverture catégories/éléments ;
- version de méthode.

### 10.3 Endpoint de méthode

Route publique proposée :

```text
GET /api/ratings/methodology/current
GET /api/ratings/methodology/{version}
GET /api/ratings/methodology
```

La réponse structurée permet au front de générer les tableaux sans dupliquer les nombres, mais le texte éditorial localisé reste contrôlé par les ressources de traduction ou un contenu versionné dédié.

### 10.4 Compatibilité

- rendre les nouveaux champs additifs ;
- conserver temporairement `ratingCount` ;
- documenter sa sémantique par type de réponse ;
- introduire ensuite `ratingObservationCount` comme nom explicite ;
- ne supprimer un champ qu’après une version majeure de contrat ou une période de dépréciation annoncée.

## 11. Expérience Web publique

### 11.1 Page principale des classements

En tête de page :

- date de dernière génération ;
- version de méthode ;
- lien « Comment ce classement est calculé » ;
- phrase claire sur les seuils ;
- nombre de cibles classées ;
- nombre de tendances provisoires non rangées.

Dans chaque carte ou ligne :

- rang uniquement si éligible ;
- moyenne ;
- contributeurs uniques ;
- badge de preuve ;
- info-bulle ou panneau accessible ;
- composition pour les parcs ;
- aucun nombre à deux décimales qui suggère une précision excessive si le volume est faible.

### 11.2 Détail parc ou élément

Cas 1 à 2 contributeurs :

> « 2 personnes ont évalué ce lieu. La moyenne actuelle est visible, mais il faut 10 contributeurs uniques pour entrer dans le classement. »

Cas 3 à 9 :

> « Tendance provisoire : 7 contributeurs uniques sur les 10 nécessaires au classement. »

Cas éligible :

> « Classé 12e selon la méthode ratings-2026-01, avec 38 contributeurs uniques. »

### 11.3 Page de méthodologie

Sections obligatoires :

1. ce que mesure le classement ;
2. ce qu’il ne mesure pas ;
3. qui compte comme contributeur ;
4. échelle 0,5 à 5 ;
5. lissage bayésien expliqué sans jargon puis avec formule ;
6. score d’un parc ;
7. seuils et niveaux de preuve ;
8. égalités ;
9. fermetures et cycle de vie ;
10. modération et exclusions ;
11. fréquence de recalcul ;
12. historique des versions ;
13. signaler une erreur.

Formule publique :

```text
score bayésien = (somme des notes + moyenne a priori × poids a priori)
                  / (nombre de notes + poids a priori)
```

Expliquer que le prior ralentit les variations au début, mais ne rend pas les petits échantillons représentatifs ; d’où les seuils séparés.

### 11.4 Accessibilité

- aucun badge uniquement coloré ;
- preuve lisible par lecteur d’écran ;
- formule accompagnée d’une explication textuelle ;
- tableau utilisable au clavier ;
- titres et ordre DOM cohérents ;
- graphiques éventuels accompagnés d’un tableau ;
- terminologie traduite dans les huit langues.

## 12. Administration

Créer un panneau de diagnostic limité et protégé :

- méthode courante ;
- méthode en préparation ;
- distribution des cibles par niveau ;
- nombre de contributeurs uniques ;
- cibles proches du seuil ;
- cibles exclues et raisons ;
- couverture par catégorie ;
- date/durée de dernière recomputation ;
- erreurs ;
- comparaison avant/après d’une politique candidate ;
- bouton de reconstruction avec confirmation et audit ;
- aucun réglage direct en production sans version et aperçu d’impact.

### 12.1 Simulation avant publication

Le preview doit calculer :

- nombre d’entrées gagnant/perdant l’éligibilité ;
- amplitude moyenne et maximale des changements de rang ;
- filtres ne possédant plus trois entrées ;
- parcs dont la composition devient incomplète ;
- coût estimé de recomputation ;
- différence par rapport au snapshot courant.

## 13. Anti-abus minimal

Sans prétendre résoudre toute fraude :

- conserver l’unicité utilisateur/cible ;
- rate limit ciblé sur création/modification/suppression ;
- journaliser les vagues anormales sans exposer les identités ;
- empêcher un compte supprimé ou bloqué d’alimenter les snapshots ;
- pouvoir exclure des notes par décision modérée auditée ;
- recalculer les agrégats après exclusion ;
- ne pas publier publiquement la logique exacte de détection d’abus ;
- ne pas pondérer secrètement les utilisateurs.

Toute pondération future doit être documentée publiquement et versionner la méthodologie.

## 14. Migration et déploiement

### Étape M1 — Mesurer sans changer l’affichage

- calculer en lecture les contributeurs uniques ;
- produire un rapport admin ;
- vérifier que `RatingCount` coïncide avec les utilisateurs uniques pour les cibles simples ;
- identifier les divergences ;
- mesurer la distribution 0, 1–2, 3–9, 10–29, 30–99, 100+.

### Étape M2 — Introduire les contrats additifs

- ajouter preuve et version ;
- conserver le rang actuel derrière un feature flag ;
- tester le front avec toutes les combinaisons.

### Étape M3 — Générer les snapshots candidats

- reconstruire à partir des notes courantes ;
- comparer l’ancien ordre et le nouvel ordre ;
- vérifier les performances ;
- ne rien publier encore.

### Étape M4 — Publier la méthodologie

- page publique dans huit langues ;
- métadonnées SEO et partage ;
- lien depuis chaque zone de note ;
- historique avec première version.

### Étape M5 — Activer les seuils

- masquer les rangs inéligibles ;
- afficher les tendances provisoires ;
- purger/invalider les caches SSR et Open Graph ;
- surveiller erreurs et retours.

### Étape M6 — Supprimer le chemin ancien

Après une période stable :

- retirer le flag de compatibilité ;
- supprimer le calcul de rang sans preuve ;
- verrouiller les tests de contrat ;
- conserver le snapshot précédent pour rollback.

## 15. Stratégie de rollback

- feature flag `ratings:eligibility:enabled` ;
- snapshot précédent conservé ;
- possibilité de revenir à l’affichage sans rang plutôt qu’à l’ancien rang faible ;
- jamais de rollback qui réaffiche silencieusement des rangs insuffisants ;
- migration additive ;
- suppression de champs différée ;
- procédure documentée pour réactiver la dernière méthodologie stable.

Le rollback de sécurité éditoriale est : **afficher moins**, pas revenir à une affirmation plus forte.

## 16. Tests obligatoires

### Core

- bornes de chaque niveau ;
- exactement 2/3/9/10/29/30/99/100 contributeurs ;
- couverture catégories ;
- parc mono-catégorie explicite ;
- égalités et epsilon ;
- exclusions ;
- composition directe/éléments ;
- invariance à l’ordre des entrées ;
- aucun calcul sur NaN ou infini.

### Application

- rank null lorsque inéligible ;
- raison correcte ;
- version propagée ;
- snapshot courant sélectionné ;
- méthode inconnue ;
- preview d’impact ;
- invalidation après upsert/delete ;
- compte bloqué ou note exclue.

### Infrastructure

- indexes uniques ;
- reconstruction idempotente ;
- bascule atomique du snapshot courant ;
- reprise après interruption ;
- concurrence de deux reconstructions ;
- audit complet ;
- données volumineuses.

### WebAPI

- contrats additifs ;
- OpenAPI ;
- autorisation admin ;
- cache public versionné ;
- 404 méthode inconnue ;
- Problem Details.

### Angular

- chaque état de preuve ;
- absence de rang ;
- compteur vers le seuil ;
- page méthodologie ;
- égalité ;
- responsive ;
- clavier/lecteur d’écran ;
- huit langues ;
- SSR contenant l’explication critique.

### End-to-end

- une note fait passer une cible de 9 à 10 ;
- suppression revient de 10 à 9 ;
- le rang disparaît sans conserver une valeur en cache ;
- le lien de méthode correspond à la version affichée ;
- un filtre avec deux entrées éligibles ne publie pas un top trompeur.

## 17. Observabilité

Métriques techniques :

- durée de reconstruction ;
- nombre de cibles ;
- cache hit/miss ;
- invalidations ;
- erreurs par étape ;
- taille des snapshots ;
- latence des endpoints de classement.

Métriques produit minimisées :

- ouverture de l’explication ;
- affichage d’un état provisoire ;
- clic « pourquoi pas classé » ;
- signalement d’incompréhension ;
- abandon de la page après affichage de preuve, sans surinterprétation.

Ne jamais enregistrer dans les analytics la note exacte associée à une identité directement exploitable si cela n’est pas nécessaire.

## 18. Découpage recommandé en PR

| PR | Contenu | Gate locale |
|---|---|---|
| `RANK-01` | Rapport de distribution et vérification des indexes actuels | Les volumes réels sont connus |
| `RANK-02` | Types Core `EvidenceLevel`, raisons et politique pure | Tests de bornes complets |
| `RANK-03` | Contrats additifs de preuve et version | Aucun client existant cassé |
| `RANK-04` | Calcul des contributeurs uniques et couverture | Comptages vérifiés sur fixtures |
| `RANK-05` | Modèle et persistance des snapshots | Reconstruction idempotente |
| `RANK-06` | Provider de rang fondé sur snapshot | Aucun rang sous seuil |
| `RANK-07` | Page publique de méthodologie et huit traductions | SSR et accessibilité validés |
| `RANK-08` | Badges, messages et absence de rang dans les fiches | Tous états UX couverts |
| `RANK-09` | Administration et preview d’impact | Aucun changement sans simulation |
| `RANK-10` | Activation par feature flag, purge caches et suivi | Gate `RANK-G` franchie |
| `RANK-11` | Nettoyage du chemin historique | Rollback et snapshots conservés |

## 19. Gate finale `RANK-G`

La roadmap suivante ne commence que lorsque :

- aucune cible à moins de 10 contributeurs uniques n’a de rang principal ;
- les tendances faibles restent consultables sans être présentées comme établies ;
- le nombre de personnes et le nombre d’observations sont distincts ;
- la méthodologie courante est publique, traduite, versionnée et liée partout ;
- les calculs existants sont couverts par des tests de référence ;
- un changement de seuil peut être simulé, audité et annulé ;
- la future note par visite est explicitement exclue du nombre de votes communautaires ;
- les caches ne conservent pas un ancien rang après passage sous le seuil ;
- les performances restent compatibles avec le VPS ;
- le produit préfère afficher « données insuffisantes » plutôt qu’une précision injustifiée.
