# Roadmap 03 — Récapitulatifs partageables, passeport public et comparaisons consenties

> Code programme : `SHARE`
>
> Dépendances préalables : `PASS-20` livré et volet technique de `PASS-G` vérifié,
> notamment l'ownership, l'idempotence, l'export/suppression, l'usage mobile et le
> respect du budget du VPS.
> Décision produit du 5 septembre 2026 : la validation communautaire terrain de
> `PASS-G` devient un suivi qualitatif post-livraison non bloquant. `SHARE-02` et les
> tranches suivantes peuvent avancer sans dépendre de visites ou de testeurs réels,
> sous réserve de leurs propres gates techniques, métier, sécurité et confidentialité.
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
jetons opaques et la révocation autoritative. Cette décision documentaire ne crée
aucune publication et ne valide pas la partie terrain de `PASS-G`. Conformément à la
décision produit du 5 septembre 2026, cette partie terrain ne bloque toutefois plus
`SHARE-02` ni les tranches d'implémentation suivantes.

### État de `SHARE-02` au 5 septembre 2026

Le Core `SharePublication`, son identifiant typé et la liste blanche
`ShareContentPolicy` sont livrés en version 5.2.0. Les cinq intentions partagent le
même cycle de vie sans partager aveuglément leurs capacités de contenu. Les données
privées interdites ne sont pas représentables dans la policy V1 et 52 tests ciblés
couvrent la matrice de confidentialité, les transitions, les aperçus obsolètes, les
conflits et la révocation. Les décisions et preuves sont détaillées dans
[`product-growth-share-02-domain-model-2026-09-05.md`](../../architecture/product-growth-share-02-domain-model-2026-09-05.md).

Cette tranche ne crée encore ni stockage MongoDB, ni endpoint, ni page publique.
`SHARE-03` reste responsable des jetons cryptographiques et de la persistance
autoritative.

### État de `SHARE-03` au 5 septembre 2026

La persistance autoritative est livrée en version 5.2.2 dans une collection MongoDB
dédiée. Les jetons publics portent 256 bits générés par CSPRNG, sont validés sous leur
forme Base64 URL canonique et protégés par un index unique partiel. La révocation et
la rotation s'écrivent par remplacement atomique borné au propriétaire et à une
version de concurrence distincte de la version du rendu public.

La résolution publique exige toujours le jeton complet et aucun contrat de liste
publique n'existe. Les documents ne peuvent contenir que la policy en liste blanche
et les métadonnées internes du cycle de vie ; les données privées interdites en sont
physiquement absentes. L'architecture et les preuves sont détaillées dans
[`product-growth-share-03-persistence-2026-09-05.md`](../../architecture/product-growth-share-03-persistence-2026-09-05.md).

Le nouveau stockage n'est pas encore branché sur une route publique et l'ancien
partage de classement reste seul actif jusqu'à sa migration de remplacement
`SHARE-04A`. Il n'existe donc ni double écriture ni double moteur actif.

### État de `SHARE-04` au 6 septembre 2026

L'API privée d'aperçu et son premier constructeur spécialisé de classement personnel
sont livrés en version 5.2.4. Le serveur applique la policy en liste blanche avant
de lire les notes : un aperçu sans `GlobalRatings` ne charge aucune note et le DTO
public ne contient ni identifiant utilisateur, ni identifiant de note, de parc ou
d'attraction, ni commentaire privé, ni email.

Deux révisions durables protègent l'aperçu : l'une suit les notes et toute l'identité
publique du propriétaire — pseudonyme, avatar, rôles et état du compte —, l'autre le
catalogue public qui fournit les noms et la visibilité des cibles. L'identité publique
est aussi relue après la construction pour fermer la fenêtre des mutations concurrentes.
Chaque mutation suivie réserve d'abord un lease ; l'aperçu n'est accepté que si les
révisions et l'identité restent stables. Un heartbeat distingue les écritures longues
des écritures abandonnées. La perte confirmée du lease annule l'écrivain et son délai
local expire avant le lease serveur : aucune écriture suspendue ne peut reprendre
après récupération. Une finalisation ambiguë avance encore la révision. Les
métadonnées des notes publiques sont toujours relues depuis le
catalogue courant, sans repli sur un identifiant technique. Les détails et preuves
sont consignés dans
[`product-growth-share-04-safe-preview-2026-09-06.md`](../../architecture/product-growth-share-04-safe-preview-2026-09-06.md).
La génération est limitée par compte, les catégories exposent des clés fonctionnelles
traduisibles sans divulguer les identifiants de parc, et les synchronisations d'avatar
ne peuvent plus réécrire d'autres champs du compte. Un transfert d'image courante la
rétrograde tant qu'elle n'est pas explicitement promue dans son nouveau périmètre ;
un import externe ayant gagné côté image mais perdu côté compte est réconcilié depuis
l'état MongoDB autoritaire. Les remplacements complets de compte exigent une version
inchangée et le heartbeat reprend après une panne MongoDB transitoire au lieu
d'abandonner silencieusement la protection d'une écriture longue. Le verrou
distribué de promotion d'image applique la même reprise et rétrograde les autres
images avant d'activer la cible ; une annulation ne peut donc pas laisser plusieurs
images courantes, même si elle intervient juste avant l'expiration du bail. La cible
est d'abord réservée comme non courante avec la précondition observée, afin qu'une
demande obsolète ne puisse pas rétrograder l'image légitime. Cette réservation utilise
un jeton distinct des métadonnées éditoriales et toute promotion plus récente retire
les anciens jetons avant son activation. Les changements de périmètre, d'état courant
et les suppressions refusent toute cible encore réservée. Le verrou annule ensuite
l'ancien écrivain avant la dernière expiration confirmée si MongoDB reste
indisponible. Après cette
première écriture, la rétrogradation des autres images est réconciliée de manière
idempotente sous le verrou. Les actions de masse refusent aussi d'écraser des
métadonnées concurrentes grâce à leur date observée. Les écritures de profil à
réponse ambiguë font avancer prudemment la révision et la protection IP s'exécute
avant l'authentification, tandis que le plafond propre à l'aperçu s'applique ensuite
au compte identifié. Un lot d'import de catalogue ayant échoué après son envoi fait
également avancer les révisions de façon conservatrice, car certaines écritures non
ordonnées peuvent déjà avoir été appliquées.

Cette tranche ne publie encore aucun nouveau type de lien. Le remplacement du
partage de classement est traité par `SHARE-04A`.

### État de `SHARE-04A` au 6 septembre 2026

Le partage de classement existant est centralisé en version 5.2.6 sans changer ses
routes ni les jetons déjà distribués. Les réglages, la publication, la révocation,
la résolution publique et l'éligibilité aux publications sociales utilisent
directement `SharePublication`. L'ancien agrégat, son repository, sa factory et ses
handlers sont supprimés : aucune lecture de repli ni double écriture ne subsiste.

Avant readiness, un migrateur MongoDB idempotent sous lease conserve les jetons,
vérifie les collisions, les totaux et un échantillon déterministe. Le déploiement
zéro-coupure gèle physiquement l'ancien stockage avant le candidat ; un échec avant
la bascule canonique déclenche la migration inverse des mutations centrales puis le
dégel. Après succès, l'ancienne collection reste un backup gelé et n'est plus relue.
Les détails et preuves sont consignés dans
[`product-growth-share-04a-ranking-cutover-2026-09-06.md`](../../architecture/product-growth-share-04a-ranking-cutover-2026-09-06.md).

Cette tranche ne modifie pas encore l'interface. `SHARE-05` ajoute ensuite l'éditeur
de contenu public et le résumé de confidentialité avant confirmation.

### État de `SHARE-05` au 7 septembre 2026

L'éditeur Web du classement personnel est livré en version 5.2.7 comme première
interface du moteur central. Un membre choisit s'il affiche son nom public ou partage
anonymement, tandis que les notes globales restent le contenu indispensable d'un
classement. Avant toute publication, l'API construit un aperçu versionné et
l'interface distingue explicitement ce qui sera visible de ce qui restera privé :
visites, dates, commentaires privés, email, identifiant technique et avatar.

La confirmation renvoie la version et la policy exactes de cet aperçu. Le serveur
refuse la publication si la source a évolué entre-temps et exige un nouvel aperçu ;
il enregistre ensuite la sélection dans `SharePublication`. L'ancien bouton direct
ne pilote donc plus la mise en ligne depuis le profil. La révocation et les liens
historiques restent compatibles avec le moteur central migré. Le composant dédié se
replie en une colonne, borne ses contenus et empile ses actions sur mobile. Des tests
ciblés couvrent le partage anonyme, l'obsolescence de l'aperçu, la politique minimale,
les contrats HTTP, l'orchestration Angular et le responsive.

La voie HTTP historique ne peut désormais que révoquer un partage : toute nouvelle
publication exige l'approbation d'un aperçu. Chaque lecture publique contrôle la
publication et sa source avant puis après la construction du contenu ; une évolution
concurrente rend donc la réponse indisponible plutôt que de diffuser des données non
approuvées. Le profil présente également comme privé un lien devenu obsolète. Enfin,
l'aperçu annonce le nombre de notes montrées et le nombre total qui sera publié afin
qu'un échantillon de trois lignes ne puisse pas être confondu avec le contenu complet.
Le volume public est plafonné aux 5 000 meilleures notes visibles : au-delà, l'aperçu
le signale clairement et ses statistiques portent exactement sur ces 5 000 notes.
Les endpoints publics emploient par ailleurs une projection dédiée qui exclut les
dates de notation, l'identifiant privé de la note, les clés statistiques internes et
les métadonnées techniques.
Une preuve d'approbation opaque et signée lie désormais le membre, la source, sa version
et la sélection exacte affichée : modifier un champ après l'aperçu invalide la
publication. L'absence de nom public reste une valeur sémantiquement anonyme jusqu'à la
couche de présentation, qui fournit le libellé adapté à la langue de la page.

Les contrôles de version du classement lisent les révisions membre et catalogue en
un seul snapshot MongoDB, sans upsert sur le chemin public. Un bail expiré est
interprété conservativement comme une révision avancée, tandis qu'un bail actif
interdit l'exposition. La publication recontrôle enfin ce snapshot après son écriture
avant d'annoncer le succès : une mutation concurrente impose donc un nouvel aperçu
au lieu de créer un lien immédiatement obsolète.

Si l'écrivain retardé termine après l'expiration de son bail, la génération avancée
projetée par les lectures est persistée atomiquement, même lorsqu'il déclare finalement
n'avoir rien modifié. La version ne peut donc jamais revenir en arrière puis réactiver
un ancien partage. Les battements de vie vérifient en outre l'expiration avec l'horloge
du serveur MongoDB au moment atomique de l'écriture : une requête réseau retardée ne
peut pas ressusciter un bail expiré. Les budgets de limitation des aperçus et des
confirmations sont également séparés : comparer plusieurs choix de confidentialité
ne peut pas consommer
la capacité réservée à leur confirmation.

La preuve d'approbation lie également l'identifiant et la version de la publication
existante. Une révocation ou un changement de confidentialité survenu après l'aperçu
invalide donc celui-ci : la dernière décision du membre reste toujours prioritaire.
Ce contrôle est répété juste avant l'écriture pour fermer la fenêtre de concurrence.
Une indisponibilité transitoire du dernier contrôle de source, après une écriture déjà
confirmée, ne transforme pas ce succès en faux échec ; la résolution publique continue
dans tous les cas à refuser une source dont la version ne peut pas être vérifiée. Une
mutation effectivement détectée comme active reste distinguée de cette panne technique
et empêche d'annoncer le partage comme prêt. Après une modification de note réussie,
le profil invalide ses aperçus en cours et recharge aussitôt l'état public du classement,
sans attendre un rechargement complet de la page. Une réponse de publication plus ancienne
que cette modification est également ignorée. Le classement ne propose actuellement que le
nom public facultatif et les notes globales : l'avatar est refusé tant que la page publique ne
le restitue pas réellement, et une migration idempotente retire ce champ des politiques issues
de l'ancien partage. Le heartbeat MongoDB cible enfin le bail exact par filtre de tableau,
sans dépendre d'un opérateur positionnel non lié par le filtre serveur. Si une nouvelle
publication a déjà été écrite mais que le dernier contrôle détecte une source momentanément
instable, la publication reste préparée dans un état privé et l'API exige un nouvel aperçu : le
lien public n'est créé qu'après ce dernier contrôle, ce qui évite toute compensation pour les
changements déjà observables.
Une décision concurrente reste prioritaire grâce au contrôle de version exact répété avant
l'écriture publique atomique. Un second contrôle immédiatement après cette écriture ferme la
fenêtre où une mutation pourrait commencer pendant la publication : le lien exact est alors
révoqué et l'échec de source reste renvoyé, même si la persistance de cette révocation ne peut pas
être confirmée après les reprises bornées. Dans ce dernier cas, le résolveur public reste fermé
sur la source instable ou sur sa nouvelle version. Toute republication après révision reçoit en
plus un nouveau jeton : une révocation non confirmée ne peut donc jamais réactiver une URL déjà
distribuée. Enfin, le contrôle de dépassement des 5 000
notes publiques repose sur un comptage MongoDB borné à 5 001 : aucun aperçu ne charge un historique
complet uniquement pour détecter la troncature.

Cette tranche ne crée pas encore de nouveau type de page publique. `SHARE-06`
applique ensuite le même consentement au récapitulatif public d'une visite.

### État de `SHARE-06` au 7 septembre 2026

Le récapitulatif partageable d'une visite terminée est implémenté en version 5.2.9.
Depuis le journal de visite, le membre ouvre un atelier qui choisit séparément la
précision de date, les compteurs de tours, les notes de cette visite, les éléments
manqués, les attractions visibles et une légende publique facultative. Toute
modification invalide l'aperçu précédent : la confirmation ne peut publier que la
sélection exacte que le serveur vient de reconstruire.

La note privée de visite et les commentaires privés de passages ne font pas partie
de la projection de lecture et ne peuvent donc pas être copiés implicitement. La
légende est un champ public distinct, vide par défaut et limité. La source associe la
version de la visite à la révision du catalogue public ; une visite en brouillon, une
écriture instable ou une donnée ayant changé depuis l'aperçu bloque la publication.

Chaque confirmation fige un snapshot minimal dans une collection MongoDB dédiée,
lié à l'identifiant interne de publication, à sa version publique, à la policy et à
une empreinte de la sélection. La page anonyme résout uniquement le jeton opaque,
revalide la source avant et après la lecture du snapshot, puis rend le parc, la date
autorisée, les compteurs, les notes et les attractions choisies. Une révocation coupe
immédiatement le lien. Le HTML est rendu côté serveur, marqué
`noindex,nofollow,noarchive` et servi avec `Referrer-Policy: no-referrer`.

L'interface publique prend la forme d'une carte-souvenir et d'un parcours plutôt que
d'un écran administratif. Elle propose un lien contextuel vers le parc et une entrée
vers le brouillon local du passeport, utilisable sans compte. L'atelier privé et la
page publique bornent toutes leurs grilles, textes et actions et se replient à
680, 620, 520 et 390 px afin de rester utilisables dès 320 px. `SHARE-07` peut
maintenant construire le bilan annuel sur le même consentement et les mêmes preuves.

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
    public ShareToken? ShareToken { get; private set; }
    public SharePublicationStatus Status { get; private set; }
    public ShareVisibility Visibility { get; private set; }
    public ShareContentPolicy ContentPolicy { get; private set; }
    public long SourceVersion { get; private set; }
    public long PublicationVersion { get; private set; }
    public long Version { get; private set; }
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
