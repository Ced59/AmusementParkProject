# P02 — Fondation de l'architecture front

## Objectif

Installer la structure cible de la future clean architecture front sans déplacer massivement la logique
existante ni casser le comportement courant de l'application.

P02 ne refond pas encore les features. Il pose les conventions et les zones d'atterrissage des prochaines
phases de migration.

## Périmètre de P02

- création de l'arborescence cible `core`, `shared`, `features`, `data-access`, `ui` ;
- documentation des dépendances autorisées entre couches ;
- formalisation des conventions `page` / `shell` / `présentation` ;
- formalisation des conventions `API models` / `UI models` / `mappers` ;
- conservation temporaire des dossiers historiques (`components`, `services`, `models`, `api`, etc.)
  tant que les phases P04 à P14 n'ont pas progressivement déplacé la logique.

## Structure cible

```text
src/app/
  core/
  shared/
  features/
  data-access/
  ui/
```

### `core`

Contient les briques globales de l'application, sans dépendance vers une feature métier :

- configuration applicative ;
- guards et politiques transverses ;
- interceptors HTTP ;
- bootstrap et initialisation ;
- services transverses strictement globaux.

### `shared`

Contient les éléments réutilisables entre plusieurs domaines :

- composants purement partagés ;
- utilitaires ;
- helpers d'affichage ;
- modèles transverses réutilisables ;
- pipes ou petites briques communes.

### `features`

Contient les domaines fonctionnels et leurs écrans. Une feature regroupe à terme :

- pages ;
- shells ;
- composants de présentation propres au domaine ;
- façade de feature ;
- modèles UI propres à la feature ;
- mappers propres à la feature.

### `data-access`

Contient l'accès HTTP structuré par domaine :

- services API de domaine ;
- modèles API ;
- requêtes paramétrées ;
- adaptateurs d'accès à des endpoints.

Aucune logique de page ou d'orchestration d'écran ne doit vivre ici.

### `ui`

Contient les briques d'interface génériques et orientées présentation transverse :

- layouts ;
- shells génériques d'application ;
- primitives UI réutilisables à forte valeur de composition.

## Règles d'import autorisées

| Depuis | Peut importer | Ne doit pas importer |
|---|---|---|
| `core` | `core`, `shared`, `ui` | `features`, `data-access` métier d'une feature |
| `shared` | `shared`, `ui` | `features`, `data-access`, `core` métier trop spécifique |
| `ui` | `shared`, `ui` | `features`, `data-access`, `core` métier |
| `data-access` | `data-access`, `shared` | `features`, `ui`, `components` historiques |
| `features` | `features` (même domaine), `shared`, `ui`, `data-access`, `core` | autres features de façon opportuniste |

## Conventions de séparation

### Page

Une `page` représente un écran routé. Elle :

- récupère le contexte de route ;
- délègue au shell ou à la façade ;
- reste minimale ;
- ne porte pas la totalité de la logique métier ni tout le mapping HTTP.

### Shell

Un `shell` orchestre l'écran :

- assemble les composants de présentation ;
- pilote les appels de façade ;
- gère les états d'écran ;
- reste centré sur l'orchestration et non sur le détail visuel fin.

### Composant de présentation

Un composant de présentation :

- reçoit des `@Input` ou des signaux ;
- émet des actions ;
- ne parle pas directement à l'API ;
- ne porte pas de logique de navigation ou de persistance transverse.

## Conventions modèles et mapping

### API models

Les modèles API décrivent le contrat HTTP tel qu'il est reçu ou envoyé.
Ils vivent à terme dans `data-access/<domaine>/models/api`.

### UI models

Les modèles UI décrivent la forme utile à l'écran.
Ils vivent à terme dans `features/<domaine>/models/ui`.

### Mappers

Les mappers traduisent :

- API -> UI ;
- UI -> payload d'écriture ;
- contrats techniques -> objets orientés écran.

Les pages ne doivent pas contenir un mapping volumineux en ligne.

## Coexistence avec l'existant

Pendant P02, le code historique reste en place pour préserver l'iso-fonctionnel.
Les dossiers actuels restent donc utilisables pour la maintenance minimale :

- `components/`
- `services/`
- `models/`
- `api/`
- `guards/`
- `interceptors/`

À partir de P04, toute nouvelle extraction devra viser la structure cible.

## Décision de migration

À compter de P02 :

- aucune nouvelle grosse feature ne doit être posée directement dans les zones historiques si une place claire existe
  déjà dans la structure cible ;
- les futures phases doivent déplacer la logique domaine par domaine, sans big bang ;
- la structure cible devient la référence officielle pour la suite de la roadmap.
