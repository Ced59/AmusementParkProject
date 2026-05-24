# UI Cards

Composants visuels partagés issus de la phase M05, consolidés en M17.

- Pas d'appel HTTP.
- Pas de facade ni service métier.
- Les composants consomment uniquement des view models, des liens déjà préparés ou des valeurs de présentation.
- Le style est centralisé dans `src/styles/_cards.scss`.
- Les anciens wrappers `components/public/*` ne doivent plus être réintroduits : les pages publiques consomment directement `@ui/cards`.
