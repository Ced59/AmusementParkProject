# M19 — SEO minimal indexable

## Objectif

M19 installe la base SEO minimale avant MVP publique : routes inventoriées, langue HTML dynamique, metas centralisées, canonical, noindex, robots, sitemap seed, page 404 et hreflang minimal.

## Implémentation front

### Services SEO centraux

- `core/seo/seo.service.ts`
- `core/seo/canonical-url.service.ts`
- `core/seo/hreflang.service.ts`
- `core/seo/seo-text.utils.ts`
- `core/seo/models/seo-route-data.model.ts`

Les composants publics ne manipulent pas directement `Title`, `Meta` ou les balises `<link>`. Ils passent par `SeoService`.

### Langue HTML

`TranslationService` met à jour `<html lang="...">` en navigateur et en SSR via `DOCUMENT`. Le `lang` n'est donc plus seulement un attribut statique de `index.html`.

### Pages couvertes

- Home : meta statique localisée.
- Liste parcs : meta statique localisée.
- Détail parc : title/description issus du parc chargé.
- Détail item : title/description issus de l'élément chargé.
- Admin/auth/account : `noindex,nofollow` via route defaults.
- 404 : vraie page publique `noindex,follow`.


### Politique HTTPS des URLs SEO

En production, tous les signaux SEO absolus doivent sortir en HTTPS :

- `<link rel="canonical">` ;
- `<link rel="alternate" hreflang="...">` ;
- `og:url` ;
- URLs `<loc>` du sitemap ;
- directive `Sitemap:` dans `robots.txt` ;
- liens d'emails générés depuis `Authentication:Local:FrontendBaseUrl`.

Le front s'appuie sur `environment.baseUrl`. En build production, si cette valeur est accidentellement configurée en `http://`, `CanonicalUrlService` force l'origine en `https://` et refuse de produire une origine `localhost`.

L'API s'appuie sur `Seo:PublicBaseUrl`. Hors environnement `Development`, `SeoSettings` refuse désormais une URL non HTTPS, une URL localhost ou une URL qui n'est pas une origin racine.

Le `http://localhost:4200` reste autorisé uniquement en développement local.

### Hreflang minimal

Les alternates sont générés pour les langues réellement déclarées et servies par l'application : `en`, `fr`, `es`, `de`, `it`, `pl`, `nl`, `pt`, plus `x-default` vers `en`.

Aucune langue non servie n'est déclarée.

## Implémentation API / reverse proxy

### robots.txt

L'API expose :

```http
GET /robots.txt
```

Le front Nginx proxifie la racine publique :

```http
GET /robots.txt -> API /robots.txt
```

Le fichier référence le sitemap et exclut `/api/`, admin et les parcours compte/auth.

### sitemap.xml seed

L'API expose :

```http
GET /sitemap.xml
```

Le front Nginx proxifie :

```http
GET /sitemap.xml -> API /sitemap.xml
```

Le sitemap seed contient :

- pages statiques publiques par langue ;
- parcs visibles ;
- pages items des parcs visibles ;
- items visibles rattachés à un parc visible ;
- références publiques de base : exploitants, fondateurs, constructeurs.

Ce seed reste volontairement simple. Pour éviter de transformer M19 en sitemap très gros volume, `Seo:MaxDynamicUrlsPerType` est volontairement borné à `50` par défaut, avec un plafond interne plus strict sur les pages de références. M22 reprendra le sujet avec génération dynamique avancée, découpage éventuel, cache, panneau admin et règles d'inclusion plus fines.

Le XML est généré dans un buffer UTF-8 explicite puis renvoyé en `application/xml; charset=utf-8`, afin d'éviter les réponses XML incomplètes ou incohérentes avec l'encodage HTTP.


## Validation locale avec `ng serve`

En développement local, `ng serve` ne sert pas le front via le Nginx de production. Sans proxy dev, une URL technique comme `/sitemap.xml` est donc traitée comme une route SPA inconnue : Angular renvoie `index.html`, puis le routing localisé peut retomber sur `/en/home` ou `/en/not-found`.

Pour éviter ce faux positif, le front inclut désormais `proxy.conf.json` et `angular.json` référence ce proxy pour `ng serve`.

Routes proxifiées localement vers l'API HTTPS :

```txt
http://localhost:4200/robots.txt  -> https://localhost:44391/robots.txt
http://localhost:4200/sitemap.xml -> https://localhost:44391/sitemap.xml
```

Pré-requis : l'API locale doit être démarrée sur `https://localhost:44391`.

Validation :

```powershell
curl.exe -i http://localhost:4200/robots.txt
curl.exe -i http://localhost:4200/sitemap.xml
```

Résultat attendu : réponse `200` avec `text/plain` pour `robots.txt` et `application/xml` pour `sitemap.xml`. Si l'API n'est pas démarrée, le proxy doit retourner une erreur de connexion, mais ne doit plus retomber sur la home Angular.

## Points de vigilance SEO

- Ne pas indexer `/api/**`, admin, compte, auth, 404.
- Ne pas déclarer de `hreflang` vers une route qui n'est pas réellement servie.
- Ne pas générer de canonical avec query string ou hash.
- Ne pas mettre d'URLs d'entités non visibles dans le sitemap.
- En production, vérifier depuis le domaine public réel :

```bash
curl -I https://amusement-parks.fun/fr/home
curl https://amusement-parks.fun/robots.txt
curl https://amusement-parks.fun/sitemap.xml
```
