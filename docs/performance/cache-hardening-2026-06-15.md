# Durcissement de la cohérence des caches - 2026-06-15

## Objectif

Cette passe ferme les derniers cas où une écriture admin réussie pouvait laisser survivre une version publique périmée dans une autre couche de cache.

## Changements

- Les mutations admin des sitemaps utilisent maintenant l'invalidation publique centralisée `PublicCacheScope.Seo`. Elles purgent donc l'OutputCache API et le cache SSR mémoire/disque.
- Le token `SSR_CACHE_INVALIDATION_TOKEN` est obligatoire dans le compose de production pour éviter une invalidation SSR silencieusement désactivée.
- Les réponses HTML SSR et les documents SEO servis par le front SSR n'autorisent plus de cache navigateur stale par défaut. Le cache serveur interne reste actif et protège le CPU.
- L'éviction OutputCache et l'invalidation SSR sont best-effort : une panne de cache est loggée mais ne fait pas échouer l'écriture métier.
- Le preview `admin/park-graph-upserts/preview` n'invalide plus les caches publics, seul `apply` le fait.
- Les référentiels publics qui peuvent être embarqués dans des DTO de parc ou d'attraction invalident `ReferenceData` et `Data`.

## Variables

```env
SSR_CACHE_INVALIDATION_TOKEN=CHANGE_ME_GENERATE_WITH_OPENSSL_RAND_HEX_32
SSR_PAGE_CACHE_BROWSER_CACHE_CONTROL=no-store, max-age=0
SSR_SEO_DOCUMENT_BROWSER_CACHE_CONTROL=no-cache, max-age=0, must-revalidate
SSR_INTERNAL_BASE_URL=http://front:4000
SSR_SEO_DOCUMENT_CACHE_SECONDS=0
```

`SSR_PAGE_CACHE_SECONDS`, `SSR_DISK_PAGE_CACHE_ENABLED` et `SSR_SEO_DOCUMENT_CACHE_SECONDS` continuent de piloter les caches internes serveur. Les nouveaux `Cache-Control` ne désactivent pas ces caches internes : ils empêchent seulement le navigateur ou un intermédiaire respectueux des headers de stocker ou resservir un HTML périmé, tout en gardant les documents SEO en revalidation stricte.
