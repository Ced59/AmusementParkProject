# SHARE-04 — Aperçu public sûr et révisions de source

Date : 2026-09-06

Version : 5.2.4

## Résultat métier

Une personne authentifiée peut demander au serveur l'aperçu exact des catégories
d'informations qu'elle envisage de rendre publiques. La première stratégie livrée
concerne le classement personnel ; les visites, années et passeports recevront leur
propre constructeur dans leurs jalons dédiés afin de ne pas mélanger leurs règles.

Cette tranche ne crée pas encore de publication et ne rend aucun nouveau lien
accessible. Elle sécurise l'étape préalable : voir ce qui pourrait être partagé,
sans faire transiter les données privées vers le contrat public.

## API privée

```text
POST /me/shares/preview
Authorization: compte activé et non bloqué
Cache-Control: no-store
Limite ciblée : 6 aperçus par minute et par compte

{
  "publicationType": "PersonalRanking",
  "sourceId": null,
  "datePrecision": "Hidden",
  "includedFields": ["PublicDisplayName", "GlobalRatings"]
}
```

La réponse contient la policy normalisée, la version de source et un contenu typé
`personalRanking`. Elle ne renvoie jamais la clé privée du périmètre ni un identifiant
technique de compte, de note, de parc ou d'attraction.

Les autres types sont refusés par un résultat métier contrôlé tant que leur builder
n'est pas livré. L'endpoint reste commun, mais chaque builder demeure une classe
spécialisée et testable.

## Confidentialité par construction

Le builder ne filtre pas un DTO privé après coup. Il lit uniquement ce que la policy
autorise :

| Choix | Lecture effectuée | Sortie publique |
|---|---|---|
| aucun champ | aucune lecture des notes | contenu vide, sans identité |
| `PublicDisplayName` | identité publique du compte | pseudonyme public seulement |
| `Avatar` | avatar déjà public | URL d'avatar seulement |
| `GlobalRatings` | notes dont la cible est publique | libellés, catégories et valeurs |

Les agrégats par parc conservent leur libellé humain mais abandonnent leur clé
interne. Les agrégats par type et catégorie conservent une clé fonctionnelle stable
afin que l'interface puisse traduire leur libellé sans dépendre du texte produit par
le serveur. Chaque note publique abandonne l'identifiant de la note, de sa cible, de
son parc et de son propriétaire. Les commentaires privés, emails, positions,
accompagnants et notes textuelles ne sont représentés dans aucun résultat ou DTO de
cette tranche.

La lecture et les statistiques sont plafonnées à 1 000 notes visibles pour protéger
le VPS. MongoDB applique la visibilité courante avant le tri et la limite, sans
matérialiser toutes les notes du compte en mémoire applicative. Une note visible
supplémentaire sert uniquement à signaler explicitement la troncature au lieu de
prétendre que le résultat est complet.
L'endpoint coûteux possède en plus une limite dédiée par compte, cumulée à la
protection globale par adresse IP. Cette première barrière s'exécute avant l'analyse
du jeton d'authentification ; la limite par compte s'applique ensuite, une fois
l'identité établie. Un même utilisateur ne peut donc pas multiplier les agrégations
en changeant simplement de connexion réseau et un faux jeton coûteux ne contourne
pas la protection du VPS.

## Barrière de révision

Le contenu du classement dépend de deux périmètres monotones :

```text
révision personnelle              révision du catalogue public
notes + identité publique          noms + visibilité + classement des cibles
             │                                  │
             └──────────┬───────────────────────┘
                        ▼
             SourceVersion = somme contrôlée
```

Les deux révisions ne peuvent qu'augmenter ; leur somme augmente donc dès que l'un
des périmètres change. Un dépassement 64 bits refuse l'aperçu.

Chaque mutation protégée suit ce protocole :

```text
réserver un lease renouvelé chaque minute
        │
        ├── écrire la note, le profil ou le catalogue
        │
        └── retirer le lease et incrémenter la révision si la source a changé
```

L'aperçu lit les deux révisions et l'identité publique, construit uniquement le
contenu autorisé, puis les relit. Il est rejeté si un lease est actif, si une
révision a changé ou si le pseudonyme, l'avatar ou l'état du compte diffère. Pendant
une écriture vivante, un heartbeat repousse l'expiration du lease avec cinq
minutes de marge. Le jeton d'annulation transmis à chaque écriture protégée expire
une minute avant le lease serveur et est aussi annulé dès qu'un heartbeat confirme
que le lease n'appartient plus à l'écrivain. Une écriture suspendue par une longue
indisponibilité MongoDB ne peut donc pas reprendre après que l'aperçu a récupéré son
lease. Après une interruption réelle, le lease expiré est retiré atomiquement et la
révision avance de façon conservatrice. Si une écriture avait déjà été validée mais
avait reçu une réponse ambiguë, sa finalisation avance encore la révision.
Une erreur MongoDB transitoire pendant le renouvellement n'arrête pas le heartbeat :
il réessaie toutes les cinq secondes au maximum tant que l'écriture détient son lease.

Les changements de notes, de pseudonyme, d'avatar, de rôle ou d'état du compte
protègent la révision personnelle dans toutes leurs voies d'écriture. L'identité
publique et l'image d'avatar courante sont également relues avant et après la
construction : la révision prévient les aperçus périmés et la double lecture ferme
la fenêtre d'une mutation concurrente. La relecture de l'image contourne
explicitement le cache mémoire local : chaque instance consulte MongoDB afin qu'un
déploiement sans interruption ne puisse pas réutiliser l'état périmé d'une autre
instance. Un avatar n'est émis que si l'image courante est encore publiée, appartient
bien au compte et reste dans la catégorie avatar.

La création distante, la promotion, le rattachement, la modification de métadonnées
et la suppression d'un avatar réservent le lease avant leur première écriture. Un
transfert protège simultanément l'ancien et le nouveau propriétaire, puis recalcule
leurs deux URL publiques depuis MongoDB avant de régler les leases. Une
dépublication unitaire ou en masse utilise le même chemin et retire donc l'avatar
public sans fenêtre incohérente. Toute réponse de mutation ambiguë est traitée
prudemment comme un changement possible afin de faire avancer la révision.
Les rattachements, promotions, changements de métadonnées et suppressions portent
en plus une précondition MongoDB atomique sur le propriétaire, la catégorie et
l'état courant observés : si une autre requête a transféré l'image entre la lecture
et l'écriture, la mutation devenue obsolète est refusée au lieu de modifier un
propriétaire non protégé. Les promotions qui suivent un import local ou distant
réutilisent la même précondition. L'import d'un avatar pendant une connexion externe
réserve lui aussi le lease avant de télécharger ou de créer l'image.
La resynchronisation du chemin public d'avatar utilise une écriture MongoDB partielle
qui ne touche qu'à `avatarUrl` et `updatedAt` : une lecture concurrente ne peut donc
pas rétablir d'anciens rôles, mots de passe ou états de blocage. Si l'import externe
a créé l'image mais perd ensuite la comparaison de version du compte, une relecture
autoritaire réaligne immédiatement `avatarUrl` sur l'image effectivement courante.
Lorsqu'une image courante change de propriétaire sans demander explicitement une
nouvelle promotion, elle est rétrogradée pendant le transfert ; elle ne peut ainsi
pas devenir courante dans deux périmètres différents.
Les autres remplacements complets d'un compte — profil, confirmation d'email et
flux de réinitialisation du mot de passe — exigent désormais la date de mise à jour
lue initialement. Une écriture concurrente fait échouer la comparaison atomique au
lieu d'être remplacée ; l'ancienne opération non protégée a été retirée du port. Dès
qu'une écriture susceptible de changer l'identité publique est envoyée à MongoDB,
sa finalisation avance prudemment la révision, même si la réponse est un timeout ou
un résultat de comparaison négatif.
Les promotions d'image sont en outre sérialisées par un verrou MongoDB distribué
sur le triplet propriétaire/catégorie. Deux instances API ne peuvent donc pas
promouvoir simultanément deux images du même périmètre et se rétrograder l'une
l'autre. Le verrou est renouvelé pendant l'opération et reprend son renouvellement
après une erreur MongoDB transitoire. Son opération protégée est annulée avant la
dernière expiration confirmée si MongoDB reste indisponible ; une ancienne promotion
ne peut donc pas reprendre après qu'une autre instance a acquis le verrou. Celui-ci
n'est libéré que par son propre jeton. La promotion rétrograde d'abord, sous le verrou,
les autres images courantes de façon idempotente, puis vérifie encore son bail avant
d'activer la cible. La réservation porte un jeton dédié, indépendant de l'horodatage
que peut modifier une édition de légende ou de crédit ; une promotion plus récente
retire les anciennes réservations avant d'activer sa propre cible. Une perte de bail
peut donc laisser temporairement le périmètre
sans image courante, mais jamais créer deux images courantes concurrentes ; une
annulation du client après la première écriture ne laisse donc pas deux images
courantes. Les actions de masse portent en plus la date de mise à jour observée :
elles ne peuvent pas réécrire une description ou des crédits modifiés entre-temps.
Un lot d'import de parc ou d'attraction qui échoue après son envoi est considéré
comme potentiellement appliqué : ses révisions de classement et de partage avancent
prudemment afin qu'aucun aperçu ne conserve un ancien nom ou une ancienne visibilité.
Enfin, une suppression MongoDB déjà validée reste annoncée comme réussie si le
nettoyage binaire secondaire échoue, puisque répéter la commande ne restaurerait
pas l'enregistrement supprimé.
Un échec de règlement après une écriture déjà validée est journalisé sans transformer
le succès métier en erreur ; le lease durable expirera alors prudemment. Les
changements de nom, visibilité, catégorie ou rattachement d'un parc ou d'une
attraction protègent la révision du catalogue.

Pendant un déploiement sans interruption, le candidat API dessert les routes
existantes mais répond `503` sur le nouvel aperçu. L'ancienne API ne connaît pas
encore cette route. L'aperçu ne devient donc disponible qu'avec l'instance API
canonique mise à niveau, après l'arrêt de tous les anciens processus d'écriture ;
le candidat est ensuite retiré. Cette barrière évite de mélanger un aperçu versionné avec une
écriture exécutée par une ancienne instance qui ne connaît pas encore les leases.

Les libellés de parc, catégories et types d'attraction sont reconstruits depuis le
catalogue courant. Les copies techniques présentes dans les anciens documents de
note ne servent jamais au contenu public et aucun identifiant interne ne remplace un
libellé public manquant.

## Architecture

```text
Core
  ShareContentPolicy (liste blanche)
          ▲
Application
  PreviewSharePublicationQueryHandler
          │ sélectionne une stratégie par type
          ▼
  PersonalRankingSharePreviewBuilder
          │
          ├── IShareSourceRevisionRepository
          ├── IRatingRepository (cibles visibles seulement)
          ├── IUserRepository (identité publique choisie seulement)
          ├── IImageRepository (avatar courant publié seulement)
          └── IImageCurrentMutationLock (promotion sérialisée par périmètre)
          ▲
Infrastructure
  ShareSourceRevisionRepository ── share-source-revisions
          ▲
WebAPI
  SharePublicationsController ── DTO public sans identifiants internes
```

Les règles de policy restent dans Core, l'orchestration et la construction publique
dans Application, MongoDB dans Infrastructure et HTTP dans WebAPI. Chaque classe,
record, enum et interface ajouté possède son propre fichier.

## MongoDB

La collection `share-source-revisions` est créée automatiquement au démarrage :

```text
share-source-revisions
├── _id                  clé privée du périmètre
├── revision             entier 64 bits monotone
├── mutationLeases[]
│   ├── token            corrélation opaque interne
│   └── expiresAtUtc     récupération prudente
├── createdAt
└── updatedAt
```

Elle ne contient aucun payload de profil ou de classement. Aucune intervention
manuelle MongoDB n'est requise au déploiement.

## Preuves automatisées

Les tests ciblés couvrent :

- validation de la policy avant toute lecture de source ;
- absence de lecture de notes avec la policy privée par défaut ;
- disparition des identifiants internes et de l'email dans le résultat sérialisé ;
- rejet d'un aperçu lorsque l'une des révisions change ;
- réservation et règlement des leases personnels et catalogue ;
- récupération prudente des leases expirés ;
- renouvellement des leases actifs et nouvelle révision après une finalisation tardive ;
- reprise du heartbeat après une erreur MongoDB transitoire ;
- annulation de l'écrivain avant récupération ou dès la perte confirmée de son lease ;
- reprise du heartbeat du verrou de promotion d'image sans annuler l'écriture ;
- annulation d'une promotion avant l'expiration de son dernier verrou confirmé ;
- réconciliation de la rétrogradation avant promotion après un échec MongoDB ambigu ;
- annulation après rétrogradation sans activation tardive de la cible ;
- rejet d'une action de masse fondée sur des métadonnées devenues obsolètes ;
- succès cohérent après une suppression MongoDB malgré l'échec du nettoyage binaire ;
- incrément atomique de la révision après une vraie mutation ;
- protection du changement de pseudonyme public ;
- protection des changements d'avatar et d'état du compte ;
- retrait d'un avatar dépublié, y compris par action de masse ;
- réservation du lease avant chaque écriture d'avatar concernée ;
- invalidation et resynchronisation des deux comptes lors d'un transfert d'avatar ;
- rétrogradation d'une image courante lorsqu'elle est transférée vers un autre compte ;
- écriture partielle de l'avatar sans réécriture des rôles ni de l'état du compte ;
- réconciliation autoritaire après un conflit de version suivant un import externe ;
- refus des remplacements de compte devenus obsolètes dans les flux profil et sécurité ;
- refus atomique d'un transfert fondé sur un propriétaire devenu obsolète ;
- réservation avant import d'un avatar fourni par une identité externe ;
- relecture MongoDB autoritaire de l'avatar courant sans cache local ;
- conservation du succès métier lorsque le règlement d'un lease échoue ;
- application du plafond après retrait des notes visant des contenus masqués ;
- agrégation bornée après les jointures de visibilité exécutées par MongoDB ;
- reconstruction des métadonnées publiques depuis le parc et l'attraction actuels ;
- authentification, `no-store`, parsing strict des enums et DTO HTTP ;
- limite ciblée par compte sur la génération des aperçus ;
- barrière IP avant authentification puis limite dédiée après authentification ;
- clés fonctionnelles stables pour les types et catégories, sans clé technique de parc ;
- enregistrement des ports MongoDB et du builder spécialisé.

## Limites et suite

- aucun lien n'est encore créé par le nouveau moteur ;
- l'ancien partage de classement reste l'unique moteur actif ;
- l'aperçu n'est pas encore exposé par une interface Angular ;
- les builders visite, année et passeport arrivent avec `SHARE-06` à `SHARE-08`.

`SHARE-04A` migrera ensuite les partages de classement existants, conservera leurs
jetons et routes, figera leur contenu public dans le nouveau snapshot, puis retirera
l'ancien modèle dans la même livraison.
