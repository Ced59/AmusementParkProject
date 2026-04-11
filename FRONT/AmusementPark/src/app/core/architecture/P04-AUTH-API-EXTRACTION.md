# P04 — Refactor du socle HTTP front — extraction Auth API

## Objectif

Isoler le domaine Auth du `ApiService` historique pour préparer proprement :

- la bascule future vers un modèle `HttpOnly` ;
- l'extraction progressive des autres services HTTP de domaine en P05 ;
- l'encapsulation des guards et interceptors autour d'un domaine auth plus localisé.

## Décisions prises en P04

- création de `data-access/auth/auth-api.service.ts` comme point d'entrée HTTP officiel du domaine auth ;
- regroupement dans ce service des appels `login`, `refresh-token`, `register`, `confirm-email`,
  `resend-confirmation`, `forgot-password`, `reset-password`, `external-login` et `current user` ;
- déplacement de l'implémentation des guards et de l'interceptor auth dans `core/guards` et
  `core/http/interceptors` ;
- maintien temporaire d'une compatibilité via les anciens points d'entrée (`ApiService`, `guards/`,
  `interceptors/`) pour éviter une rupture fonctionnelle.

## Limites assumées de P04

- aucun changement de transport de session n'est introduit ici ;
- le `logout` reste local côté client car le back n'expose pas encore d'endpoint dédié ;
- `ApiService` conserve provisoirement des méthodes auth qui délèguent au nouveau service,
  afin de permettre une migration progressive et iso-fonctionnelle.

## Effet attendu

À partir de P04, le domaine auth front n'est plus obligé de passer par le `god service` historique.
P05 pourra ensuite extraire les autres domaines HTTP de manière similaire.
