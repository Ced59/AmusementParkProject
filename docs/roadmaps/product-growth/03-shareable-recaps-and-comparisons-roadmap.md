# Roadmap 03 — Récapitulatifs partageables, passeport public et comparaisons consenties

> Code programme : `SHARE`
>
> Dépendance bloquante d'implémentation : gate `PASS-G` de la roadmap 02.
> `SHARE-01`, strictement documentaire, est la seule exception autorisée afin de
> figer les décisions avant persistance. `SHARE-02` et toutes les tranches de code
> restent bloquées tant que `PASS-G` n'est pas formellement validée.
>
> Base réutilisable : le dépôt possède déjà des identifiants de partage, une visibilité révocable, une page publique de classement personnel, du SSR public et des aperçus sociaux.
>
> Principe : le partage transforme une histoire personnelle réelle en objet lisible. Il ne doit jamais publier une présence, une date exacte, une note privée ou une identité sans choix explicite.

## 0. Avenant technique FOUNDATION

- les identifiants de publication restent des chaînes opaques et non dérivables ;
- `SharePublicationId` peut être un value object autour d’une chaîne sans modifier les routes ;
- le snapshot hybride conserve `SourceVersion`, `PublicationVersion` et la politique exacte utilisée ;
- une révocation est une écriture synchrone et atomique qui coupe immédiatement la résolution ;
- l’invalidation d’Open Graph, de cache et des rendus dérivés est un job coalescé par publication ;
- si le job manque, un reconciler compare source et publication ;
- aucun commentaire privé n’est copié dans le payload du job ;
- le cache public est versionné par publication et non seulement par URL.

Une indisponibilité du worker ne doit jamais empêcher une révocation. Le comportement sûr est de refuser ou suspendre le partage jusqu’à cohérence, pas de continuer à servir un snapshot dont la politique est incertaine.

### État de `SHARE-01` au 5 septembre 2026

L'ADR [`product-growth-share-01-publication-policy-2026-09-05.md`](../../architecture/product-growth-share-01-publication-policy-2026-09-05.md)
fixe les types, la liste blanche de données, les états séparés de la visibilité, le
snapshot entièrement figé en V1, les révisions 64 bits de périmètres complets, les
jetons opaques et la révocation autoritative. Cette exception documentaire ne crée
aucune publication, ne valide pas la partie terrain de `PASS-G` et n'autorise pas
`SHARE-02` : toutes les tranches d'implémentation restent bloquées tant que cette
gate n'est pas formellement franchie.

## 1. Vision produit

Après avoir enregistré une visite ou une année de visites, l’utilisateur peut générer un récit synthétique :

- récapitulatif d’une visite ;
- bilan annuel ;
- passeport public facultatif ;
- statistiques personnelles choisies ;
- classement actuel ;
- évolution de certaines notes ;
- comparaison entre deux profils ayant chacun consenti ;
- carte Open Graph fidèle aux données publiées.

La boucle recherchée est :

```text
j’enregistre une expérience
→ j’obtiens un récapitulatif utile
→ je contrôle ce qui est visible
→ je partage un lien révocable
→ le destinataire découvre le produit
→ il peut commencer son propre passeport
```

La croissance provient de l’utilité et de l’expression personnelle, pas d’une publication automatique.

## 2. Objectifs

- Centraliser le cycle de vie des publications personnelles dans `SharePublication`
  sans mélanger les politiques de contenu des classements, visites et passeports.
- Migrer le partage de classement existant vers cette autorité unique, sans
  adaptateur permanent ni double écriture.
- Créer des politiques de visibilité par type d’objet.
- Offrir un aperçu exact avant publication.
- Masquer par défaut les dates précises et les commentaires privés.
- Générer des pages SSR stables, accessibles et localisées.
- Révoquer immédiatement les liens et invalider les caches.
- Produire des images sociales déterministes à partir des données publiques.
- Permettre une comparaison uniquement lorsque les deux propriétaires l’acceptent.
- Mesurer les ouvertures et conversions sans tracer inutilement les visiteurs.

## 3. Non-objectifs

- fil social ;
- abonnements entre utilisateurs ;
- commentaires publics sur les passeports ;
- messagerie ;
- publication automatique sur Facebook, LinkedIn ou autre service ;
- géolocalisation publique ;
- classement public des utilisateurs par activité ;
- badges artificiels ;
- concours reposant sur le volume de rides ;
- indexation par défaut de tout profil ;
- partage d’un commentaire privé sans copie explicite vers un champ public.

## 4. Objets partageables

| Type | Contenu | Défaut | Identifiant | Indexation initiale |
|---|---|---|---|---|
| `VisitRecap` | Une visite et ses statistiques choisies | Privé | `shareId` opaque | `noindex` par défaut |
| `YearRecap` | Agrégats d’une année | Privé | `shareId` opaque | `noindex` par défaut |
| `PassportProfile` | Vue publique durable du passeport | Privé | slug/share id | Opt-in séparé |
| `PersonalRanking` | Classement existant migré vers l'autorité commune | Privé | jeton existant conservé à la migration | `noindex` par défaut |
| `ProfileComparison` | Intersection de deux profils consentants | Privé | jeton de comparaison | `noindex` |

Tous les types utilisent l'agrégat discriminé commun `SharePublication`. Chaque
type conserve toutefois sa policy et son constructeur de snapshot spécialisés ;
aucune classe géante ne connaît tous les contenus. Ne pas réutiliser un seul
booléen `IsPublic` du compte entier.

## 5. Modèle de publication

### 5.1 `SharePublication`

```csharp
public sealed class SharePublication
{
    public SharePublicationId Id { get; }
    public string OwnerUserId { get; }
    public SharePublicationType Type { get; }
    public string SourceScopeKey { get; }
    public string? ShareToken { get; private set; }
    public SharePublicationStatus Status { get; private set; }
    public ShareVisibility Visibility { get; private set; }
    public ShareContentPolicy ContentPolicy { get; private set; }
    public long SourceVersion { get; private set; }
    public long PublicationVersion { get; private set; }
    public DateTime? PublishedAtUtc { get; private set; }
    public DateTime? RevokedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
}
```

`ShareVisibility` :

- `Private` ;
- `Unlisted` : accessible par lien opaque ;
- `Public` : accessible, partageable et éventuellement indexable si l’opt-in SEO est distinct ;

Le cycle de vie est séparé : `Draft`, `Published`, `NeedsReview`, `Revoked`. Un
objet révoqué ne redevient pas public par un simple changement de visibilité.

### 5.2 `ShareContentPolicy`

Valeurs explicites plutôt qu’un snapshot implicite de l’écran :

- afficher/masquer date exacte ;
- afficher seulement l’année ou le mois ;
- afficher/masquer nombre de rides ;
- afficher/masquer notes temporelles ;
- afficher/masquer note globale ;
- afficher/masquer commentaires publics dédiés ;
- afficher/masquer statistiques géographiques ;
- afficher/masquer éléments manqués ;
- afficher/masquer profil public et avatar ;
- afficher/masquer noms des accompagnants — valeur initiale toujours `false` et fonction non prévue sans modèle de consentement.

La politique est versionnée et incluse dans le cache key.

### 5.3 Snapshot ou lecture dynamique

Deux stratégies :

#### Snapshot de publication

- stabilité de ce qui a été partagé ;
- révocation simple ;
- mais duplication des données et risque de contenu obsolète.

#### Lecture dynamique de la source

- reflète les corrections ;
- mais une modification privée peut changer un partage sans aperçu.

**Choix recommandé : hybride versionné.** La publication conserve un snapshot minimal des champs publics, lié à la `SourceVersion` monotone du périmètre complet. Cette révision couvre tous les documents contributeurs, y compris les occurrences dont la version évolue sans modifier leur visite parente. Lorsqu'un contributeur change :

- le partage passe à `NeedsReview` ;
- l'ancienne version est suspendue en V1 afin de conserver une règle unique et sûre ;
- le propriétaire voit les différences ;
- il republie explicitement ;
- aucune nouvelle donnée privée n’est ajoutée automatiquement.

Pour un compteur sans risque, une mise à jour dynamique peut être autorisée seulement si le champ était déjà choisi et si la politique le précise.

## 6. Identifiants et sécurité des liens

- identifiants aléatoires d’entropie suffisante ;
- aucune dérivation de `UserId` ou `VisitId` ;
- rotation possible ;
- révocation immédiate ;
- anciens identifiants répondent `404` ou `410` selon politique ;
- ne pas exposer dans les logs applicatifs complets ;
- éviter referrer vers des sites tiers par politique adaptée ;
- rate limiting sur résolution de partage ;
- aucune API permettant d’énumérer les liens non listés ;
- partage public par slug seulement après validation d’un nom stable et protection contre l’usurpation.

## 7. Récapitulatif de visite

### 7.1 Contenu minimal

- parc ;
- date selon précision autorisée ;
- nombre d’éléments distincts faits ;
- nombre total de rides ;
- catégories représentées ;
- note de parc de la visite si choisie ;
- top de la visite calculé uniquement sur les notes disponibles ;
- attraction la plus refaite ;
- liste des éléments sélectionnés par le propriétaire ;
- mention des données masquées ou incomplètes ;
- lien vers la fiche du parc ;
- CTA sobre « Créer mon propre passeport ».

### 7.2 Contenu éditorial facultatif

Créer un champ `PublicCaption` séparé du commentaire privé :

- vide par défaut ;
- taille bornée ;
- aperçu ;
- suppression ;
- modération/signalement si indexé publiquement ;
- aucune copie automatique du commentaire privé.

### 7.3 Sélection des faits marquants

Les faits sont déterministes :

- plus grand nombre de rides ;
- plus haute note de la visite ;
- découverte nouvelle pour l’utilisateur ;
- élément historique/disparu ;
- écart notable avec note globale.

L’utilisateur choisit ceux à publier. Ne jamais inventer « révélation », « déception » ou émotion à partir d’un score seul.

## 8. Bilan annuel

### 8.1 Agrégats possibles

- parcs visités ;
- visites ;
- rides ;
- éléments distincts ;
- nouvelles découvertes ;
- pays/régions ;
- catégories ;
- constructeurs ;
- parcs les plus visités ;
- éléments les plus refaits ;
- meilleures notes temporelles ;
- évolution de notes avec seuil suffisant ;
- part de données approximatives ;
- attractions désormais fermées.

### 8.2 Règles de probité

- afficher les dénominateurs ;
- ne pas qualifier de « meilleur » un élément noté une seule fois sans précision ;
- ne pas comparer l’utilisateur à une communauté sans échantillon et consentement ;
- pas de percentile fictif ;
- pas de score de passion ;
- pas de classement selon la dépense ou le volume ;
- aucun message culpabilisant lorsque l’année contient une seule visite ;
- si l’année est vide, proposer un passeport rétrospectif, pas une fausse carte.

### 8.3 Génération

- disponible à la demande toute l’année ;
- année civile selon calendrier choisi ;
- fuseau et date partielle documentés ;
- version de calcul ;
- aperçu avant publication ;
- regeneration après correction seulement avec validation.

## 9. Passeport public

### 9.1 Sections sélectionnables

- présentation publique ;
- nombre de parcs ;
- carte approximative par pays, jamais positions de visite ;
- liste des parcs visités ;
- wishlist si activée ;
- classement personnel courant ;
- statistiques temporelles agrégées ;
- bilans annuels publiés ;
- récents récapitulatifs choisis ;
- historique d’évolution choisi.

### 9.2 Confidentialité granulaire

Le propriétaire choisit :

- public/non listé ;
- nom affiché ;
- avatar ;
- années visibles ;
- dates exactes ;
- parcs masqués ;
- éléments masqués ;
- notes visibles ;
- compteurs visibles ;
- indexation ;
- possibilité de comparaison.

Un résumé avant publication liste exactement les données exposées.

### 9.3 Indexation

Première version recommandée :

- pages non listées `noindex, nofollow` ;
- pages publiques `noindex` par défaut ;
- opt-in séparé pour indexation après maturité de la modération et des contenus ;
- canonical stable ;
- suppression du sitemap dès révocation ;
- `410` temporaire possible après retrait public ;
- aucune donnée structurée `Person` excessive.

## 10. Comparaison entre profils

### 10.1 Consentement bilatéral

Flux :

1. A crée une invitation de comparaison ;
2. choisit les catégories de données ;
3. lien à durée limitée ;
4. B s’authentifie ou accepte selon politique ;
5. B voit l’aperçu de ce que chacun partagera ;
6. B accepte ;
7. un objet de comparaison est créé ;
8. chacun peut révoquer ;
9. révocation rend le lien inutilisable.

Aucun profil public ne peut être comparé automatiquement sans autorisation explicite du propriétaire, même si ses données sont visibles.

### 10.2 Résultats

- parcs en commun ;
- éléments en commun ;
- préférences proches ;
- divergences de notes globales ;
- divergences temporelles seulement si les deux les partagent ;
- parcs visités par l’un et à découvrir par l’autre ;
- prochain parc possible, comme suggestion explicable ;
- couverture des données.

### 10.3 Calculs

- différence absolue de note ;
- corrélation uniquement avec un minimum de cibles communes défini ;
- aucune compatibilité en pourcentage sous le seuil ;
- pas de jugement « meilleur passionné » ;
- afficher `N cibles communes` ;
- ne pas utiliser les observations de ride comme voix communautaire.

## 11. API et cas d’usage

### 11.1 Publications

```text
POST   /api/me/shares/preview
POST   /api/me/shares
GET    /api/me/shares
GET    /api/me/shares/{publicationId}
PATCH  /api/me/shares/{publicationId}
POST   /api/me/shares/{publicationId}/republish
POST   /api/me/shares/{publicationId}/rotate-link
DELETE /api/me/shares/{publicationId}
GET    /api/shared/{shareId}
```

### 11.2 Comparaisons

```text
POST   /api/me/comparisons/invitations
GET    /api/me/comparisons/invitations/{token}/preview
POST   /api/me/comparisons/invitations/{token}/accept
DELETE /api/me/comparisons/{comparisonId}
GET    /api/shared/comparisons/{shareId}
```

### 11.3 Cas d’usage

- `PreviewSharePublicationQuery` ;
- `CreateSharePublicationCommand` ;
- `UpdateShareContentPolicyCommand` ;
- `RepublishShareSnapshotCommand` ;
- `RotateShareIdCommand` ;
- `RevokeSharePublicationCommand` ;
- `ResolveSharePublicationQuery` ;
- `GenerateShareImageCommand/Query` ;
- `CreateComparisonInvitationCommand` ;
- `AcceptComparisonInvitationCommand` ;
- `RevokeComparisonCommand` ;
- `GetProfileComparisonQuery`.

## 12. Persistance

Collections proposées :

- `share-publications` ;
- `share-publication-snapshots` ;
- `profile-comparison-invitations` ;
- `profile-comparisons` ;
- `share-render-jobs` si génération différée.

Indexes :

- unique `ShareId` ;
- `{ OwnerUserId, Type, UpdatedAtUtc }` ;
- `{ SourceType, SourceId, OwnerUserId }` ;
- TTL sur invitations expirées ;
- unique paire canonique de participants pour comparaison active si le produit l’exige ;
- aucun TTL sur publication active ;
- suppression logique courte puis purge.

## 13. Rendu SSR et caches

- route SSR publique dédiée par type ;
- cache key incluant `shareId`, `PublicationVersion`, langue et politique ;
- invalidation sur révocation, rotation, republication ou suppression source ;
- aucun accès API privé pendant le rendu public ;
- DTO public construit côté serveur ;
- en cas d’échec de rendu, ne pas servir un ancien snapshot après révocation ;
- `404` pour lien inconnu ;
- `410` facultatif pour lien révoqué récent ;
- headers robots selon visibilité ;
- CSP et images compatibles avec l’infrastructure existante.

## 14. Images sociales

### 14.1 Contenu

- nom du parc ou type de bilan ;
- données réellement publiques ;
- nombre limité de statistiques ;
- image licenciée et autorisée, ou design sans photo ;
- logo ;
- langue ;
- mention discrète de la nature personnelle ;
- aucun commentaire privé ;
- aucune date plus précise que la politique.

### 14.2 Génération

- déterministe ;
- version du template ;
- dimensions par plateforme ;
- texte tronqué proprement ;
- polices autorisées ;
- cache versionné ;
- purge ;
- fallback ;
- tests snapshot visuels ;
- alt text généré à partir des mêmes données.

### 14.3 Exactitude

L’image ne doit pas survivre à la révocation dans le CDN public sans durée bornée. Prévoir :

- URLs versionnées ;
- cache-control adapté ;
- suppression du fichier source ;
- tolérance aux caches externes expliquée au propriétaire ;
- aucune promesse d’effacement immédiat des copies déjà détenues par un réseau social.

## 15. Interface Angular

```text
features/profile/sharing/
  pages/share-center/
  pages/share-editor/
  pages/share-preview/
  components/share-content-policy-form/
  components/share-privacy-summary/
  components/share-link-control/
  state/share-editor.facade.ts

features/public/shared-passport/
features/public/shared-visit-recap/
features/public/shared-year-recap/
features/public/shared-comparison/
```

Règles :

- aperçu identique au rendu public autant que possible ;
- distinction nette entre sauvegarder un brouillon et publier ;
- contrôle de visibilité toujours visible ;
- bouton révoquer accessible ;
- copie de lien seulement après publication ;
- message sur la persistance possible des aperçus dans les caches tiers ;
- partage natif navigateur si disponible, fallback copie ;
- aucun SDK social obligatoire.

## 16. Modération et signalement

Les statistiques seules ne nécessitent pas une modération éditoriale forte. Les champs publics libres, avatars et noms l’exigent.

Première version :

- limiter le texte public ;
- signalement depuis chaque page publique ;
- raisons structurées ;
- suspension de publication ;
- audit ;
- rate limit ;
- blocage des scripts/liens dangereux ;
- règles claires ;
- ne pas rendre la visite privée inaccessible au propriétaire lors d’une suspension publique.

## 17. Export, suppression et RGPD

L’export inclut :

- publications ;
- politiques ;
- versions ;
- invitations ;
- comparaisons ;
- dates de publication/révocation.

Suppression du compte :

- révoque tous les liens avant purge ;
- retire les pages des sitemaps ;
- invalide les caches internes ;
- supprime snapshots et images ;
- expire les invitations ;
- rend les comparaisons inaccessibles ;
- conserve seulement les traces légalement nécessaires et minimisées.

## 18. Analytics minimisés

Mesurer :

- aperçu créé ;
- publication ;
- révocation ;
- rotation ;
- ouverture d’un partage ;
- CTA vers création du Passeport ;
- démarrage puis réussite de l’activation ;
- type de récapitulatif ;
- erreur de rendu.

Ne pas mesurer dans un outil tiers :

- liste complète des attractions ;
- notes exactes ;
- date exacte ;
- identités comparées ;
- commentaire public complet.

## 19. Tests obligatoires

### Core/Application

- politiques de visibilité ;
- snapshot excluant chaque champ masqué ;
- modification source -> `NeedsReview` ;
- révocation ;
- rotation ;
- consentement bilatéral ;
- seuil de comparaison ;
- confidentialité de date ;
- suppression.

### Infrastructure

- unicité share id ;
- collision ;
- TTL invitation ;
- cache invalidation ;
- jobs de rendu idempotents ;
- purge fichiers ;
- reprise après erreur.

### WebAPI

- accès privé/public ;
- lien révoqué ;
- enumération impossible ;
- rate limiting ;
- robots ;
- OpenAPI ;
- Problem Details ;
- aucune donnée supplémentaire dans le DTO public.

### Angular/SSR

- aperçu par politique ;
- huit langues ;
- clavier et lecteur d’écran ;
- responsive ;
- meta/OG exactes ;
- page sans JavaScript lisible ;
- changement de langue ;
- révocation et cache ;
- comparaison avec données insuffisantes.

### End-to-end

1. publier un récapitulatif masquant le jour ;
2. ouvrir le lien anonymement ;
3. vérifier que le jour, les commentaires et les rides exclus n’apparaissent nulle part, y compris HTML et OG ;
4. corriger la visite ;
5. vérifier l’état `NeedsReview` ;
6. republier ;
7. révoquer ;
8. vérifier page, API, image et cache ;
9. créer une comparaison ;
10. révoquer par l’un des participants.

## 20. Découpage recommandé en PR

| PR | Contenu | Critère |
|---|---|---|
| `SHARE-01` | ADR types de partage, visibilité et snapshot hybride | Politique comprise avant persistance |
| `SHARE-02` | Core `SharePublication` et policy | Tests de confidentialité exhaustifs |
| `SHARE-03` | Persistance, ids opaques, révocation | Aucun lien énumérable |
| `SHARE-04` | Preview API + DTO public | Champs privés absents par construction |
| `SHARE-04A` | Migration de remplacement du partage de classement | Un seul moteur actif, routes et liens existants inchangés |
| `SHARE-05` | Éditeur Web et résumé de confidentialité | Publication consciente |
| `SHARE-06` | Récapitulatif de visite SSR | HTML public exact |
| `SHARE-07` | Bilan annuel | Agrégats vérifiés |
| `SHARE-08` | Passeport public sélectionnable | Granularité validée |
| `SHARE-09` | Images sociales versionnées | Aucune fuite dans OG |
| `SHARE-10` | Révocation, rotation et invalidation | Caches internes purgés |
| `SHARE-11` | Invitation et consentement de comparaison | Accord bilatéral obligatoire |
| `SHARE-12` | Résultat de comparaison et seuils | Pas de pourcentage sous seuil |
| `SHARE-13` | Signalement/modération minimale | Champs publics opérables |
| `SHARE-14` | Export/suppression/analytics | Cycle de vie complet |
| `SHARE-15` | Cohorte bêta et retrait des flags | Gate franchie |

## 21. Gate finale `SHARE-G`

- aucun objet n’est public par défaut ;
- chaque champ visible résulte d’une politique explicite ;
- dates précises et commentaires privés sont masqués par défaut ;
- aperçu, HTML SSR, API et image sociale exposent le même périmètre ;
- les liens sont opaques, rotatifs et révocables ;
- une source modifiée ne publie pas silencieusement de nouvelles données ;
- les comparaisons exigent deux consentements ;
- les faibles volumes ne produisent pas de compatibilité pseudo-précise ;
- export et suppression couvrent toutes les publications ;
- l’ouverture d’un partage peut conduire au Passeport sans dark pattern ;
- la fonction reste exploitable sans fil social ni chat ;
- les premiers testeurs comprennent ce qui est public avant de confirmer.
