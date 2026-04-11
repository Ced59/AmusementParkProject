# P05 — Extraction des services API de domaine

## Objectif

Supprimer le `ApiService` monolithique et brancher directement les écrans vers des services HTTP spécialisés par domaine.

## Résultat attendu

- plus de façade de compatibilité ;
- plus de point d'entrée HTTP unique pour tout le front ;
- services dédiés dans `data-access` :
  - `auth`
  - `users`
  - `parks`
  - `park-items`
  - `manufacturers`
  - `images`
  - `countries`
  - `search`
  - `admin/data-sources`
- composants reconnectés directement à leurs dépendances métier ;
- normalisation transverse conservée via helpers partagés `data-access/shared`.

## Garde-fous

- rester iso-fonctionnel ;
- ne pas changer les contrats HTTP ;
- ne pas déplacer de logique métier UI dans les composants ;
- centraliser uniquement les helpers de mapping / unwrap utiles à plusieurs domaines.
