# SHARE-02 — Autorité métier et liste blanche des partages

Date : 2026-09-05

Version : 5.2.0

## Résultat métier

Le projet possède désormais un seul modèle métier capable de porter les futurs
partages de visite, d'année, de passeport, de classement personnel et de
comparaison. Cette tranche ne publie encore aucune donnée et ne crée ni route ni
collection MongoDB. Elle fixe d'abord les règles qui rendront les tranches suivantes
sûres.

Chaque intention crée une publication privée et non résolvable. Une publication ne
peut devenir accessible qu'après approbation d'un aperçu construit avec exactement
la même révision de source et exactement la même politique de contenu.

## Frontière de confidentialité

`ShareContentPolicy` est une liste blanche et non un filtre appliqué après lecture
d'un DTO privé. La V1 peut représenter uniquement :

- le nom public et l'avatar ;
- le nombre de passages ;
- les notes temporelles choisies ;
- les notes globales choisies ;
- un texte public dédié ;
- des statistiques géographiques agrégées ;
- des éléments manqués ;
- une précision de date bornée par le type de publication.

Les commentaires privés, notes privées textuelles, positions précises, noms
d'accompagnants et choix d'indexation SEO sont absents du contrat. Ils ne peuvent
donc pas être activés accidentellement par un booléen, une valeur par défaut ou une
sérialisation future de cette version.

Les capacités sont validées par type. Par exemple, un classement personnel ne peut
publier ni date, ni nombre de passages, ni note temporelle ; un bilan annuel ne peut
publier qu'une précision annuelle ; une comparaison pourra porter des notes
temporelles uniquement après le double consentement prévu par `SHARE-11`.

## Cycle de vie commun

```text
Draft / Private / sans jeton
        │ aperçu exact approuvé
        ▼
Published / Unlisted ou Public / jeton opaque
        │                         │
        │ source ou policy change │ révocation
        ▼                         ▼
NeedsReview / Private         Revoked / Private
        │                         sans jeton, terminal
        │ nouvel aperçu exact
        └──────────────► Published
```

- `Status` et `Visibility` sont séparés ;
- `NeedsReview` conserve le jeton pour une republication mais le rend non
  résolvable ;
- une rotation remplace obligatoirement le jeton et change la version publique ;
- une révocation retire immédiatement le jeton du modèle autoritatif ;
- un objet révoqué est terminal ;
- les versions attendues rendent les conflits explicites ;
- les versions 64 bits ne reviennent jamais à zéro en cas de dépassement ;
- toutes les dates techniques sont UTC et chronologiquement validées.

La génération cryptographique et la validation canonique du jeton appartiennent à
`SHARE-03` et à Infrastructure. `SHARE-02` exige seulement une valeur opaque non vide
et ne dérive jamais un jeton d'un identifiant utilisateur ou métier.

## Architecture

```text
AmusementPark.Core.Domain.Sharing
├── SharePublication              cycle de vie et invariants communs
├── ShareContentPolicy            liste blanche immuable par type
├── SharePublicationId            identifiant interne typé
├── enums                         type, état, visibilité, date, champs
└── erreurs métier                codes stables et exceptions dédiées
```

Le Core ne dépend ni de MongoDB, ni de HTTP, ni d'Angular. Les futurs constructeurs
de snapshots resteront spécialisés par contenu afin d'éviter une classe universelle.
Chaque classe, record ou enum ajouté occupe son propre fichier.

## Preuves automatisées

Les 52 tests ciblés couvrent notamment :

- les cinq types de publication et les huit champs de la matrice d'autorisation ;
- toutes les précisions de date acceptées et refusées par type ;
- les valeurs inconnues et versions de policy non prises en charge ;
- l'immuabilité, le tri et la déduplication de la liste blanche ;
- l'absence physique des catégories privées interdites dans le contrat public ;
- la création privée, la publication, la suspension, la republication, la rotation
  et la révocation ;
- les aperçus de source, de policy ou de publication devenus obsolètes ;
- les conflits, répétitions idempotentes, dates UTC et dépassements 64 bits ;
- l'absence de mutation partielle lorsqu'une version ne peut plus avancer.

## Suite

`SHARE-03` ajoutera la persistance MongoDB, la génération CSPRNG d'au moins 256 bits,
l'unicité des jetons et la révocation atomique. Aucun moteur existant de classement
n'est modifié dans cette tranche ; sa migration de remplacement reste dédiée à
`SHARE-04A`, sans adaptateur permanent ni double écriture.
