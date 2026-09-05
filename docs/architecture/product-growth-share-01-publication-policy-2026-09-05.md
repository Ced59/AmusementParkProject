# SHARE-01 — Types de partage, consentement et snapshot public

Date : 2026-09-05

Statut : accepté pour implémentation progressive, sans publication créée par cet ADR

Version : 5.0.44

Roadmap : `docs/roadmaps/product-growth/03-shareable-recaps-and-comparisons-roadmap.md`

## Résultat métier

Chaque futur partage sera un objet volontaire, distinct et révocable. Publier le
récapitulatif d'une visite ne rendra ni le compte, ni le passeport, ni les autres
visites publics. La personne verra avant confirmation le contenu exact qui sera
accessible et pourra couper immédiatement le lien.

Cette décision ne publie aucune donnée, ne crée aucune collection et ne nécessite
aucune migration MongoDB. Elle fixe le contrat de confidentialité que les PR
`SHARE-02` à `SHARE-10` devront respecter.

La validation de cet ADR ne vaut pas validation du suivi terrain `PASS-G`.

> Avenant produit du 5 septembre 2026 : ce suivi terrain demeure à réaliser mais ne
> bloque plus SHARE-02 ni les tranches suivantes. Chaque tranche conserve ses propres
> conditions techniques, métier, de sécurité et de confidentialité avant activation
> en production. Aucune preuve communautaire ne sera déclarée sans test réel.

## Contexte et existant réutilisable

Le projet possède déjà un partage volontaire de classement personnel :

- `UserRankingShare` porte un lien aléatoire et une révocation ;
- le générateur utilise 32 octets cryptographiquement aléatoires encodés en
  Base64 URL ;
- l'accès public vérifie la publication, puis l'état du compte ;
- le rendu public SSR et les aperçus sociaux fournissent des patrons utiles.

Son booléen `IsPublic` ne devient jamais la permission générale du passeport.
`UserRankingShare` sera remplacé par une migration dédiée vers le nouveau modèle,
sans adaptateur persistant, double écriture ni second moteur actif. Les routes et
jetons publics compatibles sont conservés, pas l'ancienne architecture. Les fichiers
multi-classes rencontrés dans ce périmètre seront séparés lorsqu'ils seront touchés,
conformément à la règle architecturale globale.

### Inventaire des mécanismes portant le mot « partage »

| Mécanisme existant | Nature | Rattachement à `SharePublication` |
|---|---|---|
| `UserRankingShare` | Publication volontaire de données personnelles | Remplacé : données migrées, ancien moteur retiré |
| `PublicSharePanelComponent` | Envoi ou copie de l'URL d'une page déjà publique | Réutilisé après publication, mais ne décide jamais de la confidentialité |
| `SocialShareEvent` | Télémétrie minimisée des clics de partage | Reste séparé ; reçoit seulement un type public et une clé analytique non résolvable |
| `SocialPublication` | Publication éditoriale administrée vers un réseau externe | Reste séparé : ce n'est pas un consentement utilisateur ni un lien révocable du Passeport |
| `RankingPublicationPointer` | Activation technique d'un snapshot de classement communautaire | Reste séparé : il publie un calcul global, pas des données personnelles choisies |

Il n'existe actuellement qu'un mécanisme de publication révocable de données
personnelles : le classement personnel. Les boutons qui partagent un parc, une
attraction, une vidéo ou une page publique n'ont pas besoin d'une publication en
base. Les faire dépendre de `SharePublication` créerait des documents inutiles et
mélangerait diffusion d'un contenu public avec consentement sur des données privées.

### Cible centralisée sans agrégat universel

Le moteur commun porte seulement les invariants transverses : propriétaire, type,
source, état, visibilité, politique versionnée, jeton, versions, aperçu,
publication, rotation, révocation et autorisation de résolution. Chaque contenu
reste construit par une stratégie typée dans son propre fichier :

```text
SharePublication (cycle de vie commun)
├── VisitRecapSnapshotBuilder
├── YearRecapSnapshotBuilder
├── PassportProfileSnapshotBuilder
├── PersonalRankingSnapshotBuilder
└── ProfileComparisonSnapshotBuilder
```

Ainsi, le système est centralisé pour la sécurité sans concentrer toutes les règles
métier dans une classe géante. Application orchestre les stratégies via un port ;
Core conserve les invariants communs et chaque policy typée ; Infrastructure ne
connaît que leur persistance et leur sérialisation.

## Décision 1 — une publication autonome par intention

Le nouvel agrégat `SharePublication` appartient à une personne et ne vise qu'une
source et qu'un type :

```text
SharePublication
├── PublicationId : SharePublicationId (chaîne interne typée)
├── OwnerUserId : chaîne opaque
├── Type : VisitRecap | YearRecap | PassportProfile
│          | PersonalRanking | ProfileComparison
├── SourceScopeKey : périmètre privé complet, jamais exposé publiquement
├── Status : Draft | Published | NeedsReview | Revoked
├── Visibility : Private | Unlisted | Public
├── ShareToken : jeton public opaque, absent avant publication
├── ContentPolicy : choix explicites et version de schéma
├── SourceVersion : révision 64 bits monotone du périmètre complet
├── PublicationVersion : entier 64 bits monotone
├── Version : clôture 64 bits de concurrence pour chaque mutation persistée
└── dates techniques et métadonnées d'audit minimisées
```

`Revoked` est un état de cycle de vie et non une visibilité. Cette séparation évite
qu'une transition de visibilité puisse réactiver par erreur un lien révoqué.
`Private` reste la visibilité obligatoire d'un brouillon. `Unlisted` signifie
accessible uniquement par jeton. `Public` autorise la diffusion du lien, mais pas
son indexation : l'opt-in SEO reste une décision distincte et ultérieure.

Une publication de comparaison possède deux consentements et sera modélisée par
les objets dédiés de `SHARE-11`. Un passeport public ne donne jamais, à lui seul,
l'autorisation de comparer son propriétaire.

## Décision 2 — identifiants opaques et frontières

`SharePublicationId` est un value object Core autour d'une chaîne et protège les
cas d'usage privés contre les mélanges d'identifiants. Les documents Mongo, les DTO
et les routes conservent des chaînes, conformément à `FOUNDATION-ADR-01`.

Le jeton public est une notion distincte :

- au moins 256 bits générés par un CSPRNG puis encodés en Base64 URL sans padding ;
- aucune dérivation depuis `UserId`, `VisitId`, une date, un slug ou un courriel ;
- unique en base, rotatif et impossible à énumérer par une API de liste ;
- validation de longueur et d'alphabet avant accès au repository ;
- jamais affiché comme identifiant métier dans l'interface, un export ou les logs ;
- ancien jeton inutilisable immédiatement après rotation ou révocation.

Les API privées peuvent transporter `publicationId` pour gérer l'objet, mais
l'interface montre toujours son libellé métier. Les API publiques ne reçoivent que
le jeton de partage.

## Décision 3 — politique en liste blanche

`ShareContentPolicy` n'est pas un filtre appliqué à un DTO privé. C'est une liste
blanche validée dans le Core, propre au type de publication. Tout champ non prévu
ou non choisi est absent du snapshot public par construction.

Règles communes :

- nom public et avatar masqués par défaut ;
- date masquée par défaut ; année, mois puis jour exigent des choix de plus en plus
  explicites et ne peuvent dépasser la précision réelle de `VisitDate` ;
- positions de visite et géolocalisation précise interdites ;
- notes temporelles, note globale, compteurs et éléments manqués désactivés par
  défaut ;
- `PublicCaption` est un texte public dédié, vide par défaut, borné et modérable ;
- commentaires privés de visite et de ride interdits dans toute politique, tout
  snapshot, tout job, tout rendu et toute image sociale ;
- accompagnants interdits tant qu'un modèle de consentement dédié n'existe pas ;
- indexation désactivée dans la première version, y compris pour `Public`.

### Capacités autorisées par type

| Capacité | Visite | Année | Passeport | Classement | Comparaison |
|---|---:|---:|---:|---:|---:|
| Nom public / avatar | Optionnel | Optionnel | Optionnel | Compatibilité existante | Deux consentements |
| Parc(s) sélectionné(s) | Oui | Oui | Oui | Cibles déjà classées | Intersection consentie |
| Date | Aucune, année, mois ou précision source | Année uniquement | Années choisies | Non | Seulement si les deux l'autorisent |
| Nombre de passages | Optionnel | Optionnel | Agrégat optionnel | Non | Agrégat commun optionnel |
| Notes temporelles | Optionnel | Agrégées et optionnelles | Agrégées et optionnelles | Non | Seulement si les deux l'autorisent |
| Préférences globales | Optionnel et sélectionné | Optionnel et sélectionné | Optionnel et sélectionné | Oui | Intersection consentie |
| Texte public dédié | Optionnel | Optionnel | Optionnel | Aucun ajout implicite | Interdit en V1 |
| Commentaire privé | Interdit | Interdit | Interdit | Interdit | Interdit |
| Position précise | Interdite | Interdite | Interdite | Sans objet | Interdite |

Chaque politique persistée contient `PolicySchemaVersion`. Une modification du
schéma ne change donc jamais silencieusement l'interprétation d'une ancienne
publication.

## Décision 4 — snapshot hybride, figé par publication

La première version de SHARE n'effectue aucune lecture dynamique de données
personnelles pendant une requête publique. La publication conserve un snapshot
minimal, construit côté serveur depuis la liste blanche et lié à la révision du
périmètre complet qui contribue au partage.

```text
scope privé v12
      │ aperçu avec politique P1
      ▼
snapshot public (source v12, publication v3, politique P1)
      │
      └── rendu API / SSR / Open Graph depuis le même snapshot
```

`SourceVersion` et `PublicationVersion` sont des entiers 64 bits, comme les
versions actuelles de `Visit` et `RideOccurrence`. Ici, `SourceVersion` ne désigne
pas la version d'un unique document : c'est la révision du périmètre partageable
complet. La publication incrémente sa version à chaque nouveau snapshot ou rotation
qui change un rendu public. Un dépassement est un conflit explicite, jamais un
retour à zéro.

Même un compteur apparemment anodin reste figé en V1. Cette discipline garantit
que l'aperçu, l'API, le HTML SSR et l'image sociale décrivent exactement le même
contenu. Une optimisation dynamique ne pourra être ajoutée qu'après mesure et avec
une politique explicite.

### Révision de tous les contributeurs

Chaque type définit une clé de périmètre et les mutations qui l'invalident :

| Type | Périmètre versionné | Mutations contributrices minimales |
|---|---|---|
| `VisitRecap` | visite complète | visite, assessments du parc, occurrences sélectionnées et leurs assessments |
| `YearRecap` | propriétaire + année | ajout, correction, déplacement, archivage ou suppression d'une visite/occurrence de l'année |
| `PassportProfile` | passeport du propriétaire | toute visite, occurrence, préférence globale ou donnée de profil autorisée par la policy |
| `PersonalRanking` | préférences globales du propriétaire | toute création, modification ou suppression d'un `UserRating` |
| `ProfileComparison` | deux révisions participantes | toute mutation contributrice de l'un ou l'autre périmètre consenti |

Un `RideOccurrence.Version` modifié sans changement de `Visit.Version` invalide donc
bien le périmètre de visite, d'année et de passeport concerné. Une suppression ou
un ajout modifie aussi la révision même si le document n'existait pas dans le
snapshot précédent.

MongoDB autonome ne permet pas de rendre atomiques le document métier et un compteur
de périmètre distinct. Les futures mutations contributrices utilisent donc une
barrière de révision explicite lorsqu'un périmètre SHARE existe :

1. réserver atomiquement une nouvelle révision et marquer la mutation comme en
   cours avant l'écriture métier ;
2. refuser aperçu, publication et résolution tant qu'une révision contributrice est
   en cours ;
3. écrire la même révision et la corrélation sur la source ou son audit minimal ;
4. valider la révision de périmètre après réussite de la mutation ;
5. annuler proprement la réservation si la mutation échoue ;
6. faire réparer les réservations abandonnées par un reconciler borné et
   idempotent.

L'aperçu lit la révision avant et après la construction et n'est accepté que si elle
est stable et sans mutation en cours. Il persiste exactement le snapshot montré.
La publication compare de nouveau la révision puis promeut ces mêmes octets publics
sans relire les sources privées. Un incrément sans mutation finale peut provoquer
une republication prudente, mais jamais une fuite ; le reconciler le qualifie avec
la corrélation d'audit.

## Décision 5 — transitions et modification de la source

```text
Draft/Private
   │ publier après aperçu explicite
   ▼
Published/Unlisted ou Published/Public
   │                         │
   │ source modifiée         │ révoquer
   ▼                         ▼
NeedsReview/Private       Revoked/Private
   │ republier                (terminal pour le jeton)
   └──────────────► Published avec un nouveau snapshot
```

- enregistrer un brouillon ne crée pas de jeton résolvable ;
- publier exige que l'aperçu porte encore la même `SourceVersion` et la même
  version de politique ;
- une mutation de n'importe quel contributeur rend la publication `NeedsReview` ;
- `NeedsReview` suspend toute résolution publique jusqu'à une nouvelle validation ;
- republier reconstruit intégralement le snapshot et incrémente
  `PublicationVersion` ;
- révoquer est atomique et invalide le jeton dans la même écriture ;
- un objet révoqué ne peut pas être réactivé avec son ancien jeton ; une nouvelle
  publication ou rotation est nécessaire selon le cas d'usage.

Le choix de suspendre tous les types lors d'une divergence de source est plus
protecteur que de maintenir temporairement un ancien snapshot. Il évite qu'une
politique incertaine continue à être servie et donne un comportement unique à
expliquer.

## Décision 6 — révocation, cache et jobs

La révocation ne dépend jamais du worker :

1. le repository applique une écriture atomique avec propriétaire, état et version
   attendus ;
2. cette écriture rend immédiatement le jeton non résolvable ;
3. toute requête publique vérifie cet état, la révision complète du périmètre et
   l'absence de mutation contributrice en cours avant de lire un rendu mis en
   cache ;
4. l'invalidation des rendus, fichiers et cartes Open Graph est demandée ensuite
   via un job idempotent et coalescé par publication ;
5. un reconciler borné recrée un job manquant à partir des versions persistées.

Le cache de contenu est indexé au minimum par type, jeton,
`PublicationVersion`, langue, `PolicySchemaVersion` et version du template. Une
entrée de contenu n'accorde jamais l'accès à elle seule. Une panne du cache ou du
worker provoque un refus ou un rendu synchrone borné, jamais le service d'une
publication révoquée.

Le payload de job ne contient que les identifiants techniques nécessaires, les
versions et la corrélation minimisée. Il ne copie ni commentaire, ni caption, ni
profil, ni snapshot.

## Décision 7 — contrat public, SSR et référencement

Le DTO public est construit dans Application depuis le snapshot public ; il ne
réutilise aucun DTO privé de passeport. WebAPI transporte ce DTO et Angular
l'affiche sans recalculer les permissions.

- lien inconnu, brouillon, `NeedsReview`, révoqué ou propriétaire invalide : `404`
  public uniforme ;
- détail de la cause visible uniquement dans l'espace privé du propriétaire ;
- `noindex, nofollow` pour `Unlisted` et `noindex` pour `Public` en V1 ;
- aucune entrée sitemap avant un futur opt-in SEO séparé ;
- URL canonique localisée et métadonnées Open Graph issues du même snapshot ;
- `Referrer-Policy: no-referrer` sur les pages non listées ;
- résolution soumise à une limite de débit et sans endpoint d'énumération ;
- HTML SSR utile sans JavaScript, responsive dès 320 px et sans débordement
  horizontal.

Un éventuel `410` ne sera étudié que pour une future page précédemment indexée. Il
ne doit pas révéler l'existence passée d'un lien non listé.

## Frontières d'architecture

- **Core** : value objects, types, transitions, invariants de politique et
  construction autorisée du snapshot ;
- **Application** : ownership, aperçu, orchestration des sources, concurrence,
  ports de persistance, publication, révocation et résolution ;
- **Infrastructure** : documents et indexes Mongo, CSPRNG, caches, jobs et rendus
  concrets ;
- **WebAPI** : authentification des routes privées, rate limiting public, DTO,
  Problem Details et en-têtes ;
- **Angular** : éditeur, résumé de confidentialité, aperçu et pages publiques via
  facades et ports.

Chaque classe, interface, record et enum créé dans les prochaines tranches occupe
son propre fichier. Les composants mobiles sont testés aux largeurs 320, 360, 390
et 768 px, avec textes longs dans les huit langues.

## Persistance cible, sans création dans SHARE-01

Les PR suivantes pourront créer `share-publications` puis
`share-publication-snapshots`. Le document de publication portera l'autorité
d'accès ; le snapshot séparé portera uniquement des champs publics.

Indexes minimaux envisagés et à confirmer par tests d'intégration :

- unicité partielle du jeton lorsqu'il existe ;
- `(OwnerUserId, Type, UpdatedAtUtc)` pour l'espace privé ;
- `(SourceScopeKey, OwnerUserId)` pour détecter les périmètres modifiés ;
- `(PublicationId, PublicationVersion)` unique pour les snapshots ;
- aucun index permettant une liste publique des propriétaires ou jetons.

Aucune collection, aucun index et aucun backfill ne sont créés par cet ADR.

## Preuves exigées dans les prochaines tranches

- tests Core exhaustifs de chaque champ autorisé et interdit par type ;
- tests démontrant l'absence physique de commentaires privés dans les snapshots,
  jobs, DTO, HTML et métadonnées ;
- publication refusée avec aperçu ou `SourceVersion` obsolète ;
- occurrence modifiée sans changement de `Visit.Version` détectée par la révision
  complète du périmètre ;
- ajout et suppression de contributeur détectés, y compris pour une année et un
  passeport multi-visites ;
- aperçu et résolution refusés pendant une mutation contributrice réservée ;
- suspension sur modification de source et republication explicite ;
- révocation et rotation sous concurrence optimiste ;
- collision de jeton et absence d'énumération ;
- résolution impossible malgré un cache de contenu encore présent ;
- tests Mongo aller-retour et compatibilité des chaînes ;
- tests SSR, SEO, huit langues, clavier, lecteur d'écran et responsive ;
- export et suppression couvrant publications et snapshots.

### Migration de remplacement du classement personnel

La centralisation du classement est une tranche dédiée après le socle API. À la
fin de cette tranche, un seul moteur reste actif :

1. exécuter avant ouverture du trafic un migrateur idempotent et reprenable qui
   crée une `SharePublication` de type `PersonalRanking` pour chaque ancien
   `UserRankingShare` ;
2. conserver son jeton existant lorsqu'il est valide afin de ne casser aucun lien ;
3. vérifier les totaux, l'unicité des jetons et un échantillon déterministe de
   snapshots avant de rendre l'instance prête ;
4. faire résoudre les routes existantes directement par les cas d'usage centraux ;
5. supprimer dans la même tranche `UserRankingShare`, son repository, son factory,
   ses handlers spécifiques et leurs injections ;
6. écrire toute nouvelle mutation uniquement dans `share-publications` ;
7. conserver au plus temporairement l'ancienne collection gelée comme sauvegarde
   de rollback, sans aucune lecture ni écriture applicative, puis la purger dans une
   opération explicitement validée.

Une erreur ou une collision bloque la readiness du partage et laisse la migration
reprenable ; elle ne déclenche ni lecture de repli, ni double écriture silencieuse.
Le plan de rollback restaure la release précédente avec une migration inverse des
seules mutations centrales postérieures au cutover, plutôt que de maintenir deux
sources de vérité.

Les boutons de partage et la télémétrie restent des consommateurs en aval. Ils ne
peuvent ni créer ni élargir une publication.

## Décisions rejetées

- un booléen public au niveau du compte ;
- réutiliser directement `UserRankingShare` pour tous les types ;
- maintenir un adaptateur permanent, un fallback de lecture ou une double écriture
  entre l'ancien et le nouveau moteur ;
- filtrer côté Angular un DTO privé ;
- lecture dynamique de la source lors d'une visite publique ;
- copie automatique d'un commentaire privé vers `PublicCaption` ;
- identifiant dérivé d'une donnée utilisateur ;
- révocation différée jusqu'au passage d'un worker ;
- cache public faisant autorité sur la visibilité ;
- comparaison automatique de deux passeports publics ;
- indexation par défaut.

## Retour arrière

SHARE-01 est documentaire. Son retour arrière retire cet ADR et les précisions de
roadmap. Aucune donnée, route, collection, interface ou publication n'est affectée.
