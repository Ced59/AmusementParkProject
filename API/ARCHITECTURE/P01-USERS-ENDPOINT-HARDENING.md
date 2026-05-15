# P01 — Durcissement back des endpoints Users

## Objectif

Réduire l'exposition inutile des endpoints `Users` sans casser les parcours actuels du front.

## Décisions appliquées

- les endpoints de lecture utilisateurs ne sont plus publics par défaut ;
- `GET /Users` est désormais réservé aux rôles `MODERATOR` et `ADMIN` ;
- `GET /Users/by-email` est désormais réservé aux rôles `MODERATOR` et `ADMIN` ;
- `GET /Users/{id}` reste disponible pour l'utilisateur lui-même, ainsi que pour `MODERATOR` et `ADMIN` ;
- `PUT /Users/{id}` conserve sa règle de mise à jour par l'utilisateur lui-même ou par un profil privilégié, avec retour `403` en cas d'accès interdit ;
- les endpoints sensibles déjà protégés (`assign/remove role`, `lock`, `unlock`, `change-password`) conservent un durcissement explicite ;
- le DTO de listing utilisateur ne renvoie plus `LastLogin` ni `LastActivity`, jugés non nécessaires pour le front actuel.

## Contraintes respectées

- pas de changement coordonné front/back imposant une refonte front ;
- navigation profil utilisateur existante conservée ;
- navigation admin utilisateurs conservée ;
- inscription, confirmation d'email, oubli de mot de passe et réinitialisation restent accessibles anonymement.

## Vérifications attendues

- un utilisateur authentifié peut toujours charger et modifier son propre profil ;
- un administrateur peut toujours lister les utilisateurs et consulter leur fiche ;
- un appel anonyme vers `GET /Users`, `GET /Users/{id}` ou `GET /Users/by-email` est désormais refusé ;
- le build back reste valide.
