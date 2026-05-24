# ui

Couche visuelle générique du front.

Cette zone doit contenir progressivement les composants Angular réutilisables issus du design system MVP : layouts, primitives, cards, forms, maps et shells de composition.

## Règle

Un composant sous `ui` ne dépend pas d'un service métier, d'une facade de feature ou d'un DTO API.
Il reçoit des données déjà préparées par une page, une feature ou un mapper.
