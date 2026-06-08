# Correctif public sitemap / robots — 2026-06-08

## Problème constaté

Les requêtes publiques `GET /sitemap.xml` retournaient bien le XML, mais les requêtes `HEAD` sur `sitemap.xml` et `robots.txt` retournaient `401 Unauthorized`.

Google Search Console peut effectuer des vérifications avec `HEAD` ou considérer ce signal comme une ressource non récupérable. Le sitemap doit donc être public pour `GET` **et** `HEAD`.

## Correctif

- Ajout de routes `HEAD` explicites dans `SeoController` pour :
  - `/robots.txt`
  - `/sitemap.xml`
  - `/sitemaps/{sectionFileName}`
  - `/{key}.txt`
- Passage des routes SEO API en templates absolus (`/sitemap.xml`, `/robots.txt`, etc.).
- Ajout de routes `HEAD` explicites côté serveur SSR Express pour proxyfier proprement les fichiers SEO vers l’API.

## Comportement attendu

Après déploiement :

```bash
curl -I https://amusement-parks.fun/sitemap.xml
curl -I https://amusement-parks.fun/robots.txt
```

doivent répondre `200`, sans `WWW-Authenticate: Bearer`.

## Rappel sur le fonctionnement sitemap

Le sitemap public n’est plus régénéré à chaque requête publique. La génération se fait via l’administration, puis le dernier snapshot est servi aux crawlers. Le snapshot est relu depuis le stockage applicatif et amorti par les caches, sans relancer la génération complète.
