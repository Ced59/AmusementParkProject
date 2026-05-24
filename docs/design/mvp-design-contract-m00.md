# M00 — Contrat de fidélité design MVP

La maquette `amusementpark-mockup-quasi-final-routed.html` reste la source visuelle de référence de la refonte MVP.
Cette phase fige le contrat avant l'intégration page par page afin d'éviter une copie locale de CSS dans chaque feature.

## Décisions gelées

- Le mode sombre est l'identité principale du site.
- Les couleurs structurantes sont centralisées dans `src/styles/_design-tokens.scss` : orange, lime, sky, rose, gold et purple.
- Les thèmes dark/light sont séparés dans `src/styles/_theme-dark.scss` et `src/styles/_theme-light.scss`.
- Les anciens alias CSS `--app-*` et `--ap-*` restent maintenus pendant la migration pour éviter une rupture brutale des composants existants.
- Les nouveaux styles réutilisables sont découpés par responsabilité : navigation, boutons, chips, surfaces, cards, forms, maps, states et admin skin.
- Les futures phases devront brancher réellement `src/app/ui/layouts` au routing, mais cette première phase ne modifie pas encore les routes.

## Garde-fous pour les phases suivantes

- Aucune couleur brute issue de la maquette ne doit être ajoutée dans un SCSS de composant.
- Les composants de page ne doivent conserver que du placement spécifique.
- Les nouveaux composants UI ne doivent dépendre d'aucun service métier.
- Les pages hors maquette devront consommer le même socle : About, Auth/Profile et Admin.
- La carte et le futur panneau de distance doivent s'appuyer sur les classes `map-shell-card`, `app-map-shell`, `distance-panel` ou leurs futurs composants Angular dédiés.

## Validation M00/M01

- Le socle SCSS de la maquette est disponible.
- Le thème existant continue à fonctionner via `light-mode` / `dark-mode`.
- Les classes publiques déjà présentes bénéficient des nouveaux tokens sans refonte fonctionnelle.
- Aucun fichier fonctionnel Angular n'a été déplacé dans cette phase.
