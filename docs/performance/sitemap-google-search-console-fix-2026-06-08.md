# Fix Google Search Console sitemap

## Date

2026-06-08

## Objectif

Durcir le chemin public des documents SEO servis par le SSR Angular afin que Google Search Console voie une reponse stable, meme lorsque le cache front est froid.

## Correction

Dans `FRONT/AmusementPark/server.ts` :

- `fetchSeoDocumentFromApi()` utilise desormais `buildSeoDocumentFetchHeaders()`.
- Le fetch interne SSR vers API ne transmet plus `Cookie`, `Authorization`, ni le `User-Agent` du visiteur.
- `buildCachedSeoDocumentHeaders()` ne propage plus `cache-control` ni `vary` depuis l API.
- Le SSR impose un `Cache-Control` public propre pour `/sitemap.xml`, `/sitemaps/*` et `/robots.txt`.

## Fonctionnel

La structure des sitemaps ne change pas. La generation reste pilotee depuis l administration et la consultation publique continue de servir le dernier snapshot disponible.
