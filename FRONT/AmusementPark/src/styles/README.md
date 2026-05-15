# Styles MVP

Ce dossier porte le socle visuel centralisé de la refonte design MVP.

## Ordre de responsabilité

1. `_design-tokens.scss` : tokens canoniques de la maquette.
2. `_tokens.scss` : alias historiques `--app-*` / `--ap-*` conservés pour l'iso-fonctionnalité pendant migration.
3. `_theme-dark.scss` / `_theme-light.scss` : mapping thème sombre/clair.
4. `_base.scss` / `_layout.scss` / `_typography.scss` : socle transversal.
5. `_navigation.scss`, `_buttons.scss`, `_chips.scss`, `_surfaces.scss`, `_cards.scss`, `_forms.scss`, `_maps.scss`, `_states.scss` : primitives design réutilisables.
6. `_admin-skin.scss` : déclinaison admin dense, sans polluer le public.
7. `_primeng-overrides.scss` : adaptations PrimeNG uniquement.

## Règle de migration

Les futures phases ne doivent pas recopier la maquette dans les SCSS de pages.
Une page consomme les tokens/primitives, puis ne garde localement que le placement strictement spécifique.
