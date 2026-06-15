# SSR HEAD et payload TransferState - 2026-06-15

## Objectif

Le GET des pages publiques de parc sert déjà un HTML SSR indexable. Cette passe aligne les requêtes `HEAD` sur le même chemin SSR et réduit le payload transféré à l'hydratation.

## Changements

- Les requêtes `HEAD` des pages publiques cacheables sont maintenant éligibles au même rendu SSR/cache que les `GET`. Un `curl -I` sur une page parc ne doit donc plus annoncer un fallback CSR quand le `GET` sert une page SSR.
- La navigation publique de parc n'appelle plus `/parks/{id}` pour construire le libellé du parc. Elle réutilise le contrat optimisé `/parks/{id}/detail-summary`, ce qui retire le doublon visible dans `ng-state` entre le détail complet et le résumé.
- Le workflow de release accepte un rerun si le tag de version existe déjà sur le commit courant.
- Le déploiement injecte `SSR_CACHE_INVALIDATION_TOKEN` depuis les GitHub Actions secrets.

## Configuration GitHub Actions

`SSR_CACHE_INVALIDATION_TOKEN` doit être configuré comme secret GitHub Actions, pas seulement comme variable. Le workflow accepte les noms suivants :

```text
SSR_CACHE_INVALIDATION_TOKEN
PROD_SSR_CACHE_INVALIDATION_TOKEN
```

Le nom recommandé est `SSR_CACHE_INVALIDATION_TOKEN`.
