# Amusement Parks Fun — Programme produit Web, confiance et croissance utile

> Statut : roadmap directrice prête à être arbitrée et découpée en PR d’implémentation.
>
> Base fonctionnelle initialement auditée : `master` au commit `943f6f9c07548b91582cbe853cf17bb14cbeb0df`, le 27 août 2026. Les fondations techniques ont ensuite été réévaluées sur `master` au commit `8742d6e657ef6c1c64f6e360e29fe2aa2ae6b019`, le 28 août 2026.
>
> Périmètre : site Web responsive Angular SSR et API .NET. L’application native, le mode visite hors ligne, la géolocalisation de fond, les widgets et les fonctions propres aux stores restent hors périmètre.
>
> Principe directeur : aucune fonctionnalité ne doit obtenir de la croissance en exagérant la preuve disponible, en créant une urgence artificielle, en masquant une incertitude ou en multipliant artificiellement le poids d’un utilisateur.

## 1. Objet du programme

Ce programme transforme Amusement Parks Fun d’un catalogue riche en un produit personnel et récurrent, sans renoncer à la fiabilité éditoriale. Il ordonne les travaux nécessaires pour :

- rendre les classements honnêtes même lorsque la communauté est encore petite ;
- expliquer publiquement la méthode de calcul, les seuils et les limites ;
- donner à chaque utilisateur un passeport de visites et un journal de rides ;
- permettre une note distincte à chaque visite de parc et à chaque occurrence d’une attraction ;
- produire des statistiques personnelles temporelles sans gonfler le vote communautaire ;
- créer des récapitulatifs partageables mais révocables ;
- aider réellement à choisir, suivre et préparer un parc ;
- exploiter les historiques existants comme avantage éditorial ;
- ne traiter les données live qu’après validation des sources, des droits et de l’exploitation ;
- mesurer la qualité du produit avec des événements utiles plutôt qu’avec des métriques de vanité.

Cette roadmap ne constitue pas un engagement à tout implémenter. Chaque phase possède une gate. Une phase suivante n’est engagée que si la précédente apporte une valeur observée et reste exploitable humainement, techniquement et juridiquement.

## 2. État actuel constaté dans le dépôt

Le socle existant doit être étendu, pas remplacé.

| Capacité actuelle | Constat | Conséquence |
|---|---|---|
| `UserRating` | Une note courante par utilisateur et cible, mise à jour par upsert | La préférence communautaire actuelle doit rester distincte des notes historiques par visite |
| `RatingAggregate` | Nombre, somme, moyenne et score bayésien matérialisés | Ajouter l’éligibilité, la preuve et la version de méthodologie sans casser le calcul actuel |
| `RatingScoreCalculator` | Échelle de 0,5 à 5, prior bayésien 3,5/10, parc composé à 70 % de note directe et 30 % d’éléments | Publier ces règles et rendre leur version explicite |
| Classements publics | Tout agrégat non vide peut actuellement recevoir un rang | Introduire des seuils avant toute autre évolution produit |
| Classement de parc | Additionne les notes directes et les notes des éléments | Ne jamais présenter ce total comme un nombre de personnes uniques |
| Classements personnels | Fondés sur la note courante par cible | Les conserver comme expression volontaire du goût actuel |
| Partage de classement | Identifiant de partage, page publique, aperçu social et révocation déjà présents | Réutiliser l’infrastructure pour les futurs passeports et récapitulatifs |
| Histoires et timelines | Modèle éditorial déjà riche | Construire un explorateur temporel plutôt qu’une seconde source d’histoire |
| Favoris, visites, rides, voyages | Absents du domaine public actuel | Introduire de nouveaux agrégats métier et de nouveaux contrats |
| Web Angular SSR | Indexable, localisé, responsive | Toutes les fonctions du présent programme doivent fonctionner d’abord sur le Web |

## 3. Décisions structurantes non négociables

### 3.1 Deux vérités de notation distinctes

Le futur modèle distingue obligatoirement :

1. **La préférence courante** : une seule `UserRating` par utilisateur et cible. Elle exprime aujourd’hui l’opinion globale volontaire de la personne et alimente au maximum une fois le classement communautaire.
2. **Les observations temporelles** : une note facultative pour chaque visite de parc et chaque occurrence de ride. Elles servent au carnet et aux statistiques personnelles dans le temps.

Les observations temporelles ne sont jamais comptées comme autant de votes communautaires. Une personne ayant fait cent tours d’une attraction ne pèse pas cent fois plus qu’une autre.

Après une nouvelle visite, le produit peut proposer : « Ta moyenne récente diffère de ta note globale. Souhaites-tu actualiser cette dernière ? » La modification reste une action explicite.

### 3.2 Pas de faux historique

Les notes courantes existantes ne sont pas transformées automatiquement en visites inventées. Elles restent des préférences globales sans date de visite. L’utilisateur peut les rattacher manuellement à une visite réelle s’il le souhaite.

### 3.3 Pas de rang sans preuve suffisante

Une moyenne peut être affichée avec son volume lorsqu’elle existe, mais une position dans un classement n’est publiée qu’après franchissement d’un seuil documenté. Les états `Insufficient`, `Provisional`, `Eligible`, `Established` et `StrongEvidence` sont explicites.

### 3.4 Méthode publique et versionnée

Chaque réponse de classement transporte la version de méthode, l’état d’éligibilité, les volumes pertinents et la raison d’une exclusion. Une page publique explique :

- l’échelle de notation ;
- le lissage bayésien ;
- les seuils ;
- la composition d’un score de parc ;
- la gestion des égalités ;
- les changements de méthode ;
- les limites et les risques de petit échantillon.

### 3.5 Vie privée par défaut

Une visite, une date précise, un journal de rides et des préférences de groupe sont privés par défaut. Le partage est choisi objet par objet, révocable et exportable. Aucune présence en parc n’est rendue publique automatiquement.

### 3.6 Recommandations explicables

Le moteur de choix d’un parc est d’abord déterministe et fondé sur des règles visibles. Il indique les critères satisfaits, les données manquantes, la fraîcheur et les sources. Une IA ne doit pas masquer l’absence de données.

### 3.7 Donnée live sourcée ou absente

Un temps d’attente, un statut ou une prévision n’est pas publié sans source durable, date de collecte, TTL, état de fraîcheur, attribution et mécanisme de repli. `0`, `fermé`, `inconnu` et `donnée périmée` restent quatre états différents.

## 3.8 Fondations techniques transverses

Deux roadmaps techniques précèdent désormais les tranches fonctionnelles :

- [`00-technical-foundations-and-architecture-decisions-roadmap.md`](00-technical-foundations-and-architecture-decisions-roadmap.md) fixe les représentations et invariants d’architecture ;
- [`00a-technical-foundations-delivery-migration-and-validation-roadmap.md`](00a-technical-foundations-delivery-migration-and-validation-roadmap.md) fixe l’ordre des PR, migrations, tests, budgets, réparateurs et rollbacks.

Elles rendent explicites les décisions qui étaient seulement illustrées dans les premiers documents :

- les identifiants restent des chaînes persistées et exposées, avec des value objects typés dans le nouveau domaine ;
- la valeur de note est représentée exactement par un nombre de demi-points ;
- une visite correspond à une session dans un parc et à un jour de service local lorsqu’il est connu ;
- les notes temporelles actives sont embarquées dans leur parent pour rester atomiques sur MongoDB autonome ;
- l’ordre des rides repose sur une position entière espacée ;
- les snapshots de classement sont limités à des scopes canoniques ;
- les travaux différés utilisent un worker .NET borné, des leases Mongo, des révisions source et des réparateurs, sans broker externe initial.

### Précédence

Lorsque les exemples antérieurs utilisent directement `Guid`, `decimal`, une collection séparée d’assessments, une séquence contiguë ou une outbox supposée transactionnelle, les roadmaps FOUNDATION indiquent la représentation retenue pour l’implémentation. Les invariants métier des documents spécialisés restent inchangés.

### Gate ajoutée

La gate `FOUNDATION-G` est requise avant toute généralisation de persistance `PASS` et avant l’activation des snapshots canoniques `RANK`. L’éligibilité et l’affichage honnête des classements peuvent toutefois être livrés avant le moteur complet de snapshots.

## 4. Roadmaps spécialisées

| Ordre | Document | Résultat attendu | Gate principale |
|---:|---|---|---|
| 0 | [`00-technical-foundations-and-architecture-decisions-roadmap.md`](00-technical-foundations-and-architecture-decisions-roadmap.md) | Conventions compatibles, exactes et proportionnées | ADR techniques figés sans migration globale inutile |
| 0A | [`00a-technical-foundations-delivery-migration-and-validation-roadmap.md`](00a-technical-foundations-delivery-migration-and-validation-roadmap.md) | PR, migrations, jobs, tests et rollbacks exécutables | `FOUNDATION-DELIVERY-G` |
| 1 | [`01-ranking-trust-and-methodology-roadmap.md`](01-ranking-trust-and-methodology-roadmap.md) | Classements honnêtes, seuils, preuve et méthode publique | Aucun rang faible n’est présenté comme établi |
| 2 | [`02-visit-passport-and-ride-log-roadmap.md`](02-visit-passport-and-ride-log-roadmap.md) | Passeport, visites, occurrences de ride, notes temporelles et statistiques | Aucune perte, aucun doublage de poids communautaire |
| 3 | [`03-shareable-recaps-and-comparisons-roadmap.md`](03-shareable-recaps-and-comparisons-roadmap.md) | Récapitulatifs de visite, bilans et comparaisons révocables | Le partage produit de la valeur sans exposer de données privées |
| 4 | [`04-park-fit-recommendation-and-comparison-roadmap.md`](04-park-fit-recommendation-and-comparison-roadmap.md) | Choix de parc explicable selon un groupe et ses contraintes | Chaque recommandation est justifiable et sourcée |
| 5 | [`05-favorites-watchlists-and-factual-alerts-roadmap.md`](05-favorites-watchlists-and-factual-alerts-roadmap.md) | Favoris, projets, surveillance et alertes factuelles | Pas de notification trompeuse ou dupliquée |
| 6 | [`06-collaborative-trip-planning-roadmap.md`](06-collaborative-trip-planning-roadmap.md) | Voyage partagé, invitations, priorités et programme | Permissions, révocation et cohérence du plan validées |
| 7 | [`07-park-history-explorer-roadmap.md`](07-park-history-explorer-roadmap.md) | Parc à travers le temps, remplacements et frises partageables | Aucune continuité historique inventée |
| 8 | [`08-live-wait-times-and-crowd-intelligence-roadmap.md`](08-live-wait-times-and-crowd-intelligence-roadmap.md) | Statuts, attentes, historiques et prévisions sous conditions | Provenance, droits, fraîcheur et exploitation démontrés |
| 9 | [`09-product-quality-privacy-and-rollout-roadmap.md`](09-product-quality-privacy-and-rollout-roadmap.md) | Instrumentation, accessibilité, sécurité, conformité et déploiement | Chaque phase est mesurable, réversible et supportable |

## 5. Ordre d’exécution et dépendances

```text
RANK — confiance des classements
  └── PASS — passeport, visites et rides
        ├── SHARE — récapitulatifs et comparaisons
        ├── WATCH — suivis et alertes
        ├── FIT — recommandation et comparaison de parcs
        │     └── TRIP — voyage collaboratif
        └── HIST — histoire personnelle + histoire éditoriale

LIVE dépend de RANK, PASS, WATCH, QUAL et de sources autorisées.
QUAL est transverse dès la première PR et verrouille chaque gate.
```

### 5.1 Phase 0 — Réparer la confiance avant d’ajouter de l’engagement

Travaux obligatoires :

- mesurer les volumes réels de contributeurs uniques ;
- introduire la politique d’éligibilité configurable ;
- supprimer les rangs publics sous le seuil ;
- afficher les états provisoires ;
- publier la méthodologie et son historique ;
- distinguer nombre d’observations et nombre de personnes ;
- instrumenter la consultation de la méthode et des explications.

Aucun développement du passeport n’est fusionné avant la gate `RANK-G`.

### 5.2 Phase 1 — Créer une valeur personnelle sauvegardable

Le Passeport commence volontairement sans partage public :

- créer une visite ;
- renseigner sa date ou une date partielle ;
- marquer les éléments faits ou manqués ;
- enregistrer plusieurs occurrences d’une attraction ;
- noter le parc pour cette visite ;
- noter chaque occurrence de ride ;
- corriger et supprimer avec audit ;
- consulter une chronologie personnelle et des statistiques simples ;
- exporter les données.

Le premier succès produit est l’enregistrement fiable d’une deuxième visite, pas le nombre brut d’inscriptions.

### 5.3 Phase 2 — Calculer l’évolution personnelle

Après validation de la saisie :

- moyenne, médiane, min, max et dispersion par cible ;
- note par visite et par année ;
- première et dernière expérience ;
- évolution uniquement lorsque le volume minimal est atteint ;
- comparaison entre note globale volontaire et historique ;
- agrégats par parc, catégorie, type et constructeur lorsque la couverture est suffisante ;
- graphiques accompagnés d’un tableau accessible.

### 5.4 Phase 3 — Produire des objets partageables

Après validation de la confidentialité :

- récapitulatif d’une visite ;
- bilan annuel ;
- passeport public facultatif ;
- top personnel ;
- comparaison consentie entre deux profils ;
- cartes Open Graph générées à partir des données réelles ;
- lien « créer mon propre passeport » sans manipulation.

### 5.5 Phase 4 — Aider à décider et à revenir

Les fonctions de découverte et de suivi sont introduites après la valeur personnelle :

- listes `préféré`, `à visiter`, `surveillé` ;
- alertes factuelles en résumé ;
- profils de groupe privés ;
- comparaison de parcs ;
- explication des compatibilités et incertitudes ;
- voyages collaboratifs sans fil social généraliste.

### 5.6 Phase 5 — Exploiter l’avantage historique

Les données éditoriales historiques alimentent :

- une frise fiable ;
- une vue du parc à une date donnée ;
- les lignées de remplacements et renommages ;
- les attractions disparues réellement disponibles lors d’une ancienne visite ;
- les anniversaires et récits partageables ;
- la contextualisation des statistiques personnelles.

### 5.7 Phase 6 — Étudier le live, sans engagement automatique

Le live reste une phase conditionnelle. L’étude est arrêtée si :

- les droits ne sont pas suffisamment établis ;
- les identifiants externes ne peuvent pas être mappés durablement ;
- la fraîcheur ne peut pas être affichée honnêtement ;
- la charge du VPS ou le coût d’exploitation dépasse les budgets ;
- les données sont trop lacunaires pour apporter plus de valeur que de confusion.

## 6. Gates globales

### Gate G0 — Baseline et preuve

- événements produit minimaux définis ;
- volumes actuels mesurés sans collecte excessive ;
- vocabulaire public validé ;
- aucun classement faible présenté comme établi ;
- rollback documenté.

### Gate G1 — Passeport fiable

- création, modification, suppression et restauration testées ;
- doublons et requêtes rejouées gérés ;
- visites avec dates complètes et partielles couvertes ;
- éléments fermés ou renommés restent lisibles ;
- export et suppression complets ;
- aucune modification du poids communautaire.

### Gate G2 — Statistiques justes

- jeux de données de référence calculés indépendamment ;
- arrondis et fuseaux horaires documentés ;
- tendances cachées lorsque le volume est insuffisant ;
- correction d’une observation recalcule exactement les agrégats concernés ;
- tableaux accessibles disponibles en plus des graphiques.

### Gate G3 — Partage sûr

- privé par défaut ;
- liens révocables ;
- date exacte masquable ;
- indexation explicite ;
- aperçu social ne révèle aucune donnée non publiée ;
- suppression et changement de visibilité invalident les caches.

### Gate G4 — Décision explicable

- chaque recommandation liste ses facteurs ;
- chaque restriction sensible possède une source et une date ;
- l’absence d’information ne devient jamais un accord implicite ;
- les profils de mineurs ne sont ni publics ni réutilisés à d’autres fins ;
- résultats vérifiés sur un petit portefeuille de parcs complets.

### Gate G5 — Collaboration maîtrisée

- invitation, expiration, révocation et rôles testés ;
- aucun chat généraliste nécessaire ;
- chaque participant contrôle ses données ;
- les changements de programme sont auditables ;
- les informations officielles restent distinguées des choix du groupe.

### Gate G6 — Live exploitable

- contrat de source et attribution validés ;
- TTL et état de fraîcheur visibles ;
- kill switch fonctionnel ;
- charge, coûts et rétention bornés ;
- correction d’un mapping sans perte d’historique ;
- prévision interdite avant le seuil statistique défini.

## 7. Règles de découpage des PR

Chaque PR d’implémentation doit :

1. partir de `master` à jour ;
2. traiter un seul invariant ou une seule tranche verticale ;
3. conserver les contrats existants lorsque la compatibilité est possible ;
4. ajouter les tests Core/Application/Infrastructure/WebAPI nécessaires ;
5. ajouter les tests Angular de façade, composant et accessibilité concernés ;
6. inclure migration, index et stratégie de retour arrière lorsqu’une donnée persiste ;
7. mettre à jour OpenAPI et vérifier les ruptures ;
8. documenter les événements produit créés ;
9. mettre à jour les huit langues pour tout texte public ;
10. incrémenter `FRONT/AmusementPark/release-version.json` selon les règles du dépôt.

Une PR ne doit pas mélanger :

- création du domaine et refonte visuelle complète ;
- changement de méthode de classement et ajout d’une nouvelle source de note ;
- ingestion live et prévision ;
- migration destructive et suppression immédiate du modèle précédent.

## 8. Matrice des données personnelles

| Donnée | Défaut | Partage possible | Export | Suppression | Rétention minimale recommandée |
|---|---|---|---|---|---|
| Préférence globale | Privée, agrégée anonymement dans la communauté | Oui, via classement personnel | Oui | Oui | Tant que le compte existe |
| Visite | Privée | Oui, visite par visite | Oui | Oui | Tant que le compte existe |
| Date exacte | Privée | Option distincte | Oui | Oui | Tant que nécessaire à la fonction |
| Occurrence de ride | Privée | Via récapitulatif choisi | Oui | Oui | Tant que le compte existe |
| Note temporelle | Privée | Agrégat ou détail choisi | Oui | Oui | Tant que le compte existe |
| Profil de groupe | Privé | Non par défaut | Oui | Oui | Suppression automatique si inutilisé à définir |
| Voyage partagé | Privé aux participants | Lien sur invitation | Oui | Oui selon rôle | Jusqu’à suppression ou échéance définie |
| Alertes | Privées | Non | Oui | Oui | Préférences tant que la surveillance existe |
| Événements analytics | Pseudonymisés et minimisés | Non | Selon régime retenu | Selon régime retenu | Durée courte documentée |

## 9. Mesures de succès utiles

### 9.1 Confiance

- part des classements accompagnés d’une preuve compréhensible ;
- part des cibles non classées faute de données ;
- consultations de la méthodologie ;
- signalements portant sur une présentation trompeuse ;
- corrections de données et délai de traitement.

### 9.2 Activation

- visite créée ;
- cinq éléments ajoutés à une visite ;
- première occurrence notée ;
- première statistique temporelle consultée ;
- données locales ou saisies sauvegardées avec succès.

### 9.3 Rétention

- deuxième visite enregistrée ;
- retour après une nouvelle visite réelle ;
- ajout d’un ride à une visite existante ;
- consultation récurrente d’une watchlist ;
- réutilisation d’un profil de groupe.

### 9.4 Partage

- récapitulatifs effectivement ouverts ;
- création d’un nouveau passeport depuis un partage ;
- révocations et changements de visibilité ;
- partages abandonnés après aperçu, afin d’identifier un problème de confidentialité ou de qualité.

### 9.5 Décision

- comparaison de parcs terminée ;
- raisons développées par l’utilisateur ;
- parc ajouté à un projet après comparaison ;
- données manquantes rencontrées ;
- taux de correction des incompatibilités signalées.

Aucune gate ne repose uniquement sur le nombre de pages vues, de comptes ou de téléchargements.

## 10. Fonctions explicitement différées

Le programme ne planifie pas encore :

- application Android ou iOS native ;
- PWA hors ligne dédiée à la journée en parc ;
- géolocalisation continue ;
- partage de position de groupe ;
- notifications push système ;
- widgets et montres ;
- réalité augmentée ;
- fil social, messagerie générale ou chat public ;
- séries quotidiennes, récompenses de connexion ou mécanismes de culpabilisation ;
- recommandation opaque générée par IA ;
- vente de visibilité dans les classements.

Ces idées ne sont pas interdites à long terme. Elles nécessitent un besoin observé, un document séparé et une gate indépendante.

## 11. Définition de terminé du programme documentaire

La présente série de roadmaps est considérée cohérente lorsque :

- chaque fonctionnalité possède des objectifs, non-objectifs et dépendances ;
- le modèle de note temporelle ne modifie pas silencieusement le classement communautaire ;
- les seuils sont la première tranche livrable ;
- les modèles de données, contrats, indexes, tests, migrations et rollbacks sont identifiés ;
- les fonctions de partage et de recommandation intègrent la probité dès le domaine ;
- le mobile reste hors périmètre sans bloquer une future réutilisation des contrats ;
- chaque phase possède une gate d’arrêt et non uniquement une liste de travaux ;
- le document peut être transformé en issues sans inventer les invariants métier.
