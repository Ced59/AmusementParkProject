# P02 — Fondation de l'architecture front

## Objet

Rendre visible dans le projet la structure cible de la future clean architecture front,
sans déplacer massivement le code historique et sans provoquer de régression fonctionnelle.

## Livrables de cette phase

- création des dossiers cibles `core`, `shared`, `features`, `data-access`, `ui` ;
- documentation centralisée des règles d'import ;
- formalisation des conventions `page` / `shell` / `présentation` ;
- formalisation des conventions `API models` / `UI models` / `mappers` ;
- ajout d'alias TypeScript pour préparer les futures extractions.

## Référence de mise en oeuvre

Le document de référence détaillé est volontairement porté côté front :

- `FRONT/AmusementPark/src/app/core/architecture/P02-FRONT-TARGET-ARCHITECTURE.md`

## Décision

À partir de cette phase, la structure cible devient la référence officielle pour tous les futurs refactors front.
Les dossiers historiques restent tolérés uniquement pour préserver l'iso-fonctionnel jusqu'aux phases d'extraction métier.
