# Inventaire des protections d'endpoints WebAPI

Snapshot basé sur la livraison `M17-cleanup-centralization-cicd-prod` durcie.

## Règle globale

L'API applique maintenant une politique de sécurité par défaut :

- `DefaultPolicy` : utilisateur authentifié requis.
- `FallbackPolicy` : utilisateur authentifié requis pour tout endpoint qui n'a pas d'attribut explicite.
- Les routes publiques sont donc des exceptions explicites via `[AllowAnonymous]`.
- Les routes d'administration métier sont protégées par `[Authorize(Roles = AuthorizationRoleGroups.Admin)]` et `[RequireActivatedUnblockedUser]`.
- Les routes utilisateurs sensibles restent protégées par les rôles historiques adaptés (`USER`, `MODERATOR`, `ADMIN`) et par `[RequireActivatedUnblockedUser]`.

## Endpoints publics conservés

Ces endpoints restent publics car ils sont nécessaires à la navigation non connectée, au SEO ou aux parcours d'authentification.

| Zone | Méthode / route | Raison |
|---|---|---|
| Auth | `POST /auth/login` | Connexion. |
| Auth | `POST /auth/refresh-token` | Renouvellement par cookie HttpOnly. |
| Auth | `POST /auth/logout` | Nettoyage du refresh cookie, même si le token d'accès est expiré. |
| Auth externe | `POST /auth/external/{provider}` | Login OAuth/OIDC public. |
| Auth externe | `GET /auth/facebook`, `GET /auth/facebook-response` | Callback/provider externe historique. |
| Compte | `POST /users` | Inscription publique. |
| Compte | `POST /users/confirm-email` | Confirmation de compte. |
| Compte | `POST /users/resend-confirmation` | Renvoi de confirmation. |
| Compte | `POST /users/forgot-password` | Demande de reset password. |
| Compte | `POST /users/reset-password` | Reset password via token. |
| Pays | `GET /countries` | Données de référence publiques. |
| Home | `GET /public-stats/home` | Statistiques home. |
| Recherche | `GET /search` | Recherche publique visible. |
| Parcs | `GET /parks`, `/parks/{id}`, `/parks/random-visible`, `/parks/home-featured`, `/parks/map-visible`, `/parks/geo-search`, `/parks/{id}/nearby`, `/parks/{id}/distances` | Parcours public, uniquement données visibles pour les anonymes. |
| Park items | `GET /park-items`, `/park-items/{id}`, `/park-items/park/{parkId}` | Parcours public, uniquement items visibles et rattachés à un parc visible pour les anonymes. |
| Zones | `GET /park-zones/park/{parkId}`, `/park-zones/{id}`, `/park-zones/park/{parkId}/explorer` | Navigation publique dans un parc visible. |
| Images publiques | `GET /images/{imageId}`, `/images/{ownerType}/{ownerId}/{category}`, `/images/{ownerType}/{ownerId}/{category}/current` | Affichage des logos/photos publiques. |
| Référentiels publics | `GET /park-operators`, `/park-operators/{id}`, `/park-founders`, `/park-founders/{id}`, `/attraction-manufacturers`, `/attraction-manufacturers/{id}` | Fiches publiques exploitants/fondateurs/constructeurs et liens depuis les fiches parcs/items. |
| Diagnostic | `GET /health` | Healthcheck CI/CD, Docker, reverse proxy. |

## Endpoints admin protégés

| Zone | Routes protégées | Protection |
|---|---|---|
| Parcs | `POST /parks`, `PUT /parks/{id}`, `PATCH /parks/{id}/visibility`, `PATCH /parks/bulk-administration` | `ADMIN` + utilisateur activé/non bloqué. |
| Park items | `POST /park-items`, `PUT /park-items/{id}`, `DELETE /park-items/{id}`, `PATCH /park-items/bulk-administration` | `ADMIN` + utilisateur activé/non bloqué. |
| Zones | `POST /park-zones`, `PUT /park-zones/{id}`, `DELETE /park-zones/{id}` | `ADMIN` + utilisateur activé/non bloqué. |
| Images admin | `POST /images`, `POST /images/links`, `PUT /images/{imageId}/current`, `DELETE /images/{imageId}`, `GET /images`, `PATCH /images/bulk-metadata`, `GET/POST/PUT /images/tags`, `GET/PUT /images/{imageId}/metadata` | `ADMIN` + utilisateur activé/non bloqué. |
| Exploitants | `POST /park-operators`, `PUT /park-operators/{id}`, `PATCH /park-operators/bulk-review-status` | `ADMIN` + utilisateur activé/non bloqué. |
| Fondateurs | `POST /park-founders`, `PUT /park-founders/{id}` | `ADMIN` + utilisateur activé/non bloqué. |
| Constructeurs | `POST /attraction-manufacturers`, `PUT /attraction-manufacturers/{id}`, `PATCH /attraction-manufacturers/bulk-review-status` | `ADMIN` + utilisateur activé/non bloqué. |
| Data sources | Toutes les routes `admin/data-sources/**` | `ADMIN` + utilisateur activé/non bloqué. |

## Endpoints utilisateur protégés

| Route | Protection | Note |
|---|---|---|
| `GET /users/{id}` | `USER,MODERATOR,ADMIN` + utilisateur activé/non bloqué + contrôle propriétaire dans l'action | Un utilisateur ne peut consulter que son propre profil, sauf admin/modérateur. |
| `PUT /users/{id}` | `USER,MODERATOR,ADMIN` + utilisateur activé/non bloqué + contrôle propriétaire dans l'action | Un utilisateur ne peut modifier que son propre profil, sauf admin/modérateur. |
| `POST /users/change-password` | `USER,MODERATOR,ADMIN` + utilisateur activé/non bloqué + contrôle propriétaire dans l'action | Un utilisateur ne peut changer que son propre mot de passe, sauf admin/modérateur. |
| `GET /users`, `GET /users/by-email`, `POST /users/lock`, `POST /users/unlock` | `MODERATOR,ADMIN` + utilisateur activé/non bloqué | Administration utilisateurs. |
| `POST /users/roles/assign/{userId}`, `DELETE /users/roles/remove/{userId}` | `ADMIN` + utilisateur activé/non bloqué | Gestion des rôles réservée admin. |

## Durcissements fonctionnels ajoutés

- `GET /parks/{id}` utilise maintenant `IncludeHidden = true` uniquement pour un admin authentifié ; un visiteur ne voit plus un parc non visible en connaissant son identifiant.
- Les listes publiques de parcs ignorent les filtres admin sensibles (`adminReviewStatus`, `isVisible`) hors admin et forcent la visibilité publique.
- `GET /park-items/{id}` utilise maintenant `IncludeHidden = true` uniquement pour un admin authentifié.
- Un park item public est refusé si son parc parent n'est pas visible.
- Les listes publiques de park items ignorent les filtres admin sensibles hors admin et forcent la visibilité publique.
- Les endpoints de zones/explorer vérifient désormais que le parc parent est visible pour les visiteurs anonymes.
- Les endpoints images de gestion (`GET /images`, tags, metadata, upload, link, current, delete, bulk metadata) ne sont plus publics.
