# M20 — SSR public réellement servi

## Principe

Avant M20, le build Angular contenait déjà `@angular/ssr`, `server.ts` et `app.routes.server.ts`, mais le container front de production servait surtout le dossier `browser` avec Nginx. Les robots voyaient donc principalement une SPA, avec des metas parfois présentes mais un contenu initial insuffisant sur les pages dynamiques.

M20 bascule le front vers un **serveur Node Angular SSR** :

```txt
Nginx Proxy Manager
        ↓
container front Node SSR
        ↓
API interne Docker
```

Le navigateur continue d'appeler l'API via `/api/...`, mais le serveur SSR appelle l'API avec une URL interne Docker (`http://api:8080`).

## Décision d'architecture

Décision retenue : **container Node SSR dédié derrière Nginx Proxy Manager**.

Raisons :

- séparation claire entre reverse proxy public et rendu Angular ;
- pas de container hybride Nginx + Node ;
- routage NPM simple vers `127.0.0.1:${PUBLIC_HTTP_PORT}` ;
- l'API reste privée, sans port public ;
- le SSR peut proxyfier `/api`, `/robots.txt` et `/sitemap.xml` comme le faisait l'ancien Nginx front.

## Routes SSR / CSR

Routes rendues SSR :

- home ;
- liste des parcs ;
- détail parc ;
- liste des éléments d'un parc ;
- détail élément ;
- références publiques : exploitant, fondateur, constructeur ;
- about, privacy, 404 publique.

Routes volontairement CSR :

- admin ;
- profil ;
- confirm-account ;
- forgot-password ;
- reset-password.

Ces routes restent `noindex` côté SEO et ne doivent pas être rendues côté serveur avec du contenu utilisateur.

## TransferState / Hydration

`provideClientHydration()` est activé côté Angular. Les données HTTP récupérées pendant le rendu serveur sont transférées au navigateur quand Angular peut les mettre en cache. Cela évite les doubles appels immédiats sur les pages publiques clés.

## Statuts HTTP

Le serveur SSR renvoie maintenant :

- `200` sur les pages publiques existantes ;
- `404` sur `/:lang/not-found` ;
- `404` quand une page détail publique reçoit une réponse API `404` pendant le SSR.

Cela limite les faux `200` indexables sur des entités inexistantes.

## Proxy intégré au serveur SSR

Le serveur Node relaie :

```txt
/api/*       -> API interne Docker, sans le préfixe /api
/robots.txt  -> API interne Docker /robots.txt
/sitemap.xml -> API interne Docker /sitemap.xml
```

Les headers CSP émis par l'API sont masqués sur les réponses proxifiées afin que le front garde la responsabilité des headers publics de page.

## Impact local / dev

`ng serve` continue de fonctionner comme avant pour le développement rapide.

Pour tester le comportement proche production, utiliser le stack local Docker décrit dans `docs/deploy/local-production-like-stack.md`.

Différences importantes :

- `ng serve` ne teste pas le vrai SSR containerisé ;
- le stack local Docker teste SSR, proxy `/api`, robots, sitemap, cookies, CSP Report-Only, Matomo local et reverse proxy ;
- sans vrai HTTPS/domaine public, Google OAuth et certains comportements de cookies restent à revalider en staging/prod.

## Validation rapide

Après démarrage du front SSR :

```bash
curl -i http://127.0.0.1:14000/en/parks
curl -i http://127.0.0.1:14000/sitemap.xml
curl -i http://127.0.0.1:14000/api/health
```

Puis, avec le stack local NPM configuré :

```bash
cd FRONT/AmusementPark
PUBLIC_BASE_URL=http://amusement.localhost:18080 npm run seo:ssr-smoke
```

Le test direct sur `localhost:14000` reste utile pour vérifier que le serveur répond, mais les canonical/hreflang du build local-production pointent volontairement vers `amusement.localhost:18080`, c'est-à-dire l'entrée reverse-proxy locale.
