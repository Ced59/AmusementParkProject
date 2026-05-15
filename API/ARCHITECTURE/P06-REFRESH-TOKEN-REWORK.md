# P06 — Refresh token back réellement traçable + usage front cohérent

## Objectif

Remplacer l'ancien faux refresh token (JWT recyclé) par un refresh token opaque, hashé, persistant, rotatif et révocable,
sans anticiper la bascule HttpOnly prévue en P07.

## Décisions

- Le refresh token n'est plus un JWT.
- Seul le hash du refresh token est persisté en base.
- Le refresh est **rotatif** : chaque appel valide révoque l'ancien token et émet un nouveau couple access/refresh.
- Le front reste en stockage navigateur pour cette phase, mais utilise désormais réellement le refresh endpoint.

## Impacts principaux

### Back

- ajout d'un store Mongo dédié `refreshTokens`
- index unique sur `tokenHash`
- rotation avec traçabilité (`lastUsedAtUtc`, `revokedAtUtc`, `replacedByTokenHash`, `revocationReason`)
- réponse d'auth enrichie avec `refreshToken` et `refreshTokenExpiresAtUtc`
- endpoint `POST /Auth/refresh-token` renvoie maintenant aussi `accessToken`

### Front

- stockage séparé `auth_token` / `refresh_token`
- conservation d'une date d'expiration du refresh côté client
- interceptor capable de rafraîchir le bearer expiré avant de rejouer la requête
- pas de cookie HttpOnly dans cette phase

## Critères de fin

- un refresh token invalide, expiré ou déjà roté ne permet plus d'obtenir une nouvelle session
- la rotation produit un nouveau refresh token à chaque refresh réussi
- le front ne traite plus le refresh token comme une simple curiosité inutilisée
- la phase reste compatible avec une migration ultérieure vers cookie HttpOnly
