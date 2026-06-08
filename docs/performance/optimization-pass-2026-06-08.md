# Passe d'optimisation performance — 2026-06-08

Objectif : réduire drastiquement la charge CPU, mémoire et bande passante sans changer les fonctionnalités visibles.

## Principes appliqués

- Les routes publiques conservent leurs contrats et leurs URLs.
- Les sitemaps conservent leur structure actuelle.
- La génération sitemap publique à la demande est désactivée : le snapshot est généré depuis l'administration puis servi rapidement.
- Les caches HTTP/API ne sont appliqués qu'aux requêtes anonymes sûres.
- Les endpoints admin et les mutations ne sont pas mis en cache.

## Optimisations principales

- Ajout du middleware de compression des réponses API.
- Ajout de policies `OutputCache` ciblées pour les documents SEO, données publiques courtes, données publiques moyennes et référentiels publics.
- Cache mémoire des snapshots sitemap et des réglages sitemap.
- Éviction du cache SEO quand l'administration modifie les réglages sitemap ou relance une génération.
- Cache mémoire repository pour les images par propriétaire, image courante, image par identifiant et tags d'images.
- Cache navigateur long sur les binaires image, avec variation par `Accept`.
- Micro-cache SSR court côté serveur Node pour les pages publiques anonymes, désactivable via `SSR_PAGE_CACHE_SECONDS=0`.

## Variables utiles

- `SSR_PAGE_CACHE_SECONDS` : durée du micro-cache SSR public en secondes. Défaut : `30`.
- `SSR_PAGE_CACHE_MAX_ENTRIES` : nombre maximal d'entrées HTML en mémoire côté SSR. Défaut : `250`.

## Attention opérationnelle

Après déploiement, génère une première fois le sitemap depuis le panel admin. Tant qu'aucun snapshot n'existe, `/sitemap.xml` renvoie une erreur 404 au lieu de lancer une génération coûteuse côté requête publique.
