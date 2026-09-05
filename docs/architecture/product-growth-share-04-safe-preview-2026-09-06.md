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
interne. Chaque note publique abandonne l'identifiant de la note, de sa cible, de
son parc et de son propriétaire. Les commentaires privés, emails, positions,
accompagnants et notes textuelles ne sont représentés dans aucun résultat ou DTO de
cette tranche.

La lecture est plafonnée à 1 000 notes visibles pour protéger le VPS. Le résultat
signale explicitement une éventuelle troncature au lieu de prétendre être complet.

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
réserver un lease de 5 minutes
        │
        ├── écrire la note, le profil ou le catalogue
        │
        └── retirer le lease et incrémenter la révision si la source a changé
```

L'aperçu lit les deux révisions et l'identité publique, construit uniquement le
contenu autorisé, puis les relit. Il est rejeté si un lease est actif, si une
révision a changé ou si le pseudonyme, l'avatar ou l'état du compte diffère. Après
une interruption, un lease expiré est retiré atomiquement et la révision avance de façon
conservatrice : cela peut demander un nouvel aperçu inutilement, mais ne peut pas
publier silencieusement une donnée différente.

Les changements de notes, de pseudonyme, d'avatar, de rôle ou d'état du compte
protègent la révision personnelle dans toutes leurs voies d'écriture. L'identité
publique est également relue avant et après la construction : la révision prévient
les aperçus périmés et la double lecture ferme la fenêtre d'une mutation concurrente.
Un échec de règlement après une écriture déjà validée est journalisé sans transformer
le succès métier en erreur ; le lease durable expirera alors prudemment. Les
changements de nom, visibilité, catégorie ou rattachement d'un parc ou d'une
attraction protègent la révision du catalogue.

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
          └── IUserRepository (identité publique choisie seulement)
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
- incrément atomique de la révision après une vraie mutation ;
- protection du changement de pseudonyme public ;
- protection des changements d'avatar et d'état du compte ;
- conservation du succès métier lorsque le règlement d'un lease échoue ;
- application du plafond après retrait des notes visant des contenus masqués ;
- authentification, `no-store`, parsing strict des enums et DTO HTTP ;
- enregistrement des ports MongoDB et du builder spécialisé.

## Limites et suite

- aucun lien n'est encore créé par le nouveau moteur ;
- l'ancien partage de classement reste l'unique moteur actif ;
- l'aperçu n'est pas encore exposé par une interface Angular ;
- les builders visite, année et passeport arrivent avec `SHARE-06` à `SHARE-08`.

`SHARE-04A` migrera ensuite les partages de classement existants, conservera leurs
jetons et routes, figera leur contenu public dans le nouveau snapshot, puis retirera
l'ancien modèle dans la même livraison.
