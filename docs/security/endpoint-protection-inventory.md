# Inventaire M18.1 des protections d'endpoints WebAPI

Snapshot basé sur `AmusementParkProject.zip` fourni avant l'implémentation de M18.2.

## Conclusion M18.1

L'étape M18.1 ne nécessite pas de correction de code applicatif sur ce snapshot :

- la sécurité par défaut est bien fermée côté API ;
- les endpoints publics sont des exceptions explicites ;
- aucune route de mutation métier publique non justifiée n'a été identifiée ;
- les mutations publiques restantes correspondent aux parcours nécessaires d'authentification, inscription, confirmation email et reset mot de passe.

Le passage à M18.2 est donc pertinent dans ce même livrable : verrouillage de `AllowedHosts` pour éviter le wildcard en production.

## Règle globale constatée

L'API applique une politique sécurisée par défaut dans `AuthenticationServiceCollectionExtensions` :

- `DefaultPolicy` : utilisateur authentifié requis ;
- `FallbackPolicy` : utilisateur authentifié requis pour tout endpoint sans attribut explicite ;
- les routes publiques utilisent `[AllowAnonymous]` ;
- les contrôleurs d'administration métier sont protégés par `[Authorize(Roles = AuthorizationRoleGroups.Admin)]` et `[RequireActivatedUnblockedUser]` ;
- les routes utilisateurs sensibles utilisent les groupes de rôles adaptés et `[RequireActivatedUnblockedUser]`.

## Endpoints publics assumés

Ces endpoints restent publics car ils sont nécessaires à la navigation non connectée, au SEO, à l'affichage des médias publics ou aux parcours d'authentification.

| Zone | Méthode / route | Justification |
|---|---|---|
| Auth | `POST /auth/login` | Connexion publique. |
| Auth | `POST /auth/refresh-token` | Renouvellement par cookie HttpOnly ; nécessaire même sans access token valide. |
| Auth | `POST /auth/logout` | Nettoyage du cookie de refresh même si l'access token est expiré. |
| Auth externe | `POST /auth/external/{provider}` | Connexion OAuth/OIDC publique. |
| Auth externe | `GET /auth/facebook`, `GET /auth/facebook-response` | Provider/callback externe historique. |
| Compte | `POST /users` | Inscription publique. |
| Compte | `POST /users/confirm-email` | Confirmation de compte par token. |
| Compte | `POST /users/resend-confirmation` | Renvoi de confirmation email. |
| Compte | `POST /users/forgot-password` | Demande de reset mot de passe. |
| Compte | `POST /users/reset-password` | Reset mot de passe via token. |
| Pays | `GET /countries` | Donnée de référence publique. |
| Home | `GET /public-stats/home` | Statistiques publiques de la home. |
| Recherche | `GET /search` | Recherche publique sur contenu indexé/visible. |
| Parcs | `GET /parks` | Liste publique ; les filtres admin sensibles sont ignorés hors admin. |
| Parcs | `GET /parks/{id}` | Détail public ; un visiteur ne voit pas un parc non visible. |
| Parcs | `GET /parks/random-visible` | Suggestions publiques visibles. |
| Parcs | `GET /parks/home-featured` | Sélection home publique visible. |
| Parcs | `GET /parks/map-visible` | Points cartographiques publics visibles. |
| Parcs | `GET /parks/geo-search` | Recherche géographique publique. |
| Parcs | `GET /parks/{id}/nearby` | Parcs proches publics ; respecte la visibilité. |
| Parcs | `GET /parks/{id}/distances` | Distances publiques ; respecte la visibilité. |
| Park items | `GET /park-items` | Liste publique ; forcée sur les items visibles hors admin. |
| Park items | `GET /park-items/{id}` | Détail public ; item non visible refusé hors admin. |
| Park items | `GET /park-items/park/{parkId}` | Items publics d'un parc visible. |
| Zones | `GET /park-zones/park/{parkId}` | Navigation publique d'un parc visible. |
| Zones | `GET /park-zones/{id}` | Détail de zone ; parc parent visible requis hors admin. |
| Zones | `GET /park-zones/park/{parkId}/explorer` | Exploration publique d'un parc visible. |
| Images publiques | `GET /images/{imageId}` | Affichage binaire d'image publique. |
| Images publiques | `GET /images/{ownerType}/{ownerId}/{category}` | Galerie publique d'un propriétaire. |
| Images publiques | `GET /images/{ownerType}/{ownerId}/{category}/current` | Logo/photo courante publique. |
| Exploitants | `GET /park-operators`, `GET /park-operators/{id}` | Référentiel public et liens depuis fiches parcs. |
| Fondateurs | `GET /park-founders`, `GET /park-founders/{id}` | Référentiel public et liens depuis fiches parcs. |
| Constructeurs | `GET /attraction-manufacturers`, `GET /attraction-manufacturers/{id}` | Référentiel public et liens depuis attractions. |
| Diagnostic | `GET /health` | Healthcheck Docker, CI/CD et reverse proxy. |

## Endpoints admin protégés

| Zone | Routes protégées | Protection |
|---|---|---|
| Parcs | `POST /parks`, `PUT /parks/{id}`, `PATCH /parks/{id}/visibility`, `PATCH /parks/bulk-administration` | `ADMIN` + utilisateur activé/non bloqué. |
| Park items | `POST /park-items`, `PUT /park-items/{id}`, `DELETE /park-items/{id}`, `PATCH /park-items/bulk-administration` | `ADMIN` + utilisateur activé/non bloqué. |
| Zones | `POST /park-zones`, `PUT /park-zones/{id}`, `DELETE /park-zones/{id}` | `ADMIN` + utilisateur activé/non bloqué. |
| Images admin | `POST /images`, `POST /images/links`, `PUT /images/{imageId}/current`, `DELETE /images/{imageId}`, `GET /images`, `PATCH /images/bulk-metadata`, `GET /images/tags`, `POST /images/tags`, `PUT /images/tags/{id}`, `GET /images/{imageId}/metadata`, `PUT /images/{imageId}/metadata` | `ADMIN` + utilisateur activé/non bloqué. |
| Exploitants | `POST /park-operators`, `PUT /park-operators/{id}`, `PATCH /park-operators/bulk-review-status` | `ADMIN` + utilisateur activé/non bloqué. |
| Fondateurs | `POST /park-founders`, `PUT /park-founders/{id}` | `ADMIN` + utilisateur activé/non bloqué. |
| Constructeurs | `POST /attraction-manufacturers`, `PUT /attraction-manufacturers/{id}`, `PATCH /attraction-manufacturers/bulk-review-status` | `ADMIN` + utilisateur activé/non bloqué. |
| Data sources | `GET /admin/data-sources`, `GET /admin/data-sources/{sourceKey}/status`, `GET /admin/data-sources/{sourceKey}/settings`, `PUT /admin/data-sources/{sourceKey}/settings`, `GET /admin/data-sources/{sourceKey}/sessions/latest`, `GET /admin/data-sources/{sourceKey}/sessions/{sessionId}`, `GET /admin/data-sources/{sourceKey}/comparison-results`, `POST /admin/data-sources/{sourceKey}/import`, `POST /admin/data-sources/{sourceKey}/apply` | `ADMIN` + utilisateur activé/non bloqué. |

## Endpoints utilisateur protégés

| Route | Protection | Contrôle métier |
|---|---|---|
| `GET /users/{id}` | `USER,MODERATOR,ADMIN` + utilisateur activé/non bloqué | Un utilisateur ne peut consulter que son propre profil, sauf admin/modérateur. |
| `PUT /users/{id}` | `USER,MODERATOR,ADMIN` + utilisateur activé/non bloqué | Un utilisateur ne peut modifier que son propre profil, sauf admin/modérateur. |
| `POST /users/change-password` | `USER,MODERATOR,ADMIN` + utilisateur activé/non bloqué | Un utilisateur ne peut changer que son propre mot de passe, sauf admin/modérateur. |
| `GET /users` | `MODERATOR,ADMIN` + utilisateur activé/non bloqué | Administration utilisateurs. |
| `GET /users/by-email` | `MODERATOR,ADMIN` + utilisateur activé/non bloqué | Recherche utilisateur réservée modération/admin. |
| `POST /users/lock` | `MODERATOR,ADMIN` + utilisateur activé/non bloqué | Verrouillage utilisateur réservé modération/admin. |
| `POST /users/unlock` | `MODERATOR,ADMIN` + utilisateur activé/non bloqué | Déverrouillage utilisateur réservé modération/admin. |
| `POST /users/roles/assign/{userId}` | `ADMIN` + utilisateur activé/non bloqué | Gestion des rôles réservée admin. |
| `DELETE /users/roles/remove/{userId}` | `ADMIN` + utilisateur activé/non bloqué | Gestion des rôles réservée admin. |

## Points sensibles vérifiés

- `GET /parks/{id}` passe `IncludeHidden = true` uniquement pour un admin authentifié.
- `GET /parks` force `isVisible = true` hors admin et ignore les filtres admin sensibles (`adminReviewStatus`, `isVisible`).
- `GET /park-items/{id}` passe `IncludeHidden = true` uniquement pour un admin authentifié.
- `GET /park-items` force `isVisible = true` hors admin et ignore `adminReviewStatus` hors admin.
- Les zones publiques vérifient la visibilité du parc parent hors admin.
- Les endpoints images de gestion ne sont pas anonymes ; seules les lectures publiques d'images restent ouvertes.
- Les endpoints `admin/data-sources/**` restent entièrement sous rôle `ADMIN`.

## Exceptions publiques de mutation acceptées

Les mutations publiques suivantes sont justifiées et ne sont pas des mutations métier d'administration :

- authentification : login, refresh-token, logout, external login ;
- cycle de vie compte : inscription, confirmation, renvoi de confirmation ;
- reset mot de passe : forgot-password, reset-password.

Ces endpoints devront être renforcés ensuite par M18.6 avec un rate limiting ciblé auth, car le rate limit global ne suffit pas pour limiter proprement brute force et spam de reset.

## Validation M18.1

- Document d'inventaire mis à jour : oui.
- Route mutation métier publique non justifiée : aucune détectée.
- Passage à M18.2 dans ce livrable : oui.
